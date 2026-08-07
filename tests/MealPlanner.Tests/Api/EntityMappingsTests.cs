using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Combos;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Pantry;
using MealPlanner.Contracts.People;
using MealPlanner.Contracts.Planning;
using MealPlanner.Contracts.Prices;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Contracts.Stores;
using MealPlanner.Domain.Costing;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;
using ContractDayType = MealPlanner.Contracts.DayType;
using ContractFoodCategory = MealPlanner.Contracts.FoodCategory;
using ContractMealAssignee = MealPlanner.Contracts.MealAssignee;
using ContractMealType = MealPlanner.Contracts.MealType;
using ContractStorageLocation = MealPlanner.Contracts.StorageLocation;
using ContractUnit = MealPlanner.Contracts.MeasurementUnit;
using DomainDayType = MealPlanner.Domain.Entities.DayType;
using DomainFoodCategory = MealPlanner.Domain.Entities.FoodCategory;
using DomainMealAssignee = MealPlanner.Domain.Entities.MealAssignee;
using DomainMealType = MealPlanner.Domain.Entities.MealType;
using DomainStorageLocation = MealPlanner.Domain.Entities.StorageLocation;
using DomainUnit = MealPlanner.Domain.Entities.MeasurementUnit;

namespace MealPlanner.Tests.Api;

/// <summary>
/// Verifies <see cref="EntityMappings"/> correctly converts all enum values between domain and
/// contract representations, projects entities to DTOs, and applies save requests to entities.
/// </summary>
[TestFixture]
public class EntityMappingsTests
{
    // == MeasurementUnit ======================================================================

    [TestCase(DomainUnit.Gram, ContractUnit.Gram)]
    [TestCase(DomainUnit.Millilitre, ContractUnit.Millilitre)]
    [TestCase(DomainUnit.Each, ContractUnit.Each)]
    public void MeasurementUnit_ToContract_MapsCorrectly(DomainUnit domain, ContractUnit expected)
    {
        Assert.That(domain.ToContract(), Is.EqualTo(expected));
    }

    [TestCase(ContractUnit.Gram, DomainUnit.Gram)]
    [TestCase(ContractUnit.Millilitre, DomainUnit.Millilitre)]
    [TestCase(ContractUnit.Each, DomainUnit.Each)]
    public void MeasurementUnit_ToDomain_MapsCorrectly(ContractUnit contract, DomainUnit expected)
    {
        Assert.That(contract.ToDomain(), Is.EqualTo(expected));
    }

