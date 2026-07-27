namespace MealPlanner.Domain.Entities;

/// <summary>
/// A single day within a <see cref="MealPlan"/>, carrying its day type (which controls whether it
/// counts toward nutrition goals), an optional note, and the meals planned for it.
/// </summary>
public class DayPlan
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the owning meal plan.</summary>
    public int MealPlanId { get; set; }

    /// <summary>Gets or sets the owning meal plan. Populated by EF Core when included.</summary>
    public MealPlan? MealPlan { get; set; }

    /// <summary>Gets or sets the calendar date this day represents.</summary>
    public DateOnly Date { get; set; }

    /// <summary>Gets or sets how the day is treated when tracking goals and prep load.</summary>
    public DayType DayType { get; set; }

    /// <summary>Gets or sets an optional free-text note for the day.</summary>
    public string? Note { get; set; }

    /// <summary>Gets the meals planned for the day.</summary>
    public ICollection<PlannedMeal> Meals { get; } = [];
}
