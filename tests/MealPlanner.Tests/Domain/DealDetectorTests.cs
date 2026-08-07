using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Shopping;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="DealDetector"/> compares the latest normalised price against the historical
/// average and flags a deal only when the latest price is sufficiently below it.
/// </summary>
[TestFixture]
public class DealDetectorTests
{
    private static Ingredient Flour() => new()
    {
        Id = 1,
        Name = "Flour",
        BaseUnit = MeasurementUnit.Gram,
    };

    private static IngredientPrice Price(decimal price, double packageQty, DateOnly date) => new()
    {
        IngredientId = 1,
        Price = price,
        PackageQuantity = packageQty,
        PackageUnit = MeasurementUnit.Gram,
        RecordedDate = date,
    };

    [Test]
    public void Evaluate_LatestWellBelowAverage_IsDeal()
    {
        var prices = new List<IngredientPrice>
        {
            Price(5m, 1000, new DateOnly(2026, 1, 1)),  // $0.005/g
            Price(5m, 1000, new DateOnly(2026, 1, 8)),  // $0.005/g
            Price(3m, 1000, new DateOnly(2026, 1, 15)), // $0.003/g (latest, 40% below)
        };

        var result = DealDetector.Evaluate(Flour(), prices);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.IsDeal, Is.True);
            Assert.That(result.PercentBelowAverage, Is.EqualTo(40).Within(0.001));
        });
    }

    [Test]
    public void Evaluate_LatestNearAverage_IsNotDeal()
    {
        var prices = new List<IngredientPrice>
        {
            Price(5m, 1000, new DateOnly(2026, 1, 1)),
            Price(5m, 1000, new DateOnly(2026, 1, 8)),
            Price(4.9m, 1000, new DateOnly(2026, 1, 15)), // only 2% below
        };

        var result = DealDetector.Evaluate(Flour(), prices);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDeal, Is.False);
    }

    [Test]
    public void Evaluate_NormalisesDifferentPackageSizes()
    {
        var prices = new List<IngredientPrice>
        {
            Price(5m, 1000, new DateOnly(2026, 1, 1)),   // $0.005/g
            Price(10m, 2000, new DateOnly(2026, 1, 8)),  // $0.005/g
            Price(6m, 2000, new DateOnly(2026, 1, 15)),  // $0.003/g (latest, 40% below)
        };

        var result = DealDetector.Evaluate(Flour(), prices);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDeal, Is.True);
    }

    [Test]
    public void Evaluate_FewerThanTwoPrices_ReturnsNull()
    {
        var prices = new List<IngredientPrice>
        {
            Price(5m, 1000, new DateOnly(2026, 1, 1)),
        };

        var result = DealDetector.Evaluate(Flour(), prices);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void Evaluate_NullArguments_Throw()
    {
        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => DealDetector.Evaluate(null!, []));
            Assert.Throws<ArgumentNullException>(() => DealDetector.Evaluate(Flour(), null!));
        });
    }

    [Test]
    public void Evaluate_CustomThreshold_UsesProvidedValue()
    {
        // 8% below average is a deal with 5% threshold but not with 10%
        var prices = new List<IngredientPrice>
        {
            Price(5m, 1000, new DateOnly(2026, 1, 1)),  // $0.005/g
            Price(5m, 1000, new DateOnly(2026, 1, 8)),  // $0.005/g
            Price(4.6m, 1000, new DateOnly(2026, 1, 15)), // $0.0046/g (8% below)
        };

        var resultStrict = DealDetector.Evaluate(Flour(), prices, thresholdPercent: 10.0);
        var resultLoose = DealDetector.Evaluate(Flour(), prices, thresholdPercent: 5.0);

        Assert.Multiple(() =>
        {
            Assert.That(resultStrict!.IsDeal, Is.False);
            Assert.That(resultLoose!.IsDeal, Is.True);
        });
    }

    [Test]
    public void Evaluate_PricesForDifferentIngredients_AreFiltered()
    {
        var flour = Flour();
        var prices = new List<IngredientPrice>
        {
            Price(5m, 1000, new DateOnly(2026, 1, 1)),  // flour price
            new() { IngredientId = 999, Price = 100m, PackageQuantity = 100, PackageUnit = MeasurementUnit.Gram, RecordedDate = new DateOnly(2026, 1, 2) },
            Price(3m, 1000, new DateOnly(2026, 1, 15)), // flour price (latest)
        };

        var result = DealDetector.Evaluate(flour, prices);

        // Only flour prices (IngredientId=1) are considered.
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.IsDeal, Is.True);
    }

    [Test]
    public void Evaluate_LatestAboveAverage_IsNotDealWithNegativePercent()
    {
        var prices = new List<IngredientPrice>
        {
            Price(3m, 1000, new DateOnly(2026, 1, 1)),
            Price(3m, 1000, new DateOnly(2026, 1, 8)),
            Price(5m, 1000, new DateOnly(2026, 1, 15)), // above average
        };

        var result = DealDetector.Evaluate(Flour(), prices);

        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.IsDeal, Is.False);
            Assert.That(result.PercentBelowAverage, Is.LessThan(0));
        });
    }

    [Test]
    public void Evaluate_IncompatiblePackageUnit_SkipsPrice()
    {
        var flour = Flour(); // Gram-based
        var prices = new List<IngredientPrice>
        {
            // Millilitre package can't be normalised for a gram-based ingredient
            new() { IngredientId = 1, Price = 5m, PackageQuantity = 500, PackageUnit = MeasurementUnit.Millilitre, RecordedDate = new DateOnly(2026, 1, 1) },
            Price(5m, 1000, new DateOnly(2026, 1, 8)),
        };

        // Only one price can be normalised, so fewer than 2 => null
        var result = DealDetector.Evaluate(flour, prices);

        Assert.That(result, Is.Null);
    }
}
