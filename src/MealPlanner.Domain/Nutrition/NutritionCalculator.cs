using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Nutrition;

/// <summary>
/// Computes a recipe's nutrition from its ingredient lines. Each line's quantity is converted to the
/// ingredient's base unit and scaled against its per-100 values. The result is flagged as estimated
/// when any contributing ingredient's nutrition is estimated or a line could not be converted.
/// </summary>
public static class NutritionCalculator
{
    /// <summary>Computes the total nutrition for the whole recipe.</summary>
    /// <param name="recipe">The recipe, with its <see cref="RecipeIngredient.Ingredient"/> navigations loaded.</param>
    /// <returns>The recipe's total nutrition.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recipe"/> is <see langword="null"/>.</exception>
    public static NutritionFacts ForRecipe(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        var calories = 0.0;
        var protein = 0.0;
        var fiber = 0.0;
        var carbs = 0.0;
        var fat = 0.0;
        var estimated = false;

        foreach (var line in recipe.Ingredients)
        {
            var ingredient = line.Ingredient;
            if (ingredient is null)
            {
                // Missing nutrition source: can't compute this line exactly.
                estimated = true;
                continue;
            }

            if (!UnitConverter.TryToBaseUnits(
                    ingredient.BaseUnit, ingredient.ServingWeightG, line.Quantity, line.Unit, out var baseAmount))
            {
                estimated = true;
                continue;
            }

            var factor = baseAmount / 100.0;
            calories += ingredient.CaloriesPer100 * factor;
            protein += ingredient.ProteinPer100 * factor;
            fiber += ingredient.FiberPer100 * factor;
            carbs += ingredient.CarbsPer100 * factor;
            fat += ingredient.FatPer100 * factor;
            estimated |= ingredient.IsNutritionEstimated;
        }

        return new NutritionFacts(calories, protein, fiber, carbs, fat, estimated);
    }

    /// <summary>Computes the nutrition per serving for the recipe.</summary>
    /// <param name="recipe">The recipe, with its <see cref="RecipeIngredient.Ingredient"/> navigations loaded.</param>
    /// <returns>The per-serving nutrition; equal to the total when servings is one or less.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="recipe"/> is <see langword="null"/>.</exception>
    public static NutritionFacts PerServing(Recipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);

        var total = ForRecipe(recipe);
        var servings = Math.Max(1, recipe.Servings);
        return total.Scale(1.0 / servings);
    }
}
