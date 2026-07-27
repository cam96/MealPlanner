namespace MealPlanner.Data.Cnf;

/// <summary>A lightweight search result from the Canadian Nutrient File (CNF).</summary>
/// <param name="FoodCode">The CNF food code, stored on an ingredient as its CNF link.</param>
/// <param name="Description">The English food description.</param>
public sealed record CnfFoodSummary(int FoodCode, string Description);

/// <summary>
/// Per-100-gram nutrition for a single CNF food, limited to the values MealPlanner tracks.
/// </summary>
/// <param name="FoodCode">The CNF food code.</param>
/// <param name="Description">The English food description.</param>
/// <param name="CaloriesPer100">Energy per 100 g of edible portion, in kilocalories (nutrient 208).</param>
/// <param name="ProteinPer100">Protein per 100 g of edible portion, in grams (nutrient 203).</param>
/// <param name="FiberPer100">Total dietary fibre per 100 g of edible portion, in grams (nutrient 291).</param>
/// <param name="CarbsPer100">Total carbohydrate per 100 g of edible portion, in grams (nutrient 205).</param>
/// <param name="FatPer100">Total fat per 100 g of edible portion, in grams (nutrient 204).</param>
public sealed record CnfFoodNutrition(
    int FoodCode,
    string Description,
    double CaloriesPer100,
    double ProteinPer100,
    double FiberPer100,
    double CarbsPer100,
    double FatPer100);
