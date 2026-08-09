extern alias WebAssembly;

using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

using WebProgram = WebAssembly::Program;

namespace MealPlanner.Tests.Auth;

/// <summary>
/// Integration tests verifying the login page workflow: unauthenticated users see the
/// login splash page before being redirected to Google OAuth.
/// </summary>
[TestFixture]
public sealed class LoginWorkflowTests
{
    private WebApplicationFactory<WebProgram> _factory = null!;
    private HttpClient _client = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _factory = new WebApplicationFactory<WebProgram>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Authentication:Google:ClientId", "fake-client-id");
                builder.UseSetting("Authentication:Google:ClientSecret", "fake-client-secret");
                builder.UseSetting("Authentication:Jwt:Key",
                    "TestSigningKeyThatIsLongEnoughForHmacSha256!!");
            });

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    /// <summary>
    /// The login page must be accessible without authentication so users can see
    /// the splash page explaining Google sign-in.
    /// </summary>
    [Test]
    public async Task LoginPage_Anonymous_ReturnsSuccess()
    {
        var response = await _client.GetAsync("/login");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }

    /// <summary>
    /// Unauthenticated requests to protected pages must redirect to the login page,
    /// not directly to Google OAuth. This ensures users see the splash page first.
    /// </summary>
    [Test]
    public async Task ProtectedPage_Anonymous_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.AbsolutePath, Is.EqualTo("/login"));
    }

    /// <summary>
    /// The /auth/login endpoint must challenge with Google OAuth, redirecting the user
    /// to Google's authorization endpoint.
    /// </summary>
    [Test]
    public async Task AuthLogin_Anonymous_RedirectsToGoogle()
    {
        var response = await _client.GetAsync("/auth/login");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.Host, Is.EqualTo("accounts.google.com"));
    }

    /// <summary>
    /// The /auth/logout endpoint must redirect to the login page after signing out.
    /// </summary>
    [Test]
    public async Task AuthLogout_RedirectsToLogin()
    {
        var response = await _client.GetAsync("/auth/logout");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        Assert.That(response.Headers.Location?.OriginalString, Is.EqualTo("/login"));
    }
}
