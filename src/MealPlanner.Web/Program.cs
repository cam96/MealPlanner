using MealPlanner.Web.Components;
using MealPlanner.Web.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire cross-cutting concerns: OpenTelemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

// MudBlazor UI services.
builder.Services.AddMudServices();

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Typed HTTP client to the API, resolved by Aspire service discovery ("api").
builder.Services.AddHttpClient<MealPlannerApiClient>(client =>
    client.BaseAddress = new Uri("https+http://api"));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAntiforgery();

// Aspire default endpoints (health/liveness).
app.MapDefaultEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
