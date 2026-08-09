using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Pantry;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Pantry API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class PantryEndpointsTests
{
    private static System.Text.Json.JsonSerializerOptions Json => ApiFixture.JsonOptions;

    private ApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private TestDataBuilder _builder = null!;
    private IngredientDto _ingredient = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _fixture = new ApiFixture();
        _client = _fixture.CreateAuthenticatedClient();
        _builder = new TestDataBuilder(_client);

        _ingredient = await _builder.CreateIngredientAsync("Pantry Test Ingredient");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _fixture.Dispose();
    }

    [Test]
    public async Task GetAll_ReturnsEmptyList_WhenNoPantryItemsExist()
    {
        var items = await _client.GetFromJsonAsync<PantryItemDto[]>("/api/pantry", Json);
        Assert.That(items, Is.Not.Null);
    }

    [Test]
    public async Task CreatePantryItem_ReturnsCreated_WithValidRequest()
    {
        var ingredient = await _builder.CreateIngredientAsync("Pantry Create Test");
        var request = new SavePantryItemRequest(
            ingredient.Id, 500, MeasurementUnit.Gram, StorageLocation.Pantry);
        var response = await _client.PostAsJsonAsync("/api/pantry", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var item = await response.Content.ReadFromJsonAsync<PantryItemDto>(Json);
        Assert.That(item, Is.Not.Null);
        Assert.That(item!.IngredientId, Is.EqualTo(ingredient.Id));
        Assert.That(item.QuantityOnHand, Is.EqualTo(500));
        Assert.That(item.Unit, Is.EqualTo(MeasurementUnit.Gram));
        Assert.That(item.Location, Is.EqualTo(StorageLocation.Pantry));
    }

    [Test]
    public async Task GetById_ReturnsPantryItem_WhenExists()
    {
        var ingredient = await _builder.CreateIngredientAsync("Pantry GetById Test");
        var pantryItem = await _builder.CreatePantryItemAsync(
            ingredient.Id, 250, MeasurementUnit.Millilitre, StorageLocation.Fridge);

        var item = await _client.GetFromJsonAsync<PantryItemDto>($"/api/pantry/{pantryItem.Id}", Json);

        Assert.That(item, Is.Not.Null);
        Assert.That(item!.Location, Is.EqualTo(StorageLocation.Fridge));
        Assert.That(item.QuantityOnHand, Is.EqualTo(250));
    }

    [Test]
    public async Task GetById_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync("/api/pantry/99999");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdatePantryItem_ReturnsOk_WithUpdatedValues()
    {
        var ingredient = await _builder.CreateIngredientAsync("Pantry Update Test");
        var pantryItem = await _builder.CreatePantryItemAsync(ingredient.Id, 300);

        var updateRequest = new SavePantryItemRequest(
            ingredient.Id, 150, MeasurementUnit.Gram, StorageLocation.Freezer);
        var response = await _client.PutAsJsonAsync($"/api/pantry/{pantryItem.Id}", updateRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<PantryItemDto>(Json);
        Assert.That(updated!.QuantityOnHand, Is.EqualTo(150));
        Assert.That(updated.Location, Is.EqualTo(StorageLocation.Freezer));
    }

    [Test]
    public async Task DeletePantryItem_ReturnsOk_WhenExists()
    {
        var ingredient = await _builder.CreateIngredientAsync("Pantry Delete Test");
        var pantryItem = await _builder.CreatePantryItemAsync(ingredient.Id, 100);

        var response = await _client.DeleteAsync($"/api/pantry/{pantryItem.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await _client.GetAsync($"/api/pantry/{pantryItem.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/pantry");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
