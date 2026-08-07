namespace MealPlanner.Contracts.Shopping;

/// <summary>A single ingredient row on a shopping list.</summary>
/// <param name="IngredientId">The ingredient to buy.</param>
/// <param name="IngredientName">The ingredient's display name.</param>
/// <param name="Unit">The base unit quantities are expressed in.</param>
/// <param name="RequiredQuantity">The total quantity the plan needs, in base units.</param>
/// <param name="PantryQuantity">The quantity already on hand, in base units.</param>
/// <param name="ToBuyQuantity">The quantity that still needs buying, in base units.</param>
/// <param name="StoreId">The preferred store's identifier, when priced.</param>
/// <param name="StoreName">The preferred store's display name, when priced.</param>
/// <param name="PackagesToBuy">The number of whole packages to purchase.</param>
/// <param name="EstimatedCost">The cost of the packages to buy, in Canadian dollars.</param>
/// <param name="IsCostEstimated">Whether the cost is estimated or could not be fully priced.</param>
/// <param name="IsShared">Whether the ingredient is used by more than one planned recipe.</param>
/// <param name="IsBulk">Whether a single package substantially exceeds the quantity needed.</param>
/// <param name="IsDeal">Whether the latest price is a deal versus the historical average.</param>
/// <param name="PercentBelowAverage">How far the latest price sits below the average, as a percentage.</param>
/// <param name="IsInCart">Whether the item has been placed in the cart (checked off).</param>
public record ShoppingListLineDto(
    int IngredientId,
    string IngredientName,
    MeasurementUnit Unit,
    double RequiredQuantity,
    double PantryQuantity,
    double ToBuyQuantity,
    int? StoreId,
    string? StoreName,
    int PackagesToBuy,
    decimal EstimatedCost,
    bool IsCostEstimated,
    bool IsShared,
    bool IsBulk,
    bool IsDeal,
    double PercentBelowAverage,
    bool IsInCart);

/// <summary>A user-added item on the shopping list, not derived from a meal plan.</summary>
/// <param name="Id">The manual item identifier.</param>
/// <param name="Name">The free-text item name.</param>
/// <param name="IngredientId">The linked ingredient identifier, when the item is an ingredient.</param>
/// <param name="Quantity">An optional quantity to buy.</param>
/// <param name="Unit">The unit the quantity is expressed in, when specified.</param>
/// <param name="IsInCart">Whether the item has been placed in the cart (checked off).</param>
/// <param name="Prices">Price observations for the linked ingredient, empty when unlinked.</param>
public record ManualShoppingItemDto(
    int Id,
    string Name,
    int? IngredientId,
    double? Quantity,
    MeasurementUnit? Unit,
    bool IsInCart,
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

/// <summary>A generated shopping list for a month's plan, compared against the budget.</summary>
/// <param name="Year">The calendar year.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Lines">The ingredient rows to buy, ordered by store then name.</param>
/// <param name="ManualItems">User-added items not derived from the meal plan.</param>
/// <param name="EstimatedTotal">The total estimated cost of the list, in Canadian dollars.</param>
/// <param name="IsEstimated">Whether any line's cost is estimated or unpriced.</param>
/// <param name="MonthlyBudget">The household's configured monthly grocery budget.</param>
/// <param name="IsOverBudget">Whether the estimated total exceeds the budget.</param>
/// <param name="RemainingBudget">The budget left after the estimated total (may be negative).</param>
public record ShoppingListDto(
    int Year,
    int Month,
    IReadOnlyList<ShoppingListLineDto> Lines,
    IReadOnlyList<ManualShoppingItemDto> ManualItems,
    decimal EstimatedTotal,
    bool IsEstimated,
    decimal MonthlyBudget,
    bool IsOverBudget,
    decimal RemainingBudget);
