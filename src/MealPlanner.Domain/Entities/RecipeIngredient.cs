namespace MealPlanner.Domain.Entities;

/// <summary>
/// A single ingredient line within a <see cref="Recipe"/>: how much of an ingredient the recipe
/// uses, expressed in a chosen unit.
/// </summary>
public class RecipeIngredient
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the owning recipe.</summary>
    public int RecipeId { get; set; }

    /// <summary>Gets or sets the recipe navigation property.</summary>
    public Recipe? Recipe { get; set; }

    /// <summary>Gets or sets the ingredient used.</summary>
    public int IngredientId { get; set; }

    /// <summary>Gets or sets the ingredient navigation property.</summary>
    public Ingredient? Ingredient { get; set; }

    /// <summary>Gets or sets the quantity of the ingredient used, in <see cref="Unit"/>.</summary>
    public double Quantity { get; set; }

    /// <summary>Gets or sets the unit the <see cref="Quantity"/> is expressed in.</summary>
    public MeasurementUnit Unit { get; set; }
}
