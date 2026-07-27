namespace MealPlanner.Domain.Entities;

/// <summary>
/// A single application setting stored as a key/value pair, such as the household's monthly grocery
/// budget. Kept as strings so new settings can be added without schema changes.
/// </summary>
public class AppSetting
{
    /// <summary>Gets or sets the unique setting key.</summary>
    public required string Key { get; set; }

    /// <summary>Gets or sets the setting value, serialised as a string.</summary>
    public string Value { get; set; } = string.Empty;
}
