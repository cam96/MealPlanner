namespace MealPlanner.Domain.Entities;

/// <summary>
/// A member of the household whose daily nutrition goals meals are planned against.
/// </summary>
public class Person
{
    /// <summary>Gets the surrogate primary key.</summary>
    public int Id { get; set; }

    /// <summary>Gets or sets the identifier of the household this person belongs to.</summary>
    public int HouseholdId { get; set; }

    /// <summary>Gets or sets the household this person belongs to.</summary>
    public Household? Household { get; set; }

    /// <summary>Gets or sets the person's display name (for example "Me" or "Partner").</summary>
    public required string Name { get; set; }

    /// <summary>Gets or sets the target daily energy intake, in kilocalories.</summary>
    public int DailyCalorieGoal { get; set; }

    /// <summary>Gets or sets the target daily protein intake, in grams.</summary>
    public int DailyProteinGoal { get; set; }

    /// <summary>Gets or sets the target daily dietary fibre intake, in grams.</summary>
    public int DailyFiberGoal { get; set; }

    /// <summary>Gets or sets the target daily carbohydrate intake, in grams.</summary>
    public int DailyCarbGoal { get; set; }

    /// <summary>Gets or sets the target daily fat intake, in grams.</summary>
    public int DailyFatGoal { get; set; }
}
