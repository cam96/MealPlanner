using Aspire.Hosting;
using Aspire.Hosting.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.AcceptanceTests;

/// <summary>
/// Starts the full MealPlanner distributed application (Api + Web) via Aspire for acceptance testing.
/// Provides the Web app URL for Playwright browser tests.
/// </summary>
public sealed class AppFixture : IAsyncDisposable
{
    private DistributedApplication? _app;

    /// <summary>The base URL of the Web application.</summary>
    public string WebBaseUrl { get; private set; } = string.Empty;

    /// <summary>The base URL of the API.</summary>
    public string ApiBaseUrl { get; private set; } = string.Empty;

    /// <summary>Starts the distributed application and waits for resources to become healthy.</summary>
    public async Task StartAsync()
    {
        var builder = await DistributedApplicationTestingBuilder
            .CreateAsync<Projects.MealPlanner_AppHost>();

        builder.Services.ConfigureHttpClientDefaults(clientBuilder =>
            clientBuilder.AddStandardResilienceHandler());

        _app = await builder.BuildAsync();

        await _app.StartAsync();

        // Wait for the web resource to be healthy (includes API since web WaitsFor api)
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        await _app.ResourceNotifications
            .WaitForResourceHealthyAsync("web", cts.Token);

        WebBaseUrl = _app.GetEndpoint("web").ToString();
        ApiBaseUrl = _app.GetEndpoint("api").ToString();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }
}
