using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Domain.Planning;

/// <summary>A person's accumulated nutrition for a single day of a plan.</summary>
/// <param name="PersonId">The person the totals are for.</param>
/// <param name="Date">The calendar date.</param>
/// <param name="Nutrition">The accumulated nutrition consumed that day.</param>
public record PersonDayNutrition(int PersonId, DateOnly Date, NutritionFacts Nutrition);

/// <summary>A person's accumulated nutrition across a whole plan.</summary>
/// <param name="PersonId">The person the totals are for.</param>
/// <param name="Nutrition">The accumulated nutrition consumed across all counted days.</param>
public record PersonNutrition(int PersonId, NutritionFacts Nutrition);

/// <summary>The total hands-on preparation and cooking time planned for a single day.</summary>
/// <param name="Date">The calendar date.</param>
/// <param name="TotalMinutes">The summed prep and cook minutes for the day's planned recipes.</param>
public record DayPrepLoad(DateOnly Date, int TotalMinutes);
