using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="NutritionFacts"/> record methods: <see cref="NutritionFacts.Zero"/>,
/// <see cref="NutritionFacts.Scale"/>, and <see cref="NutritionFacts.Add"/>.
/// </summary>
[TestFixture]
public class NutritionFactsTests
{
    // -- Zero ---------------------------------------------------------------------------------

    [Test]
    public void Zero_ReturnsAllZeroesAndNotEstimated()
    {
        var zero = NutritionFacts.Zero;

        Assert.Multiple(() =>
        {
            Assert.That(zero.Calories, Is.EqualTo(0));
            Assert.That(zero.Protein, Is.EqualTo(0));
            Assert.That(zero.Fiber, Is.EqualTo(0));
            Assert.That(zero.Carbs, Is.EqualTo(0));
            Assert.That(zero.Fat, Is.EqualTo(0));
            Assert.That(zero.IsEstimated, Is.False);
        });
    }

    [Test]
    public void Zero_IsSameReference()
    {
        var first = NutritionFacts.Zero;
        var second = NutritionFacts.Zero;

        Assert.That(first, Is.SameAs(second));
    }

    // -- Scale --------------------------------------------------------------------------------

    [Test]
    public void Scale_MultipliesAllValues()
    {
        var facts = new NutritionFacts(200, 50, 10, 30, 15, false);

        var scaled = facts.Scale(0.5);

        Assert.Multiple(() =>
        {
            Assert.That(scaled.Calories, Is.EqualTo(100).Within(0.001));
            Assert.That(scaled.Protein, Is.EqualTo(25).Within(0.001));
            Assert.That(scaled.Fiber, Is.EqualTo(5).Within(0.001));
            Assert.That(scaled.Carbs, Is.EqualTo(15).Within(0.001));
            Assert.That(scaled.Fat, Is.EqualTo(7.5).Within(0.001));
        });
    }

    [Test]
    public void Scale_PreservesIsEstimatedFlagWhenTrue()
    {
        var facts = new NutritionFacts(100, 10, 5, 20, 8, true);

        var scaled = facts.Scale(2.0);

        Assert.That(scaled.IsEstimated, Is.True);
    }

    [Test]
    public void Scale_PreservesIsEstimatedFlagWhenFalse()
    {
        var facts = new NutritionFacts(100, 10, 5, 20, 8, false);

        var scaled = facts.Scale(3.0);

        Assert.That(scaled.IsEstimated, Is.False);
    }

    [Test]
    public void Scale_WithZeroFactor_ReturnsAllZeroes()
    {
        var facts = new NutritionFacts(500, 100, 25, 60, 30, false);

        var scaled = facts.Scale(0.0);

        Assert.Multiple(() =>
        {
            Assert.That(scaled.Calories, Is.EqualTo(0));
            Assert.That(scaled.Protein, Is.EqualTo(0));
            Assert.That(scaled.Fiber, Is.EqualTo(0));
            Assert.That(scaled.Carbs, Is.EqualTo(0));
            Assert.That(scaled.Fat, Is.EqualTo(0));
            Assert.That(scaled.IsEstimated, Is.False);
        });
    }

    [Test]
    public void Scale_WithNegativeFactor_ProducesNegativeValues()
    {
        var facts = new NutritionFacts(100, 10, 5, 20, 8, false);

        var scaled = facts.Scale(-1.0);

        Assert.That(scaled.Calories, Is.EqualTo(-100).Within(0.001));
    }

    // -- Add ----------------------------------------------------------------------------------

    [Test]
    public void Add_SumsAllValues()
    {
        var a = new NutritionFacts(100, 10, 5, 20, 8, false);
        var b = new NutritionFacts(200, 20, 10, 40, 16, false);

        var sum = a.Add(b);

        Assert.Multiple(() =>
        {
            Assert.That(sum.Calories, Is.EqualTo(300).Within(0.001));
            Assert.That(sum.Protein, Is.EqualTo(30).Within(0.001));
            Assert.That(sum.Fiber, Is.EqualTo(15).Within(0.001));
            Assert.That(sum.Carbs, Is.EqualTo(60).Within(0.001));
            Assert.That(sum.Fat, Is.EqualTo(24).Within(0.001));
        });
    }

    [Test]
    public void Add_WhenEitherIsEstimated_ResultIsEstimated()
    {
        var estimated = new NutritionFacts(100, 10, 5, 20, 8, true);
        var exact = new NutritionFacts(200, 20, 10, 40, 16, false);

        Assert.Multiple(() =>
        {
            Assert.That(estimated.Add(exact).IsEstimated, Is.True);
            Assert.That(exact.Add(estimated).IsEstimated, Is.True);
        });
    }

    [Test]
    public void Add_WhenBothEstimated_ResultIsEstimated()
    {
        var a = new NutritionFacts(100, 10, 5, 20, 8, true);
        var b = new NutritionFacts(200, 20, 10, 40, 16, true);

        Assert.That(a.Add(b).IsEstimated, Is.True);
    }

    [Test]
    public void Add_WhenNeitherEstimated_ResultIsNotEstimated()
    {
        var a = new NutritionFacts(100, 10, 5, 20, 8, false);
        var b = new NutritionFacts(200, 20, 10, 40, 16, false);

        Assert.That(a.Add(b).IsEstimated, Is.False);
    }

    [Test]
    public void Add_WithZero_ReturnsSameValues()
    {
        var facts = new NutritionFacts(100, 10, 5, 20, 8, false);

        var sum = facts.Add(NutritionFacts.Zero);

        Assert.Multiple(() =>
        {
            Assert.That(sum.Calories, Is.EqualTo(100).Within(0.001));
            Assert.That(sum.Protein, Is.EqualTo(10).Within(0.001));
        });
    }

    [Test]
    public void Add_NullOther_ThrowsArgumentNullException()
    {
        var facts = new NutritionFacts(100, 10, 5, 20, 8, false);

        Assert.Throws<ArgumentNullException>(() => facts.Add(null!));
    }
}
