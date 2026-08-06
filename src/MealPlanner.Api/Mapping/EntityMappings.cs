using MealPlanner.Contracts.Combos;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Pantry;
using MealPlanner.Contracts.People;
using MealPlanner.Contracts.Planning;
using MealPlanner.Contracts.Prices;
using MealPlanner.Contracts.Recipes;
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

namespace MealPlanner.Api.Mapping;

/// <summary>
/// Maps between domain entities and the DTOs exchanged over HTTP. Keeping mapping in the API
/// composition root lets the domain and contracts assemblies stay independent of each other.
/// </summary>
internal static class EntityMappings
{
    /// <summary>Converts a domain measurement unit to its wire representation.</summary>
    public static ContractUnit ToContract(this DomainUnit unit) => unit switch
    {
        DomainUnit.Gram => ContractUnit.Gram,
        DomainUnit.Millilitre => ContractUnit.Millilitre,
        DomainUnit.Each => ContractUnit.Each,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown measurement unit."),
    };

    /// <summary>Converts a wire measurement unit to its domain representation.</summary>
    public static DomainUnit ToDomain(this ContractUnit unit) => unit switch
    {
        ContractUnit.Gram => DomainUnit.Gram,
        ContractUnit.Millilitre => DomainUnit.Millilitre,
        ContractUnit.Each => DomainUnit.Each,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown measurement unit."),
    };

    /// <summary>Converts a domain meal type to its wire representation.</summary>
    public static ContractMealType ToContract(this DomainMealType mealType) => mealType switch
    {
        DomainMealType.Breakfast => ContractMealType.Breakfast,
        DomainMealType.Lunch => ContractMealType.Lunch,
        DomainMealType.Dinner => ContractMealType.Dinner,
        DomainMealType.Snack => ContractMealType.Snack,
        _ => throw new ArgumentOutOfRangeException(nameof(mealType), mealType, "Unknown meal type."),
    };

    /// <summary>Converts a wire meal type to its domain representation.</summary>
    public static DomainMealType ToDomain(this ContractMealType mealType) => mealType switch
    {
        ContractMealType.Breakfast => DomainMealType.Breakfast,
        ContractMealType.Lunch => DomainMealType.Lunch,
        ContractMealType.Dinner => DomainMealType.Dinner,
        ContractMealType.Snack => DomainMealType.Snack,
        _ => throw new ArgumentOutOfRangeException(nameof(mealType), mealType, "Unknown meal type."),
    };

    /// <summary>Converts a domain storage location to its wire representation.</summary>
    public static ContractStorageLocation ToContract(this DomainStorageLocation location) => location switch
    {
        DomainStorageLocation.Pantry => ContractStorageLocation.Pantry,
        DomainStorageLocation.Fridge => ContractStorageLocation.Fridge,
        DomainStorageLocation.Freezer => ContractStorageLocation.Freezer,
        _ => throw new ArgumentOutOfRangeException(nameof(location), location, "Unknown storage location."),
    };

    /// <summary>Converts a wire storage location to its domain representation.</summary>
    public static DomainStorageLocation ToDomain(this ContractStorageLocation location) => location switch
    {
        ContractStorageLocation.Pantry => DomainStorageLocation.Pantry,
        ContractStorageLocation.Fridge => DomainStorageLocation.Fridge,
        ContractStorageLocation.Freezer => DomainStorageLocation.Freezer,
        _ => throw new ArgumentOutOfRangeException(nameof(location), location, "Unknown storage location."),
    };

    /// <summary>Converts a domain day type to its wire representation.</summary>
    public static ContractDayType ToContract(this DomainDayType dayType) => dayType switch
    {
        DomainDayType.Normal => ContractDayType.Normal,
        DomainDayType.EatingOut => ContractDayType.EatingOut,
        DomainDayType.Event => ContractDayType.Event,
        _ => throw new ArgumentOutOfRangeException(nameof(dayType), dayType, "Unknown day type."),
    };

    /// <summary>Converts a wire day type to its domain representation.</summary>
    public static DomainDayType ToDomain(this ContractDayType dayType) => dayType switch
    {
        ContractDayType.Normal => DomainDayType.Normal,
        ContractDayType.EatingOut => DomainDayType.EatingOut,
        ContractDayType.Event => DomainDayType.Event,
        _ => throw new ArgumentOutOfRangeException(nameof(dayType), dayType, "Unknown day type."),
    };

