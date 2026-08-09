namespace MealPlanner.Contracts.Auth;

/// <summary>
/// Response returned by the ensure-user endpoint containing the authenticated user's assigned roles
/// and database identifier.
/// </summary>
/// <param name="UserId">The user's database identifier.</param>
/// <param name="Roles">The list of role names assigned to the user.</param>
public sealed record UserRolesResponse(int UserId, IReadOnlyList<string> Roles);
