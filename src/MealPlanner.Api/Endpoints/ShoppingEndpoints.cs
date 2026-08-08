using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps endpoints for the shopping list: generation, manual items, and cart management.</summary>
public static class ShoppingEndpoints
{
    /// <summary>Registers the shopping-list endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapShoppingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/plans/{year:int}/{month:int}/shopping-list")
            .WithTags("Shopping").RequireAuthorization();

        group.MapGet("/", GetAsync);

        group.MapPost("/manual-items", AddManualItemAsync);
        group.MapPut("/manual-items/{id:int}", UpdateManualItemAsync);
        group.MapDelete("/manual-items/{id:int}", DeleteManualItemAsync);
        group.MapPut("/manual-items/{id:int}/cart", ToggleManualItemCartAsync);

        group.MapDelete("/cart", ClearCartAsync);

        return app;
    }

    private static async Task<Results<Ok<ShoppingListDto>, ValidationProblem>> GetAsync(
        int year,
        int month,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(month)] = ["A valid year and month are required."],
            });
        }

        var budget = await SettingsEndpoints.GetMonthlyBudgetAsync(db, cancellationToken);

        // Load manual items for this month with their linked ingredients.
        var manualEntities = await db.ManualShoppingItems
            .AsNoTracking()
            .Include(m => m.Ingredient)
            .Where(m => m.Year == year && m.Month == month)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);

        // Load prices for any linked ingredients on manual items.
        var linkedIngredientIds = manualEntities
            .Where(m => m.IngredientId.HasValue)
            .Select(m => m.IngredientId!.Value)
            .Distinct()
            .ToList();

        var manualItemPrices = linkedIngredientIds.Count > 0
            ? await db.IngredientPrices
                .AsNoTracking()
                .Include(p => p.Store)
                .Where(p => linkedIngredientIds.Contains(p.IngredientId))
                .ToListAsync(cancellationToken)
            : [];

        var manualPricesByIngredient = manualItemPrices
            .GroupBy(p => p.IngredientId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var manualPriceDtosByIngredient = manualPricesByIngredient
            .ToDictionary(
                kvp => kvp.Key,
                kvp => (IReadOnlyList<ManualItemPriceDto>)kvp.Value
                    .OrderByDescending(p => p.RecordedDate)
                    .Select(p => new ManualItemPriceDto(
                        p.Store?.Name ?? string.Empty,
                        p.Price,
                        p.PackageQuantity,
                        p.PackageUnit.ToContract(),
                        p.RecordedDate,
                        p.IsPreferredStore))
                    .ToList());

        // Compute costs for manual items and build DTOs.
        var totalCost = 0m;
        var anyEstimated = false;
        var items = manualEntities
            .Select(m =>
            {
                var (cost, isCostEstimated) = ComputeManualItemCost(m, manualPricesByIngredient);
                totalCost += cost;
                anyEstimated |= isCostEstimated;

                var priceDtos = m.IngredientId.HasValue
                    && manualPriceDtosByIngredient.TryGetValue(m.IngredientId.Value, out var p)
                    ? p
                    : null;

                return m.ToDto(cost, isCostEstimated, priceDtos);
            })
            .ToList();

        var dto = new ShoppingListDto(
            year,
            month,
            items,
            totalCost,
            anyEstimated,
            budget,
            budget > 0 && totalCost > budget,
            budget - totalCost);

        return TypedResults.Ok(dto);
    }

    /// <summary>
    /// Computes the estimated cost for a manual shopping item using its linked ingredient's
    /// latest or preferred-store price. When the item has no quantity or the unit cannot be
    /// converted, falls back to one package at the recorded price.
    /// </summary>
    private static (decimal Cost, bool IsCostEstimated) ComputeManualItemCost(
        ManualShoppingItem item,
        Dictionary<int, List<IngredientPrice>> pricesByIngredient)
    {
        if (!item.IngredientId.HasValue || item.Ingredient is null)
        {
            return (0m, true);
        }

        if (!pricesByIngredient.TryGetValue(item.IngredientId.Value, out var ingredientPrices)
            || ingredientPrices.Count == 0)
        {
            return (0m, true);
        }

        var chosen = PickPrice(ingredientPrices);
        if (chosen is null)
        {
            return (0m, true);
        }

        // When no quantity/unit is specified, assume the user needs one package.
        if (!item.Quantity.HasValue || item.Quantity.Value <= 0 || item.Unit is null)
        {
            return (chosen.Price, chosen.IsEstimated);
        }

        var ingredient = item.Ingredient;
        if (!UnitConverter.TryToBaseUnits(
                ingredient.BaseUnit, ingredient.ServingWeightG, item.Quantity.Value, item.Unit.Value, out var itemBase)
            || itemBase <= 0)
        {
            // Unit conversion failed — fall back to one package.
            return (chosen.Price, true);
        }

        if (!UnitConverter.TryToBaseUnits(
                ingredient.BaseUnit, ingredient.ServingWeightG, chosen.PackageQuantity, chosen.PackageUnit, out var packageBase)
            || packageBase <= 0)
        {
            // Package unit conversion failed — fall back to one package.
            return (chosen.Price, true);
        }

        var packages = Math.Max(1, (int)Math.Ceiling(itemBase / packageBase));
        var cost = packages * chosen.Price;
        return (cost, chosen.IsEstimated);
    }

    /// <summary>Picks the best price for an ingredient: preferred store first, then most recent.</summary>
    private static IngredientPrice? PickPrice(IEnumerable<IngredientPrice> prices) =>
        prices
            .OrderByDescending(p => p.IsPreferredStore)
            .ThenByDescending(p => p.RecordedDate)
            .FirstOrDefault();

    private static async Task<Results<Created<ManualShoppingItemDto>, ValidationProblem>> AddManualItemAsync(
        int year,
        int month,
        AddManualShoppingItemRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            errors[nameof(month)] = ["A valid year and month are required."];
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["An item name is required."];
        }

        if (request.Quantity is < 0)
        {
            errors[nameof(request.Quantity)] = ["Quantity cannot be negative."];
        }

        if (request.IngredientId is > 0
            && !await db.Ingredients.AnyAsync(i => i.Id == request.IngredientId, cancellationToken))
        {
            errors[nameof(request.IngredientId)] = ["The specified ingredient does not exist."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var item = new ManualShoppingItem
        {
            Year = year,
            Month = month,
            CreatedAt = DateTime.UtcNow,
        };
        item.Apply(request);

        db.ManualShoppingItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        // Load prices for linked ingredient to return in response.
        IReadOnlyList<ManualItemPriceDto>? itemPrices = null;
        if (item.IngredientId.HasValue)
        {
            itemPrices = await db.IngredientPrices
                .AsNoTracking()
                .Include(p => p.Store)
                .Where(p => p.IngredientId == item.IngredientId.Value)
                .OrderByDescending(p => p.RecordedDate)
                .Select(p => new ManualItemPriceDto(
                    p.Store!.Name,
                    p.Price,
                    p.PackageQuantity,
                    p.PackageUnit.ToContract(),
                    p.RecordedDate,
                    p.IsPreferredStore))
                .ToListAsync(cancellationToken);
        }

        return TypedResults.Created(
            $"/api/plans/{year}/{month}/shopping-list/manual-items/{item.Id}",
            item.ToDto(prices: itemPrices));
    }

    private static async Task<Results<Ok<ManualShoppingItemDto>, NotFound, ValidationProblem>> UpdateManualItemAsync(
        int year,
        int month,
        int id,
        AddManualShoppingItemRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.ManualShoppingItems
            .FirstOrDefaultAsync(m => m.Id == id && m.Year == year && m.Month == month, cancellationToken);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["An item name is required."];
        }

        if (request.Quantity is < 0)
        {
            errors[nameof(request.Quantity)] = ["Quantity cannot be negative."];
        }

        if (request.IngredientId is > 0
            && !await db.Ingredients.AnyAsync(i => i.Id == request.IngredientId, cancellationToken))
        {
            errors[nameof(request.IngredientId)] = ["The specified ingredient does not exist."];
        }

        if (errors.Count > 0)
        {
            return TypedResults.ValidationProblem(errors);
        }

        item.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(item.ToDto());
    }

    private static async Task<Results<NoContent, NotFound>> DeleteManualItemAsync(
        int year,
        int month,
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.ManualShoppingItems
            .FirstOrDefaultAsync(m => m.Id == id && m.Year == year && m.Month == month, cancellationToken);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        db.ManualShoppingItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<ManualShoppingItemDto>, NotFound>> ToggleManualItemCartAsync(
        int year,
        int month,
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.ManualShoppingItems
            .FirstOrDefaultAsync(m => m.Id == id && m.Year == year && m.Month == month, cancellationToken);

        if (item is null)
        {
            return TypedResults.NotFound();
        }

        item.IsInCart = !item.IsInCart;
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(item.ToDto());
    }

    private static async Task<NoContent> ClearCartAsync(
        int year,
        int month,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        // Remove manual items that are in the cart (they've been purchased).
        await db.ManualShoppingItems
            .Where(m => m.Year == year && m.Month == month && m.IsInCart)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
