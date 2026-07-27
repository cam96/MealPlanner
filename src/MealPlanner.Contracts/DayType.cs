namespace MealPlanner.Contracts;

/// <summary>How a day is treated when planning. Wire representation exchanged over HTTP.</summary>
public enum DayType
{
    /// <summary>A normal day: planned meals count toward goals and prep load.</summary>
    Normal = 0,

    /// <summary>Eating out: excluded from goal tracking.</summary>
    EatingOut = 1,

    /// <summary>A special event: excluded from goal tracking.</summary>
    Event = 2,
}
