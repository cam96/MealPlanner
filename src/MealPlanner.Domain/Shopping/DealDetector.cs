using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Domain.Shopping;

/// <summary>
/// Detects whether an ingredient's most recent price is a deal compared with the average of its
/// earlier prices. Prices are normalised to a per-base-unit cost so packages of different sizes are
/// comparable.
/// </summary>
public static class DealDetector
{
    /// <summary>The default percentage below the historical average that counts as a deal.</summary>
    public const double DefaultThresholdPercent = 10.0;

    /// <summary>Evaluates whether the latest recorded price for an ingredient is a deal.</summary>
    /// <param name="ingredient">The ingredient, used to normalise package prices to base units.</param>
    /// <param name="prices">The recorded prices for the ingredient.</param>
    /// <param name="thresholdPercent">The percentage below the average that qualifies as a deal.</param>
    /// <returns>
    /// The comparison result, or <see langword="null"/> when fewer than two prices can be normalised
    /// (there is no basis for comparison).
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="ingredient"/> or <paramref name="prices"/> is <see langword="null"/>.</exception>
    public static DealResult? Evaluate(
        Ingredient ingredient,
        IReadOnlyList<IngredientPrice> prices,
        double thresholdPercent = DefaultThresholdPercent)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        ArgumentNullException.ThrowIfNull(prices);

        var unitPrices = new List<(DateOnly Date, decimal UnitPrice)>();
        foreach (var price in prices.Where(p => p.IngredientId == ingredient.Id))
        {
            if (UnitConverter.TryToBaseUnits(
                    ingredient.BaseUnit, ingredient.ServingWeightG, price.PackageQuantity, price.PackageUnit, out var packageBase)
                && packageBase > 0)
            {
                unitPrices.Add((price.RecordedDate, price.Price / (decimal)packageBase));
            }
        }

        if (unitPrices.Count < 2)
        {
            return null;
        }

        var ordered = unitPrices.OrderByDescending(u => u.Date).ToList();
        var latest = ordered[0].UnitPrice;
        var average = ordered.Skip(1).Average(u => u.UnitPrice);
        if (average <= 0)
        {
            return null;
        }

        var percentBelow = (double)((average - latest) / average) * 100.0;
        var isDeal = percentBelow >= thresholdPercent;
        return new DealResult(ingredient.Id, latest, average, isDeal, percentBelow);
    }
}
