using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts.People;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the People API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class PeopleEndpointsTests
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
    public async Task GetAll_ReturnsEmptyList_WhenNoPeopleExist()
    {
        var people = await _client.GetFromJsonAsync<PersonDto[]>("/api/people", Json);
        Assert.That(people, Is.Not.Null);
    }

    [Test]
    public async Task CreatePerson_ReturnsCreated_WithValidRequest()
    {
        var request = new SavePersonRequest("Alice", 2000, 150, 30, 250, 65);
        var response = await _client.PostAsJsonAsync("/api/people", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var person = await response.Content.ReadFromJsonAsync<PersonDto>(Json);
        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Name, Is.EqualTo("Alice"));
        Assert.That(person.DailyCalorieGoal, Is.EqualTo(2000));
        Assert.That(person.DailyProteinGoal, Is.EqualTo(150));
        Assert.That(person.DailyFiberGoal, Is.EqualTo(30));
        Assert.That(person.DailyCarbGoal, Is.EqualTo(250));
        Assert.That(person.DailyFatGoal, Is.EqualTo(65));
        Assert.That(person.Id, Is.GreaterThan(0));
    }

    [Test]
    public async Task CreatePerson_ReturnsBadRequest_WhenNameIsEmpty()
    {
        var request = new SavePersonRequest("", 2000, 150, 30, 250, 65);
        var response = await _client.PostAsJsonAsync("/api/people", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task CreatePerson_ReturnsBadRequest_WhenNameIsWhitespace()
    {
        var request = new SavePersonRequest("   ", 2000, 150, 30, 250, 65);
        var response = await _client.PostAsJsonAsync("/api/people", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetById_ReturnsPerson_WhenExists()
    {
        var request = new SavePersonRequest("GetById Test", 1800, 120, 25, 200, 55);
        var createResponse = await _client.PostAsJsonAsync("/api/people", request, Json);
        var created = await createResponse.Content.ReadFromJsonAsync<PersonDto>(Json);

        var person = await _client.GetFromJsonAsync<PersonDto>($"/api/people/{created!.Id}", Json);

        Assert.That(person, Is.Not.Null);
        Assert.That(person!.Id, Is.EqualTo(created.Id));
        Assert.That(person.Name, Is.EqualTo("GetById Test"));
    }

    [Test]
    public async Task GetById_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.GetAsync("/api/people/99999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdatePerson_ReturnsOk_WithUpdatedValues()
    {
        var createRequest = new SavePersonRequest("Before Update", 2000, 150, 30, 250, 65);
        var createResponse = await _client.PostAsJsonAsync("/api/people", createRequest, Json);
        var created = await createResponse.Content.ReadFromJsonAsync<PersonDto>(Json);

        var updateRequest = new SavePersonRequest("After Update", 2200, 160, 35, 270, 70);
        var response = await _client.PutAsJsonAsync($"/api/people/{created!.Id}", updateRequest, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var updated = await response.Content.ReadFromJsonAsync<PersonDto>(Json);
        Assert.That(updated!.Name, Is.EqualTo("After Update"));
        Assert.That(updated.DailyCalorieGoal, Is.EqualTo(2200));
    }

    [Test]
    public async Task UpdatePerson_ReturnsNotFound_WhenDoesNotExist()
    {
        var request = new SavePersonRequest("Ghost", 2000, 150, 30, 250, 65);
        var response = await _client.PutAsJsonAsync("/api/people/99999", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeletePerson_ReturnsOk_WhenExists()
    {
        var request = new SavePersonRequest("To Delete", 2000, 150, 30, 250, 65);
        var createResponse = await _client.PostAsJsonAsync("/api/people", request, Json);
        var created = await createResponse.Content.ReadFromJsonAsync<PersonDto>(Json);

        var response = await _client.DeleteAsync($"/api/people/{created!.Id}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify deletion
        var getResponse = await _client.GetAsync($"/api/people/{created.Id}");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task DeletePerson_ReturnsNotFound_WhenDoesNotExist()
    {
        var response = await _client.DeleteAsync("/api/people/99999");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/people");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
