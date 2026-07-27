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
}
