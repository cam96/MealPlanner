namespace MealPlanner.Contracts;

/// <summary>Who a planned meal is for. Wire representation exchanged over HTTP.</summary>
public enum MealAssignee
{
    /// <summary>The first household member.</summary>
    FirstPerson = 0,

    /// <summary>The second household member.</summary>
    SecondPerson = 1,

    /// <summary>Both members share the meal.</summary>
    Shared = 2,
}
