using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Reporting;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="DashboardBuilder"/> averages nutrition over counted days, summarises prep
/// load, compares spend against the budget, and raises the expected alerts.
/// </summary>
[TestFixture]
public class DashboardBuilderTests
{
    private static Ingredient Flour() => new()
    {
        Id = 1,
        Name = "Flour",
        BaseUnit = MeasurementUnit.Gram,
        CaloriesPer100 = 364,
        ProteinPer100 = 10,
        FiberPer100 = 3,
    };

    // 100 g flour in a single-serving recipe => 364 kcal, 10 g protein, 3 g fibre per serving.
    private static Recipe Bread(int prep = 0, int cook = 0) => new()
    {
        Id = 1,
        Name = "Bread",
        Servings = 1,
        PrepMinutes = prep,
        CookMinutes = cook,
        Ingredients =
        {
            new RecipeIngredient { IngredientId = 1, Ingredient = Flour(), Quantity = 100, Unit = MeasurementUnit.Gram },
        },
    };

    private static IReadOnlyList<Person> TwoPeople() =>
    [
        new Person { Id = 1, Name = "Alex", DailyCalorieGoal = 2000, DailyProteinGoal = 100, DailyFiberGoal = 30 },
        new Person { Id = 2, Name = "Blake", DailyCalorieGoal = 2200, DailyProteinGoal = 110, DailyFiberGoal = 35 },
    ];

    private static DayPlan NormalDay(DateOnly date, params PlannedMeal[] meals)
    {
        var day = new DayPlan { Date = date, DayType = DayType.Normal };
        foreach (var meal in meals)
        {
            day.Meals.Add(meal);
        }

        return day;
    }

    [Test]
    public void Build_AveragesNutritionOverCountedDays()
    {
        var recipe = Bread();
        var plan = new MealPlan { Year = 2026, Month = 3 };
        plan.Days.Add(NormalDay(
            new DateOnly(2026, 3, 1),
            new PlannedMeal { Slot = MealType.Lunch, Assignee = MealAssignee.FirstPerson, Recipe = recipe, RecipeId = 1, Servings = 2 }));
        plan.Days.Add(NormalDay(new DateOnly(2026, 3, 2))); // empty normal day

        var summary = DashboardBuilder.Build(plan, TwoPeople(), 0m, 0m, false);

        var alex = summary.People.Single(p => p.PersonId == 1);
        var blake = summary.People.Single(p => p.PersonId == 2);
        Assert.Multiple(() =>
        {
            Assert.That(summary.CountedDays, Is.EqualTo(2));
            Assert.That(summary.PlannedMealCount, Is.EqualTo(1));
            // 2 servings * 364 kcal = 728 kcal over 2 counted days => 364 kcal/day.
            Assert.That(alex.AverageCalories, Is.EqualTo(364).Within(0.001));
            Assert.That(blake.AverageCalories, Is.EqualTo(0).Within(0.001));
        });
    }

    [Test]
    public void Build_PrepSummary_SumsAndFindsBusiestDay()
    {
        var plan = new MealPlan { Year = 2026, Month = 3 };
        plan.Days.Add(NormalDay(
            new DateOnly(2026, 3, 1),
            new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = Bread(prep: 15, cook: 45), RecipeId = 1, Servings = 1 }));
        plan.Days.Add(NormalDay(
            new DateOnly(2026, 3, 2),
            new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = Bread(prep: 10, cook: 20), RecipeId = 1, Servings = 1 }));

        var summary = DashboardBuilder.Build(plan, TwoPeople(), 0m, 0m, false);

        Assert.Multiple(() =>
        {
            Assert.That(summary.Prep.TotalMinutes, Is.EqualTo(90));
            Assert.That(summary.Prep.AverageMinutesPerCountedDay, Is.EqualTo(45).Within(0.001));
            Assert.That(summary.Prep.BusiestDate, Is.EqualTo(new DateOnly(2026, 3, 1)));
            Assert.That(summary.Prep.BusiestDayMinutes, Is.EqualTo(60));
        });
    }

    [Test]
    public void Build_OverBudget_RaisesWarningAlert()
    {
        var plan = new MealPlan { Year = 2026, Month = 3 };
        plan.Days.Add(NormalDay(
            new DateOnly(2026, 3, 1),
            new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = Bread(), RecipeId = 1, Servings = 1 }));

        var summary = DashboardBuilder.Build(plan, TwoPeople(), 100m, 150m, true);

        Assert.Multiple(() =>
        {
            Assert.That(summary.IsOverBudget, Is.True);
            Assert.That(summary.RemainingBudget, Is.EqualTo(-50m));
            Assert.That(
                summary.Alerts,
                Has.Some.Matches<DashboardAlert>(a =>
                    a.Level == DashboardAlertLevel.Warning && a.Message.Contains("exceeds", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void Build_UnderCalorieGoal_RaisesWarningAlert()
    {
        var plan = new MealPlan { Year = 2026, Month = 3 };
        plan.Days.Add(NormalDay(
            new DateOnly(2026, 3, 1),
            new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = Bread(), RecipeId = 1, Servings = 1 }));

        var summary = DashboardBuilder.Build(plan, TwoPeople(), 0m, 0m, false);

        // Each person averages 182 kcal/day, far below their goal.
        Assert.That(
            summary.Alerts,
            Has.Some.Matches<DashboardAlert>(a =>
                a.Level == DashboardAlertLevel.Warning && a.Message.Contains("below", StringComparison.Ordinal)));
    }

    [Test]
    public void Build_UnplannedNormalDays_RaisesInfoAlert()
    {
        var plan = new MealPlan { Year = 2026, Month = 3 };
        plan.Days.Add(NormalDay(
            new DateOnly(2026, 3, 1),
            new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = Bread(), RecipeId = 1, Servings = 1 }));
        plan.Days.Add(NormalDay(new DateOnly(2026, 3, 2))); // no meals

        var summary = DashboardBuilder.Build(plan, TwoPeople(), 0m, 0m, false);

        Assert.That(
            summary.Alerts,
            Has.Some.Matches<DashboardAlert>(a =>
                a.Level == DashboardAlertLevel.Info && a.Message.Contains("no meals planned", StringComparison.Ordinal)));
    }

    [Test]
    public void Build_NullPlan_Throws() =>
        Assert.Throws<ArgumentNullException>(() => DashboardBuilder.Build(null!, TwoPeople(), 0m, 0m, false));

    [Test]
    public void Build_NullPeople_Throws() =>
        Assert.Throws<ArgumentNullException>(() =>
            DashboardBuilder.Build(new MealPlan { Year = 2026, Month = 3 }, null!, 0m, 0m, false));
}
