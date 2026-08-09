using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using MealPlanner.Contracts.Auth;
using MealPlanner.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.IdentityModel.Tokens;

namespace MealPlanner.Tests.Auth;

/// <summary>
/// Integration tests verifying role-based authorization enforcement: endpoints require the correct
/// role claim in the JWT, and the ensure-user endpoint provisions new users with the default role.
/// </summary>
[TestFixture]
public sealed class RoleAuthorizationTests
{
    private const string JwtKey = "IntegrationTestSigningKeyThatIsLongEnoughForHmacSha256!!";

    private WebApplicationFactory<Program> _factory = null!;
    private string _dbPath = null!;
    private int _testUserId;

    [OneTimeSetUp]
    public async Task OneTimeSetUp()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"mealplanner_role_test_{Guid.NewGuid():N}.db");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Jwt:Key", JwtKey);
                builder.UseSetting("ConnectionStrings:mealplanner", $"Data Source={_dbPath}");
            });

        // Provision the test user so endpoints can resolve the app_user_id claim.
        using var client = CreateClientWithoutUserId();
        var response = await client.PostAsync("/api/auth/ensure-user", null);
        var result = await response.Content.ReadFromJsonAsync<UserRolesResponse>();
        _testUserId = result!.UserId;
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _factory.Dispose();

        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            try { File.Delete(_dbPath); } catch (IOException) { }
        }
    }

    /// <summary>
    /// A token without any role claims should be rejected with 403 Forbidden by endpoints that
    /// require the "User" policy.
    /// </summary>
    [TestCase("/api/people")]
    [TestCase("/api/stores")]
    [TestCase("/api/ingredients")]
    [TestCase("/api/recipes")]
    [TestCase("/api/pantry")]
    [TestCase("/api/combos")]
    [TestCase("/api/settings")]
    public async Task ProtectedEndpoint_AuthenticatedWithoutUserRole_ReturnsForbidden(string url)
    {
        using var client = CreateClient();
        var response = await client.GetAsync(url);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// A token with the "User" role should be accepted by standard business endpoints.
    /// </summary>
    [TestCase("/api/people")]
    [TestCase("/api/stores")]
    [TestCase("/api/ingredients")]
    [TestCase("/api/recipes")]
    [TestCase("/api/pantry")]
    [TestCase("/api/combos")]
    [TestCase("/api/settings")]
    public async Task ProtectedEndpoint_WithUserRole_DoesNotReturnForbidden(string url)
    {
        using var client = CreateClient("User");
        var response = await client.GetAsync(url);

        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// The /api/users admin endpoint requires the "Admin" role and should return 403 for
    /// a normal "User" role token.
    /// </summary>
    [Test]
    public async Task UsersEndpoint_WithUserRoleOnly_ReturnsForbidden()
    {
        using var client = CreateClient("User");
        var response = await client.GetAsync("/api/users");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    /// <summary>
    /// The /api/users admin endpoint accepts requests from tokens with the "Admin" role.
    /// </summary>
    [Test]
    public async Task UsersEndpoint_WithAdminRole_DoesNotReturnForbidden()
    {
        using var client = CreateClient("Admin");
        var response = await client.GetAsync("/api/users");

        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Forbidden));
        Assert.That(response.StatusCode, Is.Not.EqualTo(HttpStatusCode.Unauthorized));
    }

    /// <summary>
    /// The ensure-user endpoint creates a new user with the default "User" role on first call.
    /// </summary>
    [Test]
    public async Task EnsureUser_NewUser_CreatesWithDefaultUserRole()
    {
        using var client = CreateClient();
        var response = await client.PostAsync("/api/auth/ensure-user", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await response.Content.ReadFromJsonAsync<UserRolesResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Roles, Does.Contain("User"));
    }

    /// <summary>
    /// The ensure-user endpoint returns existing roles on subsequent calls.
    /// </summary>
    [Test]
    public async Task EnsureUser_ExistingUser_ReturnsSameRoles()
    {
        using var client = CreateClient();

        // First call — creates user.
        var first = await client.PostAsync("/api/auth/ensure-user", null);
        Assert.That(first.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        // Second call — returns same roles.
        var second = await client.PostAsync("/api/auth/ensure-user", null);
        Assert.That(second.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var result = await second.Content.ReadFromJsonAsync<UserRolesResponse>();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Roles, Does.Contain("User"));
    }

    /// <summary>
    /// The ensure-user endpoint only requires authentication (no specific role) so new users
    /// can self-provision.
    /// </summary>
    [Test]
    public async Task EnsureUser_Anonymous_ReturnsUnauthorized()
    {
        using var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        var response = await client.PostAsync("/api/auth/ensure-user", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
    }

    private HttpClient CreateClient(params string[] roles)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(_testUserId, roles));
        return client;
    }

    private HttpClient CreateClientWithoutUserId(params string[] roles)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", GenerateToken(appUserId: null, roles));
        return client;
    }

    private static string GenerateToken(int? appUserId, params string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "role-test-user-id"),
            new(JwtRegisteredClaimNames.Name, "Role Test User"),
            new(JwtRegisteredClaimNames.Email, "roletest@example.com"),
        };

        if (appUserId.HasValue)
        {
            claims.Add(new Claim(MealPlannerClaimTypes.AppUserId, appUserId.Value.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

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
