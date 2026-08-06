namespace MealPlanner.Contracts.Prices;

/// <summary>A price observation for an ingredient at a store on a date.</summary>
/// <param name="Id">The price record's unique identifier.</param>
/// <param name="IngredientId">The ingredient the price applies to.</param>
/// <param name="StoreId">The store the price was observed at.</param>
/// <param name="StoreName">The store's display name (for presentation).</param>
/// <param name="Price">The price paid for the package, in Canadian dollars.</param>
/// <param name="PackageQuantity">The quantity contained in the priced package.</param>
/// <param name="PackageUnit">The unit of <paramref name="PackageQuantity"/>.</param>
/// <param name="RecordedDate">The date the price was recorded.</param>
/// <param name="IsEstimated">Whether the price is an estimate.</param>
/// <param name="IsPreferredStore">Whether this store is the preferred place to buy the ingredient.</param>
public record IngredientPriceDto(
    int Id,
    int IngredientId,
    int StoreId,
    string StoreName,
    decimal Price,
    double PackageQuantity,
    MeasurementUnit PackageUnit,
    DateOnly RecordedDate,
    bool IsEstimated,
    bool IsPreferredStore);

/// <summary>Payload to create or update an ingredient price observation.</summary>
/// <param name="StoreId">The store the price was observed at.</param>
/// <param name="Price">The price paid for the package, in Canadian dollars.</param>
/// <param name="PackageQuantity">The quantity contained in the priced package.</param>
/// <param name="PackageUnit">The unit of <paramref name="PackageQuantity"/>.</param>
/// <param name="RecordedDate">The date the price was recorded.</param>
/// <param name="IsEstimated">Whether the price is an estimate.</param>
/// <param name="IsPreferredStore">Whether this store is the preferred place to buy the ingredient.</param>
public record SaveIngredientPriceRequest(
    int StoreId,
    decimal Price,
    double PackageQuantity,
    MeasurementUnit PackageUnit,
    DateOnly RecordedDate,
    bool IsEstimated,
    bool IsPreferredStore);

/// <summary>A price observation with its ingredient name, for flat listing across all ingredients.</summary>
/// <param name="Id">The price record's unique identifier.</param>
/// <param name="IngredientId">The ingredient the price applies to.</param>
/// <param name="IngredientName">The ingredient's display name.</param>
/// <param name="StoreId">The store the price was observed at.</param>
/// <param name="StoreName">The store's display name.</param>
/// <param name="Price">The price paid for the package, in Canadian dollars.</param>
/// <param name="PackageQuantity">The quantity contained in the priced package.</param>
/// <param name="PackageUnit">The unit of <paramref name="PackageQuantity"/>.</param>
/// <param name="RecordedDate">The date the price was recorded.</param>
/// <param name="IsEstimated">Whether the price is an estimate.</param>
/// <param name="IsPreferredStore">Whether this store is the preferred place to buy the ingredient.</param>
public record RecentPriceDto(
    int Id,
    int IngredientId,
    string IngredientName,
    int StoreId,
    string StoreName,
    decimal Price,
    double PackageQuantity,
    MeasurementUnit PackageUnit,
    DateOnly RecordedDate,
    bool IsEstimated,
    bool IsPreferredStore);
