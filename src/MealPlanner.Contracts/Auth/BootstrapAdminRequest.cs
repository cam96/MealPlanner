namespace MealPlanner.Contracts.Auth;

/// <summary>
/// Request body for the admin bootstrap endpoint. Specifies the email address that should
/// receive the Admin role on first login.
/// </summary>
/// <param name="Email">The Google account email to promote to admin.</param>
public sealed record BootstrapAdminRequest(string Email);
