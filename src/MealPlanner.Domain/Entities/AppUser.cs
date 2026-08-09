namespace MealPlanner.Domain.Entities;

/// <summary>
/// An authenticated application user, provisioned automatically on first Google login.
/// </summary>
public class AppUser
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the user's Google account identifier (subject claim).</summary>
    public required string GoogleId { get; set; }

    /// <summary>Gets or sets the user's email address.</summary>
    public required string Email { get; set; }

    /// <summary>Gets or sets the user's display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the user record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the user's most recent login.</summary>
    public DateTime LastLoginAt { get; set; }

    /// <summary>Gets the collection of roles assigned to this user.</summary>
    public ICollection<AppUserRole> UserRoles { get; set; } = [];
}
