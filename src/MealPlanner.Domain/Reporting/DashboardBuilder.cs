using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;
using MealPlanner.Domain.Planning;

namespace MealPlanner.Domain.Reporting;

/// <summary>
/// Builds a <see cref="DashboardSummary"/> from a month's plan: average per-person nutrition versus
/// goals, preparation load, budget versus projected spend, and generated alerts. Only <see
/// cref="DayType.Normal"/> days count toward goals, matching <see cref="PlanAggregator"/>.
/// </summary>
public static class DashboardBuilder
{
    /// <summary>The fraction below a goal at which an under-target alert is raised.</summary>
    private const double UnderGoalThreshold = 0.9;

    /// <summary>The fraction above a goal at which an over-target alert is raised.</summary>
    private const double OverGoalThreshold = 1.1;

    /// <summary>Builds the dashboard summary for a plan.</summary>
    /// <param name="plan">The plan, with days, meals, recipes and recipe ingredients loaded.</param>
    /// <param name="people">The household members whose goals the plan is tracked against.</param>
    /// <param name="monthlyBudget">The configured monthly grocery budget.</param>
    /// <param name="projectedSpend">The projected grocery spend for the month.</param>
    /// <param name="spendIsEstimated">Whether the projected spend is estimated or unpriced.</param>
    /// <returns>The assembled dashboard summary.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="plan"/> or <paramref name="people"/> is <see langword="null"/>.</exception>
    public static DashboardSummary Build(
        MealPlan plan,
        IReadOnlyList<Person> people,
        decimal monthlyBudget,
        decimal projectedSpend,
        bool spendIsEstimated)
    {
        ArgumentNullException.ThrowIfNull(plan);
        ArgumentNullException.ThrowIfNull(people);

        var countedDays = plan.Days.Count(d => d.DayType == DayType.Normal);
        var plannedMealCount = plan.Days
            .Where(d => d.DayType == DayType.Normal)
            .SelectMany(d => d.Meals)
            .Count(m => m.Recipe is not null);

        var peopleSummaries = BuildPeople(plan, people, countedDays);
        var prep = BuildPrep(plan, countedDays);

        var isOverBudget = monthlyBudget > 0m && projectedSpend > monthlyBudget;
        var remainingBudget = monthlyBudget - projectedSpend;

        var alerts = BuildAlerts(
            plan,
            peopleSummaries,
            countedDays,
            monthlyBudget,
            projectedSpend,
            isOverBudget);

        return new DashboardSummary(
            plan.Year,
            plan.Month,
            countedDays,
            plannedMealCount,
            peopleSummaries,
            prep,
            monthlyBudget,
            projectedSpend,
            spendIsEstimated,
            isOverBudget,
            remainingBudget,
            alerts);
    }

    private static IReadOnlyList<PersonNutritionSummary> BuildPeople(
        MealPlan plan,
        IReadOnlyList<Person> people,
        int countedDays)
    {
        var totals = PlanAggregator.PerPersonMonth(plan, people)
            .ToDictionary(r => r.PersonId, r => r.Nutrition);
        var divisor = Math.Max(1, countedDays);

        return people
            .OrderBy(p => p.Id)
            .Select(p =>
            {
                var total = totals.TryGetValue(p.Id, out var facts) ? facts : NutritionFacts.Zero;
                return new PersonNutritionSummary(
                    p.Id,
                    p.Name,
                    total.Calories / divisor,
                    total.Protein / divisor,
                    total.Fiber / divisor,
                    total.Carbs / divisor,
                    total.Fat / divisor,
                    p.DailyCalorieGoal,
                    p.DailyProteinGoal,
                    p.DailyFiberGoal,
                    p.DailyCarbGoal,
                    p.DailyFatGoal,
                    total.IsEstimated);
            })
            .ToList();
    }

    private static PrepSummary BuildPrep(MealPlan plan, int countedDays)
    {
        var loads = PrepTimeLoad.PerDay(plan);
        var total = loads.Sum(l => l.TotalMinutes);
        var average = countedDays > 0 ? (double)total / countedDays : 0;
        var busiest = loads
            .OrderByDescending(l => l.TotalMinutes)
            .ThenBy(l => l.Date)
            .FirstOrDefault();

        return new PrepSummary(
            total,
            average,
            busiest is null ? null : busiest.Date,
            busiest?.TotalMinutes ?? 0);
    }

    private static IReadOnlyList<DashboardAlert> BuildAlerts(
        MealPlan plan,
        IReadOnlyList<PersonNutritionSummary> people,
        int countedDays,
        decimal monthlyBudget,
        decimal projectedSpend,
        bool isOverBudget)
    {
        var alerts = new List<DashboardAlert>();

        if (isOverBudget)
        {
            alerts.Add(new DashboardAlert(
                DashboardAlertLevel.Warning,
                $"Projected spend {projectedSpend:C} exceeds the {monthlyBudget:C} budget."));
        }

        if (countedDays > 0)
        {
            foreach (var person in people)
            {
                if (person.CalorieGoal > 0 && person.AverageCalories < person.CalorieGoal * UnderGoalThreshold)
                {
                    alerts.Add(new DashboardAlert(
                        DashboardAlertLevel.Warning,
                        $"{person.PersonName} averages {person.AverageCalories:F0} kcal/day, below the {person.CalorieGoal} kcal goal."));
                }
                else if (person.CalorieGoal > 0 && person.AverageCalories > person.CalorieGoal * OverGoalThreshold)
                {
                    alerts.Add(new DashboardAlert(
                        DashboardAlertLevel.Warning,
                        $"{person.PersonName} averages {person.AverageCalories:F0} kcal/day, above the {person.CalorieGoal} kcal goal."));
                }

                if (person.ProteinGoal > 0 && person.AverageProtein < person.ProteinGoal * UnderGoalThreshold)
                {
                    alerts.Add(new DashboardAlert(
                        DashboardAlertLevel.Info,
                        $"{person.PersonName} averages {person.AverageProtein:F0} g protein/day, below the {person.ProteinGoal} g goal."));
                }
            }
        }

        var unplannedDays = plan.Days
            .Where(d => d.DayType == DayType.Normal)
            .Count(d => !d.Meals.Any(m => m.Recipe is not null));
        if (unplannedDays > 0)
        {
            alerts.Add(new DashboardAlert(
                DashboardAlertLevel.Info,
                $"{unplannedDays} normal day(s) have no meals planned."));
        }

        return alerts;
    }
}
