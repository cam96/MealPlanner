using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Planning;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="PlanAggregator"/> rolls up planned meals into per-person nutrition, counting
/// only normal days, splitting shared meals, and honouring per-person assignments.
/// </summary>
[TestFixture]
public class PlanAggregatorTests
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
    private static Recipe Bread() => new()
    {
        Id = 1,
        Name = "Bread",
        Servings = 1,
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

    [Test]
    public void PerPersonPerDay_SharedMeal_SplitsEvenlyAcrossHousehold()
    {
        var recipe = Bread();
        var plan = new MealPlan
        {
            Year = 2026,
            Month = 1,
            Days =
            {
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 5),
                    DayType = DayType.Normal,
                    Meals =
                    {
                        new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = recipe, RecipeId = 1, Servings = 1 },
                    },
                },
            },
        };

        var result = PlanAggregator.PerPersonPerDay(plan, TwoPeople());

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Nutrition.Calories, Is.EqualTo(182).Within(0.001));
            Assert.That(result[1].Nutrition.Calories, Is.EqualTo(182).Within(0.001));
        });
    }

    [Test]
    public void PerPersonPerDay_FirstPersonMeal_CountsForFirstPersonOnly()
    {
        var recipe = Bread();
        var plan = new MealPlan
        {
            Year = 2026,
            Month = 1,
            Days =
            {
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 6),
                    DayType = DayType.Normal,
                    Meals =
                    {
                        new PlannedMeal { Slot = MealType.Lunch, Assignee = MealAssignee.FirstPerson, Recipe = recipe, RecipeId = 1, Servings = 2 },
                    },
                },
            },
        };

        var result = PlanAggregator.PerPersonPerDay(plan, TwoPeople());

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].PersonId, Is.EqualTo(1));
            Assert.That(result[0].Nutrition.Calories, Is.EqualTo(728).Within(0.001));
        });
    }

    [Test]
    public void PerPersonPerDay_NonNormalDays_AreExcluded()
    {
        var recipe = Bread();
        var plan = new MealPlan
        {
            Year = 2026,
            Month = 1,
            Days =
            {
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 7),
                    DayType = DayType.EatingOut,
                    Meals =
                    {
                        new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = recipe, RecipeId = 1, Servings = 1 },
                    },
                },
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 8),
                    DayType = DayType.Event,
                    Meals =
                    {
                        new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = recipe, RecipeId = 1, Servings = 1 },
                    },
                },
            },
        };

        var result = PlanAggregator.PerPersonPerDay(plan, TwoPeople());

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void PerPersonMonth_SumsCountedDays()
    {
        var plan = new MealPlan
        {
            Year = 2026,
            Month = 1,
            Days =
            {
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 5),
                    DayType = DayType.Normal,
                    Meals =
                    {
                        new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.FirstPerson, Recipe = Bread(), RecipeId = 1, Servings = 1 },
                    },
                },
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 6),
                    DayType = DayType.Normal,
                    Meals =
                    {
                        new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.FirstPerson, Recipe = Bread(), RecipeId = 1, Servings = 1 },
                    },
                },
            },
        };

        var result = PlanAggregator.PerPersonMonth(plan, TwoPeople());

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].PersonId, Is.EqualTo(1));
            Assert.That(result[0].Nutrition.Calories, Is.EqualTo(728).Within(0.001));
        });
    }

    [Test]
    public void PerPersonPerDay_NullPlan_Throws() =>
        Assert.Throws<ArgumentNullException>(() => PlanAggregator.PerPersonPerDay(null!, TwoPeople()));

    [Test]
    public void PerPersonPerDay_NullPeople_Throws() =>
        Assert.Throws<ArgumentNullException>(() => PlanAggregator.PerPersonPerDay(new MealPlan(), null!));
}
