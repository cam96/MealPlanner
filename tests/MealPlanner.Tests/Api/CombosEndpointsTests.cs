using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Combos;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Combos API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class CombosEndpointsTests
{
    private static System.Text.Json.JsonSerializerOptions Json => ApiFixture.JsonOptions;

    private ApiFixture _fixture = null!;
    private HttpClient _client = null!;
    private TestDataBuilder _builder = null!;
    private IngredientDto _protein = null!;
    private IngredientDto _carb = null!;
    private IngredientDto _vegetable = null!;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _fixture = new ApiFixture();
        _client = _fixture.CreateAuthenticatedClient();
        _builder = new TestDataBuilder(_client);

        // Create categorized ingredients
        _protein = await _builder.CreateIngredientAsync(
            "Combo Chicken", category: FoodCategory.Protein);
        _carb = await _builder.CreateIngredientAsync(
            "Combo Rice", category: FoodCategory.Carbohydrate);
        _vegetable = await _builder.CreateIngredientAsync(
            "Combo Broccoli", category: FoodCategory.Vegetable);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _fixture.Dispose();
    }

    [Test]
    public async Task GetBoard_ReturnsCategoryBoard()
    {
        var board = await _client.GetFromJsonAsync<CategoryBoardDto>("/api/combos/board", Json);

        Assert.That(board, Is.Not.Null);
        Assert.That(board!.Protein, Is.Not.Null);
        Assert.That(board.Carbohydrate, Is.Not.Null);
        Assert.That(board.Vegetable, Is.Not.Null);
    }

    [Test]
    public async Task CreateCombo_ReturnsCreated_WithIngredientRefs()
    {
        var request = new SaveMealComboRequest(
            "Chicken Bowl", _protein.Id, _carb.Id, _vegetable.Id);
        var response = await _client.PostAsJsonAsync("/api/combos", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var combo = await response.Content.ReadFromJsonAsync<MealComboDto>(Json);
        Assert.That(combo, Is.Not.Null);
        Assert.That(combo!.Name, Is.EqualTo("Chicken Bowl"));
        Assert.That(combo.ProteinIngredientId, Is.EqualTo(_protein.Id));
        Assert.That(combo.CarbohydrateIngredientId, Is.EqualTo(_carb.Id));
        Assert.That(combo.VegetableIngredientId, Is.EqualTo(_vegetable.Id));
    }

    [Test]
    public async Task CreateCombo_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var request = new SaveMealComboRequest("", null, null, null);
        var response = await _client.PostAsJsonAsync("/api/combos", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetAll_ReturnsComboList()
    {
        await _builder.CreateComboAsync("List Test Combo", _protein.Id);

        var combos = await _client.GetFromJsonAsync<MealComboDto[]>("/api/combos", Json);

        Assert.That(combos, Is.Not.Null);
        Assert.That(combos!.Length, Is.GreaterThanOrEqualTo(1));
    }

    [Test]
    public async Task UpdateCombo_ReturnsOk_WithNewName()
    {
        var combo = await _builder.CreateComboAsync("Before", _protein.Id);

        var updateRequest = new SaveMealComboRequest("After", _protein.Id, _carb.Id, _vegetable.Id);
        var response = await _client.PutAsJsonAsync($"/api/combos/{combo.Id}", updateRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updated = await response.Content.ReadFromJsonAsync<MealComboDto>(Json);
        Assert.That(updated!.Name, Is.EqualTo("After"));
    }

    [Test]
    public async Task DeleteCombo_ReturnsNoContent_WhenExists()
    {
        var combo = await _builder.CreateComboAsync("To Delete", _protein.Id);

        var response = await _client.DeleteAsync($"/api/combos/{combo.Id}");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/combos");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
