namespace MealPlanner.Contracts.Auth;

/// <summary>
/// Request body for updating a user's role assignments.
/// </summary>
/// <param name="Roles">The complete set of role names to assign to the user.</param>
public sealed record UpdateUserRolesRequest(IReadOnlyList<string> Roles);
