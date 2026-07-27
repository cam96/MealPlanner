using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Nutrition;

/// <summary>
/// Converts a quantity expressed in one <see cref="MeasurementUnit"/> into the amount expressed in
/// an ingredient's base unit, so that per-100 nutrition and package pricing can be applied.
/// </summary>
public static class UnitConverter
{
    /// <summary>
    /// Attempts to convert <paramref name="quantity"/> given in <paramref name="fromUnit"/> to the
    /// amount expressed in <paramref name="baseUnit"/>.
    /// </summary>
    /// <param name="baseUnit">The ingredient's base unit that nutrition/pricing are expressed in.</param>
    /// <param name="servingWeightG">The weight of a single item in grams, when known.</param>
    /// <param name="quantity">The quantity to convert.</param>
    /// <param name="fromUnit">The unit the <paramref name="quantity"/> is expressed in.</param>
    /// <param name="result">The converted amount in <paramref name="baseUnit"/> when successful.</param>
    /// <returns><see langword="true"/> when the conversion is possible; otherwise <see langword="false"/>.</returns>
    public static bool TryToBaseUnits(
        MeasurementUnit baseUnit,
        double? servingWeightG,
        double quantity,
        MeasurementUnit fromUnit,
        out double result)
    {
        // Same unit: no conversion needed.
        if (fromUnit == baseUnit)
        {
            result = quantity;
            return true;
        }

        // Counting items of a gram-based ingredient: multiply by the per-item weight.
        if (fromUnit == MeasurementUnit.Each
            && baseUnit == MeasurementUnit.Gram
            && servingWeightG is > 0)
        {
            result = quantity * servingWeightG.Value;
            return true;
        }

        // Weighing a count-based ingredient: divide by the per-item weight.
        if (fromUnit == MeasurementUnit.Gram
            && baseUnit == MeasurementUnit.Each
            && servingWeightG is > 0)
        {
            result = quantity / servingWeightG.Value;
            return true;
        }

        // Grams <-> millilitres can't be converted without a density, so decline.
        result = 0;
        return false;
    }
}
