using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Shopping;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="ShoppingListGenerator"/> aggregates planned ingredients, subtracts pantry
/// stock, prices at the preferred store, and flags shared and bulk purchases.
/// </summary>
[TestFixture]
public class ShoppingListGeneratorTests
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

    // A recipe yielding 2 servings that uses 500 g flour.
    private static Recipe Bread(Ingredient flour) => new()
    {
        Id = 1,
        Name = "Bread",
        Servings = 2,
        Ingredients =
        {
            new RecipeIngredient { IngredientId = flour.Id, Ingredient = flour, Quantity = 500, Unit = MeasurementUnit.Gram },
        },
    };

    private static MealPlan PlanWith(Recipe recipe, int servings, DayType dayType = DayType.Normal) => new()
    {
        Year = 2026,
        Month = 1,
        Days =
        {
            new DayPlan
            {
                Date = new DateOnly(2026, 1, 5),
                DayType = dayType,
                Meals =
                {
                    new PlannedMeal { Slot = MealType.Dinner, Assignee = MealAssignee.Shared, Recipe = recipe, RecipeId = recipe.Id, Servings = servings },
                },
            },
        },
    };

    private static IngredientPrice Price(int ingredientId, decimal price, double packageQty, bool preferred = false, DateOnly? date = null) => new()
    {
        IngredientId = ingredientId,
        StoreId = 1,
        Store = new Store { Id = 1, Name = "Superstore" },
        Price = price,
        PackageQuantity = packageQty,
        PackageUnit = MeasurementUnit.Gram,
        RecordedDate = date ?? new DateOnly(2026, 1, 1),
        IsPreferredStore = preferred,
    };

    [Test]
    public void Generate_ScalesQuantityByPlannedServings()
    {
        var flour = Flour();
        // 2 planned servings of a 2-serving recipe => 1x the recipe => 500 g flour.
        var plan = PlanWith(Bread(flour), servings: 2);

        var list = ShoppingListGenerator.Generate(plan, [], [Price(1, 4m, 1000)]);

        Assert.That(list.Lines, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(list.Lines[0].RequiredQuantity, Is.EqualTo(500).Within(0.001));
            Assert.That(list.Lines[0].ToBuyQuantity, Is.EqualTo(500).Within(0.001));
            Assert.That(list.Lines[0].PackagesToBuy, Is.EqualTo(1));
            Assert.That(list.Lines[0].EstimatedCost, Is.EqualTo(4m));
        });
    }

    [Test]
    public void Generate_SubtractsPantryStock()
    {
        var flour = Flour();
        var plan = PlanWith(Bread(flour), servings: 2); // needs 500 g
        var pantry = new List<PantryItem>
        {
            new() { IngredientId = 1, Ingredient = flour, QuantityOnHand = 200, Unit = MeasurementUnit.Gram, Location = StorageLocation.Pantry },
        };

        var list = ShoppingListGenerator.Generate(plan, pantry, [Price(1, 4m, 1000)]);

        Assert.That(list.Lines, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(list.Lines[0].PantryQuantity, Is.EqualTo(200).Within(0.001));
            Assert.That(list.Lines[0].ToBuyQuantity, Is.EqualTo(300).Within(0.001));
        });
    }

    [Test]
    public void Generate_PantryFullyCovers_ExcludesLine()
    {
        var flour = Flour();
        var plan = PlanWith(Bread(flour), servings: 2); // needs 500 g
        var pantry = new List<PantryItem>
        {
            new() { IngredientId = 1, Ingredient = flour, QuantityOnHand = 800, Unit = MeasurementUnit.Gram, Location = StorageLocation.Pantry },
        };

        var list = ShoppingListGenerator.Generate(plan, pantry, [Price(1, 4m, 1000)]);

        Assert.That(list.Lines, Is.Empty);
    }

    [Test]
    public void Generate_LargePackage_FlagsBulk()
    {
        var flour = Flour();
        var plan = PlanWith(Bread(flour), servings: 2); // needs 500 g
        // A 5 kg package is 10x the 500 g needed => bulk.
        var list = ShoppingListGenerator.Generate(plan, [], [Price(1, 20m, 5000)]);

        Assert.That(list.Lines[0].IsBulkPurchase, Is.True);
    }

    [Test]
    public void Generate_IngredientInMultipleRecipes_FlagsShared()
    {
        var flour = Flour();
        var recipe1 = Bread(flour);
        var recipe2 = new Recipe
        {
            Id = 2,
            Name = "Pancakes",
            Servings = 2,
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 1, Ingredient = flour, Quantity = 200, Unit = MeasurementUnit.Gram },
            },
        };

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
                        new PlannedMeal { Slot = MealType.Dinner, Recipe = recipe1, RecipeId = 1, Servings = 2 },
                        new PlannedMeal { Slot = MealType.Breakfast, Recipe = recipe2, RecipeId = 2, Servings = 2 },
                    },
                },
            },
        };

        var list = ShoppingListGenerator.Generate(plan, [], [Price(1, 4m, 1000)]);

        Assert.That(list.Lines, Has.Count.EqualTo(1));
        Assert.Multiple(() =>
        {
            Assert.That(list.Lines[0].IsSharedAcrossRecipes, Is.True);
            // 500 g + 200 g = 700 g => 1 package of 1000 g.
            Assert.That(list.Lines[0].RequiredQuantity, Is.EqualTo(700).Within(0.001));
            Assert.That(list.Lines[0].PackagesToBuy, Is.EqualTo(1));
        });
    }

    [Test]
    public void Generate_MultiplePackagesNeeded_RoundsUp()
    {
        var flour = Flour();
        // 8 planned servings of a 2-serving recipe => 4x => 2000 g. Package is 500 g => 4 packages.
        var plan = PlanWith(Bread(flour), servings: 8);

        var list = ShoppingListGenerator.Generate(plan, [], [Price(1, 2m, 500)]);

        Assert.Multiple(() =>
        {
            Assert.That(list.Lines[0].RequiredQuantity, Is.EqualTo(2000).Within(0.001));
            Assert.That(list.Lines[0].PackagesToBuy, Is.EqualTo(4));
            Assert.That(list.Lines[0].EstimatedCost, Is.EqualTo(8m));
        });
    }

    [Test]
    public void Generate_NoPrice_MarksEstimated()
    {
        var flour = Flour();
        var plan = PlanWith(Bread(flour), servings: 2);

        var list = ShoppingListGenerator.Generate(plan, [], []);

        Assert.Multiple(() =>
        {
            Assert.That(list.Lines[0].IsCostEstimated, Is.True);
            Assert.That(list.IsEstimated, Is.True);
            Assert.That(list.EstimatedTotal, Is.EqualTo(0m));
        });
    }

    [Test]
    public void Generate_PrefersPreferredStorePrice()
    {
        var flour = Flour();
        var plan = PlanWith(Bread(flour), servings: 2); // 500 g
        var prices = new List<IngredientPrice>
        {
            Price(1, 4m, 1000, preferred: false, date: new DateOnly(2026, 1, 2)),
            new()
            {
                IngredientId = 1,
                StoreId = 2,
                Store = new Store { Id = 2, Name = "Costco" },
                Price = 10m,
                PackageQuantity = 5000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 1, 1),
                IsPreferredStore = true,
            },
        };

        var list = ShoppingListGenerator.Generate(plan, [], prices);

        Assert.That(list.Lines[0].PreferredStoreName, Is.EqualTo("Costco"));
    }

    [Test]
    public void Generate_NonNormalDays_AreExcluded()
    {
        var flour = Flour();
        var plan = PlanWith(Bread(flour), servings: 2, dayType: DayType.EatingOut);

        var list = ShoppingListGenerator.Generate(plan, [], [Price(1, 4m, 1000)]);

        Assert.That(list.Lines, Is.Empty);
    }

    [Test]
    public void Generate_NullArguments_Throw()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => ShoppingListGenerator.Generate(null!, [], []));
            Assert.Throws<ArgumentNullException>(() => ShoppingListGenerator.Generate(new MealPlan(), null!, []));
            Assert.Throws<ArgumentNullException>(() => ShoppingListGenerator.Generate(new MealPlan(), [], null!));
        });
    }
}
