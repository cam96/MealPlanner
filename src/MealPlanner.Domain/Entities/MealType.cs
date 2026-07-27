namespace MealPlanner.Domain.Entities;

/// <summary>The meal a recipe is intended for.</summary>
public enum MealType
{
    /// <summary>Breakfast.</summary>
    Breakfast = 0,

    /// <summary>Lunch.</summary>
    Lunch = 1,

    /// <summary>Dinner.</summary>
    Dinner = 2,

    /// <summary>A snack or side.</summary>
    Snack = 3,
}
