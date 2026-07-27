namespace MealPlanner.Contracts;

/// <summary>
/// Wire representation of a meal-building category an ingredient can belong to. Mirrors the domain
/// category but keeps the Contracts assembly free of any dependency on the domain model.
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
