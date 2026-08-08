using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace MealPlanner.Tests.Auth;

/// <summary>
/// Integration tests verifying that API endpoints require JWT authentication and reject
/// unauthenticated requests with 401 Unauthorized.
/// </summary>
[TestFixture]
public sealed class EndpointAuthorizationTests
{
    private const string JwtKey = "IntegrationTestSigningKeyThatIsLongEnoughForHmacSha256!!";

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _anonymousClient = null!;
    private HttpClient _authenticatedClient = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Jwt:Key", JwtKey);
                builder.UseSetting("ConnectionStrings:mealplanner", "Data Source=:memory:");
            });

        _anonymousClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        _authenticatedClient = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        _authenticatedClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", GenerateTestToken());
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _authenticatedClient.Dispose();
        _anonymousClient.Dispose();
        _factory.Dispose();
    }

    /// <summary>The /ping endpoint must remain accessible without authentication.</summary>
    [Test]
    public async Task Ping_Anonymous_ReturnsOk()
    {
        var response = await _anonymousClient.GetAsync("/ping");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>Protected endpoints must return 401 without a Bearer token.</summary>
    [TestCase("/api/people")]
    [TestCase("/api/stores")]
    [TestCase("/api/ingredients")]
    [TestCase("/api/recipes")]
    [TestCase("/api/pantry")]
    [TestCase("/api/plans/2026/8")]
    [TestCase("/api/combos")]
    [TestCase("/api/settings")]
    [TestCase("/api/cnf/status")]
    [TestCase("/api/plans/2026/8/dashboard")]
    [TestCase("/api/plans/2026/8/shopping-list")]
    public async Task ProtectedEndpoint_Anonymous_ReturnsUnauthorized(string url)
    {
        var response = await _anonymousClient.GetAsync(url);
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>Protected endpoints must accept requests with a valid Bearer token.</summary>
    [TestCase("/api/people")]
    [TestCase("/api/stores")]
    [TestCase("/api/ingredients")]
    [TestCase("/api/recipes")]
    [TestCase("/api/pantry")]
    [TestCase("/api/plans/2026/8")]
    [TestCase("/api/combos")]
    [TestCase("/api/settings")]
    [TestCase("/api/cnf/status")]
    public async Task ProtectedEndpoint_WithValidToken_DoesNotReturn401(string url)
    {
        var response = await _authenticatedClient.GetAsync(url);

        // We expect the endpoint to NOT return 401 (may return 200, 404, etc. depending on data).
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
    }

    private static string GenerateTestToken()
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, "test-user-id"),
            new Claim(JwtRegisteredClaimNames.Name, "Test User"),
            new Claim(JwtRegisteredClaimNames.Email, "test@example.com"),
        };

        var token = new JwtSecurityToken(
            issuer: "MealPlanner.Web",
            audience: "MealPlanner.Api",
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