    /// <summary>Converts a domain meal assignee to its wire representation.</summary>
    public static ContractMealAssignee ToContract(this DomainMealAssignee assignee) => assignee switch
    {
        DomainMealAssignee.FirstPerson => ContractMealAssignee.FirstPerson,
        DomainMealAssignee.SecondPerson => ContractMealAssignee.SecondPerson,
        DomainMealAssignee.Shared => ContractMealAssignee.Shared,
        _ => throw new ArgumentOutOfRangeException(nameof(assignee), assignee, "Unknown meal assignee."),
    };

    /// <summary>Converts a wire meal assignee to its domain representation.</summary>
    public static DomainMealAssignee ToDomain(this ContractMealAssignee assignee) => assignee switch
    {
        ContractMealAssignee.FirstPerson => DomainMealAssignee.FirstPerson,
        ContractMealAssignee.SecondPerson => DomainMealAssignee.SecondPerson,
        ContractMealAssignee.Shared => DomainMealAssignee.Shared,
        _ => throw new ArgumentOutOfRangeException(nameof(assignee), assignee, "Unknown meal assignee."),
    };

    /// <summary>Converts a domain food category to its wire representation.</summary>
    public static ContractFoodCategory ToContract(this DomainFoodCategory category) => category switch
    {
        DomainFoodCategory.None => ContractFoodCategory.None,
        DomainFoodCategory.Protein => ContractFoodCategory.Protein,
        DomainFoodCategory.Carbohydrate => ContractFoodCategory.Carbohydrate,
        DomainFoodCategory.Vegetable => ContractFoodCategory.Vegetable,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown food category."),
    };

    /// <summary>Converts a wire food category to its domain representation.</summary>
    public static DomainFoodCategory ToDomain(this ContractFoodCategory category) => category switch
    {
        ContractFoodCategory.None => DomainFoodCategory.None,
        ContractFoodCategory.Protein => DomainFoodCategory.Protein,
        ContractFoodCategory.Carbohydrate => DomainFoodCategory.Carbohydrate,
        ContractFoodCategory.Vegetable => DomainFoodCategory.Vegetable,
        _ => throw new ArgumentOutOfRangeException(nameof(category), category, "Unknown food category."),
    };

    // -- Person ---------------------------------------------------------------------------------

    /// <summary>Projects a <see cref="Person"/> to a <see cref="PersonDto"/>.</summary>
    public static PersonDto ToDto(this Person person) => new(
        person.Id,
        person.Name,
        person.DailyCalorieGoal,
        person.DailyProteinGoal,
        person.DailyFiberGoal,
        person.DailyCarbGoal,
        person.DailyFatGoal);

    /// <summary>Applies a save request onto a <see cref="Person"/> entity.</summary>
    public static void Apply(this Person person, SavePersonRequest request)
    {
        person.Name = request.Name.Trim();
        person.DailyCalorieGoal = request.DailyCalorieGoal;
        person.DailyProteinGoal = request.DailyProteinGoal;
        person.DailyFiberGoal = request.DailyFiberGoal;
        person.DailyCarbGoal = request.DailyCarbGoal;
        person.DailyFatGoal = request.DailyFatGoal;
    }

    // -- Store ----------------------------------------------------------------------------------

    /// <summary>Projects a <see cref="Store"/> to a <see cref="StoreDto"/>.</summary>
    public static StoreDto ToDto(this Store store) => new(store.Id, store.Name);

    // -- Ingredient -----------------------------------------------------------------------------

    /// <summary>Projects an <see cref="Ingredient"/> to an <see cref="IngredientDto"/>.</summary>
    public static IngredientDto ToDto(this Ingredient ingredient) => new(
        ingredient.Id,
        ingredient.Name,
        ingredient.BaseUnit.ToContract(),
        ingredient.Category.ToContract(),
        ingredient.CaloriesPer100,
        ingredient.ProteinPer100,
        ingredient.FiberPer100,
        ingredient.CarbsPer100,
        ingredient.FatPer100,
        ingredient.IsNutritionEstimated,
        ingredient.CnfFoodCode,
        ingredient.ServingWeightG);

