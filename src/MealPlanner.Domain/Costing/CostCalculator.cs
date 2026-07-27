using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Domain.Costing;

/// <summary>
/// Computes a recipe's cost from recorded ingredient prices. For each ingredient the calculator
/// picks the preferred store's most recent price, falling back to the most recent price at any store.
/// When an ingredient has no usable price, or a chosen price is itself an estimate, the result is
/// flagged as estimated.
/// </summary>
public static class CostCalculator
{
    /// <summary>Computes the cost of a recipe using the supplied ingredient prices.</summary>
    /// <param name="recipe">The recipe, with its <see cref="RecipeIngredient.Ingredient"/> navigations loaded.</param>
    /// <param name="prices">Recorded prices for the ingredients used by the recipe.</param>
    /// <returns>The recipe's total and per-serving cost.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recipe"/> or <paramref name="prices"/> is <see langword="null"/>.</exception>
    public static RecipeCost ForRecipe(Recipe recipe, IEnumerable<IngredientPrice> prices)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        ArgumentNullException.ThrowIfNull(prices);

        var pricesByIngredient = prices
            .GroupBy(p => p.IngredientId)
            .ToDictionary(g => g.Key, PickPrice);

        var total = 0m;
        var estimated = false;

        foreach (var line in recipe.Ingredients)
        {
            var ingredient = line.Ingredient;
            if (ingredient is null
                || !pricesByIngredient.TryGetValue(line.IngredientId, out var price)
                || price is null)
            {
                estimated = true;
                continue;
            }

            if (!UnitConverter.TryToBaseUnits(
                    ingredient.BaseUnit, ingredient.ServingWeightG, line.Quantity, line.Unit, out var lineBase)
                || !UnitConverter.TryToBaseUnits(
                    ingredient.BaseUnit, ingredient.ServingWeightG, price.PackageQuantity, price.PackageUnit, out var packageBase)
                || packageBase <= 0)
            {
                estimated = true;
                continue;
            }

            total += price.Price * (decimal)(lineBase / packageBase);
            estimated |= price.IsEstimated;
        }

        var servings = Math.Max(1, recipe.Servings);
        var perServing = total / servings;
        return new RecipeCost(total, perServing, estimated);
    }

    private static IngredientPrice? PickPrice(IGrouping<int, IngredientPrice> prices) =>
        prices
            .OrderByDescending(p => p.IsPreferredStore)
            .ThenByDescending(p => p.RecordedDate)
            .FirstOrDefault();
}
