namespace MealPlanner.Contracts.Settings;

/// <summary>The household's application settings.</summary>
/// <param name="MonthlyBudget">The monthly grocery budget, in Canadian dollars.</param>
public record AppSettingsDto(decimal MonthlyBudget);

/// <summary>Payload to update the application settings.</summary>
/// <param name="MonthlyBudget">The monthly grocery budget, in Canadian dollars.</param>
public record SaveSettingsRequest(decimal MonthlyBudget);
