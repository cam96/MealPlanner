extern alias WebAssembly;

using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;
using WebAssembly::MealPlanner.Web.Services;

namespace MealPlanner.Tests.Auth;

/// <summary>Tests for <see cref="JwtTokenService"/>.</summary>
[TestFixture]
public sealed class JwtTokenServiceTests
{
    private const string TestKey = "ThisIsATestSigningKeyThatIsLongEnoughForHmacSha256!";
    private const string Issuer = "MealPlanner.Web";
    private const string Audience = "MealPlanner.Api";

    private JwtTokenSettings _settings = null!;

    [SetUp]
    public void SetUp()
    {
        _settings = new JwtTokenSettings(
            Key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestKey)),
            Issuer: Issuer,
            Audience: Audience,
            Lifetime: TimeSpan.FromHours(1));
    }

    [Test]
    public async Task GetTokenAsync_AuthenticatedUser_ReturnsValidJwt()
    {
        // Arrange
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "google-123"),
            new Claim(ClaimTypes.Name, "Test User"),
            new Claim(ClaimTypes.Email, "test@example.com"),
        };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        var provider = new FakeAuthStateProvider(authState);

        var service = new JwtTokenService(provider, _settings);

        // Act
        var token = await service.GetTokenAsync();

        // Assert
        Assert.That(token, Is.Not.Null);

        var handler = new JwtSecurityTokenHandler();
        var validationParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = _settings.Key,
        };

        var result = await handler.ValidateTokenAsync(token, validationParams);
        Assert.That(result.IsValid, Is.True);

        // Verify claims are present (claim names may use URI-format keys in the validation result).
        var claimsIdentity = result.ClaimsIdentity;
        Assert.That(claimsIdentity.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? claimsIdentity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value,
            Is.EqualTo("test@example.com"));
        Assert.That(claimsIdentity.FindFirst(JwtRegisteredClaimNames.Name)?.Value
            ?? claimsIdentity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value,
            Is.EqualTo("Test User"));
    }

    [Test]
    public async Task GetTokenAsync_UnauthenticatedUser_ReturnsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = new AuthenticationState(principal);
        var provider = new FakeAuthStateProvider(authState);

        var service = new JwtTokenService(provider, _settings);

        // Act
        var token = await service.GetTokenAsync();

        // Assert
        Assert.That(token, Is.Null);
    }

    [Test]
    public async Task GetTokenAsync_CachesTokenOnSubsequentCalls()
    {
        // Arrange
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, "user-1") };
        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);
        var authState = new AuthenticationState(principal);
        var provider = new FakeAuthStateProvider(authState);

        var service = new JwtTokenService(provider, _settings);

        // Act
        var token1 = await service.GetTokenAsync();
        var token2 = await service.GetTokenAsync();

        // Assert — same instance means it was cached
        Assert.That(token2, Is.SameAs(token1));
    }

    [Test]
    public void Constructor_NullAuthStateProvider_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(null!, _settings));
    }

    [Test]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        var provider = new FakeAuthStateProvider(
            new AuthenticationState(new ClaimsPrincipal()));
        Assert.Throws<ArgumentNullException>(() => new JwtTokenService(provider, null!));
    }

    /// <summary>Simple fake to supply a fixed authentication state in tests.</summary>
    private sealed class FakeAuthStateProvider(AuthenticationState state) : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(state);
    }
}
