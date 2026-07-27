namespace MealPlanner.Contracts.Planning;

/// <summary>A meal planned for a slot on a day.</summary>
/// <param name="Id">The planned meal's unique identifier.</param>
/// <param name="Slot">The meal slot (breakfast, lunch, dinner, snack).</param>
/// <param name="Assignee">Who the meal is for.</param>
/// <param name="RecipeId">The recipe used, when assigned.</param>
/// <param name="RecipeName">The recipe's display name (for presentation).</param>
/// <param name="MealComboId">The meal combo used, when assigned.</param>
/// <param name="MealComboName">The meal combo's display name (for presentation).</param>
/// <param name="Servings">The number of servings consumed.</param>
public record PlannedMealDto(
    int Id,
    MealType Slot,
    MealAssignee Assignee,
    int? RecipeId,
    string? RecipeName,
    int? MealComboId,
    string? MealComboName,
    int Servings);

/// <summary>A single day within a plan, with its meals.</summary>
/// <param name="Id">The day plan's unique identifier.</param>
/// <param name="Date">The calendar date.</param>
/// <param name="DayType">How the day is treated for goal tracking.</param>
/// <param name="Note">An optional free-text note.</param>
/// <param name="PrepMinutes">The total prep and cook minutes planned for the day.</param>
/// <param name="Meals">The meals planned for the day.</param>
public record DayPlanDto(
    int Id,
    DateOnly Date,
    DayType DayType,
    string? Note,
    int PrepMinutes,
    IReadOnlyList<PlannedMealDto> Meals);

/// <summary>A person's nutrition total for a day, compared against their daily goals.</summary>
/// <param name="PersonId">The person the totals are for.</param>
/// <param name="PersonName">The person's display name.</param>
/// <param name="Date">The calendar date.</param>
/// <param name="Calories">Energy consumed, in kilocalories.</param>
/// <param name="Protein">Protein consumed, in grams.</param>
/// <param name="Fiber">Dietary fibre consumed, in grams.</param>
/// <param name="Carbs">Carbohydrates consumed, in grams.</param>
/// <param name="Fat">Total fat consumed, in grams.</param>
/// <param name="CalorieGoal">The person's daily calorie goal.</param>
/// <param name="ProteinGoal">The person's daily protein goal.</param>
/// <param name="FiberGoal">The person's daily fibre goal.</param>
/// <param name="CarbGoal">The person's daily carbohydrate goal.</param>
/// <param name="FatGoal">The person's daily fat goal.</param>
/// <param name="IsEstimated">Whether any contributing nutrition is estimated.</param>
public record PersonDayNutritionDto(
    int PersonId,
    string PersonName,
    DateOnly Date,
    double Calories,
    double Protein,
    double Fiber,
    double Carbs,
    double Fat,
    int CalorieGoal,
    int ProteinGoal,
    int FiberGoal,
    int CarbGoal,
    int FatGoal,
    bool IsEstimated);

/// <summary>A whole month's plan with its days and per-person daily nutrition rollups.</summary>
/// <param name="Id">The plan's unique identifier.</param>
/// <param name="Year">The calendar year.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Days">The day plans that make up the month, ordered by date.</param>
/// <param name="Nutrition">Per-person, per-day nutrition rollups versus goals.</param>
public record MealPlanDto(
    int Id,
    int Year,
    int Month,
    IReadOnlyList<DayPlanDto> Days,
    IReadOnlyList<PersonDayNutritionDto> Nutrition);

/// <summary>Payload to set a day's type and note.</summary>
/// <param name="DayType">How the day is treated for goal tracking.</param>
/// <param name="Note">An optional free-text note.</param>
public record SaveDayRequest(DayType DayType, string? Note);

/// <summary>Payload to add or update a planned meal.</summary>
/// <param name="Slot">The meal slot.</param>
/// <param name="Assignee">Who the meal is for.</param>
/// <param name="RecipeId">The recipe to assign, when any.</param>
/// <param name="MealComboId">The meal combo to assign, when any.</param>
/// <param name="Servings">The number of servings consumed.</param>
public record SavePlannedMealRequest(
    MealType Slot,
    MealAssignee Assignee,
    int? RecipeId,
    int? MealComboId,
    int Servings);
