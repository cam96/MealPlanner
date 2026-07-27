namespace MealPlanner.Data.Cnf;

/// <summary>Configures where the Canadian Nutrient File (CNF) CSV dataset is read from.</summary>
public sealed class CnfOptions
{
    /// <summary>
    /// Gets the directory containing the extracted CNF CSV files (for example <c>data/cnf</c>),
    /// resolved relative to the process working directory when not absolute.
    /// </summary>
    public required string Directory { get; init; }
}
