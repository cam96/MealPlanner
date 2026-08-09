namespace MealPlanner.Domain.Entities;

/// <summary>
/// A single-use invitation token that allows one user to join a household. Invites expire after a
/// configurable period and can be revoked by the household owner before they are accepted.
/// </summary>
public class HouseholdInvite
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the household this invite is for.</summary>
    public int HouseholdId { get; set; }

    /// <summary>Gets or sets the household navigation property.</summary>
    public Household? Household { get; set; }

    /// <summary>Gets or sets the unique token embedded in the invite URL.</summary>
    public required string Token { get; set; }

    /// <summary>Gets or sets the identifier of the user who created the invite.</summary>
    public int CreatedByUserId { get; set; }

    /// <summary>Gets or sets the user who created the invite.</summary>
    public AppUser? CreatedByUser { get; set; }

    /// <summary>Gets or sets the identifier of the user who accepted the invite, if any.</summary>
    public int? AcceptedByUserId { get; set; }

    /// <summary>Gets or sets the user who accepted the invite.</summary>
    public AppUser? AcceptedByUser { get; set; }

    /// <summary>Gets or sets the UTC timestamp when the invite was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp after which the invite is no longer valid.</summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>Gets or sets the current status of the invite.</summary>
    public InviteStatus Status { get; set; }
}
