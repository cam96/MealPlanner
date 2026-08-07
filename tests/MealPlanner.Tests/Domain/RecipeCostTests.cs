using MealPlanner.Domain.Costing;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="RecipeCost"/> record properties: <see cref="RecipeCost.Zero"/>.
/// </summary>
[TestFixture]
public class RecipeCostTests
{
    [Test]
    public void Zero_ReturnsAllZeroesAndNotEstimated()
    {
        var zero = RecipeCost.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(zero.TotalCost, Is.EqualTo(0m));
            Assert.That(zero.CostPerServing, Is.EqualTo(0m));
            Assert.That(zero.IsEstimated, Is.False);
        });
    }

    [Test]
    public void Zero_IsSameReference()
    {
        var first = RecipeCost.Zero;
        var second = RecipeCost.Zero;

        Assert.That(first, Is.SameAs(second));
    }

    [Test]
    public void RecordEquality_SameValues_AreEqual()
    {
        var a = new RecipeCost(10.50m, 2.10m, false);
        var b = new RecipeCost(10.50m, 2.10m, false);

        Assert.That(a, Is.EqualTo(b));
    }

    [Test]
    public void RecordEquality_DifferentValues_AreNotEqual()
    {
        var a = new RecipeCost(10.50m, 2.10m, false);
        var b = new RecipeCost(10.50m, 2.10m, true);

        Assert.That(a, Is.Not.EqualTo(b));
    }
}
