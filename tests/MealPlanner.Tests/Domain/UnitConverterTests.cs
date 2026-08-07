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

    // -- Kilogram conversions ------------------------------------------------------------------

    [Test]
    public void TryToBaseUnits_KilogramToGram_Converts()
    {
        // 2.5 kg → 2500 g
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: null, quantity: 2.5, MeasurementUnit.Kilogram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(2500.0).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_GramToKilogram_Converts()
    {
        // 500 g → 0.5 kg
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Kilogram, servingWeightG: null, quantity: 500, MeasurementUnit.Gram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(0.5).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_KilogramToKilogram_Passthrough()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Kilogram, servingWeightG: null, quantity: 3.0, MeasurementUnit.Kilogram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(3.0).Within(0.001));
        });
    }

    // -- Pound conversions ---------------------------------------------------------------------

    [Test]
    public void TryToBaseUnits_PoundToGram_Converts()
    {
        // 1 lb → 453.592 g
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: null, quantity: 1.0, MeasurementUnit.Pound, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(453.592).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_GramToPound_Converts()
    {
        // 453.592 g → 1 lb
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Pound, servingWeightG: null, quantity: 453.592, MeasurementUnit.Gram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(1.0).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_PoundToKilogram_Converts()
    {
        // 2.2 lbs → ~0.998 kg (cross-conversion via grams)
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Kilogram, servingWeightG: null, quantity: 2.2, MeasurementUnit.Pound, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(2.2 * 453.592 / 1000.0).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_KilogramToPound_Converts()
    {
        // 1 kg → ~2.205 lbs
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Pound, servingWeightG: null, quantity: 1.0, MeasurementUnit.Kilogram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(1000.0 / 453.592).Within(0.001));
        });
    }

    // -- Litre conversions ---------------------------------------------------------------------

    [Test]
    public void TryToBaseUnits_LitreToMillilitre_Converts()
    {
        // 1.5 L → 1500 ml
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Millilitre, servingWeightG: null, quantity: 1.5, MeasurementUnit.Litre, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(1500.0).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_MillilitreToLitre_Converts()
    {
        // 750 ml → 0.75 L
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Litre, servingWeightG: null, quantity: 750, MeasurementUnit.Millilitre, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(0.75).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_LitreToLitre_Passthrough()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Litre, servingWeightG: null, quantity: 2.0, MeasurementUnit.Litre, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(2.0).Within(0.001));
        });
    }

    // -- Cross-dimension incompatibility with new units ----------------------------------------

    [Test]
    public void TryToBaseUnits_KilogramToMillilitre_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Millilitre, servingWeightG: null, quantity: 1.0, MeasurementUnit.Kilogram, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_LitreToGram_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Gram, servingWeightG: null, quantity: 1.0, MeasurementUnit.Litre, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_PoundToLitre_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Litre, servingWeightG: null, quantity: 2.0, MeasurementUnit.Pound, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }

    [Test]
    public void TryToBaseUnits_EachToKilogram_WithServingWeight_Converts()
    {
        // 3 items × 50 g = 150 g → 0.15 kg
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Kilogram, servingWeightG: 50.0, quantity: 3, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(0.15).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_PoundToEach_WithServingWeight_Converts()
    {
        // 1 lb = 453.592 g ÷ 50 g per item = ~9.072 items
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Each, servingWeightG: 50.0, quantity: 1.0, MeasurementUnit.Pound, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.True);
            Assert.That(result, Is.EqualTo(453.592 / 50.0).Within(0.001));
        });
    }

    [Test]
    public void TryToBaseUnits_EachToLitre_ReturnsFalse()
    {
        var success = UnitConverter.TryToBaseUnits(
            MeasurementUnit.Litre, servingWeightG: 50.0, quantity: 2, MeasurementUnit.Each, out var result);

        Assert.Multiple(() =>
        {
            Assert.That(success, Is.False);
            Assert.That(result, Is.EqualTo(0));
        });
    }
}
