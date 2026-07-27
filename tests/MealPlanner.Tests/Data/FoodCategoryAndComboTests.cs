using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Tests.Data;

/// <summary>
/// Verifies the food-category column and meal-combo schema, including reference nullability and the
/// guard that prevents deleting a combo that a planned meal still uses.
/// </summary>
[TestFixture]
public class FoodCategoryAndComboTests
{
    private SqliteConnection _connection = default!;
    private MealPlannerDbContext _context = default!;

    [SetUp]
    public async Task SetUpAsync()
    {
        // A shared open connection keeps the in-memory database alive for the test's lifetime.
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();

        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new MealPlannerDbContext(options);
        await _context.Database.MigrateAsync();
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task Ingredient_RoundTripsFoodCategory()
    {
        var ingredient = new Ingredient
        {
            Name = "Chicken breast",
            BaseUnit = MeasurementUnit.Gram,
            Category = FoodCategory.Protein,
        };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.Ingredients.SingleAsync(i => i.Id == ingredient.Id);

        Assert.That(saved.Category, Is.EqualTo(FoodCategory.Protein));
    }

    [Test]
    public async Task Ingredient_DefaultsToNoneCategory()
    {
        var ingredient = new Ingredient { Name = "Olive oil", BaseUnit = MeasurementUnit.Millilitre };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.Ingredients.SingleAsync(i => i.Id == ingredient.Id);

        Assert.That(saved.Category, Is.EqualTo(FoodCategory.None));
    }

    [Test]
    public async Task MealCombo_RoundTripsIngredientReferences()
    {
        var protein = new Ingredient { Name = "Chicken", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Protein };
        var carb = new Ingredient { Name = "Rice", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Carbohydrate };
        var veg = new Ingredient { Name = "Broccoli", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Vegetable };
        _context.Ingredients.AddRange(protein, carb, veg);
        await _context.SaveChangesAsync();

        _context.MealCombos.Add(new MealCombo
        {
            Name = "Chicken + rice + broccoli",
            ProteinIngredientId = protein.Id,
            CarbohydrateIngredientId = carb.Id,
            VegetableIngredientId = veg.Id,
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.MealCombos
            .Include(c => c.ProteinIngredient)
            .Include(c => c.CarbohydrateIngredient)
            .Include(c => c.VegetableIngredient)
            .SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Name, Is.EqualTo("Chicken + rice + broccoli"));
            Assert.That(saved.ProteinIngredient!.Name, Is.EqualTo("Chicken"));
            Assert.That(saved.CarbohydrateIngredient!.Name, Is.EqualTo("Rice"));
            Assert.That(saved.VegetableIngredient!.Name, Is.EqualTo("Broccoli"));
        });
    }

    [Test]
    public async Task DeletingIngredient_UsedByCombo_SetsReferenceToNull()
    {
        var protein = new Ingredient { Name = "Tofu", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Protein };
        var carb = new Ingredient { Name = "Quinoa", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Carbohydrate };
        _context.Ingredients.AddRange(protein, carb);
        await _context.SaveChangesAsync();

        _context.MealCombos.Add(new MealCombo
        {
            Name = "Tofu + quinoa",
            ProteinIngredientId = protein.Id,
            CarbohydrateIngredientId = carb.Id,
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.Ingredients.SingleAsync(i => i.Id == protein.Id);
        _context.Ingredients.Remove(reloaded);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var combo = await _context.MealCombos.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(combo.ProteinIngredientId, Is.Null);
            Assert.That(combo.CarbohydrateIngredientId, Is.Not.Null);
        });
    }

    [Test]
    public async Task DeletingCombo_UsedByPlannedMeal_IsBlocked()
    {
        var combo = new MealCombo { Name = "Simple dinner" };
        _context.MealCombos.Add(combo);
        await _context.SaveChangesAsync();

        var plan = new MealPlan { Year = 2026, Month = 1 };
        var day = new DayPlan { Date = new DateOnly(2026, 1, 5), DayType = DayType.Normal };
        day.Meals.Add(new PlannedMeal
        {
            Slot = MealType.Dinner,
            Assignee = MealAssignee.Shared,
            MealComboId = combo.Id,
            Servings = 2,
        });
        plan.Days.Add(day);
        _context.MealPlans.Add(plan);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.MealCombos.SingleAsync(c => c.Id == combo.Id);
        _context.MealCombos.Remove(reloaded);

        Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }

    [Test]
    public async Task PlannedMeal_RoundTripsComboReference()
    {
        var combo = new MealCombo { Name = "Leftovers night" };
        _context.MealCombos.Add(combo);
        await _context.SaveChangesAsync();

        var plan = new MealPlan { Year = 2026, Month = 2 };
        var day = new DayPlan { Date = new DateOnly(2026, 2, 3), DayType = DayType.Normal };
        day.Meals.Add(new PlannedMeal
        {
            Slot = MealType.Dinner,
            Assignee = MealAssignee.Shared,
            MealComboId = combo.Id,
            Servings = 1,
        });
        plan.Days.Add(day);
        _context.MealPlans.Add(plan);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var meal = await _context.Set<PlannedMeal>()
            .Include(m => m.MealCombo)
            .SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(meal.RecipeId, Is.Null);
            Assert.That(meal.MealCombo!.Name, Is.EqualTo("Leftovers night"));
        });
    }
}
