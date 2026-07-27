namespace MealPlanner.Contracts.People;

/// <summary>A household member and their daily nutrition goals.</summary>
/// <param name="Id">The person's unique identifier.</param>
/// <param name="Name">The person's display name.</param>
/// <param name="DailyCalorieGoal">Target daily energy intake, in kilocalories.</param>
/// <param name="DailyProteinGoal">Target daily protein intake, in grams.</param>
/// <param name="DailyFiberGoal">Target daily dietary fibre intake, in grams.</param>
/// <param name="DailyCarbGoal">Target daily carbohydrate intake, in grams.</param>
/// <param name="DailyFatGoal">Target daily fat intake, in grams.</param>
public record PersonDto(
    int Id,
    string Name,
    int DailyCalorieGoal,
    int DailyProteinGoal,
    int DailyFiberGoal,
    int DailyCarbGoal,
    int DailyFatGoal);

/// <summary>Payload to create or update a household member.</summary>
/// <param name="Name">The person's display name.</param>
/// <param name="DailyCalorieGoal">Target daily energy intake, in kilocalories.</param>
/// <param name="DailyProteinGoal">Target daily protein intake, in grams.</param>
/// <param name="DailyFiberGoal">Target daily dietary fibre intake, in grams.</param>
/// <param name="DailyCarbGoal">Target daily carbohydrate intake, in grams.</param>
/// <param name="DailyFatGoal">Target daily fat intake, in grams.</param>
public record SavePersonRequest(
    string Name,
    int DailyCalorieGoal,
    int DailyProteinGoal,
    int DailyFiberGoal,
    int DailyCarbGoal,
    int DailyFatGoal);
