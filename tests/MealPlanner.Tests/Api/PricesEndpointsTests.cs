using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Contracts.Prices;
using MealPlanner.Contracts.Stores;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Prices API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class PricesEndpointsTests
{
    private static System.Text.Json.JsonSerializerOptions Json => ApiFixture.JsonOptions;

    private ApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private TestDataBuilder _builder = null!;
    private IngredientDto _ingredient = null!;
    private StoreDto _store = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _fixture = new ApiFixture();
        _client = _fixture.CreateAuthenticatedClient();
        _builder = new TestDataBuilder(_client);

        // Create prerequisite data
        _ingredient = await _builder.CreateIngredientAsync("Price Test Ingredient");
        _store = await _builder.CreateStoreAsync("Price Test Store");
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _fixture.Dispose();
    }

    [Test]
    public async Task CreatePrice_ReturnsCreated_WithValidRequest()
    {
        var request = new SaveIngredientPriceRequest(
            _store.Id, 5.99m, 500, MeasurementUnit.Gram,
            DateOnly.FromDateTime(DateTime.Today), false, true);
        var response = await _client.PostAsJsonAsync(
            $"/api/ingredients/{_ingredient.Id}/prices", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var price = await response.Content.ReadFromJsonAsync<IngredientPriceDto>(Json);
        Assert.That(price, Is.Not.Null);
        Assert.That(price!.IngredientId, Is.EqualTo(_ingredient.Id));
        Assert.That(price.StoreId, Is.EqualTo(_store.Id));
        Assert.That(price.Price, Is.EqualTo(5.99m));
        Assert.That(price.PackageQuantity, Is.EqualTo(500));
        Assert.That(price.IsPreferredStore, Is.True);
    }

    [Test]
    public async Task GetPricesForIngredient_ReturnsPriceList()
    {
        var ingredient = await _builder.CreateIngredientAsync("Priced Ingredient");
        await _builder.CreatePriceAsync(ingredient.Id, _store.Id, 3.49m, 250);

        var prices = await _client.GetFromJsonAsync<IngredientPriceDto[]>(
            $"/api/ingredients/{ingredient.Id}/prices", Json);

        Assert.That(prices, Is.Not.Null);
        Assert.That(prices!.Length, Is.GreaterThanOrEqualTo(1));
        Assert.That(prices[0].Price, Is.EqualTo(3.49m));
    }

    [Test]
    public async Task UpdatePrice_ReturnsOk_WithNewValues()
    {
        var ingredient = await _builder.CreateIngredientAsync("Update Price Ingredient");
        var price = await _builder.CreatePriceAsync(ingredient.Id, _store.Id, 4.99m, 400);

        var updateRequest = new SaveIngredientPriceRequest(
            _store.Id, 6.49m, 600, MeasurementUnit.Gram,
            DateOnly.FromDateTime(DateTime.Today), false, true);
        var response = await _client.PutAsJsonAsync($"/api/prices/{price.Id}", updateRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<IngredientPriceDto>(Json);
        Assert.That(updated!.Price, Is.EqualTo(6.49m));
        Assert.That(updated.PackageQuantity, Is.EqualTo(600));
    }

    [Test]
    public async Task DeletePrice_ReturnsOk_WhenExists()
    {
        var ingredient = await _builder.CreateIngredientAsync("Delete Price Ingredient");
        var price = await _builder.CreatePriceAsync(ingredient.Id, _store.Id, 2.99m, 200);

        var response = await _client.DeleteAsync($"/api/prices/{price.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task GetRecentPrices_ReturnsAllRecentPrices()
    {
        var ingredient = await _builder.CreateIngredientAsync("Recent Price Ingredient");
        await _builder.CreatePriceAsync(ingredient.Id, _store.Id, 7.99m, 1000);

        var prices = await _client.GetFromJsonAsync<RecentPriceDto[]>("/api/prices/recent", Json);

        Assert.That(prices, Is.Not.Null);
        Assert.That(prices!.Length, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync($"/api/ingredients/{_ingredient.Id}/prices");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
