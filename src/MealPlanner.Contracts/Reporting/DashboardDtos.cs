namespace MealPlanner.Contracts.Reporting;

/// <summary>The severity of a dashboard alert.</summary>
public enum DashboardAlertLevel
{
    /// <summary>Informational; worth noticing but not a problem.</summary>
    Info = 0,

    /// <summary>A warning that likely needs attention.</summary>
    Warning = 1,
}

/// <summary>A person's average daily nutrition across a month's counted days, versus their goals.</summary>
/// <param name="PersonId">The person the summary is for.</param>
/// <param name="PersonName">The person's display name.</param>
/// <param name="AverageCalories">Average energy per counted day, in kilocalories.</param>
/// <param name="AverageProtein">Average protein per counted day, in grams.</param>
/// <param name="AverageFiber">Average dietary fibre per counted day, in grams.</param>
/// <param name="AverageCarbs">Average carbohydrates per counted day, in grams.</param>
/// <param name="AverageFat">Average fat per counted day, in grams.</param>
/// <param name="CalorieGoal">The person's daily calorie goal.</param>
/// <param name="ProteinGoal">The person's daily protein goal.</param>
/// <param name="FiberGoal">The person's daily fibre goal.</param>
/// <param name="CarbGoal">The person's daily carbohydrate goal.</param>
/// <param name="FatGoal">The person's daily fat goal.</param>
/// <param name="IsEstimated">Whether any contributing nutrition is estimated.</param>
public record PersonNutritionSummaryDto(
    int PersonId,
    string PersonName,
    double AverageCalories,
    double AverageProtein,
    double AverageFiber,
    double AverageCarbs,
    double AverageFat,
    int CalorieGoal,
    int ProteinGoal,
    int FiberGoal,
    int CarbGoal,
    int FatGoal,
    bool IsEstimated);

/// <summary>The preparation load across a month's counted days.</summary>
/// <param name="TotalMinutes">The total prep and cook minutes for the month.</param>
/// <param name="AverageMinutesPerCountedDay">The average minutes across counted days.</param>
/// <param name="BusiestDate">The counted day with the most prep, when any.</param>
/// <param name="BusiestDayMinutes">The prep minutes on the busiest day.</param>
public record PrepSummaryDto(
    int TotalMinutes,
    double AverageMinutesPerCountedDay,
    DateOnly? BusiestDate,
    int BusiestDayMinutes);

/// <summary>A single dashboard alert.</summary>
/// <param name="Level">The alert's severity.</param>
/// <param name="Message">The human-readable message.</param>
public record DashboardAlertDto(DashboardAlertLevel Level, string Message);

/// <summary>An at-a-glance summary of a month's plan: nutrition, prep load, budget and alerts.</summary>
/// <param name="Year">The calendar year.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="CountedDays">The number of normal days that count toward goals.</param>
/// <param name="PlannedMealCount">The number of planned meals with a recipe on counted days.</param>
/// <param name="People">Per-person average daily nutrition versus goals.</param>
/// <param name="Prep">The month's preparation load.</param>
/// <param name="MonthlyBudget">The household's configured monthly grocery budget.</param>
/// <param name="ProjectedSpend">The projected grocery spend for the month.</param>
/// <param name="SpendIsEstimated">Whether the projected spend is estimated or unpriced.</param>
/// <param name="IsOverBudget">Whether the projected spend exceeds the budget.</param>
/// <param name="RemainingBudget">The budget left after projected spend (may be negative).</param>
/// <param name="Alerts">Generated alerts for the month.</param>
public record DashboardDto(
    int Year,
    int Month,
    int CountedDays,
    int PlannedMealCount,
    IReadOnlyList<PersonNutritionSummaryDto> People,
    PrepSummaryDto Prep,
    decimal MonthlyBudget,
    decimal ProjectedSpend,
    bool SpendIsEstimated,
    bool IsOverBudget,
    decimal RemainingBudget,
    IReadOnlyList<DashboardAlertDto> Alerts);
