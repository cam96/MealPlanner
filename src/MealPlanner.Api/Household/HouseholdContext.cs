using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Household;

/// <summary>
/// Resolves the current user's identity and household membership from JWT claims. This avoids
/// redundant DB lookups by caching results per request via DI scoping.
/// </summary>
public sealed class HouseholdContext
{
    private readonly MealPlannerDbContext _db;
    private readonly ClaimsPrincipal _user;
    private AppUser? _appUser;
    private bool _resolved;

    /// <summary>Initializes a new instance of the <see cref="HouseholdContext"/> class.</summary>
    /// <param name="db">The database context.</param>
    /// <param name="httpContextAccessor">Provides access to the current HTTP context.</param>
    public HouseholdContext(MealPlannerDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        ArgumentNullException.ThrowIfNull(db);
        ArgumentNullException.ThrowIfNull(httpContextAccessor);

        _db = db;
        _user = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    }

    /// <summary>
    /// Gets the current authenticated user's <see cref="AppUser"/> record, or null if not found.
    /// </summary>
    public async Task<AppUser?> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        if (!_resolved)
        {
            var googleId = _user.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? _user.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? "";

            if (!string.IsNullOrEmpty(googleId))
            {
                _appUser = await _db.AppUsers
                    .FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);
            }

            _resolved = true;
        }

        return _appUser;
    }

    /// <summary>
    /// Gets the current user's household ID, or null if the user has no household.
    /// </summary>
    public async Task<int?> GetHouseholdIdAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentUserAsync(cancellationToken);
        return user?.HouseholdId;
    }

    /// <summary>
    /// Gets the current user's household ID. Returns a <see cref="IResult"/> 403 response if the
    /// user does not belong to a household.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The household ID, or -1 with an error result if the user has no household.</returns>
    public async Task<(int HouseholdId, IResult? Error)> RequireHouseholdAsync(
        CancellationToken cancellationToken = default)
    {
        var householdId = await GetHouseholdIdAsync(cancellationToken);
        if (householdId is null)
        {
            return (-1, Results.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "No household",
                detail: "You must create or join a household before accessing this resource."));
        }

        return (householdId.Value, null);
    }
}
