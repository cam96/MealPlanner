using System.Net.Http.Json;
using System.Text.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Combos;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Pantry;
using MealPlanner.Contracts.People;
using MealPlanner.Contracts.Planning;
using MealPlanner.Contracts.Prices;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Contracts.Settings;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Contracts.Stores;

namespace MealPlanner.Tests.Api.Fixtures;

/// <summary>
/// Provides helper methods for creating prerequisite test entities through the API.
/// All methods use HTTP calls so they exercise the full pipeline.
/// </summary>
public sealed class TestDataBuilder
{
    private static JsonSerializerOptions Json => ApiFixture.JsonOptions;

    private readonly HttpClient _client;

    public TestDataBuilder(HttpClient client)
    {
        _client = client;
    }

    /// <summary>Creates a person with sensible defaults for nutrition goals.</summary>
    public async Task<PersonDto> CreatePersonAsync(
        string name = "Test Person",
        int calories = 2000,
        int protein = 150,
        int fiber = 30,
        int carbs = 250,
        int fat = 65)
    {
        var request = new SavePersonRequest(name, calories, protein, fiber, carbs, fat);
        var response = await _client.PostAsJsonAsync("/api/people", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PersonDto>(Json))!;
    }

    /// <summary>Creates a store.</summary>
    public async Task<StoreDto> CreateStoreAsync(string name = "Test Store")
    {
        var request = new SaveStoreRequest(name);
        var response = await _client.PostAsJsonAsync("/api/stores", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StoreDto>(Json))!;
    }

    /// <summary>Creates an ingredient with optional nutrition values.</summary>
    public async Task<IngredientDto> CreateIngredientAsync(
        string name = "Test Ingredient",
        MeasurementUnit baseUnit = MeasurementUnit.Gram,
        FoodCategory category = FoodCategory.None,
        double caloriesPer100 = 100,
        double proteinPer100 = 10,
        double fiberPer100 = 5,
        double carbsPer100 = 20,
        double fatPer100 = 3,
        double? servingWeightG = null)
    {
        var request = new SaveIngredientRequest(
            name, baseUnit, category, caloriesPer100, proteinPer100,
            fiberPer100, carbsPer100, fatPer100,
            IsNutritionEstimated: false, CnfFoodCode: null,
            ServingWeightG: servingWeightG);
        var response = await _client.PostAsJsonAsync("/api/ingredients", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IngredientDto>(Json))!;
    }

    /// <summary>Creates a price entry for an ingredient at a store.</summary>
    public async Task<IngredientPriceDto> CreatePriceAsync(
        int ingredientId,
        int storeId,
        decimal price = 5.99m,
        double packageQuantity = 500,
        MeasurementUnit packageUnit = MeasurementUnit.Gram,
        bool isPreferredStore = true)
    {
        var request = new SaveIngredientPriceRequest(
            storeId, price, packageQuantity, packageUnit,
            DateOnly.FromDateTime(DateTime.Today), IsEstimated: false,
            IsPreferredStore: isPreferredStore);
        var response = await _client.PostAsJsonAsync(
            $"/api/ingredients/{ingredientId}/prices", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IngredientPriceDto>(Json))!;
    }

    /// <summary>Creates a recipe with one or more ingredient lines.</summary>
    public async Task<RecipeDto> CreateRecipeAsync(
        string name = "Test Recipe",
        MealType mealType = MealType.Dinner,
        int servings = 2,
        IReadOnlyList<SaveRecipeIngredientRequest>? ingredients = null)
    {
        ingredients ??= [];
        var request = new SaveRecipeRequest(
            name, mealType, PrepMinutes: 15, CookMinutes: 30,
            Servings: servings, Instructions: "Test instructions",
            Ingredients: ingredients);
        var response = await _client.PostAsJsonAsync("/api/recipes", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RecipeDto>(Json))!;
    }

    /// <summary>Creates a pantry item for an ingredient.</summary>
    public async Task<PantryItemDto> CreatePantryItemAsync(
        int ingredientId,
        double quantityOnHand = 500,
        MeasurementUnit unit = MeasurementUnit.Gram,
        StorageLocation location = StorageLocation.Pantry)
    {
        var request = new SavePantryItemRequest(ingredientId, quantityOnHand, unit, location);
        var response = await _client.PostAsJsonAsync("/api/pantry", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PantryItemDto>(Json))!;
    }

    /// <summary>Creates a meal combo from categorized ingredients.</summary>
    public async Task<MealComboDto> CreateComboAsync(
        string name = "Test Combo",
        int? proteinId = null,
        int? carbId = null,
        int? vegetableId = null)
    {
        var request = new SaveMealComboRequest(name, proteinId, carbId, vegetableId);
        var response = await _client.PostAsJsonAsync("/api/combos", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MealComboDto>(Json))!;
    }

    /// <summary>Adds a planned meal to a day in the specified month's plan.</summary>
    public async Task<PlannedMealDto> AddPlannedMealAsync(
        int dayId,
        int? recipeId = null,
        int? comboId = null,
        MealType slot = MealType.Dinner,
        MealAssignee assignee = MealAssignee.Shared,
        int servings = 2)
    {
        var request = new SavePlannedMealRequest(slot, assignee, recipeId, comboId, servings);
        var response = await _client.PostAsJsonAsync($"/api/plans/days/{dayId}/meals", request, Json);
        response.EnsureSuccessStatusCode();

        // The Planner endpoints return the full MealPlanDto; extract the new meal from it.
        var plan = await response.Content.ReadFromJsonAsync<MealPlanDto>(Json);
        var day = plan!.Days.First(d => d.Id == dayId);
        return day.Meals[^1]; // The newly added meal is last in the list.
    }

    /// <summary>Gets (or creates) the meal plan for a given month, returning the plan DTO.</summary>
    public async Task<MealPlanDto> GetOrCreatePlanAsync(int year, int month)
    {
        var response = await _client.GetAsync($"/api/plans/{year}/{month}");
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MealPlanDto>(Json))!;
    }

    /// <summary>Updates the monthly budget setting.</summary>
    public async Task SetBudgetAsync(decimal monthlyBudget)
    {
        var request = new SaveSettingsRequest(monthlyBudget);
        var response = await _client.PutAsJsonAsync("/api/settings", request, Json);
        response.EnsureSuccessStatusCode();
    }
}
