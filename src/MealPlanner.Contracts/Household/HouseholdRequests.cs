namespace MealPlanner.Contracts.Household;

/// <summary>Request to create a new household.</summary>
public sealed record CreateHouseholdRequest(string Name);

/// <summary>Request to update an existing household's name.</summary>
public sealed record UpdateHouseholdRequest(string Name);
