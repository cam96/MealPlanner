using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;

namespace MealPlanner.Tests.Domain;

/// <summary>
/// Verifies <see cref="NutritionCalculator"/> converts ingredient lines to base units, scales
/// per-100 values, propagates estimate flags, and divides by servings.
/// </summary>
[TestFixture]
public class NutritionCalculatorTests
{
    private static Ingredient Flour(bool estimated = false) => new()
    {
        Id = 1,
        Name = "Flour",
        BaseUnit = MeasurementUnit.Gram,
        CaloriesPer100 = 364,
        ProteinPer100 = 10,
        FiberPer100 = 3,
        CarbsPer100 = 76,
        FatPer100 = 1,
        IsNutritionEstimated = estimated,
    };

    [Test]
    public void ForRecipe_GramLine_ScalesPer100Values()
    {
        var recipe = new Recipe
        {
            Name = "Bread",
            Servings = 1,
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 1, Ingredient = Flour(), Quantity = 250, Unit = MeasurementUnit.Gram },
            },
        };

        var facts = NutritionCalculator.ForRecipe(recipe);

        Assert.Multiple(() =>
        {
            Assert.That(facts.Calories, Is.EqualTo(910).Within(0.001));
            Assert.That(facts.Protein, Is.EqualTo(25).Within(0.001));
            Assert.That(facts.Fiber, Is.EqualTo(7.5).Within(0.001));
            Assert.That(facts.Carbs, Is.EqualTo(190).Within(0.001));
            Assert.That(facts.Fat, Is.EqualTo(2.5).Within(0.001));
            Assert.That(facts.IsEstimated, Is.False);
        });
    }

    [Test]
    public void PerServing_DividesByServings()
    {
        var recipe = new Recipe
        {
            Name = "Bread",
            Servings = 2,
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 1, Ingredient = Flour(), Quantity = 250, Unit = MeasurementUnit.Gram },
            },
        };

        var facts = NutritionCalculator.PerServing(recipe);

        Assert.That(facts.Calories, Is.EqualTo(455).Within(0.001));
    }

    [Test]
    public void ForRecipe_EachLineWithServingWeight_ConvertsToGrams()
    {
        // Ingredient stored in grams, recipe line expressed as a count of items.
        var egg = new Ingredient
        {
            Id = 2,
            Name = "Egg",
            BaseUnit = MeasurementUnit.Gram,
            CaloriesPer100 = 155,
            ProteinPer100 = 13,
            FiberPer100 = 0,
            ServingWeightG = 50,
        };
        var recipe = new Recipe
        {
            Name = "Omelette",
            Servings = 1,
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 2, Ingredient = egg, Quantity = 2, Unit = MeasurementUnit.Each },
            },
        };

        var facts = NutritionCalculator.ForRecipe(recipe);

        // 2 eggs * 50 g = 100 g -> exactly the per-100 values.
        Assert.That(facts.Calories, Is.EqualTo(155).Within(0.001));
    }

    [Test]
    public void ForRecipe_EstimatedIngredient_FlagsResultEstimated()
    {
        var recipe = new Recipe
        {
            Name = "Bread",
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 1, Ingredient = Flour(estimated: true), Quantity = 100, Unit = MeasurementUnit.Gram },
            },
        };

        Assert.That(NutritionCalculator.ForRecipe(recipe).IsEstimated, Is.True);
    }

    [Test]
    public void ForRecipe_MissingIngredientNavigation_FlagsEstimatedAndSkips()
    {
        var recipe = new Recipe
        {
            Name = "Mystery",
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 99, Ingredient = null, Quantity = 100, Unit = MeasurementUnit.Gram },
            },
        };

        var facts = NutritionCalculator.ForRecipe(recipe);

        Assert.Multiple(() =>
        {
            Assert.That(facts.Calories, Is.EqualTo(0));
            Assert.That(facts.IsEstimated, Is.True);
        });
    }

    [Test]
    public void ForRecipe_IncompatibleUnit_FlagsEstimatedAndSkips()
    {
        var recipe = new Recipe
        {
            Name = "Bread",
            Ingredients =
            {
                // Gram-based ingredient measured in millilitres cannot be converted.
                new RecipeIngredient { IngredientId = 1, Ingredient = Flour(), Quantity = 100, Unit = MeasurementUnit.Millilitre },
            },
        };

        var facts = NutritionCalculator.ForRecipe(recipe);

        Assert.Multiple(() =>
        {
            Assert.That(facts.Calories, Is.EqualTo(0));
            Assert.That(facts.IsEstimated, Is.True);
        });
    }

    [Test]
    public void ForRecipe_NullRecipe_Throws() =>
        Assert.Throws<ArgumentNullException>(() => NutritionCalculator.ForRecipe(null!));

    [Test]
    public void PerServing_NullRecipe_Throws() =>
        Assert.Throws<ArgumentNullException>(() => NutritionCalculator.PerServing(null!));

    [Test]
    public void ForRecipe_MultipleIngredients_AccumulatesCorrectly()
    {
        var flour = Flour();
        var egg = new Ingredient
        {
            Id = 2,
            Name = "Egg",
            BaseUnit = MeasurementUnit.Gram,
            CaloriesPer100 = 155,
            ProteinPer100 = 13,
            FiberPer100 = 0,
            CarbsPer100 = 1.1,
            FatPer100 = 11,
            ServingWeightG = 50,
        };
        var recipe = new Recipe
        {
            Name = "Bread with egg wash",
            Servings = 1,
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 1, Ingredient = flour, Quantity = 100, Unit = MeasurementUnit.Gram },
                new RecipeIngredient { IngredientId = 2, Ingredient = egg, Quantity = 50, Unit = MeasurementUnit.Gram },
            },
        };

        var facts = NutritionCalculator.ForRecipe(recipe);

        Assert.Multiple(() =>
        {
            // 100g flour (364) + 50g egg (155/2) = 364 + 77.5 = 441.5
            Assert.That(facts.Calories, Is.EqualTo(441.5).Within(0.001));
            // 100g flour (10) + 50g egg (13/2) = 10 + 6.5 = 16.5
            Assert.That(facts.Protein, Is.EqualTo(16.5).Within(0.001));
            Assert.That(facts.IsEstimated, Is.False);
        });
    }

    [Test]
    public void PerServing_ServingsOfZero_TreatedAsOne()
    {
        var recipe = new Recipe
        {
            Name = "Edge case",
            Servings = 0,
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 1, Ingredient = Flour(), Quantity = 100, Unit = MeasurementUnit.Gram },
            },
        };

        var facts = NutritionCalculator.PerServing(recipe);

        // Should not divide by zero; Servings clamped to 1
        Assert.That(facts.Calories, Is.EqualTo(364).Within(0.001));
    }

    [Test]
    public void ForRecipe_EmptyIngredients_ReturnsZeroNutrition()
    {
        var recipe = new Recipe { Name = "Empty", Servings = 2 };

        var facts = NutritionCalculator.ForRecipe(recipe);

        Assert.Multiple(() =>
        {
            Assert.That(facts.Calories, Is.EqualTo(0));
            Assert.That(facts.Protein, Is.EqualTo(0));
            Assert.That(facts.IsEstimated, Is.False);
        });
    }

    [Test]
    public void ForRecipe_EachLineWithoutServingWeight_FlagsEstimatedAndSkips()
    {
        // Ingredient base unit is Gram but has no serving weight; recipe uses Each.
        var ingredient = new Ingredient
        {
            Id = 3,
            Name = "Banana",
            BaseUnit = MeasurementUnit.Gram,
            CaloriesPer100 = 89,
            ProteinPer100 = 1.1,
            ServingWeightG = null,
        };
        var recipe = new Recipe
        {
            Name = "Fruit bowl",
            Servings = 1,
            Ingredients =
            {
                new RecipeIngredient { IngredientId = 3, Ingredient = ingredient, Quantity = 2, Unit = MeasurementUnit.Each },
            },
        };

        var facts = NutritionCalculator.ForRecipe(recipe);

        Assert.Multiple(() =>
        {
            Assert.That(facts.Calories, Is.EqualTo(0));
            Assert.That(facts.IsEstimated, Is.True);
        });
    }
}
