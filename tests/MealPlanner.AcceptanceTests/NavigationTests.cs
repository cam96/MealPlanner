namespace MealPlanner.AcceptanceTests;

/// <summary>Acceptance tests verifying navigation and page rendering.</summary>
[TestFixture]
[Category("Acceptance")]
public sealed class NavigationTests : AcceptanceTestBase
{
    [Test]
    public async Task ErrorPage_Renders()
    {
        await Page.GotoAsync($"{WebBaseUrl}/Error");
        await Page.WaitForLoadStateAsync();

        // Error page should render without crashing
        var content = await Page.ContentAsync();
        Assert.That(content, Is.Not.Empty);
    }

    [Test]
    public async Task NotFoundPage_RendersForInvalidRoute()
    {
        await Page.GotoAsync($"{WebBaseUrl}/this-page-does-not-exist");
        await Page.WaitForLoadStateAsync();

        var content = await Page.ContentAsync();
        Assert.That(content, Is.Not.Empty);
    }

    [Test]
    public async Task AllProtectedPages_RequireAuthentication()
    {
        var protectedRoutes = new[]
        {
            "/people", "/ingredients", "/prices", "/recipes",
            "/pantry", "/planner", "/shopping-list", "/settings",
        };

        foreach (var route in protectedRoutes)
        {
            await Page.GotoAsync($"{WebBaseUrl}{route}");
            await Page.WaitForLoadStateAsync();

            var url = Page.Url;
            Assert.That(url, Does.Contain("login").IgnoreCase,
                $"Route '{route}' should redirect to login when unauthenticated");
        }
    }
}
