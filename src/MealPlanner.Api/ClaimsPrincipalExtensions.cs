using System.Security.Claims;
using MealPlanner.ServiceDefaults.Authorization;

namespace MealPlanner.Api;

/// <summary>
/// Extension methods for <see cref="ClaimsPrincipal"/> to extract MealPlanner-specific claims.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    /// <summary>
    /// Gets the authenticated user's database identifier from the <see cref="MealPlannerClaimTypes.AppUserId"/>
    /// claim. Throws if the claim is missing or invalid.
    /// </summary>
    /// <param name="user">The current claims principal.</param>
    /// <returns>The <c>AppUser.Id</c> value.</returns>
    /// <exception cref="UnauthorizedAccessException">Thrown when the claim is missing or not a valid integer.</exception>
    public static int GetAppUserId(this ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var value = user.FindFirstValue(MealPlannerClaimTypes.AppUserId);

        if (string.IsNullOrEmpty(value) || !int.TryParse(value, out var userId))
        {
            throw new UnauthorizedAccessException(
                "The app_user_id claim is missing or invalid. Re-authenticate to obtain a valid token.");
        }

        return userId;
    }
}
