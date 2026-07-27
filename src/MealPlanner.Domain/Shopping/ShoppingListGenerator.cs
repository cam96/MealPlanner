using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Domain.Shopping;

/// <summary>
/// Builds a combined shopping list from a meal plan. Ingredient quantities are aggregated across the
/// plan's normal days, reduced by what is already in the pantry, priced at each ingredient's
/// preferred store, and flagged for shared use, bulk purchases, and current deals.
/// </summary>
public static class ShoppingListGenerator
{
    /// <summary>Generates a shopping list for a meal plan.</summary>
    /// <param name="plan">The plan, with days, meals, recipes and recipe ingredients loaded.</param>
    /// <param name="pantryItems">The current pantry and freezer stock, with ingredients loaded.</param>
    /// <param name="prices">Recorded prices for the ingredients used by the plan.</param>
    /// <returns>The combined shopping list with per-line and total cost estimates.</returns>
    /// <exception cref="ArgumentNullException">Any argument is <see langword="null"/>.</exception>
    public static ShoppingList Generate(
        MealPlan plan,
        IReadOnlyList<PantryItem> pantryItems,
        IReadOnlyList<IngredientPrice> prices)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(pantryItems);
        ArgumentNullException.ThrowIfNull(prices);

        var required = AggregateRequirements(plan);
        var pantryByIngredient = AggregatePantry(pantryItems, required);
        var pricesByIngredient = prices
            .GroupBy(p => p.IngredientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var lines = new List<ShoppingListLine>();
        var total = 0m;
        var anyEstimated = false;

        foreach (var (ingredientId, acc) in required)
        {
            var pantry = pantryByIngredient.GetValueOrDefault(ingredientId);
            var toBuy = Math.Max(0, acc.Quantity - pantry);
            if (toBuy <= 0 && !acc.HasUnconvertible)
            {
                continue; // Already fully covered by the pantry.
            }

            var line = BuildLine(acc, ingredientId, pantry, toBuy, pricesByIngredient);
            anyEstimated |= line.IsCostEstimated;
            total += line.EstimatedCost;
            lines.Add(line);
        }

        var orderedLines = lines
            .OrderBy(l => l.PreferredStoreName ?? "\uffff")
            .ThenBy(l => l.IngredientName)
            .ToList();

        return new ShoppingList(orderedLines, total, anyEstimated);
    }

    private static Dictionary<int, RequirementAccumulator> AggregateRequirements(MealPlan plan)
    {
        var required = new Dictionary<int, RequirementAccumulator>();

        foreach (var day in plan.Days.Where(d => d.DayType == DayType.Normal))
        {
            foreach (var meal in day.Meals)
            {
                var recipe = meal.Recipe;
                if (recipe is null)
                {
                    continue;
                }

                var scale = Math.Max(1, meal.Servings) / (double)Math.Max(1, recipe.Servings);
                foreach (var recipeLine in recipe.Ingredients)
                {
                    var ingredient = recipeLine.Ingredient;
                    if (ingredient is null)
                    {
                        continue;
                    }

                    if (!required.TryGetValue(recipeLine.IngredientId, out var acc))
                    {
                        acc = new RequirementAccumulator(ingredient);
                        required[recipeLine.IngredientId] = acc;
                    }

                    acc.RecipeIds.Add(recipe.Id);
                    if (UnitConverter.TryToBaseUnits(
                            ingredient.BaseUnit, ingredient.ServingWeightG, recipeLine.Quantity, recipeLine.Unit, out var baseQty))
                    {
                        acc.Quantity += baseQty * scale;
                    }
                    else
                    {
                        acc.HasUnconvertible = true;
                    }
                }
            }
        }

        return required;
    }

    private static Dictionary<int, double> AggregatePantry(
        IReadOnlyList<PantryItem> pantryItems,
        Dictionary<int, RequirementAccumulator> required)
    {
        var pantryByIngredient = new Dictionary<int, double>();

        foreach (var item in pantryItems)
        {
            if (!required.TryGetValue(item.IngredientId, out var acc))
            {
                continue; // Nothing planned needs this ingredient.
            }

            var ingredient = acc.Ingredient;
            if (UnitConverter.TryToBaseUnits(
                    ingredient.BaseUnit, ingredient.ServingWeightG, item.QuantityOnHand, item.Unit, out var baseQty))
            {
                pantryByIngredient[item.IngredientId] =
                    pantryByIngredient.GetValueOrDefault(item.IngredientId) + baseQty;
            }
        }

        return pantryByIngredient;
    }

    private static ShoppingListLine BuildLine(
        RequirementAccumulator acc,
        int ingredientId,
        double pantry,
        double toBuy,
        Dictionary<int, List<IngredientPrice>> pricesByIngredient)
    {
        int? storeId = null;
        string? storeName = null;
        var packages = 0;
        var cost = 0m;
        var costEstimated = false;
        var isBulk = false;
        var isDeal = false;
        var percentBelow = 0.0;

        var chosen = pricesByIngredient.TryGetValue(ingredientId, out var ingredientPrices)
            ? PickPrice(ingredientPrices)
            : null;

        if (chosen is not null
            && UnitConverter.TryToBaseUnits(
                acc.Ingredient.BaseUnit, acc.Ingredient.ServingWeightG, chosen.PackageQuantity, chosen.PackageUnit, out var packageBase)
            && packageBase > 0)
        {
            storeId = chosen.StoreId;
            storeName = chosen.Store?.Name;
            packages = toBuy > 0 ? Math.Max(1, (int)Math.Ceiling(toBuy / packageBase)) : 0;
            cost = packages * chosen.Price;
            costEstimated = chosen.IsEstimated;
            isBulk = toBuy > 0 && packageBase >= 2 * toBuy;

            var deal = DealDetector.Evaluate(acc.Ingredient, ingredientPrices!);
            if (deal is not null)
            {
                isDeal = deal.IsDeal;
                percentBelow = deal.PercentBelowAverage;
            }
        }
        else
        {
            costEstimated = true; // No usable price for this ingredient.
        }

        costEstimated |= acc.HasUnconvertible;

        return new ShoppingListLine(
            ingredientId,
            acc.Ingredient.Name,
            acc.Ingredient.BaseUnit,
            acc.Quantity,
            pantry,
            toBuy,
            storeId,
            storeName,
            packages,
            cost,
            costEstimated,
            acc.RecipeIds.Count > 1,
            isBulk,
            isDeal,
            percentBelow);
    }

    private static IngredientPrice? PickPrice(IEnumerable<IngredientPrice> prices) =>
        prices
            .OrderByDescending(p => p.IsPreferredStore)
            .ThenByDescending(p => p.RecordedDate)
            .FirstOrDefault();

    private sealed class RequirementAccumulator(Ingredient ingredient)
    {
        public Ingredient Ingredient { get; } = ingredient;

        public double Quantity { get; set; }

        public HashSet<int> RecipeIds { get; } = [];

        public bool HasUnconvertible { get; set; }
    }
}
