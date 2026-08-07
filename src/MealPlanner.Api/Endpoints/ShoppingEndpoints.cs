using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Shopping;
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
            .WithTags("Shopping");

        group.MapGet("/", GetAsync);

        group.MapPost("/manual-items", AddManualItemAsync);
        group.MapDelete("/manual-items/{id:int}", DeleteManualItemAsync);
        group.MapPut("/manual-items/{id:int}/cart", ToggleManualItemCartAsync);

        group.MapPut("/items/{ingredientId:int}/cart", ToggleGeneratedItemCartAsync);

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

        var plan = await db.MealPlans
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Recipe!)
                        .ThenInclude(r => r.Ingredients)
                            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(p => p.Year == year && p.Month == month, cancellationToken);

        // Load cart entries for generated items.
        var cartedIngredientIds = await db.GeneratedItemCartEntries
            .AsNoTracking()
            .Where(e => e.Year == year && e.Month == month)
            .Select(e => e.IngredientId)
            .ToHashSetAsync(cancellationToken);

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

        var pricesByIngredient = manualItemPrices
            .GroupBy(p => p.IngredientId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<ManualItemPriceDto>)g
                    .OrderByDescending(p => p.RecordedDate)
                    .Select(p => new ManualItemPriceDto(
                        p.Store?.Name ?? string.Empty,
                        p.Price,
                        p.PackageQuantity,
                        p.PackageUnit.ToContract(),
                        p.RecordedDate,
                        p.IsPreferredStore))
                    .ToList());

        var manualItems = manualEntities
            .Select(m => m.ToDto(
                m.IngredientId.HasValue && pricesByIngredient.TryGetValue(m.IngredientId.Value, out var prices)
                    ? prices
                    : null))
            .ToList();

        if (plan is null)
        {
            var empty = new ShoppingListDto(year, month, [], manualItems, 0m, false, budget, false, budget);
            return TypedResults.Ok(empty);
        }

        var pantry = await db.PantryItems
            .AsNoTracking()
            .Include(p => p.Ingredient)
            .ToListAsync(cancellationToken);

        var prices = await db.IngredientPrices
            .AsNoTracking()
            .Include(p => p.Store)
            .ToListAsync(cancellationToken);

        var list = ShoppingListGenerator.Generate(plan, pantry, prices);

        var lines = list.Lines
            .Select(l => new ShoppingListLineDto(
                l.IngredientId,
                l.IngredientName,
                l.Unit.ToContract(),
                l.RequiredQuantity,
                l.PantryQuantity,
                l.ToBuyQuantity,
                l.PreferredStoreId,
                l.PreferredStoreName,
                l.PackagesToBuy,
                l.EstimatedCost,
                l.IsCostEstimated,
                l.IsSharedAcrossRecipes,
                l.IsBulkPurchase,
                l.IsDeal,
                l.PercentBelowAverage,
                cartedIngredientIds.Contains(l.IngredientId)))
            .ToList();

        var dto = new ShoppingListDto(
            year,
            month,
            lines,
            manualItems,
            list.EstimatedTotal,
            list.IsEstimated,
            budget,
            budget > 0 && list.EstimatedTotal > budget,
            budget - list.EstimatedTotal);

        return TypedResults.Ok(dto);
    }

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
            item.ToDto(itemPrices));
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

    private static async Task<Ok> ToggleGeneratedItemCartAsync(
        int year,
        int month,
        int ingredientId,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var existing = await db.GeneratedItemCartEntries
            .FirstOrDefaultAsync(
                e => e.Year == year && e.Month == month && e.IngredientId == ingredientId,
                cancellationToken);

        if (existing is not null)
        {
            db.GeneratedItemCartEntries.Remove(existing);
        }
        else
        {
            db.GeneratedItemCartEntries.Add(new GeneratedItemCartEntry
            {
                Year = year,
                Month = month,
                IngredientId = ingredientId,
                AddedToCartAt = DateTime.UtcNow,
            });
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    private static async Task<NoContent> ClearCartAsync(
        int year,
        int month,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        // Remove all generated item cart entries for this month.
        await db.GeneratedItemCartEntries
            .Where(e => e.Year == year && e.Month == month)
            .ExecuteDeleteAsync(cancellationToken);

        // Remove manual items that are in the cart (they've been purchased).
        await db.ManualShoppingItems
            .Where(m => m.Year == year && m.Month == month && m.IsInCart)
            .ExecuteDeleteAsync(cancellationToken);

        return TypedResults.NoContent();
    }
}
