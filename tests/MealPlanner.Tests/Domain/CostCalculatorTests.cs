using MealPlanner.Domain.Costing;
using MealPlanner.Domain.Entities;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="CostCalculator"/> selects the preferred/most-recent price, prorates package
/// prices to the used quantity, propagates estimate flags, and divides by servings.
/// </summary>
[TestFixture]
public class CostCalculatorTests
{
    private static Ingredient Flour() => new()
    {
        Id = 1,
        Name = "Flour",
        BaseUnit = MeasurementUnit.Gram,
        CaloriesPer100 = 364,
    };

    private static Recipe RecipeUsing(Ingredient ingredient, double quantity, int servings = 1) => new()
    {
        Name = "Bread",
        Servings = servings,
        Ingredients =
        {
            new RecipeIngredient
            {
                IngredientId = ingredient.Id,
                Ingredient = ingredient,
                Quantity = quantity,
                Unit = MeasurementUnit.Gram,
            },
        },
    };

    [Test]
    public void ForRecipe_ProratesPackagePriceToUsedQuantity()
    {
        var recipe = RecipeUsing(Flour(), quantity: 500);
        var prices = new[]
        {
            new IngredientPrice
            {
                IngredientId = 1,
                Price = 4.00m,
                PackageQuantity = 1000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 1, 1),
            },
        };

        var cost = CostCalculator.ForRecipe(recipe, prices);

        Assert.Multiple(() =>
        {
            Assert.That(cost.TotalCost, Is.EqualTo(2.00m));
            Assert.That(cost.CostPerServing, Is.EqualTo(2.00m));
            Assert.That(cost.IsEstimated, Is.False);
        });
    }

    [Test]
    public void ForRecipe_PrefersPreferredStorePrice()
    {
        var recipe = RecipeUsing(Flour(), quantity: 1000);
        var prices = new[]
        {
            new IngredientPrice
            {
                IngredientId = 1,
                Price = 3.00m,
                PackageQuantity = 1000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 2, 1),
                IsPreferredStore = false,
            },
            new IngredientPrice
            {
                IngredientId = 1,
                Price = 5.00m,
                PackageQuantity = 1000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 1, 1),
                IsPreferredStore = true,
            },
        };

        var cost = CostCalculator.ForRecipe(recipe, prices);

        Assert.That(cost.TotalCost, Is.EqualTo(5.00m));
    }

    [Test]
    public void ForRecipe_NoPreferred_UsesMostRecentPrice()
    {
        var recipe = RecipeUsing(Flour(), quantity: 1000);
        var prices = new[]
        {
            new IngredientPrice
            {
                IngredientId = 1,
                Price = 3.00m,
                PackageQuantity = 1000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 1, 1),
            },
            new IngredientPrice
            {
                IngredientId = 1,
                Price = 4.50m,
                PackageQuantity = 1000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 3, 1),
            },
        };

        var cost = CostCalculator.ForRecipe(recipe, prices);

        Assert.That(cost.TotalCost, Is.EqualTo(4.50m));
    }

    [Test]
    public void ForRecipe_EstimatedPrice_FlagsResultEstimated()
    {
        var recipe = RecipeUsing(Flour(), quantity: 1000);
        var prices = new[]
        {
            new IngredientPrice
            {
                IngredientId = 1,
                Price = 4.00m,
                PackageQuantity = 1000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 1, 1),
                IsEstimated = true,
            },
        };

        Assert.That(CostCalculator.ForRecipe(recipe, prices).IsEstimated, Is.True);
    }

    [Test]
    public void ForRecipe_NoPriceForIngredient_FlagsEstimated()
    {
        var recipe = RecipeUsing(Flour(), quantity: 500);

        var cost = CostCalculator.ForRecipe(recipe, []);

        Assert.Multiple(() =>
        {
            Assert.That(cost.TotalCost, Is.EqualTo(0m));
            Assert.That(cost.IsEstimated, Is.True);
        });
    }

    [Test]
    public void ForRecipe_DividesCostByServings()
    {
        var recipe = RecipeUsing(Flour(), quantity: 1000, servings: 4);
        var prices = new[]
        {
            new IngredientPrice
            {
                IngredientId = 1,
                Price = 4.00m,
                PackageQuantity = 1000,
                PackageUnit = MeasurementUnit.Gram,
                RecordedDate = new DateOnly(2026, 1, 1),
            },
        };

        var cost = CostCalculator.ForRecipe(recipe, prices);

        Assert.Multiple(() =>
        {
            Assert.That(cost.TotalCost, Is.EqualTo(4.00m));
            Assert.That(cost.CostPerServing, Is.EqualTo(1.00m));
        });
    }

    [Test]
    public void ForRecipe_NullRecipe_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CostCalculator.ForRecipe(null!, []));

    [Test]
    public void ForRecipe_NullPrices_Throws() =>
        Assert.Throws<ArgumentNullException>(() => CostCalculator.ForRecipe(RecipeUsing(Flour(), 100), null!));
}
