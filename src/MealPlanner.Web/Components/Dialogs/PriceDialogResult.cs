namespace MealPlanner.Web.Components.Dialogs;

/// <summary>Result returned when the price dialog includes the ingredient picker.</summary>
/// <param name="IngredientId">The selected ingredient identifier.</param>
/// <param name="Request">The price save request.</param>
public record PriceDialogResult(int IngredientId, MealPlanner.Contracts.Prices.SaveIngredientPriceRequest Request);
