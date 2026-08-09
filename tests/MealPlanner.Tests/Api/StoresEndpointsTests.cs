using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts.Stores;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Stores API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class StoresEndpointsTests
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
    public async Task GetAll_ReturnsEmptyList_WhenNoStoresExist()
    {
        var stores = await _client.GetFromJsonAsync<StoreDto[]>("/api/stores", Json);
        Assert.That(stores, Is.Not.Null);
    }

    [Test]
    public async Task CreateStore_ReturnsCreated_WithValidName()
    {
        var request = new SaveStoreRequest("Costco");
        var response = await _client.PostAsJsonAsync("/api/stores", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var store = await response.Content.ReadFromJsonAsync<StoreDto>(Json);
        Assert.That(store, Is.Not.Null);
        Assert.That(store!.Name, Is.EqualTo("Costco"));
        Assert.That(store.Id, Is.GreaterThan(0));
    }

    [Test]
    public async Task CreateStore_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var request = new SaveStoreRequest("");
        var response = await _client.PostAsJsonAsync("/api/stores", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreateStore_ReturnsBadRequest_WhenNameIsWhitespace()
    {
        var request = new SaveStoreRequest("   ");
        var response = await _client.PostAsJsonAsync("/api/stores", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetById_ReturnsStore_WhenExists()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/stores", new SaveStoreRequest("Superstore"), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<StoreDto>(Json);

        var store = await _client.GetFromJsonAsync<StoreDto>($"/api/stores/{created!.Id}", Json);

        Assert.That(store, Is.Not.Null);
        Assert.That(store!.Name, Is.EqualTo("Superstore"));
    }

    [Test]
    public async Task GetById_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync("/api/stores/99999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdateStore_ReturnsOk_WithNewName()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/stores", new SaveStoreRequest("Old Name"), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<StoreDto>(Json);

        var response = await _client.PutAsJsonAsync($"/api/stores/{created!.Id}", new SaveStoreRequest("New Name"), Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<StoreDto>(Json);
        Assert.That(updated!.Name, Is.EqualTo("New Name"));
    }

    [Test]
    public async Task UpdateStore_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.PutAsJsonAsync("/api/stores/99999", new SaveStoreRequest("Ghost"), Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteStore_ReturnsOk_WhenExists()
    {
        var createResponse = await _client.PostAsJsonAsync("/api/stores", new SaveStoreRequest("To Delete"), Json);
        var created = await createResponse.Content.ReadFromJsonAsync<StoreDto>(Json);

        var response = await _client.DeleteAsync($"/api/stores/{created!.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var getResponse = await _client.GetAsync($"/api/stores/{created.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeleteStore_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.DeleteAsync("/api/stores/99999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/stores");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
