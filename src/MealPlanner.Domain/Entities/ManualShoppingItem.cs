namespace MealPlanner.Domain.Entities;

/// <summary>
/// An item manually added to a shopping list by the user, independent of any meal plan. Manual
/// items let users track extra purchases (cleaning supplies, snacks, etc.) alongside the
/// auto-generated ingredient list.
/// </summary>
public class ManualShoppingItem
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the calendar year of the shopping period.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the calendar month (1-12) of the shopping period.</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the free-text name of the item to buy.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional quantity to buy.</summary>
    public double? Quantity { get; set; }

    /// <summary>Gets or sets the unit the <see cref="Quantity"/> is expressed in, when specified.</summary>
    public MeasurementUnit? Unit { get; set; }

    /// <summary>Gets or sets whether the item has been placed in the cart (checked off).</summary>
    public bool IsInCart { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the item was added.</summary>
    public DateTime CreatedAt { get; set; }
}
