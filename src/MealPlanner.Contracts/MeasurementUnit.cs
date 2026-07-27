namespace MealPlanner.Contracts;

/// <summary>
/// Wire representation of a measurement unit exchanged over HTTP. Mirrors the domain unit but keeps
/// the Contracts assembly free of any dependency on the domain model.
/// </summary>
public enum MeasurementUnit
{
    /// <summary>Mass in grams (g).</summary>
    Gram = 0,

    /// <summary>Volume in millilitres (ml).</summary>
    Millilitre = 1,

    /// <summary>A whole count of items.</summary>
    Each = 2,
}
