using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Data;

/// <summary>
/// Entity Framework Core database context for the MealPlanner application. This is the single
/// component that reads from and writes to the SQLite database. Entity sets and their
/// configurations are added per feature phase; configurations are discovered automatically from
/// this assembly via <see cref="ModelBuilder.ApplyConfigurationsFromAssembly"/>.
/// </summary>
/// <param name="options">The options used to configure the context (provider, connection string).</param>
public class MealPlannerDbContext(DbContextOptions<MealPlannerDbContext> options) : DbContext(options)
{
    /// <summary>Gets the household members and their nutrition goals.</summary>
    public DbSet<Person> People => Set<Person>();

    /// <summary>Gets the grocery stores.</summary>
    public DbSet<Store> Stores => Set<Store>();

    /// <summary>Gets the ingredients and their nutrition values.</summary>
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    /// <summary>Gets the recorded ingredient prices across stores over time.</summary>
    public DbSet<IngredientPrice> IngredientPrices => Set<IngredientPrice>();

    /// <summary>Gets the recipes.</summary>
    public DbSet<Recipe> Recipes => Set<Recipe>();

    /// <summary>Gets the recipe ingredient lines.</summary>
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    /// <summary>Gets the informal meal combos (protein + carbohydrate + vegetable pairings).</summary>
    public DbSet<MealCombo> MealCombos => Set<MealCombo>();

    /// <summary>Gets the pantry and freezer inventory items.</summary>
    public DbSet<PantryItem> PantryItems => Set<PantryItem>();

    /// <summary>Gets the monthly meal plans.</summary>
    public DbSet<MealPlan> MealPlans => Set<MealPlan>();

    /// <summary>Gets the day plans within meal plans.</summary>
    public DbSet<DayPlan> DayPlans => Set<DayPlan>();

    /// <summary>Gets the planned meals within day plans.</summary>
    public DbSet<PlannedMeal> PlannedMeals => Set<PlannedMeal>();

    /// <summary>Gets the application settings (key/value pairs).</summary>
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();

    /// <summary>Gets the manually added shopping list items.</summary>
    public DbSet<ManualShoppingItem> ManualShoppingItems => Set<ManualShoppingItem>();

    /// <summary>Gets the cart entries for generated (meal-plan-derived) shopping list items.</summary>
    public DbSet<GeneratedItemCartEntry> GeneratedItemCartEntries => Set<GeneratedItemCartEntry>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealPlannerDbContext).Assembly);
    }
}