    /// <summary>Applies a save request onto an <see cref="Ingredient"/> entity.</summary>
    public static void Apply(this Ingredient ingredient, SaveIngredientRequest request)
    {
        ingredient.Name = request.Name.Trim();
        ingredient.BaseUnit = request.BaseUnit.ToDomain();
        ingredient.Category = request.Category.ToDomain();
        ingredient.CaloriesPer100 = request.CaloriesPer100;
        ingredient.ProteinPer100 = request.ProteinPer100;
        ingredient.FiberPer100 = request.FiberPer100;
        ingredient.CarbsPer100 = request.CarbsPer100;
        ingredient.FatPer100 = request.FatPer100;
        ingredient.IsNutritionEstimated = request.IsNutritionEstimated;
        ingredient.CnfFoodCode = request.CnfFoodCode;
        ingredient.ServingWeightG = request.ServingWeightG;
    }

    // -- IngredientPrice ------------------------------------------------------------------------

    /// <summary>Projects an <see cref="IngredientPrice"/> to an <see cref="IngredientPriceDto"/>.</summary>
    public static IngredientPriceDto ToDto(this IngredientPrice price) => new(
        price.Id,
        price.IngredientId,
        price.StoreId,
        price.Store?.Name ?? string.Empty,
        price.Price,
        price.PackageQuantity,
        price.PackageUnit.ToContract(),
        price.RecordedDate,
        price.IsEstimated,
        price.IsPreferredStore);

    /// <summary>Projects an <see cref="IngredientPrice"/> to a <see cref="RecentPriceDto"/> including the ingredient name.</summary>
    public static RecentPriceDto ToRecentDto(this IngredientPrice price) => new(
        price.Id,
        price.IngredientId,
        price.Ingredient?.Name ?? string.Empty,
        price.StoreId,
        price.Store?.Name ?? string.Empty,
        price.Price,
        price.PackageQuantity,
        price.PackageUnit.ToContract(),
        price.RecordedDate,
        price.IsEstimated,
        price.IsPreferredStore);

    /// <summary>Applies a save request onto an <see cref="IngredientPrice"/> entity.</summary>
    public static void Apply(this IngredientPrice price, SaveIngredientPriceRequest request)
    {
        price.StoreId = request.StoreId;
        price.Price = request.Price;
        price.PackageQuantity = request.PackageQuantity;
        price.PackageUnit = request.PackageUnit.ToDomain();
        price.RecordedDate = request.RecordedDate;
        price.IsEstimated = request.IsEstimated;
        price.IsPreferredStore = request.IsPreferredStore;
    }

    // -- Recipe ---------------------------------------------------------------------------------

    /// <summary>Projects a <see cref="RecipeIngredient"/> to a <see cref="RecipeIngredientDto"/>.</summary>
    public static RecipeIngredientDto ToDto(this RecipeIngredient line) => new(
        line.Id,
        line.IngredientId,
        line.Ingredient?.Name ?? string.Empty,
        line.Quantity,
        line.Unit.ToContract());

    /// <summary>
    /// Projects a <see cref="Recipe"/> to a <see cref="RecipeDto"/> using pre-computed per-serving
    /// nutrition and cost.
    /// </summary>
    public static RecipeDto ToDto(this Recipe recipe, NutritionFacts perServing, RecipeCost cost) => new(
        recipe.Id,
        recipe.Name,
        recipe.MealType.ToContract(),
        recipe.PrepMinutes,
        recipe.CookMinutes,
        recipe.Servings,
        recipe.Instructions,
        recipe.Ingredients.Select(i => i.ToDto()).ToList(),
        perServing.Calories,
        perServing.Protein,
        perServing.Fiber,
        perServing.Carbs,
        perServing.Fat,
        perServing.IsEstimated,
        cost.CostPerServing,
        cost.TotalCost,
        cost.IsEstimated);

    /// <summary>
    /// Projects a <see cref="Recipe"/> to a <see cref="RecipeSummaryDto"/> using pre-computed
    /// per-serving nutrition and cost.
    /// </summary>
    public static RecipeSummaryDto ToSummaryDto(this Recipe recipe, NutritionFacts perServing, RecipeCost cost) => new(
        recipe.Id,
        recipe.Name,
        recipe.MealType.ToContract(),
        recipe.PrepMinutes,
        recipe.CookMinutes,
        recipe.Servings,
        perServing.Calories,
        perServing.Protein,
        perServing.Fiber,
        perServing.Carbs,
        perServing.Fat,
        perServing.IsEstimated,
        cost.CostPerServing,
        cost.IsEstimated);

