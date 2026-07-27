namespace MealPlanner.Domain.Entities;

/// <summary>
/// The unit a quantity is measured in. The application works in metric mass and volume only;
/// counts (<see cref="Each"/>) are converted to grams via an ingredient's serving weight.
/// </summary>
public enum MeasurementUnit
{
    /// <summary>Mass in grams (g).</summary>
    Gram = 0,

    /// <summary>Volume in millilitres (ml).</summary>
    Millilitre = 1,

    /// <summary>A whole count of items; converted to grams using a per-ingredient serving weight.</summary>
    Each = 2,
}
