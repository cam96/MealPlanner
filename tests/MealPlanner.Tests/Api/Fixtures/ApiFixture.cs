using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MealPlanner.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using MealPlanner.ServiceDefaults.Authorization;

namespace MealPlanner.Tests.Api.Fixtures;

/// <summary>
/// Shared test fixture that provides a <see cref="WebApplicationFactory{TEntryPoint}"/>
/// configured with in-memory SQLite and a test JWT signing key.
/// </summary>
public sealed class ApiFixture : IDisposable
{
    /// <summary>Symmetric key used to sign test JWT tokens.</summary>
    public const string JwtKey = "IntegrationTestSigningKeyThatIsLongEnoughForHmacSha256!!";

    /// <summary>
    /// JSON serializer options matching the API's configuration (string enum converter).
    /// Use this when deserializing API responses.
    /// </summary>
    public static JsonSerializerOptions JsonOptions { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private const string Issuer = "MealPlanner.Web";
    private const string Audience = "MealPlanner.Api";

    private readonly SqliteConnection _connection;
    private readonly WebApplicationFactory<Program> _factory;

    public ApiFixture()
    {
        // Keep an open connection so the in-memory database survives across requests.
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Jwt:Key", JwtKey);
                builder.UseSetting("MealPlanner:SeedDemoData", "false");
                // Use a dummy connection string so AddMealPlannerData doesn't fail;
                // we override the actual DbContext registration below.
                builder.UseSetting("ConnectionStrings:mealplanner", "Data Source=:memory:");

                builder.ConfigureServices(services =>
                {
                    // Remove the existing DbContext registration and replace with
                    // our shared in-memory connection.
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<MealPlannerDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<MealPlannerDbContext>(options =>
                        options.UseSqlite(_connection));
                });
            });
    }

    /// <summary>Creates an unauthenticated <see cref="HttpClient"/>.</summary>
    public HttpClient CreateAnonymousClient()
    {
        return _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    /// <summary>Creates an <see cref="HttpClient"/> with a valid JWT Bearer token.</summary>
    /// <param name="roles">Roles to include in the token. Defaults to <see cref="AppRoles.User"/>.</param>
    public HttpClient CreateAuthenticatedClient(params string[] roles)
    {
        if (roles.Length == 0)
        {
            roles = [AppRoles.User];
        }

        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var token = GenerateToken(roles);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    /// <summary>Creates an <see cref="HttpClient"/> with the Admin role.</summary>
    public HttpClient CreateAdminClient()
    {
        return CreateAuthenticatedClient(AppRoles.Admin);
    }

    /// <summary>Generates a signed JWT with the specified roles.</summary>
    private static string GenerateToken(string[] roles)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(JwtKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, "test-user-id"),
            new(JwtRegisteredClaimNames.Name, "Test User"),
            new(JwtRegisteredClaimNames.Email, "test@example.com"),
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: Issuer,
            audience: Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public void Dispose()
    {
        _factory.Dispose();
        _connection.Dispose();
    }
}
