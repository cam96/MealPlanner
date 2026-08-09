using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Tests.Data;

/// <summary>
/// Verifies the core-data schema, seed data, and relationship behaviour by applying the EF Core
/// migration to an in-memory SQLite database.
/// </summary>
[TestFixture]
public class CoreDataSchemaTests
{
    private SqliteConnection _connection = default!;
    private MealPlannerDbContext _context = default!;
    private int _testUserId;

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

        var user = new AppUser { GoogleId = "test", Email = "test@test.com", Name = "Test", CreatedAt = DateTime.UtcNow, LastLoginAt = DateTime.UtcNow };
        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();
        _testUserId = user.Id;
    }

    [TearDown]
    public async Task TearDownAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    public async Task Migration_SeedsDefaultStores()
    {
        var stores = await _context.Stores.OrderBy(s => s.Id).Select(s => s.Name).ToListAsync();

        Assert.That(stores, Is.EqualTo(new[] { "Costco", "Superstore", "Safeway" }));
    }

    [Test]
    public async Task IngredientPrice_RoundTripsUnitAndPrice()
    {
        var ingredient = new Ingredient { Name = "Rolled oats", BaseUnit = MeasurementUnit.Gram };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.IngredientPrices.Add(new IngredientPrice
        {
            IngredientId = ingredient.Id,
            StoreId = 1,
            Price = 9.99m,
            PackageQuantity = 1000,
            PackageUnit = MeasurementUnit.Millilitre,
            RecordedDate = new DateOnly(2026, 1, 15),
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.IngredientPrices.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Price, Is.EqualTo(9.99m));
            Assert.That(saved.PackageUnit, Is.EqualTo(MeasurementUnit.Millilitre));
        });
    }

    [Test]
    public async Task DeletingIngredient_CascadeDeletesItsPrices()
    {
        var ingredient = new Ingredient { Name = "Chicken breast", BaseUnit = MeasurementUnit.Gram };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.IngredientPrices.Add(new IngredientPrice
        {
            IngredientId = ingredient.Id,
            StoreId = 2,
            Price = 12.50m,
            PackageQuantity = 900,
            PackageUnit = MeasurementUnit.Gram,
            RecordedDate = new DateOnly(2026, 2, 1),
        });
        await _context.SaveChangesAsync();

        _context.Ingredients.Remove(ingredient);
        await _context.SaveChangesAsync();

        Assert.That(await _context.IngredientPrices.CountAsync(), Is.Zero);
    }

    [Test]
    public async Task PantryItem_RoundTripsUnitAndLocation()
    {
        var ingredient = new Ingredient { Name = "Frozen peas", BaseUnit = MeasurementUnit.Gram };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.PantryItems.Add(new PantryItem
        {
            AppUserId = _testUserId,
            IngredientId = ingredient.Id,
            QuantityOnHand = 750,
            Unit = MeasurementUnit.Gram,
            Location = StorageLocation.Freezer,
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.PantryItems.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.QuantityOnHand, Is.EqualTo(750));
            Assert.That(saved.Unit, Is.EqualTo(MeasurementUnit.Gram));
            Assert.That(saved.Location, Is.EqualTo(StorageLocation.Freezer));
        });
    }

    [Test]
    public async Task DeletingIngredient_WithPantryItem_IsBlocked()
    {
        var ingredient = new Ingredient { Name = "Canned beans", BaseUnit = MeasurementUnit.Each };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.PantryItems.Add(new PantryItem
        {
            AppUserId = _testUserId,
            IngredientId = ingredient.Id,
            QuantityOnHand = 4,
            Unit = MeasurementUnit.Each,
            Location = StorageLocation.Pantry,
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.Ingredients.SingleAsync(i => i.Id == ingredient.Id);
        _context.Ingredients.Remove(reloaded);

        Assert.ThrowsAsync<DbUpdateException>(async () => await _context.SaveChangesAsync());
    }
}
