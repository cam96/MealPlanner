using Microsoft.Playwright;

namespace MealPlanner.AcceptanceTests;

/// <summary>Base class for acceptance tests providing Playwright browser and page instances.</summary>
[TestFixture]
[Category("Acceptance")]
public abstract class AcceptanceTestBase
{
    private static AppFixture? _appFixture;
    private static IPlaywright? _playwright;
    private static IBrowser? _browser;

    /// <summary>The Playwright page instance for the current test.</summary>
    protected IPage Page { get; private set; } = null!;

    /// <summary>The base URL of the Web application.</summary>
    protected static string WebBaseUrl => _appFixture?.WebBaseUrl ?? string.Empty;

    /// <summary>The base URL of the API.</summary>
    protected static string ApiBaseUrl => _appFixture?.ApiBaseUrl ?? string.Empty;

    [OneTimeSetUp]
    public async Task GlobalSetUp()
    {
        // Start the distributed application
        _appFixture = new AppFixture();
        await _appFixture.StartAsync();

        // Set up Playwright
        _playwright = await Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    [SetUp]
    public async Task SetUp()
    {
        Page = await _browser!.NewPageAsync();
    }

    [TearDown]
    public async Task TearDown()
    {
        await Page.CloseAsync();
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        if (_browser is not null)
        {
            await _browser.CloseAsync();
        }

        _playwright?.Dispose();

        if (_appFixture is not null)
        {
            await _appFixture.DisposeAsync();
        }
    }
}
