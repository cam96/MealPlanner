namespace MealPlanner.Contracts.Cnf;

/// <summary>Reports whether the Canadian Nutrient File (CNF) dataset is available on the server.</summary>
/// <param name="IsAvailable">Whether CNF search and lookup are usable.</param>
public record CnfStatusDto(bool IsAvailable);

/// <summary>A CNF food search result.</summary>
/// <param name="FoodCode">The CNF food code, stored on an ingredient as its CNF link.</param>
/// <param name="Description">The English food description.</param>
public record CnfFoodSummaryDto(int FoodCode, string Description);

/// <summary>Per-100-gram nutrition for a single CNF food.</summary>
/// <param name="FoodCode">The CNF food code.</param>
/// <param name="Description">The English food description.</param>
/// <param name="CaloriesPer100">Energy per 100 g, in kilocalories.</param>
/// <param name="ProteinPer100">Protein per 100 g, in grams.</param>
/// <param name="FiberPer100">Total dietary fibre per 100 g, in grams.</param>
/// <param name="CarbsPer100">Total carbohydrate per 100 g, in grams.</param>
/// <param name="FatPer100">Total fat per 100 g, in grams.</param>
public record CnfFoodNutritionDto(
    int FoodCode,
    string Description,
    double CaloriesPer100,
    double ProteinPer100,
    double FiberPer100,
    double CarbsPer100,
    double FatPer100);
