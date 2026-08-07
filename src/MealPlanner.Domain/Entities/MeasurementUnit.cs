namespace MealPlanner.Domain.Entities;

/// <summary>
/// The unit a quantity is measured in. The application stores user-entered units as-is but
/// normalizes to base units (grams for mass, millilitres for volume) when performing calculations.
/// </summary>
public enum MeasurementUnit
{
    /// <summary>Mass in grams (g). Base unit for mass calculations.</summary>
    Gram = 0,

    /// <summary>Volume in millilitres (ml). Base unit for volume calculations.</summary>
    Millilitre = 1,

    /// <summary>A whole count of items; converted to grams using a per-ingredient serving weight.</summary>
    Each = 2,

    /// <summary>Mass in kilograms (kg). Converted to grams (×1000) for calculations.</summary>
    Kilogram = 3,

    /// <summary>Mass in pounds (lb). Converted to grams (×453.592) for calculations.</summary>
    Pound = 4,

    /// <summary>Volume in litres (L). Converted to millilitres (×1000) for calculations.</summary>
    Litre = 5,
}
