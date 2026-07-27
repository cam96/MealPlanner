namespace MealPlanner.Domain.Entities;

/// <summary>
/// A recipe: a set of ingredient quantities plus preparation details that yields a number of
/// servings. Nutrition and cost are derived from the recipe's ingredients rather than stored.
/// </summary>
public class Recipe
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the recipe's display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the meal this recipe is intended for.</summary>
    public MealType MealType { get; set; }

    /// <summary>Gets or sets the hands-on preparation time, in minutes.</summary>
    public int PrepMinutes { get; set; }

    /// <summary>Gets or sets the cooking time, in minutes.</summary>
    public int CookMinutes { get; set; }

    /// <summary>Gets or sets the number of servings the recipe yields. Always at least one.</summary>
    public int Servings { get; set; } = 1;

    /// <summary>Gets or sets the free-text preparation instructions.</summary>
    public string? Instructions { get; set; }

    /// <summary>Gets the ingredient lines that make up the recipe.</summary>
    public ICollection<RecipeIngredient> Ingredients { get; } = [];
}
