namespace MealPlanner.Data.Cnf;

/// <summary>
/// Reads and searches the Canadian Nutrient File (CNF) CSV dataset to populate ingredient nutrition.
/// The dataset is read locally (never committed) and cached in memory on first use.
/// </summary>
public interface ICnfFoodRepository
{
    /// <summary>
    /// Gets a value indicating whether the CNF dataset is present and can be searched. When
    /// <see langword="false"/>, callers should hide CNF features rather than fail.
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>Searches foods whose description contains <paramref name="query"/>.</summary>
    /// <param name="query">The (case-insensitive) text to search for in food descriptions.</param>
    /// <param name="maxResults">The maximum number of matches to return.</param>
    /// <returns>The matching foods, best matches first; empty when the dataset is unavailable.</returns>
    IReadOnlyList<CnfFoodSummary> Search(string query, int maxResults);

    /// <summary>Gets the tracked nutrition for a single food by its CNF food code.</summary>
    /// <param name="foodCode">The CNF food code.</param>
    /// <returns>The food's nutrition, or <see langword="null"/> when not found.</returns>
    CnfFoodNutrition? GetByFoodCode(int foodCode);
}
