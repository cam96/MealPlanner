using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.IdentityModel.Tokens;

namespace MealPlanner.Web.Services;

/// <summary>
/// Generates JWT tokens for authenticating outbound API calls. The token is created from the
/// current user's authentication state (populated by the Google OAuth cookie) and signed with
/// the shared HMAC key that the API validates. Tokens expire after 30 minutes of inactivity;
/// each API call resets the inactivity window by generating a fresh token.
/// </summary>
public sealed class JwtTokenService
{
    private readonly AuthenticationStateProvider _authStateProvider;
    private readonly JwtTokenSettings _settings;

    private string? _cachedToken;
    private DateTime _cacheExpiry;
    private DateTime _lastActivity;

    /// <summary>Initializes a new instance of the <see cref="JwtTokenService"/> class.</summary>
    /// <param name="authStateProvider">Provides the current user's authentication state.</param>
    /// <param name="settings">JWT signing configuration.</param>
    public JwtTokenService(AuthenticationStateProvider authStateProvider, JwtTokenSettings settings)
    {
        ArgumentNullException.ThrowIfNull(authStateProvider);
        ArgumentNullException.ThrowIfNull(settings);

        _authStateProvider = authStateProvider;
        _settings = settings;
        _lastActivity = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a valid JWT for the currently authenticated user. Returns <see langword="null"/> if
    /// the user is not authenticated or the session has been idle longer than the configured
    /// token lifetime. Each successful call resets the inactivity timer.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The Bearer token string, or <see langword="null"/> if unauthenticated or expired due to inactivity.</returns>
    public async Task<string?> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        // If the user has been inactive longer than the token lifetime, force re-authentication.
        if (DateTime.UtcNow - _lastActivity > _settings.Lifetime)
        {
            _cachedToken = null;
            return null;
        }

        // Record activity — resets the inactivity window.
        _lastActivity = DateTime.UtcNow;

        if (_cachedToken is not null && DateTime.UtcNow < _cacheExpiry)
        {
            return _cachedToken;
        }

        var authState = await _authStateProvider.GetAuthenticationStateAsync();
        var user = authState.User;

        if (user.Identity is not { IsAuthenticated: true })
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var expires = now.Add(_settings.Lifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.FindFirstValue(ClaimTypes.NameIdentifier) ?? ""),
            new(JwtRegisteredClaimNames.Name, user.FindFirstValue(ClaimTypes.Name) ?? ""),
            new(JwtRegisteredClaimNames.Email, user.FindFirstValue(ClaimTypes.Email) ?? ""),
        };

        var credentials = new SigningCredentials(_settings.Key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: credentials);

        _cachedToken = new JwtSecurityTokenHandler().WriteToken(token);
        _cacheExpiry = expires.AddMinutes(-5);

        return _cachedToken;
    }

    /// <summary>
    /// Revokes the current cached token, forcing a new one to be generated on the next API call.
    /// Called on logout to ensure the token cannot be reused.
    /// </summary>
    public void Revoke()
    {
        _cachedToken = null;
        _cacheExpiry = DateTime.MinValue;
    }
}
