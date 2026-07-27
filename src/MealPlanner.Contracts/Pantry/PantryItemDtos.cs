namespace MealPlanner.Contracts.Pantry;

/// <summary>A quantity of an ingredient on hand at a storage location.</summary>
/// <param name="Id">The item's unique identifier.</param>
/// <param name="IngredientId">The ingredient held.</param>
/// <param name="IngredientName">The ingredient's display name (for presentation).</param>
/// <param name="QuantityOnHand">The quantity on hand, in <paramref name="Unit"/>.</param>
/// <param name="Unit">The unit the <paramref name="QuantityOnHand"/> is expressed in.</param>
/// <param name="Location">Where the item is stored.</param>
public record PantryItemDto(
    int Id,
    int IngredientId,
    string IngredientName,
    double QuantityOnHand,
    MeasurementUnit Unit,
    StorageLocation Location);

/// <summary>Payload to create or update a pantry item.</summary>
/// <param name="IngredientId">The ingredient held.</param>
/// <param name="QuantityOnHand">The quantity on hand, in <paramref name="Unit"/>.</param>
/// <param name="Unit">The unit the <paramref name="QuantityOnHand"/> is expressed in.</param>
/// <param name="Location">Where the item is stored.</param>
public record SavePantryItemRequest(
    int IngredientId,
    double QuantityOnHand,
    MeasurementUnit Unit,
    StorageLocation Location);
