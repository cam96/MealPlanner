namespace MealPlanner.Domain.Entities;

/// <summary>Who a planned meal is for in the two-person household.</summary>
public enum MealAssignee
{
    /// <summary>The first household member (people ordered by identifier).</summary>
    FirstPerson = 0,

    /// <summary>The second household member.</summary>
    SecondPerson = 1,

    /// <summary>Both members share the meal; servings are split evenly for goal tracking.</summary>
    Shared = 2,
}
