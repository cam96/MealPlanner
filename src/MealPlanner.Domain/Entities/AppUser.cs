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

    /// <summary>Gets the meal plans owned by this user.</summary>
    public ICollection<MealPlan> MealPlans { get; } = [];

    /// <summary>Gets the household members defined by this user.</summary>
    public ICollection<Person> People { get; } = [];

    /// <summary>Gets the pantry items owned by this user.</summary>
    public ICollection<PantryItem> PantryItems { get; } = [];

    /// <summary>Gets the manual shopping items created by this user.</summary>
    public ICollection<ManualShoppingItem> ManualShoppingItems { get; } = [];

    /// <summary>Gets the generated cart entries for this user.</summary>
    public ICollection<GeneratedItemCartEntry> GeneratedItemCartEntries { get; } = [];
}
