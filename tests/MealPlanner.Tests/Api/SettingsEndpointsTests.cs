using System.Net;
using System.Net.Http.Json;
using MealPlanner.Contracts.Settings;
using MealPlanner.Tests.Api.Fixtures;

namespace MealPlanner.Tests.Api;

/// <summary>Integration tests for the Settings API endpoints.</summary>
[TestFixture]
[Category("Integration")]
public sealed class SettingsEndpointsTests
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
    public async Task GetSettings_ReturnsDefaultSettings()
    {
        var settings = await _client.GetFromJsonAsync<AppSettingsDto>("/api/settings", Json);

        Assert.That(settings, Is.Not.Null);
        Assert.That(settings!.MonthlyBudget, Is.GreaterThanOrEqualTo(0));
    }

    [Test]
    public async Task UpdateSettings_ReturnsOk_WithNewBudget()
    {
        var request = new SaveSettingsRequest(750m);
        var response = await _client.PutAsJsonAsync("/api/settings", request, Json);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var settings = await _client.GetFromJsonAsync<AppSettingsDto>("/api/settings", Json);
        Assert.That(settings!.MonthlyBudget, Is.EqualTo(750m));
    }

    [Test]
    public async Task UpdateSettings_PersistsAcrossRequests()
    {
        var request = new SaveSettingsRequest(600m);
        await _client.PutAsJsonAsync("/api/settings", request, Json);

        // Fetch again to confirm persistence
        var settings = await _client.GetFromJsonAsync<AppSettingsDto>("/api/settings", Json);
        Assert.That(settings!.MonthlyBudget, Is.EqualTo(600m));
    }

    [Test]
    public async Task AnonymousRequest_ReturnsUnauthorized()
    {
        using var anonClient = _fixture.CreateAnonymousClient();
        var response = await anonClient.GetAsync("/api/settings");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }
}
