using Microsoft.IdentityModel.Tokens;

namespace MealPlanner.Web.Services;

/// <summary>
/// Immutable settings used by <see cref="JwtAuthorizationHandler"/> to sign tokens for API calls.
/// </summary>
/// <param name="Key">The symmetric signing key shared with the API.</param>
/// <param name="Issuer">The token issuer claim (this Web service).</param>
/// <param name="Audience">The token audience claim (the API service).</param>
/// <param name="Lifetime">How long issued tokens remain valid.</param>
public sealed record JwtTokenSettings(
    SymmetricSecurityKey Key,
    string Issuer,
    string Audience,
    TimeSpan Lifetime);
