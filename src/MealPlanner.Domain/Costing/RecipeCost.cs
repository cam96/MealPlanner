namespace MealPlanner.Domain.Costing;

/// <summary>
/// Computed cost for a recipe. When <see cref="IsEstimated"/> is <see langword="true"/>, at least one
/// ingredient used an estimated price or had no recorded price, and the UI must flag it.
/// </summary>
/// <param name="TotalCost">The cost to make the whole recipe, in Canadian dollars.</param>
/// <param name="CostPerServing">The cost per serving, in Canadian dollars.</param>
/// <param name="IsEstimated">Whether the cost is estimated rather than fully priced.</param>
public record RecipeCost(decimal TotalCost, decimal CostPerServing, bool IsEstimated)
{
    /// <summary>A zero, non-estimated cost.</summary>
    public static RecipeCost Zero { get; } = new(0m, 0m, false);
}
