namespace MealPlanner.Contracts.Auth;

/// <summary>
/// DTO representing an application user and their assigned roles.
/// </summary>
/// <param name="Id">The user's database identifier.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Roles">The list of role names assigned to the user.</param>
/// <param name="CreatedAt">UTC timestamp of when the user was first provisioned.</param>
/// <param name="LastLoginAt">UTC timestamp of the user's most recent login.</param>
public sealed record AppUserDto(
    int Id,
    string Email,
    string Name,
    IReadOnlyList<string> Roles,
    DateTime CreatedAt,
    DateTime LastLoginAt);
