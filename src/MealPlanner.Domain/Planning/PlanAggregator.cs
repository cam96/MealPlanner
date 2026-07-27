using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Domain.Planning;

/// <summary>
/// Rolls up a meal plan's planned meals into per-person nutrition totals. Only <see
/// cref="DayType.Normal"/> days count; eating-out and event days are excluded from goal tracking.
/// Meals assigned to a specific person contribute their full servings to that person; shared meals
/// split their servings evenly across the household.
/// </summary>
public static class PlanAggregator
{
    /// <summary>Computes per-person nutrition for each counted day of the plan.</summary>
    /// <param name="plan">The plan, with days, meals, recipes and recipe ingredients loaded.</param>
    /// <param name="people">The household members whose goals the plan is tracked against.</param>
    /// <returns>Per-person, per-day nutrition totals, ordered by person then date.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> or <paramref name="people"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<PersonDayNutrition> PerPersonPerDay(
        MealPlan plan,
        IReadOnlyList<Person> people)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(people);

        var ordered = people.OrderBy(p => p.Id).ToList();
        var totals = new Dictionary<(int PersonId, DateOnly Date), NutritionFacts>();

        foreach (var day in plan.Days.Where(d => d.DayType == DayType.Normal))
        {
            foreach (var meal in day.Meals)
            {
                if (meal.Recipe is null)
                {
                    continue;
                }

                var perServing = NutritionCalculator.PerServing(meal.Recipe);
                var servings = Math.Max(1, meal.Servings);

                foreach (var (personId, factor) in Targets(meal.Assignee, ordered))
                {
                    var contribution = perServing.Scale(servings * factor);
                    var key = (personId, day.Date);
                    totals[key] = totals.TryGetValue(key, out var current)
                        ? current.Add(contribution)
                        : contribution;
                }
            }
        }

        return totals
            .Select(kvp => new PersonDayNutrition(kvp.Key.PersonId, kvp.Key.Date, kvp.Value))
            .OrderBy(r => r.PersonId)
            .ThenBy(r => r.Date)
            .ToList();
    }

    /// <summary>Computes each person's nutrition totals across the whole plan.</summary>
    /// <param name="plan">The plan, with days, meals, recipes and recipe ingredients loaded.</param>
    /// <param name="people">The household members whose goals the plan is tracked against.</param>
    /// <returns>Per-person totals across all counted days, ordered by person.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> or <paramref name="people"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<PersonNutrition> PerPersonMonth(
        MealPlan plan,
        IReadOnlyList<Person> people)
    {
        var perDay = PerPersonPerDay(plan, people);

        return perDay
            .GroupBy(r => r.PersonId)
            .Select(g => new PersonNutrition(
                g.Key,
                g.Aggregate(NutritionFacts.Zero, (acc, r) => acc.Add(r.Nutrition))))
            .OrderBy(r => r.PersonId)
            .ToList();
    }

    private static IEnumerable<(int PersonId, double Factor)> Targets(
        MealAssignee assignee,
        IReadOnlyList<Person> ordered)
    {
        switch (assignee)
        {
            case MealAssignee.FirstPerson when ordered.Count >= 1:
                yield return (ordered[0].Id, 1.0);
                break;
            case MealAssignee.SecondPerson when ordered.Count >= 2:
                yield return (ordered[1].Id, 1.0);
                break;
            case MealAssignee.Shared when ordered.Count > 0:
                var factor = 1.0 / ordered.Count;
                foreach (var person in ordered)
                {
                    yield return (person.Id, factor);
                }

                break;
        }
    }
}
