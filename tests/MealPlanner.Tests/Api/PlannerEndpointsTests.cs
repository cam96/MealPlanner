using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts;
using MealPlanner.Contracts.Planning;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Planner API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class PlannerEndpointsTests
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
    public async Task GetPlan_CreatesPlanIfNotExists_ReturnsMonthPlan()
    {
        var plan = await _builder.GetOrCreatePlanAsync(2026, 8);

        Assert.That(plan, Is.Not.Null);
        Assert.That(plan.Year, Is.EqualTo(2026));
        Assert.That(plan.Month, Is.EqualTo(8));
        Assert.That(plan.Days, Is.Not.Null);
        Assert.That(plan.Days.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task UpdateDay_ReturnsOk_WithUpdatedDayType()
    {
        var plan = await _builder.GetOrCreatePlanAsync(2026, 9);
        var dayId = plan.Days[0].Id;

        var request = new SaveDayRequest(DayType.EatingOut, "Date night");
        var response = await _client.PutAsJsonAsync($"/api/plans/days/{dayId}", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedPlan = await response.Content.ReadFromJsonAsync<MealPlanDto>(Json);
        var updatedDay = updatedPlan!.Days.Single(d => d.Id == dayId);
        Assert.That(updatedDay.DayType, Is.EqualTo(DayType.EatingOut));
        Assert.That(updatedDay.Note, Is.EqualTo("Date night"));
    }

    [Test]
    public async Task AddMeal_ReturnsOk_WithRecipe()
    {
        var recipe = await _builder.CreateRecipeAsync("Planner Test Recipe", MealType.Dinner, 2);
        var plan = await _builder.GetOrCreatePlanAsync(2026, 10);
        var dayId = plan.Days[0].Id;

        var request = new SavePlannedMealRequest(
            MealType.Dinner, MealAssignee.Shared, recipe.Id, null, 2);
        var response = await _client.PostAsJsonAsync($"/api/plans/days/{dayId}/meals", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedPlan = await response.Content.ReadFromJsonAsync<MealPlanDto>(Json);
        var day = updatedPlan!.Days.Single(d => d.Id == dayId);
        var meal = day.Meals.Single(m => m.RecipeId == recipe.Id);
        Assert.That(meal.Slot, Is.EqualTo(MealType.Dinner));
        Assert.That(meal.Assignee, Is.EqualTo(MealAssignee.Shared));
        Assert.That(meal.Servings, Is.EqualTo(2));
    }

    [Test]
    public async Task UpdateMeal_ReturnsOk_WithNewValues()
    {
        var recipe = await _builder.CreateRecipeAsync("Update Meal Recipe");
        var plan = await _builder.GetOrCreatePlanAsync(2026, 11);
        var dayId = plan.Days[1].Id;
        var meal = await _builder.AddPlannedMealAsync(dayId, recipeId: recipe.Id);

        var updateRequest = new SavePlannedMealRequest(
            MealType.Lunch, MealAssignee.FirstPerson, recipe.Id, null, 1);
        var response = await _client.PutAsJsonAsync($"/api/plans/meals/{meal.Id}", updateRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var updatedPlan = await response.Content.ReadFromJsonAsync<MealPlanDto>(Json);
        var updated = updatedPlan!.Days.SelectMany(d => d.Meals).Single(m => m.Id == meal.Id);
        Assert.That(updated.Slot, Is.EqualTo(MealType.Lunch));
        Assert.That(updated.Assignee, Is.EqualTo(MealAssignee.FirstPerson));
        Assert.That(updated.Servings, Is.EqualTo(1));
    }

    [Test]
    public async Task DeleteMeal_ReturnsOk_WhenExists()
    {
        var recipe = await _builder.CreateRecipeAsync("Delete Meal Recipe");
        var plan = await _builder.GetOrCreatePlanAsync(2026, 12);
        var dayId = plan.Days[2].Id;
        var meal = await _builder.AddPlannedMealAsync(dayId, recipeId: recipe.Id);

        var response = await _client.DeleteAsync($"/api/plans/meals/{meal.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    [Test]
    public async Task GetPlan_IncludesNutritionData_WhenPeopleAndMealsExist()
    {
        await _builder.CreatePersonAsync("Nutrition Person", 2000, 150, 30, 250, 65);
        var ingredient = await _builder.CreateIngredientAsync(
            "Nutrition Ingredient", caloriesPer100: 200, proteinPer100: 25);
        var recipe = await _builder.CreateRecipeAsync(
            "Nutrition Recipe", MealType.Dinner, 2,
            [new SaveRecipeIngredientRequest(ingredient.Id, 400, MeasurementUnit.Gram)]);

        var plan = await _builder.GetOrCreatePlanAsync(2027, 1);
        var dayId = plan.Days[0].Id;
        await _builder.AddPlannedMealAsync(dayId, recipeId: recipe.Id);

        // Re-fetch the plan to include nutrition
        var updatedPlan = await _builder.GetOrCreatePlanAsync(2027, 1);
        Assert.That(updatedPlan.Nutrition, Is.Not.Null);
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/plans/2026/8");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