    [Test]
    public void MeasurementUnit_ToContract_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((DomainUnit)99).ToContract());
    }

    [Test]
    public void MeasurementUnit_ToDomain_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContractUnit)99).ToDomain());
    }

    // == MealType =============================================================================

    [TestCase(DomainMealType.Breakfast, ContractMealType.Breakfast)]
    [TestCase(DomainMealType.Lunch, ContractMealType.Lunch)]
    [TestCase(DomainMealType.Dinner, ContractMealType.Dinner)]
    [TestCase(DomainMealType.Snack, ContractMealType.Snack)]
    public void MealType_ToContract_MapsCorrectly(DomainMealType domain, ContractMealType expected)
    {
        Assert.That(domain.ToContract(), Is.EqualTo(expected));
    }

    [TestCase(ContractMealType.Breakfast, DomainMealType.Breakfast)]
    [TestCase(ContractMealType.Lunch, DomainMealType.Lunch)]
    [TestCase(ContractMealType.Dinner, DomainMealType.Dinner)]
    [TestCase(ContractMealType.Snack, DomainMealType.Snack)]
    public void MealType_ToDomain_MapsCorrectly(ContractMealType contract, DomainMealType expected)
    {
        Assert.That(contract.ToDomain(), Is.EqualTo(expected));
    }

    [Test]
    public void MealType_ToContract_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((DomainMealType)99).ToContract());
    }

    [Test]
    public void MealType_ToDomain_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContractMealType)99).ToDomain());
    }

    // == StorageLocation ======================================================================

    [TestCase(DomainStorageLocation.Pantry, ContractStorageLocation.Pantry)]
    [TestCase(DomainStorageLocation.Fridge, ContractStorageLocation.Fridge)]
    [TestCase(DomainStorageLocation.Freezer, ContractStorageLocation.Freezer)]
    public void StorageLocation_ToContract_MapsCorrectly(DomainStorageLocation domain, ContractStorageLocation expected)
    {
        Assert.That(domain.ToContract(), Is.EqualTo(expected));
    }

    [TestCase(ContractStorageLocation.Pantry, DomainStorageLocation.Pantry)]
    [TestCase(ContractStorageLocation.Fridge, DomainStorageLocation.Fridge)]
    [TestCase(ContractStorageLocation.Freezer, DomainStorageLocation.Freezer)]
    public void StorageLocation_ToDomain_MapsCorrectly(ContractStorageLocation contract, DomainStorageLocation expected)
    {
        Assert.That(contract.ToDomain(), Is.EqualTo(expected));
    }

    [Test]
    public void StorageLocation_ToContract_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((DomainStorageLocation)99).ToContract());
    }

    [Test]
    public void StorageLocation_ToDomain_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContractStorageLocation)99).ToDomain());
    }

    // == DayType ==============================================================================

    [TestCase(DomainDayType.Normal, ContractDayType.Normal)]
    [TestCase(DomainDayType.EatingOut, ContractDayType.EatingOut)]
    [TestCase(DomainDayType.Event, ContractDayType.Event)]
    public void DayType_ToContract_MapsCorrectly(DomainDayType domain, ContractDayType expected)
    {
        Assert.That(domain.ToContract(), Is.EqualTo(expected));
    }

    [TestCase(ContractDayType.Normal, DomainDayType.Normal)]
    [TestCase(ContractDayType.EatingOut, DomainDayType.EatingOut)]
    [TestCase(ContractDayType.Event, DomainDayType.Event)]
    public void DayType_ToDomain_MapsCorrectly(ContractDayType contract, DomainDayType expected)
    {
        Assert.That(contract.ToDomain(), Is.EqualTo(expected));
    }

    [Test]
    public void DayType_ToContract_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((DomainDayType)99).ToContract());
    }

    [Test]
    public void DayType_ToDomain_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContractDayType)99).ToDomain());
    }

    // == MealAssignee =========================================================================

    [TestCase(DomainMealAssignee.FirstPerson, ContractMealAssignee.FirstPerson)]
    [TestCase(DomainMealAssignee.SecondPerson, ContractMealAssignee.SecondPerson)]
    [TestCase(DomainMealAssignee.Shared, ContractMealAssignee.Shared)]
    public void MealAssignee_ToContract_MapsCorrectly(DomainMealAssignee domain, ContractMealAssignee expected)
    {
        Assert.That(domain.ToContract(), Is.EqualTo(expected));
    }

    [TestCase(ContractMealAssignee.FirstPerson, DomainMealAssignee.FirstPerson)]
    [TestCase(ContractMealAssignee.SecondPerson, DomainMealAssignee.SecondPerson)]
    [TestCase(ContractMealAssignee.Shared, DomainMealAssignee.Shared)]
    public void MealAssignee_ToDomain_MapsCorrectly(ContractMealAssignee contract, DomainMealAssignee expected)
    {
        Assert.That(contract.ToDomain(), Is.EqualTo(expected));
    }

    [Test]
    public void MealAssignee_ToContract_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((DomainMealAssignee)99).ToContract());
    }

    [Test]
    public void MealAssignee_ToDomain_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContractMealAssignee)99).ToDomain());
    }

    // == FoodCategory =========================================================================

    [TestCase(DomainFoodCategory.None, ContractFoodCategory.None)]
    [TestCase(DomainFoodCategory.Protein, ContractFoodCategory.Protein)]
    [TestCase(DomainFoodCategory.Carbohydrate, ContractFoodCategory.Carbohydrate)]
    [TestCase(DomainFoodCategory.Vegetable, ContractFoodCategory.Vegetable)]
    public void FoodCategory_ToContract_MapsCorrectly(DomainFoodCategory domain, ContractFoodCategory expected)
    {
        Assert.That(domain.ToContract(), Is.EqualTo(expected));
    }

    [TestCase(ContractFoodCategory.None, DomainFoodCategory.None)]
    [TestCase(ContractFoodCategory.Protein, DomainFoodCategory.Protein)]
    [TestCase(ContractFoodCategory.Carbohydrate, DomainFoodCategory.Carbohydrate)]
    [TestCase(ContractFoodCategory.Vegetable, DomainFoodCategory.Vegetable)]
    public void FoodCategory_ToDomain_MapsCorrectly(ContractFoodCategory contract, DomainFoodCategory expected)
    {
        Assert.That(contract.ToDomain(), Is.EqualTo(expected));
    }

    [Test]
    public void FoodCategory_ToContract_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((DomainFoodCategory)99).ToContract());
    }

    [Test]
    public void FoodCategory_ToDomain_InvalidValue_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ((ContractFoodCategory)99).ToDomain());
    }

    // == Person ToDto / Apply ==================================================================

    [Test]
    public void Person_ToDto_MapsAllFields()
    {
        var person = new Person
        {
            Id = 5,
            Name = "Alex",
            DailyCalorieGoal = 2000,
            DailyProteinGoal = 100,
            DailyFiberGoal = 30,
            DailyCarbGoal = 250,
            DailyFatGoal = 65,
        };

        var dto = person.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(5));
            Assert.That(dto.Name, Is.EqualTo("Alex"));
            Assert.That(dto.DailyCalorieGoal, Is.EqualTo(2000));
            Assert.That(dto.DailyProteinGoal, Is.EqualTo(100));
            Assert.That(dto.DailyFiberGoal, Is.EqualTo(30));
            Assert.That(dto.DailyCarbGoal, Is.EqualTo(250));
            Assert.That(dto.DailyFatGoal, Is.EqualTo(65));
        });
    }

    [Test]
    public void Person_Apply_SetsAllFieldsAndTrimsName()
    {
        var person = new Person { Name = "Old" };
        var request = new SavePersonRequest("  New Name  ", 1800, 90, 25, 200, 55);

        person.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(person.Name, Is.EqualTo("New Name"));
            Assert.That(person.DailyCalorieGoal, Is.EqualTo(1800));
            Assert.That(person.DailyProteinGoal, Is.EqualTo(90));
            Assert.That(person.DailyFiberGoal, Is.EqualTo(25));
            Assert.That(person.DailyCarbGoal, Is.EqualTo(200));
            Assert.That(person.DailyFatGoal, Is.EqualTo(55));
        });
    }

    // == Store ToDto ===========================================================================

    [Test]
    public void Store_ToDto_MapsIdAndName()
    {
        var store = new Store { Id = 3, Name = "Costco" };

        var dto = store.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(3));
            Assert.That(dto.Name, Is.EqualTo("Costco"));
        });
    }

    // == Ingredient ToDto / Apply ==============================================================

    [Test]
    public void Ingredient_ToDto_MapsAllFields()
    {
        var ingredient = new Ingredient
        {
            Id = 7,
            Name = "Chicken breast",
            BaseUnit = DomainUnit.Gram,
            Category = DomainFoodCategory.Protein,
            CaloriesPer100 = 165,
            ProteinPer100 = 31,
            FiberPer100 = 0,
            CarbsPer100 = 0,
            FatPer100 = 3.6,
            IsNutritionEstimated = false,
            CnfFoodCode = 12345,
            ServingWeightG = 120,
        };

        var dto = ingredient.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(7));
            Assert.That(dto.Name, Is.EqualTo("Chicken breast"));
            Assert.That(dto.BaseUnit, Is.EqualTo(ContractUnit.Gram));
            Assert.That(dto.Category, Is.EqualTo(ContractFoodCategory.Protein));
            Assert.That(dto.CaloriesPer100, Is.EqualTo(165));
            Assert.That(dto.ProteinPer100, Is.EqualTo(31));
            Assert.That(dto.FiberPer100, Is.EqualTo(0));
            Assert.That(dto.CarbsPer100, Is.EqualTo(0));
            Assert.That(dto.FatPer100, Is.EqualTo(3.6).Within(0.001));
            Assert.That(dto.IsNutritionEstimated, Is.False);
            Assert.That(dto.CnfFoodCode, Is.EqualTo(12345));
            Assert.That(dto.ServingWeightG, Is.EqualTo(120));
        });
    }

    [Test]
    public void Ingredient_Apply_SetsAllFieldsAndTrimsName()
    {
        var ingredient = new Ingredient { Name = "Old" };
        var request = new SaveIngredientRequest(
            " Oats ", ContractUnit.Gram, ContractFoodCategory.Carbohydrate,
            389, 16.9, 10.6, 66.3, 6.9, true, 4567, 40);

        ingredient.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(ingredient.Name, Is.EqualTo("Oats"));
            Assert.That(ingredient.BaseUnit, Is.EqualTo(DomainUnit.Gram));
            Assert.That(ingredient.Category, Is.EqualTo(DomainFoodCategory.Carbohydrate));
            Assert.That(ingredient.CaloriesPer100, Is.EqualTo(389));
            Assert.That(ingredient.ProteinPer100, Is.EqualTo(16.9).Within(0.001));
            Assert.That(ingredient.FiberPer100, Is.EqualTo(10.6).Within(0.001));
            Assert.That(ingredient.CarbsPer100, Is.EqualTo(66.3).Within(0.001));
            Assert.That(ingredient.FatPer100, Is.EqualTo(6.9).Within(0.001));
            Assert.That(ingredient.IsNutritionEstimated, Is.True);
            Assert.That(ingredient.CnfFoodCode, Is.EqualTo(4567));
            Assert.That(ingredient.ServingWeightG, Is.EqualTo(40));
        });
    }

    // == IngredientPrice ToDto / ToRecentDto / Apply ===========================================

    [Test]
    public void IngredientPrice_ToDto_MapsAllFields()
    {
        var price = new IngredientPrice
        {
            Id = 10,
            IngredientId = 7,
            StoreId = 2,
            Store = new Store { Id = 2, Name = "Superstore" },
            Price = 4.99m,
            PackageQuantity = 500,
            PackageUnit = DomainUnit.Gram,
            RecordedDate = new DateOnly(2026, 5, 10),
            IsEstimated = false,
            IsPreferredStore = true,
        };

        var dto = price.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(10));
            Assert.That(dto.IngredientId, Is.EqualTo(7));
            Assert.That(dto.StoreId, Is.EqualTo(2));
            Assert.That(dto.StoreName, Is.EqualTo("Superstore"));
            Assert.That(dto.Price, Is.EqualTo(4.99m));
            Assert.That(dto.PackageQuantity, Is.EqualTo(500));
            Assert.That(dto.PackageUnit, Is.EqualTo(ContractUnit.Gram));
            Assert.That(dto.RecordedDate, Is.EqualTo(new DateOnly(2026, 5, 10)));
            Assert.That(dto.IsEstimated, Is.False);
            Assert.That(dto.IsPreferredStore, Is.True);
        });
    }

    [Test]
    public void IngredientPrice_ToRecentDto_IncludesIngredientName()
    {
        var price = new IngredientPrice
        {
            Id = 11,
            IngredientId = 7,
            Ingredient = new Ingredient { Id = 7, Name = "Chicken breast" },
            StoreId = 1,
            Store = new Store { Id = 1, Name = "Costco" },
            Price = 12.99m,
            PackageQuantity = 1000,
            PackageUnit = DomainUnit.Gram,
            RecordedDate = new DateOnly(2026, 6, 1),
            IsEstimated = true,
            IsPreferredStore = false,
        };

        var dto = price.ToRecentDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(11));
            Assert.That(dto.IngredientName, Is.EqualTo("Chicken breast"));
            Assert.That(dto.StoreName, Is.EqualTo("Costco"));
            Assert.That(dto.IsEstimated, Is.True);
        });
    }

    [Test]
    public void IngredientPrice_ToDto_NullStore_UsesEmptyString()
    {
        var price = new IngredientPrice
        {
            Id = 12,
            IngredientId = 7,
            StoreId = 1,
            Store = null,
            Price = 5m,
            PackageQuantity = 100,
            PackageUnit = DomainUnit.Gram,
            RecordedDate = new DateOnly(2026, 1, 1),
        };

        Assert.That(price.ToDto().StoreName, Is.EqualTo(string.Empty));
    }

    [Test]
    public void IngredientPrice_Apply_SetsAllFields()
    {
        var price = new IngredientPrice { IngredientId = 1 };
        var request = new SaveIngredientPriceRequest(3, 9.99m, 2000, ContractUnit.Gram, new DateOnly(2026, 7, 1), true, false);

        price.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(price.StoreId, Is.EqualTo(3));
            Assert.That(price.Price, Is.EqualTo(9.99m));
            Assert.That(price.PackageQuantity, Is.EqualTo(2000));
            Assert.That(price.PackageUnit, Is.EqualTo(DomainUnit.Gram));
            Assert.That(price.RecordedDate, Is.EqualTo(new DateOnly(2026, 7, 1)));
            Assert.That(price.IsEstimated, Is.True);
            Assert.That(price.IsPreferredStore, Is.False);
        });
    }

    // == Recipe ToDto / ToSummaryDto / Apply ====================================================

    [Test]
    public void Recipe_ToDto_MapsAllFields()
    {
        var recipe = new Recipe
        {
            Id = 1,
            Name = "Omelette",
            MealType = DomainMealType.Breakfast,
            PrepMinutes = 5,
            CookMinutes = 10,
            Servings = 2,
            Instructions = "Beat eggs, cook.",
            Ingredients =
            {
                new RecipeIngredient { Id = 1, IngredientId = 2, Ingredient = new Ingredient { Id = 2, Name = "Egg" }, Quantity = 3, Unit = DomainUnit.Each },
            },
        };
        var nutrition = new NutritionFacts(200, 18, 0, 2, 14, false);
        var cost = new RecipeCost(3.50m, 1.75m, false);

        var dto = recipe.ToDto(nutrition, cost);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(1));
            Assert.That(dto.Name, Is.EqualTo("Omelette"));
            Assert.That(dto.MealType, Is.EqualTo(ContractMealType.Breakfast));
            Assert.That(dto.PrepMinutes, Is.EqualTo(5));
            Assert.That(dto.CookMinutes, Is.EqualTo(10));
            Assert.That(dto.Servings, Is.EqualTo(2));
            Assert.That(dto.Instructions, Is.EqualTo("Beat eggs, cook."));
            Assert.That(dto.Ingredients, Has.Count.EqualTo(1));
            Assert.That(dto.Ingredients[0].IngredientName, Is.EqualTo("Egg"));
            Assert.That(dto.CaloriesPerServing, Is.EqualTo(200));
            Assert.That(dto.ProteinPerServing, Is.EqualTo(18));
            Assert.That(dto.CostPerServing, Is.EqualTo(1.75m));
            Assert.That(dto.TotalCost, Is.EqualTo(3.50m));
            Assert.That(dto.NutritionIsEstimated, Is.False);
            Assert.That(dto.CostIsEstimated, Is.False);
        });
    }

    [Test]
    public void Recipe_ToSummaryDto_MapsCorrectly()
    {
        var recipe = new Recipe
        {
            Id = 2,
            Name = "Stir fry",
            MealType = DomainMealType.Dinner,
            PrepMinutes = 15,
            CookMinutes = 20,
            Servings = 4,
        };
        var nutrition = new NutritionFacts(350, 25, 5, 40, 12, true);
        var cost = new RecipeCost(12m, 3m, true);

        var dto = recipe.ToSummaryDto(nutrition, cost);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(2));
            Assert.That(dto.Name, Is.EqualTo("Stir fry"));
            Assert.That(dto.MealType, Is.EqualTo(ContractMealType.Dinner));
            Assert.That(dto.CaloriesPerServing, Is.EqualTo(350));
            Assert.That(dto.CostPerServing, Is.EqualTo(3m));
            Assert.That(dto.NutritionIsEstimated, Is.True);
            Assert.That(dto.CostIsEstimated, Is.True);
        });
    }

    [Test]
    public void Recipe_Apply_SetsFieldsAndReplacesIngredients()
    {
        var recipe = new Recipe
        {
            Name = "Old",
            Ingredients = { new RecipeIngredient { IngredientId = 1, Quantity = 100, Unit = DomainUnit.Gram } },
        };
        var request = new SaveRecipeRequest(
            "  New Recipe  ",
            ContractMealType.Lunch,
            10, 25, 3,
            "  Step 1. Do it.  ",
            [new SaveRecipeIngredientRequest(5, 200, ContractUnit.Millilitre)]);

        recipe.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(recipe.Name, Is.EqualTo("New Recipe"));
            Assert.That(recipe.MealType, Is.EqualTo(DomainMealType.Lunch));
            Assert.That(recipe.PrepMinutes, Is.EqualTo(10));
            Assert.That(recipe.CookMinutes, Is.EqualTo(25));
            Assert.That(recipe.Servings, Is.EqualTo(3));
            Assert.That(recipe.Instructions, Is.EqualTo("Step 1. Do it."));
            Assert.That(recipe.Ingredients, Has.Count.EqualTo(1));
            Assert.That(recipe.Ingredients.First().IngredientId, Is.EqualTo(5));
            Assert.That(recipe.Ingredients.First().Quantity, Is.EqualTo(200));
            Assert.That(recipe.Ingredients.First().Unit, Is.EqualTo(DomainUnit.Millilitre));
        });
    }

    [Test]
    public void Recipe_Apply_ServingsOfZeroOrLess_ClampsToOne()
    {
        var recipe = new Recipe { Name = "Test" };
        var request = new SaveRecipeRequest("Test", ContractMealType.Dinner, 0, 0, 0, null, []);

        recipe.Apply(request);

        Assert.That(recipe.Servings, Is.EqualTo(1));
    }

    [Test]
    public void Recipe_Apply_WhitespaceInstructions_SetsNull()
    {
        var recipe = new Recipe { Name = "Test", Instructions = "old" };
        var request = new SaveRecipeRequest("Test", ContractMealType.Dinner, 0, 0, 1, "   ", []);

        recipe.Apply(request);

        Assert.That(recipe.Instructions, Is.Null);
    }

    // == RecipeIngredient ToDto ================================================================

    [Test]
    public void RecipeIngredient_ToDto_MapsAllFields()
    {
        var line = new RecipeIngredient
        {
            Id = 3,
            IngredientId = 5,
            Ingredient = new Ingredient { Id = 5, Name = "Rice" },
            Quantity = 200,
            Unit = DomainUnit.Gram,
        };

        var dto = line.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(3));
            Assert.That(dto.IngredientId, Is.EqualTo(5));
            Assert.That(dto.IngredientName, Is.EqualTo("Rice"));
            Assert.That(dto.Quantity, Is.EqualTo(200));
            Assert.That(dto.Unit, Is.EqualTo(ContractUnit.Gram));
        });
    }

    [Test]
    public void RecipeIngredient_ToDto_NullIngredient_UsesEmptyString()
    {
        var line = new RecipeIngredient { Id = 1, IngredientId = 99, Ingredient = null, Quantity = 50, Unit = DomainUnit.Each };

        Assert.That(line.ToDto().IngredientName, Is.EqualTo(string.Empty));
    }

    // == PantryItem ToDto / Apply ==============================================================

    [Test]
    public void PantryItem_ToDto_MapsAllFields()
    {
        var item = new PantryItem
        {
            Id = 4,
            IngredientId = 3,
            Ingredient = new Ingredient { Id = 3, Name = "Broccoli" },
            QuantityOnHand = 500,
            Unit = DomainUnit.Gram,
            Location = DomainStorageLocation.Freezer,
        };

        var dto = item.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(4));
            Assert.That(dto.IngredientId, Is.EqualTo(3));
            Assert.That(dto.IngredientName, Is.EqualTo("Broccoli"));
            Assert.That(dto.QuantityOnHand, Is.EqualTo(500));
            Assert.That(dto.Unit, Is.EqualTo(ContractUnit.Gram));
            Assert.That(dto.Location, Is.EqualTo(ContractStorageLocation.Freezer));
        });
    }

    [Test]
    public void PantryItem_Apply_SetsAllFields()
    {
        var item = new PantryItem { IngredientId = 1, QuantityOnHand = 100, Unit = DomainUnit.Gram, Location = DomainStorageLocation.Pantry };
        var request = new SavePantryItemRequest(5, 750, ContractUnit.Millilitre, ContractStorageLocation.Fridge);

        item.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(item.IngredientId, Is.EqualTo(5));
            Assert.That(item.QuantityOnHand, Is.EqualTo(750));
            Assert.That(item.Unit, Is.EqualTo(DomainUnit.Millilitre));
            Assert.That(item.Location, Is.EqualTo(DomainStorageLocation.Fridge));
        });
    }

    // == PlannedMeal ToDto / Apply =============================================================

    [Test]
    public void PlannedMeal_ToDto_MapsAllFields()
    {
        var meal = new PlannedMeal
        {
            Id = 8,
            Slot = DomainMealType.Dinner,
            Assignee = DomainMealAssignee.Shared,
            RecipeId = 1,
            Recipe = new Recipe { Id = 1, Name = "Pasta" },
            MealComboId = null,
            MealCombo = null,
            Servings = 3,
        };

        var dto = meal.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(8));
            Assert.That(dto.Slot, Is.EqualTo(ContractMealType.Dinner));
            Assert.That(dto.Assignee, Is.EqualTo(ContractMealAssignee.Shared));
            Assert.That(dto.RecipeId, Is.EqualTo(1));
            Assert.That(dto.RecipeName, Is.EqualTo("Pasta"));
            Assert.That(dto.MealComboId, Is.Null);
            Assert.That(dto.MealComboName, Is.Null);
            Assert.That(dto.Servings, Is.EqualTo(3));
        });
    }

    [Test]
    public void PlannedMeal_Apply_SetsAllFields()
    {
        var meal = new PlannedMeal();
        var request = new SavePlannedMealRequest(ContractMealType.Lunch, ContractMealAssignee.FirstPerson, 5, null, 2);

        meal.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(meal.Slot, Is.EqualTo(DomainMealType.Lunch));
            Assert.That(meal.Assignee, Is.EqualTo(DomainMealAssignee.FirstPerson));
            Assert.That(meal.RecipeId, Is.EqualTo(5));
            Assert.That(meal.MealComboId, Is.Null);
            Assert.That(meal.Servings, Is.EqualTo(2));
        });
    }

    [Test]
    public void PlannedMeal_Apply_ServingsOfZeroOrLess_ClampsToOne()
    {
        var meal = new PlannedMeal();
        var request = new SavePlannedMealRequest(ContractMealType.Dinner, ContractMealAssignee.Shared, null, 1, 0);

        meal.Apply(request);

        Assert.That(meal.Servings, Is.EqualTo(1));
    }

    // == MealCombo ToDto / Apply ===============================================================

    [Test]
    public void MealCombo_ToDto_MapsAllFields()
    {
        var combo = new MealCombo
        {
            Id = 2,
            Name = "Chicken rice bowl",
            ProteinIngredientId = 10,
            ProteinIngredient = new Ingredient { Id = 10, Name = "Chicken" },
            CarbohydrateIngredientId = 11,
            CarbohydrateIngredient = new Ingredient { Id = 11, Name = "Rice" },
            VegetableIngredientId = 12,
            VegetableIngredient = new Ingredient { Id = 12, Name = "Broccoli" },
        };

        var dto = combo.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(2));
            Assert.That(dto.Name, Is.EqualTo("Chicken rice bowl"));
            Assert.That(dto.ProteinIngredientId, Is.EqualTo(10));
            Assert.That(dto.ProteinIngredientName, Is.EqualTo("Chicken"));
            Assert.That(dto.CarbohydrateIngredientId, Is.EqualTo(11));
            Assert.That(dto.CarbohydrateIngredientName, Is.EqualTo("Rice"));
            Assert.That(dto.VegetableIngredientId, Is.EqualTo(12));
            Assert.That(dto.VegetableIngredientName, Is.EqualTo("Broccoli"));
        });
    }

    [Test]
    public void MealCombo_ToDto_NullIngredients_MapsToNullNames()
    {
        var combo = new MealCombo { Id = 3, Name = "Partial" };

        var dto = combo.ToDto();

        Assert.Multiple(() =>
        {
            Assert.That(dto.ProteinIngredientId, Is.Null);
            Assert.That(dto.ProteinIngredientName, Is.Null);
            Assert.That(dto.CarbohydrateIngredientId, Is.Null);
            Assert.That(dto.VegetableIngredientId, Is.Null);
        });
    }

    [Test]
    public void MealCombo_Apply_SetsAllFields()
    {
        var combo = new MealCombo { Name = "Old" };
        var request = new SaveMealComboRequest("  New Combo  ", 1, 2, 3);

        combo.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(combo.Name, Is.EqualTo("New Combo"));
            Assert.That(combo.ProteinIngredientId, Is.EqualTo(1));
            Assert.That(combo.CarbohydrateIngredientId, Is.EqualTo(2));
            Assert.That(combo.VegetableIngredientId, Is.EqualTo(3));
        });
    }

    // == DayPlan ToDto =========================================================================

    [Test]
    public void DayPlan_ToDto_MapsAllFieldsWithPrepMinutes()
    {
        var day = new DayPlan
        {
            Id = 15,
            Date = new DateOnly(2026, 3, 5),
            DayType = DomainDayType.Normal,
            Note = "Busy day",
            Meals =
            {
                new PlannedMeal { Id = 1, Slot = DomainMealType.Dinner, Assignee = DomainMealAssignee.Shared, Servings = 2 },
            },
        };

        var dto = day.ToDto(prepMinutes: 45);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(15));
            Assert.That(dto.Date, Is.EqualTo(new DateOnly(2026, 3, 5)));
            Assert.That(dto.DayType, Is.EqualTo(ContractDayType.Normal));
            Assert.That(dto.Note, Is.EqualTo("Busy day"));
            Assert.That(dto.PrepMinutes, Is.EqualTo(45));
            Assert.That(dto.Meals, Has.Count.EqualTo(1));
        });
    }

    // == ManualShoppingItem ToDto / Apply ======================================================

    [Test]
    public void ManualShoppingItem_ToDto_MapsAllFields()
    {
        var item = new ManualShoppingItem
        {
            Id = 20,
            Name = "Paper towels",
            IngredientId = null,
            Quantity = 2,
            Unit = DomainUnit.Each,
            IsInCart = true,
        };

        var dto = item.ToDto(estimatedCost: 5.99m, isCostEstimated: false);

        Assert.Multiple(() =>
        {
            Assert.That(dto.Id, Is.EqualTo(20));
            Assert.That(dto.Name, Is.EqualTo("Paper towels"));
            Assert.That(dto.IngredientId, Is.Null);
            Assert.That(dto.Quantity, Is.EqualTo(2));
            Assert.That(dto.Unit, Is.EqualTo(ContractUnit.Each));
            Assert.That(dto.IsInCart, Is.True);
            Assert.That(dto.EstimatedCost, Is.EqualTo(5.99m));
            Assert.That(dto.IsCostEstimated, Is.False);
        });
    }

    [Test]
    public void ManualShoppingItem_Apply_SetsAllFields()
    {
        var item = new ManualShoppingItem { Year = 2026, Month = 8, CreatedAt = DateTime.UtcNow };
        var request = new AddManualShoppingItemRequest("  Dish soap  ", null, 1, ContractUnit.Each);

        item.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(item.Name, Is.EqualTo("Dish soap"));
            Assert.That(item.IngredientId, Is.Null);
            Assert.That(item.Quantity, Is.EqualTo(1));
            Assert.That(item.Unit, Is.EqualTo(DomainUnit.Each));
        });
    }

    [Test]
    public void ManualShoppingItem_Apply_NullUnit_SetsNull()
    {
        var item = new ManualShoppingItem { Year = 2026, Month = 8, CreatedAt = DateTime.UtcNow };
        var request = new AddManualShoppingItemRequest("Misc", null, null, null);

        item.Apply(request);

        Assert.Multiple(() =>
        {
            Assert.That(item.Quantity, Is.Null);
            Assert.That(item.Unit, Is.Null);
        });
    }
}
