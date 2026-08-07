using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Planning;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="PrepTimeLoad"/> sums prep and cook minutes per normal day and excludes
/// eating-out and event days.
/// </summary>
[TestFixture]
public class PrepTimeLoadTests
{
    private static Recipe Recipe(int prep, int cook) => new()
    {
        Id = 1,
        Name = "Stew",
        Servings = 1,
        PrepMinutes = prep,
        CookMinutes = cook,
    };

    [Test]
    public void PerDay_SumsPrepAndCookMinutes()
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
                        new PlannedMeal { Slot = MealType.Lunch, Recipe = Recipe(10, 20), RecipeId = 1, Servings = 1 },
                        new PlannedMeal { Slot = MealType.Dinner, Recipe = Recipe(15, 30), RecipeId = 1, Servings = 1 },
                    },
                },
            },
        };

        var result = PrepTimeLoad.PerDay(plan);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Date, Is.EqualTo(new DateOnly(2026, 1, 5)));
            Assert.That(result[0].TotalMinutes, Is.EqualTo(75));
        });
    }

    [Test]
    public void PerDay_ExcludesNonNormalDays()
    {
        var plan = new MealPlan
        {
            Year = 2026,
            Month = 1,
            Days =
            {
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 6),
                    DayType = DayType.Event,
                    Meals =
                    {
                        new PlannedMeal { Slot = MealType.Dinner, Recipe = Recipe(10, 20), RecipeId = 1, Servings = 1 },
                    },
                },
            },
        };

        var result = PrepTimeLoad.PerDay(plan);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void PerDay_NullPlan_Throws() =>
        Assert.Throws<ArgumentNullException>(() => PrepTimeLoad.PerDay(null!));

    [Test]
    public void PerDay_ZeroPrepAndCookMinutes_ExcludesDay()
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
                        new PlannedMeal { Slot = MealType.Lunch, Recipe = Recipe(0, 0), RecipeId = 1, Servings = 1 },
                    },
                },
            },
        };

        var result = PrepTimeLoad.PerDay(plan);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void PerDay_MultipleDays_OrderedByDate()
    {
        var plan = new MealPlan
        {
            Year = 2026,
            Month = 1,
            Days =
            {
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 10),
                    DayType = DayType.Normal,
                    Meals = { new PlannedMeal { Slot = MealType.Dinner, Recipe = Recipe(5, 10), RecipeId = 1, Servings = 1 } },
                },
                new DayPlan
                {
                    Date = new DateOnly(2026, 1, 3),
                    DayType = DayType.Normal,
                    Meals = { new PlannedMeal { Slot = MealType.Dinner, Recipe = Recipe(20, 30), RecipeId = 1, Servings = 1 } },
                },
            },
        };

        var result = PrepTimeLoad.PerDay(plan);

        Assert.That(result, Has.Count.EqualTo(2));
        Assert.Multiple(() =>
        {
            Assert.That(result[0].Date, Is.EqualTo(new DateOnly(2026, 1, 3)));
            Assert.That(result[0].TotalMinutes, Is.EqualTo(50));
            Assert.That(result[1].Date, Is.EqualTo(new DateOnly(2026, 1, 10)));
            Assert.That(result[1].TotalMinutes, Is.EqualTo(15));
        });
    }

    [Test]
    public void PerDay_MealWithNoRecipe_ContributesZero()
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
                        new PlannedMeal { Slot = MealType.Lunch, Recipe = null, MealCombo = new MealCombo { Name = "combo" }, MealComboId = 1, Servings = 1 },
                        new PlannedMeal { Slot = MealType.Dinner, Recipe = Recipe(10, 20), RecipeId = 1, Servings = 1 },
                    },
                },
            },
        };

        var result = PrepTimeLoad.PerDay(plan);

        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].TotalMinutes, Is.EqualTo(30));
    }
}
