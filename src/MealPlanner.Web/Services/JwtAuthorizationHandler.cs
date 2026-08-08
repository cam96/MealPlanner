using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace MealPlanner.Web.Services;

/// <summary>
/// A delegating handler that attaches the JWT Bearer token to outbound HTTP requests to the API.
/// Reads the current user's claims from <see cref="IHttpContextAccessor"/> (available in Blazor
/// Server) and generates a signed JWT for the API.
/// </summary>
public sealed class JwtAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtTokenSettings _settings;

    /// <summary>Initializes a new instance of the <see cref="JwtAuthorizationHandler"/> class.</summary>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    /// <param name="settings">JWT signing configuration.</param>
    public JwtAuthorizationHandler(IHttpContextAccessor httpContextAccessor, JwtTokenSettings settings)
    {
        ArgumentNullException.ThrowIfNull(httpContextAccessor);
        ArgumentNullException.ThrowIfNull(settings);

        _httpContextAccessor = httpContextAccessor;
        _settings = settings;
    }

    /// <inheritdoc/>
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var user = _httpContextAccessor.HttpContext?.User;

        if (user?.Identity is { IsAuthenticated: true })
        {
            var token = GenerateToken(user);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private string GenerateToken(ClaimsPrincipal user)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(_settings.Lifetime);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""),
            new Claim(JwtRegisteredClaimNames.Name, user.FindFirstValue(ClaimTypes.Name) ?? ""),
            new Claim(JwtRegisteredClaimNames.Email, user.FindFirstValue(ClaimTypes.Email) ?? ""),
        };

        var credentials = new SigningCredentials(_settings.Key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
