namespace MealPlanner.Contracts.Combos;

/// <summary>A quantity of a category ingredient currently stocked at a storage location.</summary>
/// <param name="Location">Where the item is stored.</param>
/// <param name="QuantityOnHand">The quantity on hand, in <paramref name="Unit"/>.</param>
/// <param name="Unit">The unit the <paramref name="QuantityOnHand"/> is expressed in.</param>
public record CategoryStockDto(
    StorageLocation Location,
    double QuantityOnHand,
    MeasurementUnit Unit);

/// <summary>An ingredient shown within a category column, with its current pantry and freezer stock.</summary>
/// <param name="IngredientId">The ingredient's unique identifier.</param>
/// <param name="Name">The ingredient's display name.</param>
/// <param name="BaseUnit">The base unit the ingredient is measured in.</param>
/// <param name="Stock">The quantities currently on hand across storage locations.</param>
public record CategoryIngredientDto(
    int IngredientId,
    string Name,
    MeasurementUnit BaseUnit,
    IReadOnlyList<CategoryStockDto> Stock);

/// <summary>
/// The three interchangeable meal-building categories, each listing the ingredients assigned to it
/// along with what is currently on hand.
/// </summary>
/// <param name="Protein">Ingredients categorised as proteins.</param>
/// <param name="Carbohydrate">Ingredients categorised as carbohydrates.</param>
/// <param name="Vegetable">Ingredients categorised as vegetables.</param>
public record CategoryBoardDto(
    IReadOnlyList<CategoryIngredientDto> Protein,
    IReadOnlyList<CategoryIngredientDto> Carbohydrate,
    IReadOnlyList<CategoryIngredientDto> Vegetable);

/// <summary>An informal saved meal combo of up to one protein, carbohydrate and vegetable.</summary>
/// <param name="Id">The combo's unique identifier.</param>
/// <param name="Name">The combo's display name.</param>
/// <param name="ProteinIngredientId">The chosen protein ingredient, when any.</param>
/// <param name="ProteinIngredientName">The chosen protein's display name (for presentation).</param>
/// <param name="CarbohydrateIngredientId">The chosen carbohydrate ingredient, when any.</param>
/// <param name="CarbohydrateIngredientName">The chosen carbohydrate's display name (for presentation).</param>
/// <param name="VegetableIngredientId">The chosen vegetable ingredient, when any.</param>
/// <param name="VegetableIngredientName">The chosen vegetable's display name (for presentation).</param>
public record MealComboDto(
    int Id,
    string Name,
    int? ProteinIngredientId,
    string? ProteinIngredientName,
    int? CarbohydrateIngredientId,
    string? CarbohydrateIngredientName,
    int? VegetableIngredientId,
    string? VegetableIngredientName);

/// <summary>Payload to create or update a meal combo.</summary>
/// <param name="Name">The combo's display name.</param>
/// <param name="ProteinIngredientId">The chosen protein ingredient, when any.</param>
/// <param name="CarbohydrateIngredientId">The chosen carbohydrate ingredient, when any.</param>
/// <param name="VegetableIngredientId">The chosen vegetable ingredient, when any.</param>
public record SaveMealComboRequest(
    string Name,
    int? ProteinIngredientId,
    int? CarbohydrateIngredientId,
    int? VegetableIngredientId);

/// <summary>Payload to assign a meal-building category to an existing ingredient.</summary>
/// <param name="Category">The category to assign; use <see cref="FoodCategory.None"/> to clear it.</param>
public record SetIngredientCategoryRequest(FoodCategory Category);
