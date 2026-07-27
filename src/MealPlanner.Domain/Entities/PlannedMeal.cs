namespace MealPlanner.Domain.Entities;

/// <summary>
/// A meal planned for a specific slot on a day, assigned to a household member or shared, optionally
/// backed by a recipe.
/// </summary>
public class PlannedMeal
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the owning day plan.</summary>
    public int DayPlanId { get; set; }

    /// <summary>Gets or sets the owning day plan. Populated by EF Core when included.</summary>
    public DayPlan? DayPlan { get; set; }

    /// <summary>Gets or sets the meal slot (breakfast, lunch, dinner, snack).</summary>
    public MealType Slot { get; set; }

    /// <summary>Gets or sets who the meal is for.</summary>
    public MealAssignee Assignee { get; set; }

    /// <summary>Gets or sets the identifier of the recipe used, when one is assigned.</summary>
    public int? RecipeId { get; set; }

    /// <summary>Gets or sets the recipe used. Populated by EF Core when included.</summary>
    public Recipe? Recipe { get; set; }

    /// <summary>Gets or sets the identifier of the meal combo used, when one is assigned.</summary>
    public int? MealComboId { get; set; }

    /// <summary>Gets or sets the meal combo used. Populated by EF Core when included.</summary>
    public MealCombo? MealCombo { get; set; }

    /// <summary>Gets or sets the number of servings consumed. Always at least one.</summary>
    public int Servings { get; set; } = 1;
}
