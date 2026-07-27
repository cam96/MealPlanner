namespace MealPlanner.Domain.Entities;

/// <summary>
/// A month of planned meals, broken down into individual days. A plan is identified uniquely by its
/// year and month.
/// </summary>
public class MealPlan
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the calendar year the plan covers.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the calendar month the plan covers (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Gets the day plans that make up the month.</summary>
    public ICollection<DayPlan> Days { get; } = [];
}
