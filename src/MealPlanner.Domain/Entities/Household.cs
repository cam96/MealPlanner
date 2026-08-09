namespace MealPlanner.Domain.Entities;

/// <summary>
/// A household groups users and their shared data (recipes, ingredients, meal plans, etc.).
/// Every user belongs to at most one household; data is scoped to the household that owns it.
/// </summary>
public class Household
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the household's display name (for example "The McKay Household").</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the identifier of the user who created and owns this household.</summary>
    public int OwnerId { get; set; }

    /// <summary>Gets or sets the owner navigation property.</summary>
    public AppUser? Owner { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the household was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets the users that belong to this household.</summary>
    public ICollection<AppUser> Members { get; } = [];

    /// <summary>Gets the invitations issued for this household.</summary>
    public ICollection<HouseholdInvite> Invites { get; } = [];
}
