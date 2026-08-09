using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Contracts.Reporting;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Dashboard API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class DashboardEndpointsTests
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
    public async Task GetDashboard_ReturnsEmptyDashboard_WhenNoDataExists()
    {
        // Use a far-future month unlikely to have data
        var response = await _client.GetAsync("/api/plans/2030/1/dashboard");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>(Json);
        Assert.That(dashboard, Is.Not.Null);
        Assert.That(dashboard!.Year, Is.EqualTo(2030));
        Assert.That(dashboard.Month, Is.EqualTo(1));
        Assert.That(dashboard.PlannedMealCount, Is.EqualTo(0));
    }

    [Test]
    public async Task GetDashboard_ReturnsMetrics_WhenMealsPlanned()
    {
        // Set up a person, recipe, and planned meal
        await _builder.CreatePersonAsync("Dashboard Person", 2000, 150, 30, 250, 65);
        var ingredient = await _builder.CreateIngredientAsync(
            "Dashboard Ingredient", caloriesPer100: 150, proteinPer100: 20);
        var store = await _builder.CreateStoreAsync("Dashboard Store");
        await _builder.CreatePriceAsync(ingredient.Id, store.Id, 10.00m, 1000);

        var recipe = await _builder.CreateRecipeAsync(
            "Dashboard Recipe", MealType.Dinner, 2,
            [new SaveRecipeIngredientRequest(ingredient.Id, 500, MeasurementUnit.Gram)]);

        await _builder.SetBudgetAsync(600m);

        var plan = await _builder.GetOrCreatePlanAsync(2028, 6);
        var dayId = plan.Days[0].Id;
        await _builder.AddPlannedMealAsync(dayId, recipeId: recipe.Id);

        var response = await _client.GetAsync("/api/plans/2028/6/dashboard");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var dashboard = await response.Content.ReadFromJsonAsync<DashboardDto>(Json);
        Assert.That(dashboard!.PlannedMealCount, Is.GreaterThan(0));
        Assert.That(dashboard.MonthlyBudget, Is.EqualTo(600m));
        Assert.That(dashboard.Prep, Is.Not.Null);
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/plans/2026/8/dashboard");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
