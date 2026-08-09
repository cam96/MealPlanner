namespace MealPlanner.Contracts.Household;

/// <summary>An invitation to join a household.</summary>
public sealed record HouseholdInviteDto(
    int Id,
    string Token,
    string InviteUrl,
    DateTime CreatedAt,
    DateTime ExpiresAt,
    string Status,
    string? AcceptedByName);

/// <summary>Preview information shown to a user before they accept an invite.</summary>
public sealed record InvitePreviewDto(
    string HouseholdName,
    string InvitedByName,
    DateTime ExpiresAt,
    bool IsExpired,
    bool IsAlreadyAccepted);

/// <summary>Request to accept an invite and join a household.</summary>
public sealed record AcceptInviteRequest(string Token);
