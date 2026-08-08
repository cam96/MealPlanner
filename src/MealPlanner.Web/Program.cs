using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
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

// Authentication: cookie session (for the Blazor circuit) backed by Google OAuth.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"]
    ?? throw new InvalidOperationException("Authentication:Google:ClientId must be configured.");
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
    ?? throw new InvalidOperationException("Authentication:Google:ClientSecret must be configured.");

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = GoogleDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();

// JWT token service for authenticating outbound API calls.
var jwtKey = builder.Configuration["Authentication:Jwt:Key"]
    ?? throw new InvalidOperationException("Authentication:Jwt:Key must be configured.");
builder.Services.AddSingleton(new JwtTokenSettings(
    Key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    Issuer: "MealPlanner.Web",
    Audience: "MealPlanner.Api",
    Lifetime: TimeSpan.FromMinutes(30)));
builder.Services.AddScoped<JwtTokenService>();

// Typed HTTP client to the API, resolved by Aspire service discovery ("api").
// The JwtAuthorizationHandler attaches a Bearer token to every outbound request.
builder.Services.AddTransient<JwtAuthorizationHandler>();
builder.Services.AddHttpClient<MealPlannerApiClient>(client =>
    client.BaseAddress = new Uri("https+http://api"))
    .AddHttpMessageHandler<JwtAuthorizationHandler>();

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

app.UseAuthentication();
app.UseAuthorization();

app.UseAntiforgery();

// Authentication endpoints — these must be server-side HTTP endpoints (not Blazor components)
// because the OAuth redirect flow requires real HTTP redirects.
app.MapGet("/auth/login", (string? returnUrl) =>
    Results.Challenge(new AuthenticationProperties { RedirectUri = returnUrl ?? "/" },
        [GoogleDefaults.AuthenticationScheme]))
    .AllowAnonymous();

app.MapGet("/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    return Results.Redirect("/login");
}).AllowAnonymous();

// Aspire default endpoints (health/liveness).
app.MapDefaultEndpoints();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
