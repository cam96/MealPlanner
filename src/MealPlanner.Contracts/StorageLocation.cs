namespace MealPlanner.Contracts;

/// <summary>Where a pantry item is stored. Wire representation exchanged over HTTP.</summary>
public enum StorageLocation
{
    /// <summary>A shelf-stable pantry or cupboard.</summary>
    Pantry = 0,

    /// <summary>The refrigerator.</summary>
    Fridge = 1,

    /// <summary>The freezer.</summary>
    Freezer = 2,
}
