namespace MealPlanner.Domain.Entities;

/// <summary>
/// A price observation for an ingredient at a particular store on a particular date. Recording
/// prices over time lets the app pick a preferred store and detect deals (price below the
/// historical average).
/// </summary>
public class IngredientPrice
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ingredient this price applies to.</summary>
    public int IngredientId { get; set; }

    /// <summary>Gets or sets the ingredient navigation property.</summary>
    public Ingredient? Ingredient { get; set; }

    /// <summary>Gets or sets the store this price was observed at.</summary>
    public int StoreId { get; set; }

    /// <summary>Gets or sets the store navigation property.</summary>
    public Store? Store { get; set; }

    /// <summary>Gets or sets the price paid for the package, in Canadian dollars.</summary>
    public decimal Price { get; set; }

    /// <summary>Gets or sets the quantity contained in the priced package.</summary>
    public double PackageQuantity { get; set; }

    /// <summary>Gets or sets the unit of <see cref="PackageQuantity"/>.</summary>
    public MeasurementUnit PackageUnit { get; set; }

    /// <summary>Gets or sets the date the price was recorded.</summary>
    public DateOnly RecordedDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the price is an estimate rather than an observed
    /// purchase. Estimated prices must be visibly marked in the UI.
    /// </summary>
    public bool IsEstimated { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this store is the preferred place to buy the
    /// ingredient. Used when building the combined shopping list.
    /// </summary>
    public bool IsPreferredStore { get; set; }
}
