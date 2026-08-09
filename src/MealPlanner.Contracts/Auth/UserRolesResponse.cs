namespace MealPlanner.Contracts.Auth;

/// <summary>
/// Response returned by the ensure-user endpoint containing the authenticated user's assigned roles.
/// </summary>
/// <param name="Roles">The list of role names assigned to the user.</param>
public sealed record UserRolesResponse(IReadOnlyList<string> Roles);
