namespace MealPlanner.Domain.Entities;

/// <summary>
/// A food item with per-100-unit nutrition values. Nutrition can be entered manually or populated
/// from the Canadian Nutrient File (CNF); estimated values are flagged so the UI can mark them.
/// </summary>
public class Ingredient
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the ingredient's display name.</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the base unit the ingredient is measured and priced in.</summary>
    public MeasurementUnit BaseUnit { get; set; }

    /// <summary>
    /// Gets or sets the meal-building category the ingredient belongs to. Used to group
    /// interchangeable foods (protein, carbohydrate, vegetable) when assembling meals.
    /// </summary>
    public FoodCategory Category { get; set; }

    /// <summary>Gets or sets the energy per 100 g/ml, in kilocalories.</summary>
    public double CaloriesPer100 { get; set; }

    /// <summary>Gets or sets the protein per 100 g/ml, in grams.</summary>
    public double ProteinPer100 { get; set; }

    /// <summary>Gets or sets the dietary fibre per 100 g/ml, in grams.</summary>
    public double FiberPer100 { get; set; }

    /// <summary>Gets or sets the carbohydrates per 100 g/ml, in grams.</summary>
    public double CarbsPer100 { get; set; }

    /// <summary>Gets or sets the total fat per 100 g/ml, in grams.</summary>
    public double FatPer100 { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the nutrition values are estimated rather than
    /// entered from a trusted source. Estimated values must be visibly marked in the UI.
    /// </summary>
    public bool IsNutritionEstimated { get; set; }

    /// <summary>Gets or sets the linked Canadian Nutrient File food code, when populated from CNF.</summary>
    public int? CnfFoodCode { get; set; }

    /// <summary>
    /// Gets or sets the weight of a single item in grams, used to convert an
    /// <see cref="MeasurementUnit.Each"/> quantity to grams. Null when not applicable.
    /// </summary>
    public double? ServingWeightG { get; set; }

    /// <summary>Gets the prices recorded for this ingredient across stores over time.</summary>
    public ICollection<IngredientPrice> Prices { get; } = [];
}
