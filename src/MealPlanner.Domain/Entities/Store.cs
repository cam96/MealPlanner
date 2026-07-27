namespace MealPlanner.Domain.Entities;

/// <summary>
/// A grocery store where ingredients are priced and purchased (for example Costco, Superstore,
/// Safeway).
/// </summary>
public class Store
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the store's display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets the prices recorded at this store.</summary>
    public ICollection<IngredientPrice> Prices { get; } = [];
}
