namespace MealPlanner.Contracts.Recipes;

/// <summary>A single ingredient line within a recipe.</summary>
/// <param name="Id">The line's unique identifier (zero for a not-yet-saved line).</param>
/// <param name="IngredientId">The ingredient used.</param>
/// <param name="IngredientName">The ingredient's display name (for presentation).</param>
/// <param name="Quantity">The quantity of the ingredient used, in <paramref name="Unit"/>.</param>
/// <param name="Unit">The unit the <paramref name="Quantity"/> is expressed in.</param>
public record RecipeIngredientDto(
    int Id,
    int IngredientId,
    string IngredientName,
    double Quantity,
    MeasurementUnit Unit);

/// <summary>A recipe with its ingredient lines and computed per-serving nutrition and cost.</summary>
/// <param name="Id">The recipe's unique identifier.</param>
/// <param name="Name">The recipe's display name.</param>
/// <param name="MealType">The meal the recipe is intended for.</param>
/// <param name="PrepMinutes">Hands-on preparation time, in minutes.</param>
/// <param name="CookMinutes">Cooking time, in minutes.</param>
/// <param name="Servings">The number of servings the recipe yields.</param>
/// <param name="Instructions">Free-text preparation instructions.</param>
/// <param name="Ingredients">The recipe's ingredient lines.</param>
/// <param name="CaloriesPerServing">Energy per serving, in kilocalories.</param>
/// <param name="ProteinPerServing">Protein per serving, in grams.</param>
/// <param name="FiberPerServing">Dietary fibre per serving, in grams.</param>
/// <param name="CarbsPerServing">Carbohydrates per serving, in grams.</param>
/// <param name="FatPerServing">Total fat per serving, in grams.</param>
/// <param name="NutritionIsEstimated">Whether the nutrition figures are estimated.</param>
/// <param name="CostPerServing">Cost per serving, in Canadian dollars.</param>
/// <param name="TotalCost">Cost to make the whole recipe, in Canadian dollars.</param>
/// <param name="CostIsEstimated">Whether the cost is estimated.</param>
public record RecipeDto(
    int Id,
    string Name,
    MealType MealType,
    int PrepMinutes,
    int CookMinutes,
    int Servings,
    string? Instructions,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    double CaloriesPerServing,
    double ProteinPerServing,
    double FiberPerServing,
    double CarbsPerServing,
    double FatPerServing,
    bool NutritionIsEstimated,
    decimal CostPerServing,
    decimal TotalCost,
    bool CostIsEstimated);

/// <summary>A concise recipe summary for list views, with per-serving nutrition and cost.</summary>
/// <param name="Id">The recipe's unique identifier.</param>
/// <param name="Name">The recipe's display name.</param>
/// <param name="MealType">The meal the recipe is intended for.</param>
/// <param name="PrepMinutes">Hands-on preparation time, in minutes.</param>
/// <param name="CookMinutes">Cooking time, in minutes.</param>
/// <param name="Servings">The number of servings the recipe yields.</param>
/// <param name="CaloriesPerServing">Energy per serving, in kilocalories.</param>
/// <param name="ProteinPerServing">Protein per serving, in grams.</param>
/// <param name="FiberPerServing">Dietary fibre per serving, in grams.</param>
/// <param name="CarbsPerServing">Carbohydrates per serving, in grams.</param>
/// <param name="FatPerServing">Total fat per serving, in grams.</param>
/// <param name="NutritionIsEstimated">Whether the nutrition figures are estimated.</param>
/// <param name="CostPerServing">Cost per serving, in Canadian dollars.</param>
/// <param name="CostIsEstimated">Whether the cost is estimated.</param>
public record RecipeSummaryDto(
    int Id,
    string Name,
    MealType MealType,
    int PrepMinutes,
    int CookMinutes,
    int Servings,
    double CaloriesPerServing,
    double ProteinPerServing,
    double FiberPerServing,
    double CarbsPerServing,
    double FatPerServing,
    bool NutritionIsEstimated,
    decimal CostPerServing,
    bool CostIsEstimated);

/// <summary>Payload to create or update a recipe ingredient line.</summary>
/// <param name="IngredientId">The ingredient used.</param>
/// <param name="Quantity">The quantity of the ingredient used, in <paramref name="Unit"/>.</param>
/// <param name="Unit">The unit the <paramref name="Quantity"/> is expressed in.</param>
public record SaveRecipeIngredientRequest(int IngredientId, double Quantity, MeasurementUnit Unit);

/// <summary>Payload to create or update a recipe.</summary>
/// <param name="Name">The recipe's display name.</param>
/// <param name="MealType">The meal the recipe is intended for.</param>
/// <param name="PrepMinutes">Hands-on preparation time, in minutes.</param>
/// <param name="CookMinutes">Cooking time, in minutes.</param>
/// <param name="Servings">The number of servings the recipe yields.</param>
/// <param name="Instructions">Free-text preparation instructions.</param>
/// <param name="Ingredients">The recipe's ingredient lines.</param>
public record SaveRecipeRequest(
    string Name,
    MealType MealType,
    int PrepMinutes,
    int CookMinutes,
    int Servings,
    string? Instructions,
    IReadOnlyList<SaveRecipeIngredientRequest> Ingredients);
