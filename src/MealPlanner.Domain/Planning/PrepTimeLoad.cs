using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Planning;

/// <summary>
/// Computes the daily preparation and cooking load of a meal plan. Only <see cref="DayType.Normal"/>
/// days are counted; a shared meal is counted once (cooking effort is shared, not doubled).
/// </summary>
public static class PrepTimeLoad
{
    /// <summary>Computes the total prep and cook minutes for each counted day with planned recipes.</summary>
    /// <param name="plan">The plan, with days, meals and recipes loaded.</param>
    /// <returns>Per-day prep load for days that have at least one planned recipe, ordered by date.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<DayPrepLoad> PerDay(MealPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var loads = new List<DayPrepLoad>();

        foreach (var day in plan.Days.Where(d => d.DayType == DayType.Normal).OrderBy(d => d.Date))
        {
            var minutes = day.Meals
                .Where(m => m.Recipe is not null)
                .Sum(m => m.Recipe!.PrepMinutes + m.Recipe.CookMinutes);

            if (minutes > 0)
            {
                loads.Add(new DayPrepLoad(day.Date, minutes));
            }
        }

        return loads;
    }
}
