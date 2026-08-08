namespace MealPlanner.Contracts.Shopping;

/// <summary>A user-added item on the shopping list.</summary>
/// <param name="Id">The manual item identifier.</param>
/// <param name="Name">The free-text item name.</param>
/// <param name="IngredientId">The linked ingredient identifier, when the item is an ingredient.</param>
/// <param name="Quantity">An optional quantity to buy.</param>
/// <param name="Unit">The unit the quantity is expressed in, when specified.</param>
/// <param name="IsInCart">Whether the item has been placed in the cart (checked off).</param>
/// <param name="EstimatedCost">The estimated cost based on the linked ingredient's latest price, in Canadian dollars.</param>
/// <param name="IsCostEstimated">Whether the cost is estimated or could not be determined (no price or no linked ingredient).</param>
/// <param name="Prices">Price observations for the linked ingredient, empty when unlinked.</param>
public record ManualShoppingItemDto(
    int Id,
    string Name,
    int? IngredientId,
    double? Quantity,
    MeasurementUnit? Unit,
    bool IsInCart,
    decimal EstimatedCost,
    bool IsCostEstimated,
    IReadOnlyList<ManualItemPriceDto> Prices);

/// <summary>A price observation for a manual shopping item linked to an ingredient.</summary>
/// <param name="StoreName">The store where the price was recorded.</param>
/// <param name="Price">The package price in Canadian dollars.</param>
/// <param name="PackageQuantity">The quantity in the priced package.</param>
/// <param name="PackageUnit">The unit the package quantity is expressed in.</param>
/// <param name="RecordedDate">The date the price was observed.</param>
/// <param name="IsPreferredStore">Whether this is the preferred store for the ingredient.</param>
public record ManualItemPriceDto(
    string StoreName,
    decimal Price,
    double PackageQuantity,
    MeasurementUnit PackageUnit,
    DateOnly RecordedDate,
    bool IsPreferredStore);

/// <summary>Payload to add a manual item to the shopping list.</summary>
/// <param name="Name">The free-text item name (used when not linked to an ingredient).</param>
/// <param name="IngredientId">Optional ingredient to link the item to for pricing.</param>
/// <param name="Quantity">An optional quantity to buy.</param>
/// <param name="Unit">The unit the quantity is expressed in, when specified.</param>
public record AddManualShoppingItemRequest(
    string Name,
    int? IngredientId,
    double? Quantity,
    MeasurementUnit? Unit);

/// <summary>The shopping list for a month, compared against the budget.</summary>
/// <param name="Year">The calendar year.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Items">User-added shopping list items.</param>
/// <param name="EstimatedTotal">The total estimated cost of the list, in Canadian dollars.</param>
/// <param name="IsEstimated">Whether any item's cost is estimated or unpriced.</param>
/// <param name="MonthlyBudget">The household's configured monthly grocery budget.</param>
/// <param name="IsOverBudget">Whether the estimated total exceeds the budget.</param>
/// <param name="RemainingBudget">The budget left after the estimated total (may be negative).</param>
public record ShoppingListDto(
    int Year,
    int Month,
    IReadOnlyList<ManualShoppingItemDto> Items,
    decimal EstimatedTotal,
    bool IsEstimated,
    decimal MonthlyBudget,
    bool IsOverBudget,
    decimal RemainingBudget);
