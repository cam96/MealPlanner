namespace MealPlanner.Domain.Entities;

/// <summary>
/// A role assignment for an <see cref="AppUser"/>.
/// </summary>
public class AppUserRole
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the owning user's foreign key.</summary>
    public int AppUserId { get; set; }

    /// <summary>Gets or sets the navigation property to the owning user.</summary>
    public AppUser AppUser { get; set; } = null!;

    /// <summary>Gets or sets the role name (e.g. "User", "Admin").</summary>
    public required string Role { get; set; }
}
