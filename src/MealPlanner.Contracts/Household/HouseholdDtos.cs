namespace MealPlanner.Contracts.Household;

/// <summary>Full household information including its members.</summary>
public sealed record HouseholdDto(
    int Id,
    string Name,
    int OwnerId,
    string OwnerName,
    IReadOnlyList<HouseholdMemberDto> Members);

/// <summary>A user who belongs to a household.</summary>
public sealed record HouseholdMemberDto(
    int UserId,
    string Name,
    string Email,
    bool IsOwner);

/// <summary>Lightweight household info for display in navigation or headers.</summary>
public sealed record HouseholdSummaryDto(
    int Id,
    string Name);
