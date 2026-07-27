using MealPlanner.Domain.Entities;

namespace MealPlanner.Domain.Shopping;

/// <summary>A single ingredient row on a generated shopping list.</summary>
/// <param name="IngredientId">The ingredient to buy.</param>
/// <param name="IngredientName">The ingredient's display name.</param>
/// <param name="Unit">The ingredient's base unit that quantities are expressed in.</param>
/// <param name="RequiredQuantity">The total quantity the plan needs, in base units.</param>
/// <param name="PantryQuantity">The quantity already on hand, in base units.</param>
/// <param name="ToBuyQuantity">The quantity that still needs buying, in base units.</param>
/// <param name="PreferredStoreId">The store the item is cheapest/preferred at, when priced.</param>
/// <param name="PreferredStoreName">The preferred store's display name, when priced.</param>
/// <param name="PackagesToBuy">The number of whole packages to purchase.</param>
/// <param name="EstimatedCost">The cost of the packages to buy, in Canadian dollars.</param>
/// <param name="IsCostEstimated">Whether the cost is estimated or could not be fully priced.</param>
/// <param name="IsSharedAcrossRecipes">Whether the ingredient is used by more than one planned recipe.</param>
/// <param name="IsBulkPurchase">Whether a single package substantially exceeds the quantity needed.</param>
/// <param name="IsDeal">Whether the latest price is a deal versus the historical average.</param>
/// <param name="PercentBelowAverage">How far the latest price sits below the historical average, as a percentage.</param>
public record ShoppingListLine(
    int IngredientId,
    string IngredientName,
    MeasurementUnit Unit,
    double RequiredQuantity,
    double PantryQuantity,
    double ToBuyQuantity,
    int? PreferredStoreId,
    string? PreferredStoreName,
    int PackagesToBuy,
    decimal EstimatedCost,
    bool IsCostEstimated,
    bool IsSharedAcrossRecipes,
    bool IsBulkPurchase,
    bool IsDeal,
    double PercentBelowAverage);

/// <summary>A generated shopping list for a meal plan, after subtracting pantry stock.</summary>
/// <param name="Lines">The ingredient rows to buy, ordered by store then name.</param>
/// <param name="EstimatedTotal">The total estimated cost of the list, in Canadian dollars.</param>
/// <param name="IsEstimated">Whether any line's cost is estimated or unpriced.</param>
public record ShoppingList(
    IReadOnlyList<ShoppingListLine> Lines,
    decimal EstimatedTotal,
    bool IsEstimated);

/// <summary>The outcome of comparing an ingredient's latest price against its history.</summary>
/// <param name="IngredientId">The ingredient the comparison is for.</param>
/// <param name="LatestUnitPrice">The most recent price per base unit, in Canadian dollars.</param>
/// <param name="AverageUnitPrice">The average of prior prices per base unit, in Canadian dollars.</param>
/// <param name="IsDeal">Whether the latest price is at or below the deal threshold.</param>
/// <param name="PercentBelowAverage">How far the latest price sits below the average, as a percentage.</param>
public record DealResult(
    int IngredientId,
    decimal LatestUnitPrice,
    decimal AverageUnitPrice,
    bool IsDeal,
    double PercentBelowAverage);
