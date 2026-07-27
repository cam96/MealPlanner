namespace MealPlanner.Domain.Entities;

/// <summary>
/// A broad meal-building category an ingredient can belong to. Household dinners are often assembled
/// by combining one protein, one carbohydrate and one vegetable, and members of a category are
/// treated as interchangeable.
/// </summary>
public enum FoodCategory
{
    /// <summary>The ingredient is not assigned to a meal-building category.</summary>
    None = 0,

    /// <summary>A protein (for example chicken, beef, tofu or eggs).</summary>
    Protein = 1,

    /// <summary>A carbohydrate (for example rice, pasta or potatoes).</summary>
    Carbohydrate = 2,

    /// <summary>A vegetable (for example broccoli, carrots or peppers).</summary>
    Vegetable = 3,
}
