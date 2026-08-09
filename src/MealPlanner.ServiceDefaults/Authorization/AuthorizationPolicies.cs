namespace MealPlanner.ServiceDefaults.Authorization;

/// <summary>
/// Defines named authorization policy identifiers used by both the API and Web projects.
/// </summary>
public static class AuthorizationPolicies
{
    /// <summary>Requires the <see cref="AppRoles.User"/> role (default household member access).</summary>
    public const string User = "User";

    /// <summary>Requires the <see cref="AppRoles.Viewer"/> role or higher.</summary>
    public const string Viewer = "Viewer";

    /// <summary>Requires the <see cref="AppRoles.Admin"/> role.</summary>
    public const string Admin = "Admin";
}
