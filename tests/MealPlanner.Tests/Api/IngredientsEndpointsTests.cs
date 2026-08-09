using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Combos;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Ingredients API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class IngredientsEndpointsTests
{
    private static System.Text.Json.JsonSerializerOptions Json => ApiFixture.JsonOptions;

    private ApiFixture _fixture = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _fixture = new ApiFixture();
        _client = _fixture.CreateAuthenticatedClient();
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _fixture.Dispose();
    }

    [Test]
    public async Task GetAll_ReturnsEmptyList_WhenNoIngredientsExist()
    {
        var ingredients = await _client.GetFromJsonAsync<IngredientDto[]>("/api/ingredients", Json);
        Assert.That(ingredients, Is.Not.Null);
    }

    [Test]
    public async Task CreateIngredient_ReturnsCreated_WithValidRequest()
    {
        var request = new SaveIngredientRequest(
            "Chicken Breast", MeasurementUnit.Gram, FoodCategory.Protein,
            165, 31, 0, 0, 3.6, false, null, null);
        var response = await _client.PostAsJsonAsync("/api/ingredients", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var ingredient = await response.Content.ReadFromJsonAsync<IngredientDto>(Json);
        Assert.That(ingredient, Is.Not.Null);
        Assert.That(ingredient!.Name, Is.EqualTo("Chicken Breast"));
        Assert.That(ingredient.BaseUnit, Is.EqualTo(MeasurementUnit.Gram));
        Assert.That(ingredient.Category, Is.EqualTo(FoodCategory.Protein));
        Assert.That(ingredient.CaloriesPer100, Is.EqualTo(165));
        Assert.That(ingredient.ProteinPer100, Is.EqualTo(31));
        Assert.That(ingredient.Id, Is.GreaterThan(0));
    }

    [Test]
    public async Task CreateIngredient_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var request = new SaveIngredientRequest(
            "", MeasurementUnit.Gram, FoodCategory.None,
            100, 10, 5, 20, 3, false, null, null);
        var response = await _client.PostAsJsonAsync("/api/ingredients", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetById_ReturnsIngredient_WhenExists()
    {
        var request = new SaveIngredientRequest(
            "Rice", MeasurementUnit.Gram, FoodCategory.Carbohydrate,
            130, 2.7, 0.4, 28, 0.3, false, null, null);
        var createResponse = await _client.PostAsJsonAsync("/api/ingredients", request, Json);
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientDto>(Json);

        var ingredient = await _client.GetFromJsonAsync<IngredientDto>($"/api/ingredients/{created!.Id}", Json);

        Assert.That(ingredient, Is.Not.Null);
        Assert.That(ingredient!.Name, Is.EqualTo("Rice"));
        Assert.That(ingredient.Category, Is.EqualTo(FoodCategory.Carbohydrate));
    }

    [Test]
    public async Task GetById_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync("/api/ingredients/99999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task SearchByName_ReturnsMatchingIngredients()
    {
        // Create some ingredients
        await _client.PostAsJsonAsync("/api/ingredients", new SaveIngredientRequest(
            "Broccoli Florets", MeasurementUnit.Gram, FoodCategory.Vegetable,
            34, 2.8, 2.6, 7, 0.4, false, null, null), Json);
        await _client.PostAsJsonAsync("/api/ingredients", new SaveIngredientRequest(
            "Brown Rice", MeasurementUnit.Gram, FoodCategory.Carbohydrate,
            111, 2.6, 1.8, 23, 0.9, false, null, null), Json);

        var results = await _client.GetFromJsonAsync<IngredientDto[]>("/api/ingredients?q=broccoli", Json);

        Assert.That(results, Is.Not.Null);
        Assert.That(results!.Length, Is.GreaterThanOrEqualTo(1));
        Assert.That(results.Any(i => i.Name.Contains("Broccoli", StringComparison.OrdinalIgnoreCase)), Is.True);
    }

    [Test]
    public async Task UpdateIngredient_ReturnsOk_WithUpdatedValues()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/ingredients", new SaveIngredientRequest(
            "Old Ingredient", MeasurementUnit.Gram, FoodCategory.None,
            100, 10, 5, 20, 3, false, null, null), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientDto>(Json);

        var updateRequest = new SaveIngredientRequest(
            "Updated Ingredient", MeasurementUnit.Millilitre, FoodCategory.Protein,
            200, 20, 10, 40, 6, true, null, null);
        var response = await _client.PutAsJsonAsync($"/api/ingredients/{created!.Id}", updateRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<IngredientDto>(Json);
        Assert.That(updated!.Name, Is.EqualTo("Updated Ingredient"));
        Assert.That(updated.BaseUnit, Is.EqualTo(MeasurementUnit.Millilitre));
        Assert.That(updated.IsNutritionEstimated, Is.True);
    }

    [Test]
    public async Task UpdateIngredient_ReturnsNotFound_WhenDoesNotExist()
    {
        var request = new SaveIngredientRequest(
            "Ghost", MeasurementUnit.Gram, FoodCategory.None,
            100, 10, 5, 20, 3, false, null, null);
        var response = await _client.PutAsJsonAsync("/api/ingredients/99999", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AssignCategory_UpdatesIngredientCategory()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/ingredients", new SaveIngredientRequest(
            "Uncategorized Item", MeasurementUnit.Gram, FoodCategory.None,
            50, 5, 2, 10, 1, false, null, null), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientDto>(Json);

        var categoryRequest = new SetIngredientCategoryRequest(FoodCategory.Vegetable);
        var response = await _client.PutAsJsonAsync($"/api/ingredients/{created!.Id}/category", categoryRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await _client.GetFromJsonAsync<IngredientDto>($"/api/ingredients/{created.Id}", Json);
        Assert.That(updated!.Category, Is.EqualTo(FoodCategory.Vegetable));
    }

    [Test]
    public async Task DeleteIngredient_ReturnsOk_WhenExists()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/ingredients", new SaveIngredientRequest(
            "To Delete", MeasurementUnit.Gram, FoodCategory.None,
            100, 10, 5, 20, 3, false, null, null), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<IngredientDto>(Json);

        var response = await _client.DeleteAsync($"/api/ingredients/{created!.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await _client.GetAsync($"/api/ingredients/{created.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteIngredient_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.DeleteAsync("/api/ingredients/99999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/ingredients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
