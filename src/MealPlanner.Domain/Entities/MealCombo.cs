namespace MealPlanner.Domain.Entities;

/// <summary>
/// An informal dinner idea assembled by combining up to one protein, one carbohydrate and one
/// vegetable ingredient. Unlike a <see cref="Recipe"/>, a combo carries no quantities or
/// instructions; it is a rough, reusable pairing of foods that becomes a meal.
/// </summary>
public class MealCombo
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the household this combo belongs to.</summary>
    public int HouseholdId { get; set; }

    /// <summary>Gets or sets the household this combo belongs to.</summary>
    public Household? Household { get; set; }

    /// <summary>Gets or sets the combo's display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the identifier of the chosen protein ingredient, when any.</summary>
    public int? ProteinIngredientId { get; set; }

    /// <summary>Gets or sets the chosen protein ingredient. Populated by EF Core when included.</summary>
    public Ingredient? ProteinIngredient { get; set; }

    /// <summary>Gets or sets the identifier of the chosen carbohydrate ingredient, when any.</summary>
    public int? CarbohydrateIngredientId { get; set; }

    /// <summary>Gets or sets the chosen carbohydrate ingredient. Populated by EF Core when included.</summary>
    public Ingredient? CarbohydrateIngredient { get; set; }

    /// <summary>Gets or sets the identifier of the chosen vegetable ingredient, when any.</summary>
    public int? VegetableIngredientId { get; set; }

    /// <summary>Gets or sets the chosen vegetable ingredient. Populated by EF Core when included.</summary>
    public Ingredient? VegetableIngredient { get; set; }
}
