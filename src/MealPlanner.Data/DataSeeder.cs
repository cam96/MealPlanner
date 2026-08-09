using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPlanner.Data;

/// <summary>
/// Seeds a representative set of demo data (people, ingredients, prices, recipes, pantry stock and a
/// monthly budget) so a fresh deployment has something to explore. Seeding is idempotent: it only
/// runs when the database has no <see cref="Person"/> rows, and is intended to be gated behind
/// configuration so production installs can start empty.
/// </summary>
public static class DataSeeder
{
    private const string MonthlyBudgetKey = "MonthlyBudget";

    /// <summary>Seeds demo data when the database is empty of people; otherwise does nothing.</summary>
    /// <param name="context">The database context to seed.</param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns><see langword="true"/> when demo data was written; otherwise <see langword="false"/>.</returns>
    public static async Task<bool> SeedDemoDataAsync(
        MealPlannerDbContext context,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        if (await context.People.AnyAsync(cancellationToken))
        {
            return false;
        }

        logger.LogInformation("Seeding demo data (empty database detected).");

        // Ensure a default household exists for demo data.
        var household = await context.Households.FirstOrDefaultAsync(cancellationToken);
        if (household is null)
        {
            var user = new AppUser
            {
                GoogleId = "demo-user",
                Email = "demo@example.com",
                Name = "Demo User",
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };
            context.AppUsers.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            household = new Household
            {
                Name = "Demo Household",
                OwnerId = user.Id,
                CreatedAt = DateTime.UtcNow,
            };
            context.Households.Add(household);
            await context.SaveChangesAsync(cancellationToken);

            user.HouseholdId = household.Id;
            await context.SaveChangesAsync(cancellationToken);
        }

        var householdId = household.Id;

        // Create stores referenced by ingredient prices.
        var store1 = new Store { Name = "Costco", HouseholdId = householdId };
        var store2 = new Store { Name = "Superstore", HouseholdId = householdId };
        var store3 = new Store { Name = "Safeway", HouseholdId = householdId };
        context.Stores.AddRange(store1, store2, store3);
        await context.SaveChangesAsync(cancellationToken);

        context.People.AddRange(
            new Person { Name = "Me", HouseholdId = householdId, DailyCalorieGoal = 2200, DailyProteinGoal = 130, DailyFiberGoal = 35, DailyCarbGoal = 250, DailyFatGoal = 70 },
            new Person { Name = "Michelle", HouseholdId = householdId, DailyCalorieGoal = 1900, DailyProteinGoal = 110, DailyFiberGoal = 30, DailyCarbGoal = 210, DailyFatGoal = 60 });

        var oats = new Ingredient { Name = "Rolled oats", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Carbohydrate, HouseholdId = householdId, CaloriesPer100 = 379, ProteinPer100 = 13, FiberPer100 = 10, CarbsPer100 = 67, FatPer100 = 7 };
        var chicken = new Ingredient { Name = "Chicken breast", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Protein, HouseholdId = householdId, CaloriesPer100 = 165, ProteinPer100 = 31, FiberPer100 = 0, CarbsPer100 = 0, FatPer100 = 4 };
        var rice = new Ingredient { Name = "Brown rice (dry)", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Carbohydrate, HouseholdId = householdId, CaloriesPer100 = 367, ProteinPer100 = 8, FiberPer100 = 4, CarbsPer100 = 76, FatPer100 = 3 };
        var broccoli = new Ingredient { Name = "Broccoli", BaseUnit = MeasurementUnit.Gram, Category = FoodCategory.Vegetable, HouseholdId = householdId, CaloriesPer100 = 34, ProteinPer100 = 3, FiberPer100 = 3, CarbsPer100 = 7, FatPer100 = 0 };
        var egg = new Ingredient { Name = "Egg", BaseUnit = MeasurementUnit.Each, Category = FoodCategory.Protein, HouseholdId = householdId, CaloriesPer100 = 143, ProteinPer100 = 13, FiberPer100 = 0, CarbsPer100 = 1, FatPer100 = 10, ServingWeightG = 50 };
        var milk = new Ingredient { Name = "Milk", BaseUnit = MeasurementUnit.Millilitre, HouseholdId = householdId, CaloriesPer100 = 61, ProteinPer100 = 3, FiberPer100 = 0, CarbsPer100 = 5, FatPer100 = 3 };

        context.Ingredients.AddRange(oats, chicken, rice, broccoli, egg, milk);

        var today = DateOnly.FromDateTime(DateTime.Today);
        context.IngredientPrices.AddRange(
            Price(oats, store1.Id, price: 6.49m, packageQuantity: 1000, MeasurementUnit.Gram, today, householdId),
            Price(chicken, store1.Id, price: 13.99m, packageQuantity: 1000, MeasurementUnit.Gram, today, householdId),
            Price(rice, store2.Id, price: 4.29m, packageQuantity: 900, MeasurementUnit.Gram, today, householdId),
            Price(broccoli, store2.Id, price: 3.49m, packageQuantity: 500, MeasurementUnit.Gram, today, householdId),
            Price(egg, store3.Id, price: 4.99m, packageQuantity: 12, MeasurementUnit.Each, today, householdId),
            Price(milk, store2.Id, price: 2.79m, packageQuantity: 2000, MeasurementUnit.Millilitre, today, householdId));

        context.Recipes.AddRange(
            new Recipe
            {
                Name = "Overnight oats",
                HouseholdId = householdId,
                MealType = MealType.Breakfast,
                PrepMinutes = 5,
                CookMinutes = 0,
                Servings = 1,
                Instructions = "Combine oats and milk; refrigerate overnight.",
                Ingredients =
                {
                    new RecipeIngredient { Ingredient = oats, Quantity = 60, Unit = MeasurementUnit.Gram },
                    new RecipeIngredient { Ingredient = milk, Quantity = 200, Unit = MeasurementUnit.Millilitre },
                },
            },
            new Recipe
            {
                Name = "Chicken, rice & broccoli",
                HouseholdId = householdId,
                MealType = MealType.Dinner,
                PrepMinutes = 15,
                CookMinutes = 25,
                Servings = 2,
                Instructions = "Cook rice; pan-sear chicken; steam broccoli; combine.",
                Ingredients =
                {
                    new RecipeIngredient { Ingredient = chicken, Quantity = 400, Unit = MeasurementUnit.Gram },
                    new RecipeIngredient { Ingredient = rice, Quantity = 200, Unit = MeasurementUnit.Gram },
                    new RecipeIngredient { Ingredient = broccoli, Quantity = 300, Unit = MeasurementUnit.Gram },
                },
            },
            new Recipe
            {
                Name = "Veggie omelette",
                HouseholdId = householdId,
                MealType = MealType.Lunch,
                PrepMinutes = 5,
                CookMinutes = 10,
                Servings = 1,
                Instructions = "Whisk eggs; fold in broccoli; cook in a pan.",
                Ingredients =
                {
                    new RecipeIngredient { Ingredient = egg, Quantity = 3, Unit = MeasurementUnit.Each },
                    new RecipeIngredient { Ingredient = broccoli, Quantity = 80, Unit = MeasurementUnit.Gram },
                },
            });

        context.PantryItems.AddRange(
            new PantryItem { Ingredient = rice, HouseholdId = householdId, QuantityOnHand = 900, Unit = MeasurementUnit.Gram, Location = StorageLocation.Pantry },
            new PantryItem { Ingredient = oats, HouseholdId = householdId, QuantityOnHand = 500, Unit = MeasurementUnit.Gram, Location = StorageLocation.Pantry },
            new PantryItem { Ingredient = chicken, HouseholdId = householdId, QuantityOnHand = 500, Unit = MeasurementUnit.Gram, Location = StorageLocation.Freezer });

        context.MealCombos.Add(new MealCombo
        {
            Name = "Chicken + rice + broccoli",
            HouseholdId = householdId,
            ProteinIngredient = chicken,
            CarbohydrateIngredient = rice,
            VegetableIngredient = broccoli,
        });

        context.AppSettings.Add(new AppSetting { HouseholdId = householdId, Key = MonthlyBudgetKey, Value = "850" });

        await context.SaveChangesAsync(cancellationToken);
        logger.LogInformation("Demo data seeded.");
        return true;
    }

    private static IngredientPrice Price(
        Ingredient ingredient,
        int storeId,
        decimal price,
        double packageQuantity,
        MeasurementUnit packageUnit,
        DateOnly recordedDate,
        int householdId) => new()
        {
            Ingredient = ingredient,
            StoreId = storeId,
            HouseholdId = householdId,
            Price = price,
            PackageQuantity = packageQuantity,
            PackageUnit = packageUnit,
            RecordedDate = recordedDate,
            IsEstimated = false,
            IsPreferredStore = true,
        };
}
