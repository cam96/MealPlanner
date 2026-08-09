using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Contracts.Stores;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Recipes API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class RecipesEndpointsTests
{
    private static System.Text.Json.JsonSerializerOptions Json => ApiFixture.JsonOptions;

    private ApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private TestDataBuilder _builder = null!;
    private IngredientDto _chicken = null!;
    private IngredientDto _rice = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _fixture = new ApiFixture();
        _client = _fixture.CreateAuthenticatedClient();
        _builder = new TestDataBuilder(_client);

        // Create ingredients for recipe tests
        _chicken = await _builder.CreateIngredientAsync(
            "Chicken for Recipe", MeasurementUnit.Gram, FoodCategory.Protein,
            caloriesPer100: 165, proteinPer100: 31, fiberPer100: 0, carbsPer100: 0, fatPer100: 3.6);
        _rice = await _builder.CreateIngredientAsync(
            "Rice for Recipe", MeasurementUnit.Gram, FoodCategory.Carbohydrate,
            caloriesPer100: 130, proteinPer100: 2.7, fiberPer100: 0.4, carbsPer100: 28, fatPer100: 0.3);

        // Add prices so cost can be computed
        var store = await _builder.CreateStoreAsync("Recipe Test Store");
        await _builder.CreatePriceAsync(_chicken.Id, store.Id, 12.99m, 1000);
        await _builder.CreatePriceAsync(_rice.Id, store.Id, 4.99m, 2000);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _fixture.Dispose();
    }

    [Test]
    public async Task GetAll_ReturnsEmptyOrExistingRecipes()
    {
        var recipes = await _client.GetFromJsonAsync<RecipeSummaryDto[]>("/api/recipes", Json);
        Assert.That(recipes, Is.Not.Null);
    }

    [Test]
    public async Task CreateRecipe_ReturnsCreated_WithComputedNutrition()
    {
        var ingredients = new List<SaveRecipeIngredientRequest>
        {
            new(_chicken.Id, 300, MeasurementUnit.Gram),
            new(_rice.Id, 200, MeasurementUnit.Gram),
        };
        var request = new SaveRecipeRequest(
            "Chicken Rice Bowl", MealType.Dinner, 15, 30, 2, "Cook it", ingredients);
        var response = await _client.PostAsJsonAsync("/api/recipes", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var recipe = await response.Content.ReadFromJsonAsync<RecipeDto>(Json);
        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Name, Is.EqualTo("Chicken Rice Bowl"));
        Assert.That(recipe.MealType, Is.EqualTo(MealType.Dinner));
        Assert.That(recipe.Servings, Is.EqualTo(2));
        Assert.That(recipe.Ingredients.Count, Is.EqualTo(2));
        // Nutrition: (300*165/100 + 200*130/100) / 2 servings = (495+260)/2 = 377.5 cal/serving
        Assert.That(recipe.CaloriesPerServing, Is.GreaterThan(0));
        Assert.That(recipe.ProteinPerServing, Is.GreaterThan(0));
        // Cost should be computed from prices
        Assert.That(recipe.CostPerServing, Is.GreaterThan(0));
    }

    [Test]
    public async Task CreateRecipe_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var request = new SaveRecipeRequest("", MealType.Dinner, 15, 30, 2, null, []);
        var response = await _client.PostAsJsonAsync("/api/recipes", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetById_ReturnsRecipe_WithIngredients()
    {
        var ingredients = new List<SaveRecipeIngredientRequest>
        {
            new(_chicken.Id, 200, MeasurementUnit.Gram),
        };
        var createResponse = await _client.PostAsJsonAsync("/api/recipes",
            new SaveRecipeRequest("Get By Id Recipe", MealType.Lunch, 10, 20, 1, null, ingredients), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<RecipeDto>(Json);

        var recipe = await _client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{created!.Id}", Json);

        Assert.That(recipe, Is.Not.Null);
        Assert.That(recipe!.Name, Is.EqualTo("Get By Id Recipe"));
        Assert.That(recipe.Ingredients.Count, Is.EqualTo(1));
        Assert.That(recipe.Ingredients[0].IngredientId, Is.EqualTo(_chicken.Id));
    }

    [Test]
    public async Task GetById_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync("/api/recipes/99999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdateRecipe_ReturnsOk_WithUpdatedValues()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/recipes",
            new SaveRecipeRequest("Before Update", MealType.Breakfast, 5, 10, 1, null, []), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<RecipeDto>(Json);

        var updateRequest = new SaveRecipeRequest(
            "After Update", MealType.Snack, 2, 5, 4, "New instructions",
            [new(_rice.Id, 100, MeasurementUnit.Gram)]);
        var response = await _client.PutAsJsonAsync($"/api/recipes/{created!.Id}", updateRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<RecipeDto>(Json);
        Assert.That(updated!.Name, Is.EqualTo("After Update"));
        Assert.That(updated.MealType, Is.EqualTo(MealType.Snack));
        Assert.That(updated.Servings, Is.EqualTo(4));
        Assert.That(updated.Ingredients.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteRecipe_ReturnsOk_WhenExists()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/recipes",
            new SaveRecipeRequest("To Delete", MealType.Dinner, 5, 10, 1, null, []), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<RecipeDto>(Json);

        var response = await _client.DeleteAsync($"/api/recipes/{created!.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await _client.GetAsync($"/api/recipes/{created.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/recipes");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
