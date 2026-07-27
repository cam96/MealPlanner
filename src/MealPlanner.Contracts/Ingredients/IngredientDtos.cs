namespace MealPlanner.Contracts.Ingredients;

/// <summary>An ingredient with per-100-unit nutrition values.</summary>
/// <param name="Id">The ingredient's unique identifier.</param>
/// <param name="Name">The ingredient's display name.</param>
/// <param name="BaseUnit">The base unit the ingredient is measured and priced in.</param>
/// <param name="Category">The meal-building category the ingredient belongs to.</param>
/// <param name="CaloriesPer100">Energy per 100 g/ml, in kilocalories.</param>
/// <param name="ProteinPer100">Protein per 100 g/ml, in grams.</param>
/// <param name="FiberPer100">Dietary fibre per 100 g/ml, in grams.</param>
/// <param name="CarbsPer100">Carbohydrates per 100 g/ml, in grams.</param>
/// <param name="FatPer100">Total fat per 100 g/ml, in grams.</param>
/// <param name="IsNutritionEstimated">Whether the nutrition values are estimated.</param>
/// <param name="CnfFoodCode">Linked Canadian Nutrient File food code, when populated from CNF.</param>
/// <param name="ServingWeightG">Weight of a single item in grams, for count-to-gram conversion.</param>
public record IngredientDto(
    int Id,
    string Name,
    MeasurementUnit BaseUnit,
    FoodCategory Category,
    double CaloriesPer100,
    double ProteinPer100,
    double FiberPer100,
    double CarbsPer100,
    double FatPer100,
    bool IsNutritionEstimated,
    int? CnfFoodCode,
    double? ServingWeightG);

/// <summary>Payload to create or update an ingredient.</summary>
/// <param name="Name">The ingredient's display name.</param>
/// <param name="BaseUnit">The base unit the ingredient is measured and priced in.</param>
/// <param name="Category">The meal-building category the ingredient belongs to.</param>
/// <param name="CaloriesPer100">Energy per 100 g/ml, in kilocalories.</param>
/// <param name="ProteinPer100">Protein per 100 g/ml, in grams.</param>
/// <param name="FiberPer100">Dietary fibre per 100 g/ml, in grams.</param>
/// <param name="CarbsPer100">Carbohydrates per 100 g/ml, in grams.</param>
/// <param name="FatPer100">Total fat per 100 g/ml, in grams.</param>
/// <param name="IsNutritionEstimated">Whether the nutrition values are estimated.</param>
/// <param name="CnfFoodCode">Linked Canadian Nutrient File food code, when populated from CNF.</param>
/// <param name="ServingWeightG">Weight of a single item in grams, for count-to-gram conversion.</param>
public record SaveIngredientRequest(
    string Name,
    MeasurementUnit BaseUnit,
    FoodCategory Category,
    double CaloriesPer100,
    double ProteinPer100,
    double FiberPer100,
    double CarbsPer100,
    double FatPer100,
    bool IsNutritionEstimated,
    int? CnfFoodCode,
    double? ServingWeightG);
