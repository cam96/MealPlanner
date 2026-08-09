namespace MealPlanner.Contracts.Auth;

/// <summary>
/// Response returned by the ensure-user endpoint containing the authenticated user's assigned roles
/// and household membership.
/// </summary>
/// <param name="Roles">The list of role names assigned to the user.</param>
/// <param name="HouseholdId">The user's current household identifier, or null if unaffiliated.</param>
public sealed record UserRolesResponse(IReadOnlyList<string> Roles, int? HouseholdId);
