using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Shopping API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class ShoppingEndpointsTests
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

    [Test]
    public async Task GetShoppingList_ReturnsListForMonth()
    {
        // Ensure a plan exists for this month
        await _builder.GetOrCreatePlanAsync(2026, 8);

        var response = await _client.GetAsync("/api/plans/2026/8/shopping-list");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var list = await response.Content.ReadFromJsonAsync<ShoppingListDto>(Json);
        Assert.That(list, Is.Not.Null);
        Assert.That(list!.Year, Is.EqualTo(2026));
        Assert.That(list.Month, Is.EqualTo(8));
    }

    [Test]
    public async Task AddManualItem_ReturnsCreated()
    {
        await _builder.GetOrCreatePlanAsync(2026, 9);

        var request = new AddManualShoppingItemRequest("Paper Towels", null, null, null);
        var response = await _client.PostAsJsonAsync(
            "/api/plans/2026/9/shopping-list/manual-items", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var item = await response.Content.ReadFromJsonAsync<ManualShoppingItemDto>(Json);
        Assert.That(item, Is.Not.Null);
        Assert.That(item!.Name, Is.EqualTo("Paper Towels"));
    }

    [Test]
    public async Task AddManualItem_WithIngredient_ReturnsCreatedWithPricing()
    {
        var store = await _builder.CreateStoreAsync("Shopping Store");
        var ingredient = await _builder.CreateIngredientAsync("Shopping Ingredient");
        await _builder.CreatePriceAsync(ingredient.Id, store.Id, 3.99m, 500);

        await _builder.GetOrCreatePlanAsync(2026, 10);

        var request = new AddManualShoppingItemRequest(
            "Shopping Ingredient", ingredient.Id, 250, MeasurementUnit.Gram);
        var response = await _client.PostAsJsonAsync(
            "/api/plans/2026/10/shopping-list/manual-items", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));
        var item = await response.Content.ReadFromJsonAsync<ManualShoppingItemDto>(Json);
        Assert.That(item!.IngredientId, Is.EqualTo(ingredient.Id));
    }

    [Test]
    public async Task ToggleCartItem_UpdatesIsInCart()
    {
        await _builder.GetOrCreatePlanAsync(2026, 11);
        var addResponse = await _client.PostAsJsonAsync(
            "/api/plans/2026/11/shopping-list/manual-items",
            new AddManualShoppingItemRequest("Toggle Item", null, null, null), Json);
        var item = await addResponse.Content.ReadFromJsonAsync<ManualShoppingItemDto>(Json);

        var response = await _client.PutAsJsonAsync(
            $"/api/plans/2026/11/shopping-list/manual-items/{item!.Id}/cart",
            new { }, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task DeleteManualItem_ReturnsOk()
    {
        await _builder.GetOrCreatePlanAsync(2027, 1);
        var addResponse = await _client.PostAsJsonAsync(
            "/api/plans/2027/1/shopping-list/manual-items",
            new AddManualShoppingItemRequest("Delete Me", null, null, null), Json);
        var item = await addResponse.Content.ReadFromJsonAsync<ManualShoppingItemDto>(Json);

        var response = await _client.DeleteAsync(
            $"/api/plans/2027/1/shopping-list/manual-items/{item!.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task ClearCart_ReturnsOk()
    {
        await _builder.GetOrCreatePlanAsync(2027, 2);

        var response = await _client.DeleteAsync("/api/plans/2027/2/shopping-list/cart");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/plans/2026/8/shopping-list");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
