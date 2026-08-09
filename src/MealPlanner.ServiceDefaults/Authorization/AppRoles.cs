namespace MealPlanner.ServiceDefaults.Authorization;

/// <summary>
/// Defines the application role names used for authorization.
/// </summary>
public static class AppRoles
{
    /// <summary>Default household member — full access to all pages and endpoints.</summary>
    public const string User = "User";

    /// <summary>Read-only guest — view dashboard, recipes, planner but no edits.</summary>
    public const string Viewer = "Viewer";

    /// <summary>Household admin — everything plus user/role management and settings.</summary>
    public const string Admin = "Admin";
}
