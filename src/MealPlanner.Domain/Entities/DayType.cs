namespace MealPlanner.Domain.Entities;

/// <summary>How a day is treated when planning meals and tracking nutrition goals.</summary>
public enum DayType
{
    /// <summary>A normal day: planned meals count toward nutrition goals and prep load.</summary>
    Normal = 0,

    /// <summary>Eating out: meals are not planned and the day is excluded from goal tracking.</summary>
    EatingOut = 1,

    /// <summary>A special event (party, travel): excluded from goal tracking.</summary>
    Event = 2,
}
