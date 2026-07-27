namespace MealPlanner.Domain.Entities;

/// <summary>
/// A quantity of an ingredient currently on hand in the household, tracked by storage location so
/// meal planning and shopping lists can account for what is already stocked.
/// </summary>
public class PantryItem
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the ingredient held.</summary>
    public int IngredientId { get; set; }

    /// <summary>Gets or sets the ingredient held. Populated by EF Core when included.</summary>
    public Ingredient? Ingredient { get; set; }

    /// <summary>Gets or sets the quantity on hand, expressed in <see cref="Unit"/>.</summary>
    public double QuantityOnHand { get; set; }

    /// <summary>Gets or sets the unit the <see cref="QuantityOnHand"/> is expressed in.</summary>
    public MeasurementUnit Unit { get; set; }

    /// <summary>Gets or sets where the item is stored.</summary>
    public StorageLocation Location { get; set; }
}
