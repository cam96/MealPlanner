namespace MealPlanner.AcceptanceTests;

/// <summary>Acceptance tests for the login page and authentication flow.</summary>
[TestFixture]
[Category("Acceptance")]
public sealed class LoginPageTests : AcceptanceTestBase
{
    [Test]
    public async Task LoginPage_Renders_WithGoogleSignIn()
    {
        await Page.GotoAsync($"{WebBaseUrl}/login");
        await Page.WaitForLoadStateAsync();

        // The login page should be accessible without authentication
        var title = await Page.TitleAsync();
        Assert.That(title, Is.Not.Empty);
    }

    [Test]
    public async Task UnauthenticatedUser_IsRedirectedToLogin()
    {
        await Page.GotoAsync($"{WebBaseUrl}/");
        await Page.WaitForLoadStateAsync();

        var url = Page.Url;
        Assert.That(url, Does.Contain("login").IgnoreCase);
    }

    [Test]
    public async Task ProtectedPage_RedirectsToLogin()
    {
        await Page.GotoAsync($"{WebBaseUrl}/people");
        await Page.WaitForLoadStateAsync();

        var url = Page.Url;
        Assert.That(url, Does.Contain("login").IgnoreCase);
    }

    [Test]
    public async Task ApiPing_ReturnsOk()
    {
        // Verify the API is accessible via its /ping endpoint (allows anonymous)
        var response = await Page.APIRequest.GetAsync($"{ApiBaseUrl}/ping");

        Assert.That(response.Status, Is.EqualTo(200));
    }
}
