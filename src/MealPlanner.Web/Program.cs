using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.IdentityModel.Tokens;
using MealPlanner.Contracts.Auth;
using MealPlanner.ServiceDefaults.Authorization;
using MealPlanner.Web.Components;
using MealPlanner.Web.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Aspire cross-cutting concerns: OpenTelemetry, health checks, resilience, service discovery.
builder.AddServiceDefaults();

// In production the Web container sits behind Caddy (TLS termination). Caddy's reverse_proxy
// sends X-Forwarded-For/Proto/Host automatically. This middleware reads those headers so
// HttpContext.Request.Scheme reports "https" — critical for generating correct OAuth redirect URIs.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                             | ForwardedHeaders.XForwardedProto
                             | ForwardedHeaders.XForwardedHost;
    // The web container is only reachable on the internal Docker network (no published ports),
    // so trusting all forwarded headers is safe — only Caddy can reach it.
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

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
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.LogoutPath = "/auth/logout";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
        options.SlidingExpiration = true;
    })
    .AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;

        // After Google authenticates the user, call the API to ensure the user exists in the DB
        // and retrieve their assigned roles. Add the roles as claims to the cookie identity.
        options.Events.OnCreatingTicket = async context =>
        {
            var identity = (ClaimsIdentity?)context.Principal?.Identity;
            if (identity is null) return;

            var settings = context.HttpContext.RequestServices.GetRequiredService<JwtTokenSettings>();
            var httpClientFactory = context.HttpContext.RequestServices.GetRequiredService<IHttpClientFactory>();

            // Generate a temporary JWT from the Google claims so the API can authenticate this call.
            var tempToken = GenerateTemporaryJwt(identity, settings);

            try
            {
                var client = httpClientFactory.CreateClient("RoleResolution");
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tempToken);

                var response = await client.PostAsync("/api/auth/ensure-user", null);
                if (response.IsSuccessStatusCode)
                {
                    var rolesResponse = await response.Content.ReadFromJsonAsync<UserRolesResponse>();
                    if (rolesResponse?.Roles is not null)
                    {
                        foreach (var role in rolesResponse.Roles)
                        {
                            identity.AddClaim(new Claim(ClaimTypes.Role, role));
                        }

                        if (rolesResponse.HouseholdId is not null)
                        {
                            identity.AddClaim(new Claim("HouseholdId", rolesResponse.HouseholdId.Value.ToString()));
                        }

                        return;
                    }
                }
            }
            catch
            {
                // Graceful degradation: if the API is unreachable, assign the default role.
            }

            // Fallback: assign the default User role.
            identity.AddClaim(new Claim(ClaimTypes.Role, AppRoles.User));
        };
    });

builder.Services.AddMealPlannerAuthorization();
builder.Services.AddCascadingAuthenticationState();

// JWT signing settings for authenticating outbound API calls.
var jwtKey = builder.Configuration["Authentication:Jwt:Key"]
    ?? throw new InvalidOperationException("Authentication:Jwt:Key must be configured.");
builder.Services.AddSingleton(new JwtTokenSettings(
    Key: new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
    Issuer: "MealPlanner.Web",
    Audience: "MealPlanner.Api",
    Lifetime: TimeSpan.FromHours(1)));

// IHttpContextAccessor is needed by JwtAuthorizationHandler to read the current user's claims.
builder.Services.AddHttpContextAccessor();

// Typed HTTP client to the API, resolved by Aspire service discovery ("api").
// The JwtAuthorizationHandler attaches a Bearer token to every outbound request.
builder.Services.AddTransient<JwtAuthorizationHandler>();
builder.Services.AddHttpClient<MealPlannerApiClient>(client =>
    client.BaseAddress = new Uri("https+http://api"))
    .AddHttpMessageHandler<JwtAuthorizationHandler>();

// Separate named HTTP client used only during OAuth login to resolve user roles.
// Does NOT use JwtAuthorizationHandler (user isn't fully authenticated yet at that point).
builder.Services.AddHttpClient("RoleResolution", client =>
    client.BaseAddress = new Uri("https+http://api"));

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseForwardedHeaders();

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

// Generates a short-lived JWT from the given identity for use during the OAuth login flow.
// This token contains only the basic identity claims (sub, name, email) — no roles — and is used
// to authenticate the call to the API's ensure-user endpoint.
static string GenerateTemporaryJwt(ClaimsIdentity identity, JwtTokenSettings settings)
{
    var now = DateTime.UtcNow;
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, identity.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? ""),
        new Claim(JwtRegisteredClaimNames.Name, identity.FindFirst(ClaimTypes.Name)?.Value ?? ""),
        new Claim(JwtRegisteredClaimNames.Email, identity.FindFirst(ClaimTypes.Email)?.Value ?? ""),
    };

    var credentials = new SigningCredentials(settings.Key, SecurityAlgorithms.HmacSha256);
    var token = new JwtSecurityToken(
        issuer: settings.Issuer,
        audience: settings.Audience,
        claims: claims,
        notBefore: now,
        expires: now.AddMinutes(5),
        signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
}
