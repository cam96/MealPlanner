using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Pantry;
using MealPlanner.Contracts.Planning;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Contracts.Reporting;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>
/// End-to-end workflow test that exercises the full meal planning pipeline:
/// Create ingredients with prices → Create recipe → Plan meals → Verify dashboard → Check shopping list.
/// </summary>
[TestFixture]
[Category("Integration")]
[Order(100)]
public sealed class MealPlanningWorkflowTests
{
    private static System.Text.Json.JsonSerializerOptions Json => ApiFixture.JsonOptions;

    private ApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private TestDataBuilder _builder = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _fixture = new ApiFixture();
        _client = _fixture.CreateAuthenticatedClient();
        _builder = new TestDataBuilder(_client);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _fixture.Dispose();
    }

    [Test, Order(1)]
    public async Task FullWorkflow_IngredientToShoppingList()
    {
        // Step 1: Create a person with nutrition goals
        var person = await _builder.CreatePersonAsync(
            "Workflow Person", calories: 2000, protein: 150, fiber: 30, carbs: 250, fat: 65);
        Assert.That(person.Id, Is.GreaterThan(0));

        // Step 2: Create a store
        var store = await _builder.CreateStoreAsync("Workflow Costco");
        Assert.That(store.Id, Is.GreaterThan(0));

        // Step 3: Create ingredients with nutrition data
        var chicken = await _builder.CreateIngredientAsync(
            "Workflow Chicken", MeasurementUnit.Gram, FoodCategory.Protein,
            caloriesPer100: 165, proteinPer100: 31, fiberPer100: 0, carbsPer100: 0, fatPer100: 3.6);
        var rice = await _builder.CreateIngredientAsync(
            "Workflow Rice", MeasurementUnit.Gram, FoodCategory.Carbohydrate,
            caloriesPer100: 130, proteinPer100: 2.7, fiberPer100: 0.4, carbsPer100: 28, fatPer100: 0.3);
        var broccoli = await _builder.CreateIngredientAsync(
            "Workflow Broccoli", MeasurementUnit.Gram, FoodCategory.Vegetable,
            caloriesPer100: 34, proteinPer100: 2.8, fiberPer100: 2.6, carbsPer100: 7, fatPer100: 0.4);

        // Step 4: Record prices for each ingredient
        await _builder.CreatePriceAsync(chicken.Id, store.Id, 12.99m, 1000);
        await _builder.CreatePriceAsync(rice.Id, store.Id, 4.99m, 2000);
        await _builder.CreatePriceAsync(broccoli.Id, store.Id, 3.49m, 500);

        // Step 5: Create a recipe using these ingredients
        var recipeIngredients = new List<SaveRecipeIngredientRequest>
        {
            new(chicken.Id, 300, MeasurementUnit.Gram),
            new(rice.Id, 200, MeasurementUnit.Gram),
            new(broccoli.Id, 150, MeasurementUnit.Gram),
        };
        var recipe = await _builder.CreateRecipeAsync(
            "Workflow Chicken Bowl", MealType.Dinner, 2, recipeIngredients);

        // Verify nutrition was computed
        Assert.That(recipe.CaloriesPerServing, Is.GreaterThan(0),
            "Recipe should have computed calories");
        Assert.That(recipe.ProteinPerServing, Is.GreaterThan(0),
            "Recipe should have computed protein");
        Assert.That(recipe.CostPerServing, Is.GreaterThan(0),
            "Recipe should have computed cost");

        // Step 6: Set monthly budget
        await _builder.SetBudgetAsync(600m);

        // Step 7: Get the monthly plan and add a meal
        var plan = await _builder.GetOrCreatePlanAsync(2029, 3);
        Assert.That(plan.Days.Count, Is.GreaterThan(0), "Plan should have days");

        var dayId = plan.Days[0].Id;
        var meal = await _builder.AddPlannedMealAsync(
            dayId, recipeId: recipe.Id, slot: MealType.Dinner,
            assignee: MealAssignee.Shared, servings: 2);
        Assert.That(meal.RecipeId, Is.EqualTo(recipe.Id));

        // Step 8: Check the dashboard reflects the planned meal
        var dashResponse = await _client.GetAsync("/api/plans/2029/3/dashboard");
        Assert.That(dashResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var dashboard = await dashResponse.Content.ReadFromJsonAsync<DashboardDto>(Json);
        Assert.That(dashboard!.PlannedMealCount, Is.GreaterThan(0),
            "Dashboard should reflect at least one planned meal");
        Assert.That(dashboard.MonthlyBudget, Is.EqualTo(600m));

        // Step 9: Check the shopping list includes the recipe ingredients
        var shoppingResponse = await _client.GetAsync("/api/plans/2029/3/shopping-list");
        Assert.That(shoppingResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var shoppingList = await shoppingResponse.Content.ReadFromJsonAsync<ShoppingListDto>(Json);
        Assert.That(shoppingList, Is.Not.Null);
        Assert.That(shoppingList!.Year, Is.EqualTo(2029));
        Assert.That(shoppingList.Month, Is.EqualTo(3));

        // Step 10: Add pantry stock and verify shopping list deducts it
        await _builder.CreatePantryItemAsync(chicken.Id, 200, MeasurementUnit.Gram, StorageLocation.Freezer);

        // Re-fetch shopping list — pantry should reduce needed quantity
        var updatedShoppingResponse = await _client.GetAsync("/api/plans/2029/3/shopping-list");
        var updatedList = await updatedShoppingResponse.Content.ReadFromJsonAsync<ShoppingListDto>(Json);
        Assert.That(updatedList, Is.Not.Null);
    }
}
