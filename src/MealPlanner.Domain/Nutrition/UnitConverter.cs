using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Nutrition;

/// <summary>
/// Converts a quantity expressed in one <see cref="MeasurementUnit"/> into the amount expressed in
/// an ingredient's base unit, so that per-100 nutrition and package pricing can be applied.
/// </summary>
public static class UnitConverter
{
    /// <summary>Grams per pound (avoirdupois).</summary>
    public const double GramsPerPound = 453.592;

    /// <summary>Grams per kilogram.</summary>
    public const double GramsPerKilogram = 1000.0;

    /// <summary>Millilitres per litre.</summary>
    public const double MillilitresPerLitre = 1000.0;

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
        // Normalize both units to their canonical base (Gram for mass, Millilitre for volume).
        var normalizedFrom = NormalizeToBase(fromUnit);
        var normalizedBase = NormalizeToBase(baseUnit);

        // Convert the quantity to canonical units.
        double quantityInCanonical = ToCanonical(quantity, fromUnit);

        // Same canonical unit: no further conversion needed.
        if (normalizedFrom == normalizedBase)
        {
            // Convert from canonical to the target base unit.
            result = FromCanonical(quantityInCanonical, baseUnit);
            return true;
        }

        // Counting items of a gram-based ingredient: multiply by the per-item weight.
        if (normalizedFrom == MeasurementUnit.Each
            && normalizedBase == MeasurementUnit.Gram
            && servingWeightG is > 0)
        {
            result = FromCanonical(quantityInCanonical * servingWeightG.Value, baseUnit);
            return true;
        }

        // Weighing a count-based ingredient: divide by the per-item weight.
        if (normalizedFrom == MeasurementUnit.Gram
            && normalizedBase == MeasurementUnit.Each
            && servingWeightG is > 0)
        {
            result = quantityInCanonical / servingWeightG.Value;
            return true;
        }

        // Mass <-> volume can't be converted without a density, so decline.
        result = 0;
        return false;
    }

    /// <summary>
    /// Returns the canonical base unit for a given measurement unit.
    /// Mass units (Gram, Kilogram, Pound) → Gram.
    /// Volume units (Millilitre, Litre) → Millilitre.
    /// Each → Each.
    /// </summary>
    private static MeasurementUnit NormalizeToBase(MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Gram => MeasurementUnit.Gram,
        MeasurementUnit.Kilogram => MeasurementUnit.Gram,
        MeasurementUnit.Pound => MeasurementUnit.Gram,
        MeasurementUnit.Millilitre => MeasurementUnit.Millilitre,
        MeasurementUnit.Litre => MeasurementUnit.Millilitre,
        MeasurementUnit.Each => MeasurementUnit.Each,
        _ => unit,
    };

    /// <summary>
    /// Converts a quantity from the given unit to its canonical base unit (grams or millilitres).
    /// </summary>
    private static double ToCanonical(double quantity, MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Kilogram => quantity * GramsPerKilogram,
        MeasurementUnit.Pound => quantity * GramsPerPound,
        MeasurementUnit.Litre => quantity * MillilitresPerLitre,
        _ => quantity,
    };

    /// <summary>
    /// Converts a quantity from the canonical base unit back to the given target unit.
    /// </summary>
    private static double FromCanonical(double quantity, MeasurementUnit unit) => unit switch
    {
        MeasurementUnit.Kilogram => quantity / GramsPerKilogram,
        MeasurementUnit.Pound => quantity / GramsPerPound,
        MeasurementUnit.Litre => quantity / MillilitresPerLitre,
        _ => quantity,
    };
}
