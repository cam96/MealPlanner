using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="UnitConverter"/> handles all supported conversion paths: same-unit passthrough,
/// Each ↔ Gram via serving weight, and correctly declines incompatible conversions.
/// </summary>
[TestFixture]
public class UnitConverterTests
{
    // -- Same-unit passthrough -----------------------------------------------------------------

    [TestCase(MeasurementUnit.Gram, 250.0)]
    [TestCase(MeasurementUnit.Millilitre, 100.5)]
    [TestCase(MeasurementUnit.Each, 3.0)]
    public void TryToBaseUnits_SameUnit_ReturnsTrueAndQuantity(MeasurementUnit unit, double quantity)
    {
        var success = UnitConverter.TryToBaseUnits(unit, servingWeightG: null, quantity, unit, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(quantity).Within(0.001));
        });
    }

    // -- Each → Gram (with valid serving weight) -----------------------------------------------

    [Test]
    public void TryToBaseUnits_EachToGram_WithServingWeight_Converts()
    {
        // 3 eggs × 50 g each = 150 g
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: 50.0, quantity: 3, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(150.0).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_EachToGram_FractionalServingWeight_Converts()
    {
        // 2.5 items × 120.5 g each = 301.25 g
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: 120.5, quantity: 2.5, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(301.25).Within(0.001));
        });
    }

    // -- Gram → Each (with valid serving weight) -----------------------------------------------

    [Test]
    public void TryToBaseUnits_GramToEach_WithServingWeight_Converts()
    {
        // 150 g ÷ 50 g per item = 3 items
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Each, servingWeightG: 50.0, quantity: 150, MeasurementUnit.Gram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(3.0).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_GramToEach_ProducesNonIntegerResult()
    {
        // 100 g ÷ 60 g per item ≈ 1.667 items
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Each, servingWeightG: 60.0, quantity: 100, MeasurementUnit.Gram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(100.0 / 60.0).Within(0.001));
        });
    }

    // -- Each ↔ Gram failure when no serving weight -------------------------------------------

    [Test]
    public void TryToBaseUnits_EachToGram_NullServingWeight_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: null, quantity: 2, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_EachToGram_ZeroServingWeight_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: 0.0, quantity: 2, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_EachToGram_NegativeServingWeight_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: -10.0, quantity: 2, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_GramToEach_NullServingWeight_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Each, servingWeightG: null, quantity: 100, MeasurementUnit.Gram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    // -- Incompatible conversions (Gram ↔ Millilitre, Each ↔ Millilitre) ----------------------

    [Test]
    public void TryToBaseUnits_GramToMillilitre_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Millilitre, servingWeightG: 50.0, quantity: 100, MeasurementUnit.Gram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_MillilitreToGram_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: 50.0, quantity: 100, MeasurementUnit.Millilitre, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_EachToMillilitre_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Millilitre, servingWeightG: 50.0, quantity: 2, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_MillilitreToEach_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Each, servingWeightG: 50.0, quantity: 100, MeasurementUnit.Millilitre, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    // -- Edge cases ---------------------------------------------------------------------------

    [Test]
    public void TryToBaseUnits_ZeroQuantity_SameUnit_ReturnsZero()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: null, quantity: 0, MeasurementUnit.Gram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_MillilitreToMillilitre_Passthrough()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Millilitre, servingWeightG: null, quantity: 500, MeasurementUnit.Millilitre, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(500).Within(0.001));
        });
    }
}
