namespace MealPlanner.Contracts.Stores;

/// <summary>A grocery store.</summary>
/// <param name="Id">The store's unique identifier.</param>
/// <param name="Name">The store's display name.</param>
public record StoreDto(int Id, string Name);

/// <summary>Payload to create or update a store.</summary>
/// <param name="Name">The store's display name.</param>
public record SaveStoreRequest(string Name);
