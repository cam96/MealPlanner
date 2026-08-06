using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Tests.Data;

/// <summary>
/// Verifies the schema, relationships, and persistence behaviour of the shopping-cart entities
/// (<see cref="ManualShoppingItem"/> and <see cref="GeneratedItemCartEntry"/>).
/// </summary>
[TestFixture]
public class ShoppingCartSchemaTests
{
    private SqliteConnection _connection = default!;
    private MealPlannerDbContext _context = default!;

    [SetUp]
    public async Task SetUpAsync()
    {
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
    public async Task ManualShoppingItem_RoundTripsAllFields()
    {
        _context.ManualShoppingItems.Add(new ManualShoppingItem
        {
            Year = 2026,
            Month = 8,
            Name = "Paper towels",
            Quantity = 2,
            Unit = MeasurementUnit.Each,
            IsInCart = false,
            CreatedAt = new DateTime(2026, 8, 1, 12, 0, 0, DateTimeKind.Utc),
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.ManualShoppingItems.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Year, Is.EqualTo(2026));
            Assert.That(saved.Month, Is.EqualTo(8));
            Assert.That(saved.Name, Is.EqualTo("Paper towels"));
            Assert.That(saved.Quantity, Is.EqualTo(2));
            Assert.That(saved.Unit, Is.EqualTo(MeasurementUnit.Each));
            Assert.That(saved.IsInCart, Is.False);
        });
    }

    [Test]
    public async Task ManualShoppingItem_NullableQuantityAndUnit_PersistAsNull()
    {
        _context.ManualShoppingItems.Add(new ManualShoppingItem
        {
            Year = 2026,
            Month = 8,
            Name = "Dish soap",
            Quantity = null,
            Unit = null,
            CreatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.ManualShoppingItems.SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Quantity, Is.Null);
            Assert.That(saved.Unit, Is.Null);
        });
    }

    [Test]
    public async Task ManualShoppingItem_IsInCart_CanBeToggled()
    {
        _context.ManualShoppingItems.Add(new ManualShoppingItem
        {
            Year = 2026,
            Month = 8,
            Name = "Sponges",
            CreatedAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        var item = await _context.ManualShoppingItems.SingleAsync();
        Assert.That(item.IsInCart, Is.False);

        item.IsInCart = true;
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var updated = await _context.ManualShoppingItems.SingleAsync();
        Assert.That(updated.IsInCart, Is.True);
    }

    [Test]
    public async Task GeneratedItemCartEntry_RoundTripsAllFields()
    {
        var ingredient = new Ingredient { Name = "Chicken breast", BaseUnit = MeasurementUnit.Gram };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.GeneratedItemCartEntries.Add(new GeneratedItemCartEntry
        {
            Year = 2026,
            Month = 8,
            IngredientId = ingredient.Id,
            AddedToCartAt = new DateTime(2026, 8, 5, 14, 30, 0, DateTimeKind.Utc),
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var saved = await _context.GeneratedItemCartEntries
            .Include(e => e.Ingredient)
            .SingleAsync();

        Assert.Multiple(() =>
        {
            Assert.That(saved.Year, Is.EqualTo(2026));
            Assert.That(saved.Month, Is.EqualTo(8));
            Assert.That(saved.IngredientId, Is.EqualTo(ingredient.Id));
            Assert.That(saved.Ingredient, Is.Not.Null);
            Assert.That(saved.Ingredient!.Name, Is.EqualTo("Chicken breast"));
        });
    }

    [Test]
    public async Task GeneratedItemCartEntry_UniqueIndex_PreventsDoubleCart()
    {
        var ingredient = new Ingredient { Name = "Rice", BaseUnit = MeasurementUnit.Gram };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.GeneratedItemCartEntries.Add(new GeneratedItemCartEntry
        {
            Year = 2026,
            Month = 8,
            IngredientId = ingredient.Id,
            AddedToCartAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        _context.GeneratedItemCartEntries.Add(new GeneratedItemCartEntry
        {
            Year = 2026,
            Month = 8,
            IngredientId = ingredient.Id,
            AddedToCartAt = DateTime.UtcNow,
        });

        Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }

    [Test]
    public async Task GeneratedItemCartEntry_SameIngredient_DifferentMonth_Allowed()
    {
        var ingredient = new Ingredient { Name = "Pasta", BaseUnit = MeasurementUnit.Gram };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.GeneratedItemCartEntries.Add(new GeneratedItemCartEntry
        {
            Year = 2026,
            Month = 8,
            IngredientId = ingredient.Id,
            AddedToCartAt = DateTime.UtcNow,
        });
        _context.GeneratedItemCartEntries.Add(new GeneratedItemCartEntry
        {
            Year = 2026,
            Month = 9,
            IngredientId = ingredient.Id,
            AddedToCartAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();

        Assert.That(await _context.GeneratedItemCartEntries.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public async Task DeletingIngredient_WithCartEntry_IsBlocked()
    {
        var ingredient = new Ingredient { Name = "Broccoli", BaseUnit = MeasurementUnit.Gram };
        _context.Ingredients.Add(ingredient);
        await _context.SaveChangesAsync();

        _context.GeneratedItemCartEntries.Add(new GeneratedItemCartEntry
        {
            Year = 2026,
            Month = 8,
            IngredientId = ingredient.Id,
            AddedToCartAt = DateTime.UtcNow,
        });
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var reloaded = await _context.Ingredients.SingleAsync(i => i.Id == ingredient.Id);
        _context.Ingredients.Remove(reloaded);

        Assert.ThrowsAsync<DbUpdateException>(() => _context.SaveChangesAsync());
    }
}
