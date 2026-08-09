namespace MealPlanner.Domain.Entities;

/// <summary>The lifecycle status of a <see cref="HouseholdInvite"/>.</summary>
public enum InviteStatus
{
    /// <summary>The invite has been created but not yet accepted or revoked.</summary>
    Pending = 0,

    /// <summary>The invite was accepted by a user who joined the household.</summary>
    Accepted = 1,

    /// <summary>The invite was revoked by the household owner before being accepted.</summary>
    Revoked = 2,
}
