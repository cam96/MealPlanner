extern alias WebAssembly;

using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using WebAssembly::MealPlanner.Web.Services;

namespace MealPlanner.Tests.Auth;

/// <summary>Tests for <see cref="JwtAuthorizationHandler"/>.</summary>
[TestFixture]
public sealed class JwtAuthorizationHandlerTests
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
    public async Task SendAsync_AuthenticatedUser_AttachesValidBearerToken()
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

        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new FakeHttpContextAccessor(httpContext);

        var handler = new JwtAuthorizationHandler(accessor, _settings)
        {
            InnerHandler = new FakeInnerHandler(),
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        var response = await client.GetAsync("/test");

        // Assert — validate the token that was attached
        var authHeader = ((FakeInnerHandler)handler.InnerHandler).LastRequest?.Headers.Authorization;
        Assert.That(authHeader, Is.Not.Null);
        Assert.That(authHeader!.Scheme, Is.EqualTo("Bearer"));

        var tokenHandler = new JwtSecurityTokenHandler();
        var result = await tokenHandler.ValidateTokenAsync(authHeader.Parameter!, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Issuer,
            ValidAudience = Audience,
            IssuerSigningKey = _settings.Key,
        });

        Assert.That(result.IsValid, Is.True);

        var claimsIdentity = result.ClaimsIdentity;
        Assert.That(claimsIdentity.FindFirst(JwtRegisteredClaimNames.Email)?.Value
            ?? claimsIdentity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")?.Value,
            Is.EqualTo("test@example.com"));
        Assert.That(claimsIdentity.FindFirst(JwtRegisteredClaimNames.Name)?.Value
            ?? claimsIdentity.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name")?.Value,
            Is.EqualTo("Test User"));
    }

    [Test]
    public async Task SendAsync_UnauthenticatedUser_DoesNotAttachHeader()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity());
        var httpContext = new DefaultHttpContext { User = principal };
        var accessor = new FakeHttpContextAccessor(httpContext);

        var handler = new JwtAuthorizationHandler(accessor, _settings)
        {
            InnerHandler = new FakeInnerHandler(),
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        await client.GetAsync("/test");

        // Assert
        var authHeader = ((FakeInnerHandler)handler.InnerHandler).LastRequest?.Headers.Authorization;
        Assert.That(authHeader, Is.Null);
    }

    [Test]
    public async Task SendAsync_NoHttpContext_DoesNotAttachHeader()
    {
        // Arrange
        var accessor = new FakeHttpContextAccessor(null);

        var handler = new JwtAuthorizationHandler(accessor, _settings)
        {
            InnerHandler = new FakeInnerHandler(),
        };

        var client = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        // Act
        await client.GetAsync("/test");

        // Assert
        var authHeader = ((FakeInnerHandler)handler.InnerHandler).LastRequest?.Headers.Authorization;
        Assert.That(authHeader, Is.Null);
    }

    [Test]
    public void Constructor_NullHttpContextAccessor_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new JwtAuthorizationHandler(null!, _settings));
    }

    [Test]
    public void Constructor_NullSettings_ThrowsArgumentNullException()
    {
        var accessor = new FakeHttpContextAccessor(null);
        Assert.Throws<ArgumentNullException>(() => new JwtAuthorizationHandler(accessor, null!));
    }

    private sealed class FakeHttpContextAccessor(HttpContext? context) : IHttpContextAccessor
    {
        public HttpContext? HttpContext { get; set; } = context;
    }

    private sealed class FakeInnerHandler : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        }
    }
}
