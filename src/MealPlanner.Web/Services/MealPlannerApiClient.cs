using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Auth;
using MealPlanner.Contracts.Cnf;
using MealPlanner.Contracts.Combos;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Pantry;
using MealPlanner.Contracts.Planning;
using MealPlanner.Contracts.Settings;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Contracts.People;
using MealPlanner.Contracts.Prices;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Contracts.Reporting;
using MealPlanner.Contracts.Stores;

namespace MealPlanner.Web.Services;

/// <summary>
/// Typed HTTP client for the MealPlanner API. The base address is resolved by Aspire service
/// discovery (the logical service name <c>api</c>); the Web UI never accesses the database directly.
/// Feature-specific methods are added per phase as endpoints come online.
/// </summary>
/// <param name="httpClient">The configured <see cref="HttpClient"/> pointing at the API service.</param>
public sealed class MealPlannerApiClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// Calls the API readiness endpoint. Used to verify service discovery end-to-end.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns><see langword="true"/> when the API responds successfully; otherwise <see langword="false"/>.</returns>
    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var response = await httpClient.GetAsync("/ping", cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    // -- People ---------------------------------------------------------------------------------

    /// <summary>Gets all household members.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of people.</returns>
    public async Task<IReadOnlyList<PersonDto>> GetPeopleAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<PersonDto>>("/api/people", JsonOptions, cancellationToken)
            ?? [];

    /// <summary>Creates a household member.</summary>
    /// <param name="request">The person to create.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created person.</returns>
    public async Task<PersonDto?> CreatePersonAsync(
        SavePersonRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/people", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PersonDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates a household member.</summary>
    /// <param name="id">The person identifier.</param>
    /// <param name="request">The updated values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated person.</returns>
    public async Task<PersonDto?> UpdatePersonAsync(
        int id,
        SavePersonRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"/api/people/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PersonDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes a household member.</summary>
    /// <param name="id">The person identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task DeletePersonAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/people/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // -- Stores ---------------------------------------------------------------------------------

    /// <summary>Gets all stores.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of stores.</returns>
    public async Task<IReadOnlyList<StoreDto>> GetStoresAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<StoreDto>>("/api/stores", JsonOptions, cancellationToken)
            ?? [];

    /// <summary>Creates a store.</summary>
    /// <param name="request">The store to create.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created store.</returns>
    public async Task<StoreDto?> CreateStoreAsync(
        SaveStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/stores", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StoreDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates a store.</summary>
    /// <param name="id">The store identifier.</param>
    /// <param name="request">The updated values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated store.</returns>
    public async Task<StoreDto?> UpdateStoreAsync(
        int id,
        SaveStoreRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"/api/stores/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<StoreDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes a store.</summary>
    /// <param name="id">The store identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns><see langword="true"/> if deleted; <see langword="false"/> if it could not be deleted (for example, it has recorded prices).</returns>
    public async Task<bool> DeleteStoreAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/stores/{id}", cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    // -- Ingredients ----------------------------------------------------------------------------

    /// <summary>Gets all ingredients.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of ingredients.</returns>
    public async Task<IReadOnlyList<IngredientDto>> GetIngredientsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<IngredientDto>>("/api/ingredients", JsonOptions, cancellationToken)
            ?? [];

    /// <summary>Searches ingredients by name (server-side filtering).</summary>
    /// <param name="query">The search term to filter by.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The filtered list of ingredients.</returns>
    public async Task<IReadOnlyList<IngredientDto>> SearchIngredientsAsync(
        string query,
        int limit = 20,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<IngredientDto>>(
            $"/api/ingredients?q={Uri.EscapeDataString(query)}&limit={limit}",
            JsonOptions,
            cancellationToken)
            ?? [];

    /// <summary>Creates an ingredient.</summary>
    /// <param name="request">The ingredient to create.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created ingredient.</returns>
    public async Task<IngredientDto?> CreateIngredientAsync(
        SaveIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/ingredients", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates an ingredient.</summary>
    /// <param name="id">The ingredient identifier.</param>
    /// <param name="request">The updated values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated ingredient.</returns>
    public async Task<IngredientDto?> UpdateIngredientAsync(
        int id,
        SaveIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"/api/ingredients/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes an ingredient.</summary>
    /// <param name="id">The ingredient identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task DeleteIngredientAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/ingredients/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Assigns (or clears) an ingredient's meal-building category.</summary>
    /// <param name="id">The ingredient identifier.</param>
    /// <param name="category">The category to assign; use <see cref="FoodCategory.None"/> to clear it.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated ingredient.</returns>
    public async Task<IngredientDto?> SetIngredientCategoryAsync(
        int id,
        FoodCategory category,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/ingredients/{id}/category",
            new SetIngredientCategoryRequest(category),
            JsonOptions,
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientDto>(JsonOptions, cancellationToken);
    }

    // -- Combos ---------------------------------------------------------------------------------

    /// <summary>Gets the meal-building category board (protein / carbohydrate / vegetable) with stock.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The category board, or an empty board when unavailable.</returns>
    public async Task<CategoryBoardDto> GetCategoryBoardAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<CategoryBoardDto>("/api/combos/board", JsonOptions, cancellationToken)
            ?? new CategoryBoardDto([], [], []);

    /// <summary>Gets all saved meal combos.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of combos.</returns>
    public async Task<IReadOnlyList<MealComboDto>> GetCombosAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<MealComboDto>>("/api/combos", JsonOptions, cancellationToken)
            ?? [];

    /// <summary>Creates a meal combo.</summary>
    /// <param name="request">The combo to create.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created combo.</returns>
    public async Task<MealComboDto?> CreateComboAsync(
        SaveMealComboRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("/api/combos", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealComboDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates a meal combo.</summary>
    /// <param name="id">The combo identifier.</param>
    /// <param name="request">The updated values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated combo.</returns>
    public async Task<MealComboDto?> UpdateComboAsync(
        int id,
        SaveMealComboRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync($"/api/combos/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealComboDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes a meal combo.</summary>
    /// <param name="id">The combo identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns><see langword="true"/> if deleted; <see langword="false"/> if it is used in a plan.</returns>
    public async Task<bool> DeleteComboAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/combos/{id}", cancellationToken);
        if (response.StatusCode is HttpStatusCode.BadRequest)
        {
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    // -- Canadian Nutrient File -----------------------------------------------------------------

    /// <summary>Gets whether the Canadian Nutrient File dataset is available on the server.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns><see langword="true"/> when CNF search and lookup are usable.</returns>
    public async Task<bool> IsCnfAvailableAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var status = await httpClient.GetFromJsonAsync<CnfStatusDto>("/api/cnf/status", JsonOptions, cancellationToken);
            return status?.IsAvailable ?? false;
        }
        catch (HttpRequestException)
        {
            return false;
        }
    }

    /// <summary>Searches Canadian Nutrient File foods by description.</summary>
    /// <param name="query">The text to search for.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The matching foods, best matches first.</returns>
    public async Task<IReadOnlyList<CnfFoodSummaryDto>> SearchCnfFoodsAsync(
        string query,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<CnfFoodSummaryDto>>(
            $"/api/cnf/foods?query={Uri.EscapeDataString(query)}", JsonOptions, cancellationToken) ?? [];

    /// <summary>Gets the per-100-gram nutrition for a Canadian Nutrient File food.</summary>
    /// <param name="foodCode">The CNF food code.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The food's nutrition, or <see langword="null"/> when not found.</returns>
    public async Task<CnfFoodNutritionDto?> GetCnfFoodAsync(
        int foodCode,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<CnfFoodNutritionDto>(
            $"/api/cnf/foods/{foodCode}", JsonOptions, cancellationToken);

    // -- Prices ---------------------------------------------------------------------------------

    /// <summary>Gets the recorded prices for an ingredient.</summary>
    /// <param name="ingredientId">The ingredient identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of price observations, newest first.</returns>
    public async Task<IReadOnlyList<IngredientPriceDto>> GetPricesAsync(
        int ingredientId,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<IngredientPriceDto>>(
            $"/api/ingredients/{ingredientId}/prices", JsonOptions, cancellationToken) ?? [];

    /// <summary>Records a price for an ingredient.</summary>
    /// <param name="ingredientId">The ingredient identifier.</param>
    /// <param name="request">The price to record.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created price observation.</returns>
    public async Task<IngredientPriceDto?> CreatePriceAsync(
        int ingredientId,
        SaveIngredientPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/api/ingredients/{ingredientId}/prices", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientPriceDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates a recorded price.</summary>
    /// <param name="ingredientId">The ingredient identifier.</param>
    /// <param name="priceId">The price identifier.</param>
    /// <param name="request">The updated values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated price observation.</returns>
    public async Task<IngredientPriceDto?> UpdatePriceAsync(
        int ingredientId,
        int priceId,
        SaveIngredientPriceRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/ingredients/{ingredientId}/prices/{priceId}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<IngredientPriceDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes a recorded price.</summary>
    /// <param name="ingredientId">The ingredient identifier.</param>
    /// <param name="priceId">The price identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task DeletePriceAsync(
        int ingredientId,
        int priceId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"/api/ingredients/{ingredientId}/prices/{priceId}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Gets recent price observations across all ingredients.</summary>
    /// <param name="query">Optional search term to filter by ingredient or store name.</param>
    /// <param name="limit">Maximum number of results to return.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of recent price observations.</returns>
    public async Task<IReadOnlyList<RecentPriceDto>> GetRecentPricesAsync(
        string? query = null,
        int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var url = $"/api/prices/recent?limit={limit}";
        if (!string.IsNullOrWhiteSpace(query))
        {
            url += $"&q={Uri.EscapeDataString(query)}";
        }

        return await httpClient.GetFromJsonAsync<IReadOnlyList<RecentPriceDto>>(url, JsonOptions, cancellationToken)
            ?? [];
    }

    // -- Recipes --------------------------------------------------------------------------------

    /// <summary>Gets all recipes with per-serving nutrition and cost summaries.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of recipe summaries.</returns>
    public async Task<IReadOnlyList<RecipeSummaryDto>> GetRecipesAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<RecipeSummaryDto>>(
            "/api/recipes", JsonOptions, cancellationToken) ?? [];

    /// <summary>Gets a single recipe with full detail, ingredient lines, nutrition and cost.</summary>
    /// <param name="id">The recipe identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The recipe, or <see langword="null"/> if it does not exist.</returns>
    public async Task<RecipeDto?> GetRecipeAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"/api/recipes/{id}", cancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Creates a recipe.</summary>
    /// <param name="request">The recipe to create.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created recipe.</returns>
    public async Task<RecipeDto?> CreateRecipeAsync(
        SaveRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/api/recipes", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates an existing recipe.</summary>
    /// <param name="id">The recipe identifier.</param>
    /// <param name="request">The updated recipe values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated recipe.</returns>
    public async Task<RecipeDto?> UpdateRecipeAsync(
        int id,
        SaveRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/recipes/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes a recipe.</summary>
    /// <param name="id">The recipe identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task DeleteRecipeAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/recipes/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // -- Pantry ---------------------------------------------------------------------------------

    /// <summary>Gets all pantry and freezer inventory items.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of pantry items.</returns>
    public async Task<IReadOnlyList<PantryItemDto>> GetPantryItemsAsync(
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<IReadOnlyList<PantryItemDto>>(
            "/api/pantry", JsonOptions, cancellationToken) ?? [];

    /// <summary>Creates a pantry item.</summary>
    /// <param name="request">The pantry item to create.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created pantry item.</returns>
    public async Task<PantryItemDto?> CreatePantryItemAsync(
        SavePantryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "/api/pantry", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PantryItemDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates an existing pantry item.</summary>
    /// <param name="id">The pantry item identifier.</param>
    /// <param name="request">The updated pantry item values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated pantry item.</returns>
    public async Task<PantryItemDto?> UpdatePantryItemAsync(
        int id,
        SavePantryItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/pantry/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PantryItemDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes a pantry item.</summary>
    /// <param name="id">The pantry item identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task DeletePantryItemAsync(int id, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/pantry/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // -- Planner --------------------------------------------------------------------------------

    /// <summary>Gets (creating if absent) the meal plan for a month, with nutrition rollups.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The month's meal plan, or <see langword="null"/> if unavailable.</returns>
    public async Task<MealPlanDto?> GetMealPlanAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<MealPlanDto>(
            $"/api/plans/{year}/{month}", JsonOptions, cancellationToken);

    /// <summary>Updates a day's type and note.</summary>
    /// <param name="dayId">The day plan identifier.</param>
    /// <param name="request">The new day values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The refreshed meal plan.</returns>
    public async Task<MealPlanDto?> UpdateDayAsync(
        int dayId,
        SaveDayRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/plans/days/{dayId}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealPlanDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Adds a planned meal to a day.</summary>
    /// <param name="dayId">The day plan identifier.</param>
    /// <param name="request">The meal to add.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The refreshed meal plan.</returns>
    public async Task<MealPlanDto?> AddPlannedMealAsync(
        int dayId,
        SavePlannedMealRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/api/plans/days/{dayId}/meals", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealPlanDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates a planned meal.</summary>
    /// <param name="mealId">The planned meal identifier.</param>
    /// <param name="request">The updated meal values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The refreshed meal plan.</returns>
    public async Task<MealPlanDto?> UpdatePlannedMealAsync(
        int mealId,
        SavePlannedMealRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/plans/meals/{mealId}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealPlanDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Deletes a planned meal.</summary>
    /// <param name="mealId">The planned meal identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The refreshed meal plan.</returns>
    public async Task<MealPlanDto?> DeletePlannedMealAsync(
        int mealId,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync($"/api/plans/meals/{mealId}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<MealPlanDto>(JsonOptions, cancellationToken);
    }

    // -- Shopping & settings --------------------------------------------------------------------

    /// <summary>Generates the shopping list for a month's plan, compared against the budget.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The generated shopping list, or <see langword="null"/> if unavailable.</returns>
    public async Task<ShoppingListDto?> GetShoppingListAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<ShoppingListDto>(
            $"/api/plans/{year}/{month}/shopping-list", JsonOptions, cancellationToken);

    /// <summary>Adds a manual item to the shopping list for a month.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="request">The item to add.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The created manual item.</returns>
    public async Task<ManualShoppingItemDto?> AddManualShoppingItemAsync(
        int year,
        int month,
        AddManualShoppingItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            $"/api/plans/{year}/{month}/shopping-list/manual-items", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ManualShoppingItemDto>(JsonOptions, cancellationToken);
    }

    /// <summary>Updates a manual item on the shopping list.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="id">The manual item identifier.</param>
    /// <param name="request">The updated item details.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task UpdateManualShoppingItemAsync(
        int year,
        int month,
        int id,
        AddManualShoppingItemRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/plans/{year}/{month}/shopping-list/manual-items/{id}", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Deletes a manual item from the shopping list.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="id">The manual item identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task DeleteManualShoppingItemAsync(
        int year,
        int month,
        int id,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"/api/plans/{year}/{month}/shopping-list/manual-items/{id}", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Toggles the cart status of a manual shopping list item.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="id">The manual item identifier.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task ToggleManualItemCartAsync(
        int year,
        int month,
        int id,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsync(
            $"/api/plans/{year}/{month}/shopping-list/manual-items/{id}/cart", null, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Clears all items currently in the cart for a month's shopping list.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    public async Task ClearShoppingCartAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.DeleteAsync(
            $"/api/plans/{year}/{month}/shopping-list/cart", cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>Gets the household's application settings.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The current settings.</returns>
    public async Task<AppSettingsDto> GetSettingsAsync(CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<AppSettingsDto>(
            "/api/settings", JsonOptions, cancellationToken) ?? new AppSettingsDto(0m);

    /// <summary>Updates the household's application settings.</summary>
    /// <param name="request">The new settings values.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated settings.</returns>
    public async Task<AppSettingsDto?> UpdateSettingsAsync(
        SaveSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PutAsJsonAsync(
            "/api/settings", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppSettingsDto>(JsonOptions, cancellationToken);
    }

    // -- Dashboard ------------------------------------------------------------------------------

    /// <summary>Gets the at-a-glance dashboard for a month's plan.</summary>
    /// <param name="year">The calendar year.</param>
    /// <param name="month">The calendar month (1-12).</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The dashboard summary, or <see langword="null"/> if unavailable.</returns>
    public async Task<DashboardDto?> GetDashboardAsync(
        int year,
        int month,
        CancellationToken cancellationToken = default) =>
        await httpClient.GetFromJsonAsync<DashboardDto>(
            $"/api/plans/{year}/{month}/dashboard", JsonOptions, cancellationToken);

    // -- Users (Admin) --------------------------------------------------------------------------

    /// <summary>Gets all application users with their role assignments.</summary>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The list of users, or an empty list if the caller is not an admin.</returns>
    public async Task<IReadOnlyList<AppUserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<IReadOnlyList<AppUserDto>>(
                "/api/users", JsonOptions, cancellationToken) ?? [];
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Forbidden)
        {
            return [];
        }
    }

    /// <summary>Updates a user's role assignments.</summary>
    /// <param name="userId">The user identifier.</param>
    /// <param name="roles">The new set of roles to assign.</param>
    /// <param name="cancellationToken">A token to cancel the request.</param>
    /// <returns>The updated user, or <see langword="null"/> on failure.</returns>
    public async Task<AppUserDto?> UpdateUserRolesAsync(
        int userId,
        IReadOnlyList<string> roles,
        CancellationToken cancellationToken = default)
    {
        var request = new UpdateUserRolesRequest(roles);
        using var response = await httpClient.PutAsJsonAsync(
            $"/api/users/{userId}/roles", request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AppUserDto>(JsonOptions, cancellationToken);
    }
}
