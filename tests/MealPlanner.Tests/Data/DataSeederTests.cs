using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MealPlanner.Tests.Data;

/// <summary>
/// Verifies <see cref="DataSeeder"/> seeds representative demo data only into an empty database and
/// is idempotent on subsequent runs.
/// </summary>
[TestFixture]
public class DataSeederTests
{
    private string _dbPath = null!;

    [SetUp]
    public void SetUp() =>
        _dbPath = Path.Combine(Path.GetTempPath(), $"mealplanner-seed-{Guid.NewGuid():N}.db");

    [TearDown]
    public void TearDown()
    {
        // Release pooled SQLite connections so the file is no longer locked before deletion.
        SqliteConnection.ClearAllPools();
        foreach (var file in Directory.GetFiles(Path.GetTempPath(), $"{Path.GetFileNameWithoutExtension(_dbPath)}*"))
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
                // Best-effort cleanup of a throwaway temp database.
            }
        }
    }

    private MealPlannerDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;
        return new MealPlannerDbContext(options);
    }

    [Test]
    public async Task SeedDemoDataAsync_EmptyDatabase_SeedsPeopleAndCatalog()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();

        var seeded = await DataSeeder.SeedDemoDataAsync(context, NullLogger.Instance);

        var peopleCount = await context.People.CountAsync();
        var ingredientCount = await context.Ingredients.CountAsync();
        var recipeCount = await context.Recipes.CountAsync();
        var priceCount = await context.IngredientPrices.CountAsync();
        var pantryCount = await context.PantryItems.CountAsync();
        var hasBudget = await context.AppSettings.AnyAsync(s => s.Key == "MonthlyBudget");

        Assert.That(seeded, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(peopleCount, Is.EqualTo(2));
            Assert.That(ingredientCount, Is.GreaterThan(0));
            Assert.That(recipeCount, Is.GreaterThan(0));
            Assert.That(priceCount, Is.GreaterThan(0));
            Assert.That(pantryCount, Is.GreaterThan(0));
            Assert.That(hasBudget, Is.True);
        });
    }

    [Test]
    public async Task SeedDemoDataAsync_AlreadySeeded_DoesNothing()
    {
        await using var context = CreateContext();
        await context.Database.MigrateAsync();
        await DataSeeder.SeedDemoDataAsync(context, NullLogger.Instance);

        var secondRun = await DataSeeder.SeedDemoDataAsync(context, NullLogger.Instance);

        Assert.That(secondRun, Is.False);
        Assert.That(await context.People.CountAsync(), Is.EqualTo(2));
    }

    [Test]
    public void SeedDemoDataAsync_NullContext_ThrowsArgumentNullException() =>
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            DataSeeder.SeedDemoDataAsync(null!, NullLogger.Instance));

    [Test]
    public async Task SeedDemoDataAsync_NullLogger_ThrowsArgumentNullException()
    {
        await using var context = CreateContext();
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            DataSeeder.SeedDemoDataAsync(context, logger: null!));
    }
}
