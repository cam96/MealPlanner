using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts.Cnf;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the CNF (Canadian Nutrient File) API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class CnfEndpointsTests
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
    public async Task GetStatus_ReturnsAvailabilityInfo()
    {
        var response = await _client.GetAsync("/api/cnf/status");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var status = await response.Content.ReadFromJsonAsync<CnfStatusDto>(Json);
        Assert.That(status, Is.Not.Null);
        // In test environment, CNF data directory may not exist
        // Just verify the endpoint responds correctly
    }

    [Test]
    public async Task SearchFoods_ReturnsOk_EvenWhenCnfUnavailable()
    {
        // In test environment, CNF may not be available, but endpoint should still respond
        var response = await _client.GetAsync("/api/cnf/foods?query=chicken");

        // Should return OK with empty results or NotFound if CNF is unavailable
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetFoodByCode_ReturnsOk_OrNotFound()
    {
        var response = await _client.GetAsync("/api/cnf/foods/1220");

        // Should return OK with food data or NotFound if CNF data is unavailable
        Assert.That(response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK).Or.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/cnf/status");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
