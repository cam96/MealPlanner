namespace MealPlanner.Domain.Nutrition;

/// <summary>
/// Computed nutrition totals. When <see cref="IsEstimated"/> is <see langword="true"/>, at least one
/// contributing value is estimated or could not be computed exactly and the UI must flag it.
/// </summary>
/// <param name="Calories">Energy, in kilocalories.</param>
/// <param name="Protein">Protein, in grams.</param>
/// <param name="Fiber">Dietary fibre, in grams.</param>
/// <param name="Carbs">Carbohydrates, in grams.</param>
/// <param name="Fat">Total fat, in grams.</param>
/// <param name="IsEstimated">Whether the figures are estimated rather than exact.</param>
public record NutritionFacts(double Calories, double Protein, double Fiber, double Carbs, double Fat, bool IsEstimated)
{
    /// <summary>An all-zero, non-estimated set of facts.</summary>
    public static NutritionFacts Zero { get; } = new(0, 0, 0, 0, 0, false);

    /// <summary>Returns these facts scaled by <paramref name="factor"/>.</summary>
    /// <param name="factor">The scaling factor (for example <c>1 / servings</c>).</param>
    /// <returns>The scaled facts, preserving the estimate flag.</returns>
    public NutritionFacts Scale(double factor) =>
        new(Calories * factor, Protein * factor, Fiber * factor, Carbs * factor, Fat * factor, IsEstimated);

    /// <summary>Returns the sum of these facts and <paramref name="other"/>.</summary>
    /// <param name="other">The facts to add.</param>
    /// <returns>The combined facts; estimated when either operand is estimated.</returns>
    public NutritionFacts Add(NutritionFacts other)
    {
        ArgumentNullException.ThrowIfNull(other);
        return new NutritionFacts(
            Calories + other.Calories,
            Protein + other.Protein,
            Fiber + other.Fiber,
            Carbs + other.Carbs,
            Fat + other.Fat,
            IsEstimated || other.IsEstimated);
    }
}