    /// <summary>Replaces a recipe's scalar fields and ingredient lines from a save request.</summary>
    public static void Apply(this Recipe recipe, SaveRecipeRequest request)
    {
        recipe.Name = request.Name.Trim();
        recipe.MealType = request.MealType.ToDomain();
        recipe.PrepMinutes = request.PrepMinutes;
        recipe.CookMinutes = request.CookMinutes;
        recipe.Servings = Math.Max(1, request.Servings);
        recipe.Instructions = string.IsNullOrWhiteSpace(request.Instructions) ? null : request.Instructions.Trim();

        recipe.Ingredients.Clear();
        foreach (var line in request.Ingredients)
        {
            recipe.Ingredients.Add(new RecipeIngredient
            {
                IngredientId = line.IngredientId,
                Quantity = line.Quantity,
                Unit = line.Unit.ToDomain(),
            });
        }
    }

    // -- PantryItem -----------------------------------------------------------------------------

    /// <summary>Projects a <see cref="PantryItem"/> to a <see cref="PantryItemDto"/>.</summary>
    public static PantryItemDto ToDto(this PantryItem item) => new(
        item.Id,
        item.IngredientId,
        item.Ingredient?.Name ?? string.Empty,
        item.QuantityOnHand,
        item.Unit.ToContract(),
        item.Location.ToContract());

    /// <summary>Applies a save request onto a <see cref="PantryItem"/> entity.</summary>
    public static void Apply(this PantryItem item, SavePantryItemRequest request)
    {
        item.IngredientId = request.IngredientId;
        item.QuantityOnHand = request.QuantityOnHand;
        item.Unit = request.Unit.ToDomain();
        item.Location = request.Location.ToDomain();
    }

    // -- Planning -------------------------------------------------------------------------------

    /// <summary>Projects a <see cref="PlannedMeal"/> to a <see cref="PlannedMealDto"/>.</summary>
    public static PlannedMealDto ToDto(this PlannedMeal meal) => new(
        meal.Id,
        meal.Slot.ToContract(),
        meal.Assignee.ToContract(),
        meal.RecipeId,
        meal.Recipe?.Name,
        meal.MealComboId,
        meal.MealCombo?.Name,
        meal.Servings);

    /// <summary>Projects a <see cref="DayPlan"/> to a <see cref="DayPlanDto"/> with its prep load.</summary>
    public static DayPlanDto ToDto(this DayPlan day, int prepMinutes) => new(
        day.Id,
        day.Date,
        day.DayType.ToContract(),
        day.Note,
        prepMinutes,
        day.Meals.OrderBy(m => m.Slot).Select(m => m.ToDto()).ToList());

    /// <summary>Applies a save request onto a <see cref="PlannedMeal"/> entity.</summary>
    public static void Apply(this PlannedMeal meal, SavePlannedMealRequest request)
    {
        meal.Slot = request.Slot.ToDomain();
        meal.Assignee = request.Assignee.ToDomain();
        meal.RecipeId = request.RecipeId;
        meal.MealComboId = request.MealComboId;
        meal.Servings = Math.Max(1, request.Servings);
    }

    // -- MealCombo ------------------------------------------------------------------------------

    /// <summary>Projects a <see cref="MealCombo"/> to a <see cref="MealComboDto"/>.</summary>
    public static MealComboDto ToDto(this MealCombo combo) => new(
        combo.Id,
        combo.Name,
        combo.ProteinIngredientId,
        combo.ProteinIngredient?.Name,
        combo.CarbohydrateIngredientId,
        combo.CarbohydrateIngredient?.Name,
        combo.VegetableIngredientId,
        combo.VegetableIngredient?.Name);

    /// <summary>Applies a save request onto a <see cref="MealCombo"/> entity.</summary>
    public static void Apply(this MealCombo combo, SaveMealComboRequest request)
    {
        combo.Name = request.Name.Trim();
        combo.ProteinIngredientId = request.ProteinIngredientId;
        combo.CarbohydrateIngredientId = request.CarbohydrateIngredientId;
        combo.VegetableIngredientId = request.VegetableIngredientId;
    }
}
