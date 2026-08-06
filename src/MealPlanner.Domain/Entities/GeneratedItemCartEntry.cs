namespace MealPlanner.Domain.Entities;

/// <summary>
/// Records that a generated (meal-plan-derived) shopping list line has been placed in the cart.
/// This lets the UI show which auto-generated items the user has already picked up without
/// altering the computed shopping list itself.
/// </summary>
public class GeneratedItemCartEntry
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the calendar year of the shopping period.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the calendar month (1-12) of the shopping period.</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the ingredient identifier that was placed in the cart.</summary>
    public int IngredientId { get; set; }

    /// <summary>Gets or sets the ingredient. Populated by EF Core when included.</summary>
    public Ingredient? Ingredient { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the item was added to the cart.</summary>
    public DateTime AddedToCartAt { get; set; }
}
