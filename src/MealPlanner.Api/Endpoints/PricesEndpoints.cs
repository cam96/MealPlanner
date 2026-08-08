using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Prices;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps CRUD endpoints for ingredient price observations, nested under an ingredient.</summary>
public static class PricesEndpoints
{
    /// <summary>Registers the price endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPricesEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/ingredients/{ingredientId:int}/prices").WithTags("Prices").RequireAuthorization();

        group.MapGet("/", GetAllAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{priceId:int}", UpdateAsync);
        group.MapDelete("/{priceId:int}", DeleteAsync);

        var flat = app.MapGroup("/api/prices").WithTags("Prices").RequireAuthorization();
        flat.MapGet("/recent", GetRecentAsync);

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<IngredientPriceDto>>, NotFound>> GetAllAsync(
        int ingredientId,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var ingredientExists = await db.Ingredients.AnyAsync(i => i.Id == ingredientId, cancellationToken);
        if (!ingredientExists)
        {
            return TypedResults.NotFound();
        }

        var prices = await db.IngredientPrices
            .AsNoTracking()
            .Where(p => p.IngredientId == ingredientId)
            .Include(p => p.Store)
            .OrderByDescending(p => p.RecordedDate)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<IngredientPriceDto>>(prices);
    }

    private static async Task<Results<Created<IngredientPriceDto>, NotFound, ValidationProblem>> CreateAsync(
        int ingredientId,
        SaveIngredientPriceRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var ingredientExists = await db.Ingredients.AnyAsync(i => i.Id == ingredientId, cancellationToken);
        if (!ingredientExists)
        {
            return TypedResults.NotFound();
        }

        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var price = new IngredientPrice { IngredientId = ingredientId };
        price.Apply(request);

        db.IngredientPrices.Add(price);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(price).Reference(p => p.Store).LoadAsync(cancellationToken);

        return TypedResults.Created(
            $"/api/ingredients/{ingredientId}/prices/{price.Id}",
            price.ToDto());
    }

    private static async Task<Results<Ok<IngredientPriceDto>, NotFound, ValidationProblem>> UpdateAsync(
        int ingredientId,
        int priceId,
        SaveIngredientPriceRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var price = await db.IngredientPrices
            .Include(p => p.Store)
            .FirstOrDefaultAsync(p => p.Id == priceId && p.IngredientId == ingredientId, cancellationToken);
        if (price is null)
        {
            return TypedResults.NotFound();
        }

        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        price.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        await db.Entry(price).Reference(p => p.Store).LoadAsync(cancellationToken);

        return TypedResults.Ok(price.ToDto());
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int ingredientId,
        int priceId,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var price = await db.IngredientPrices
            .FirstOrDefaultAsync(p => p.Id == priceId && p.IngredientId == ingredientId, cancellationToken);
        if (price is null)
        {
            return TypedResults.NotFound();
        }

        db.IngredientPrices.Remove(price);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Ok<IReadOnlyList<RecentPriceDto>>> GetRecentAsync(
        MealPlannerDbContext db,
        string? q,
        int? limit,
        CancellationToken cancellationToken)
    {
        var take = limit is > 0 ? Math.Min(limit.Value, 100) : 50;

        var query = db.IngredientPrices
            .AsNoTracking()
            .Include(p => p.Store)
            .Include(p => p.Ingredient)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(q))
        {
            query = query.Where(p => EF.Functions.Like(p.Ingredient!.Name, $"%{q}%")
                                   || EF.Functions.Like(p.Store!.Name, $"%{q}%"));
        }

        var prices = await query
            .OrderByDescending(p => p.RecordedDate)
            .ThenBy(p => p.Ingredient!.Name)
            .Take(take)
            .Select(p => new RecentPriceDto(
                p.Id,
                p.IngredientId,
                p.Ingredient!.Name,
                p.StoreId,
                p.Store!.Name,
                p.Price,
                p.PackageQuantity,
                p.PackageUnit.ToContract(),
                p.RecordedDate,
                p.IsEstimated,
                p.IsPreferredStore))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<RecentPriceDto>>(prices);
    }

    private static async Task<IDictionary<string, string[]>?> ValidateAsync(
        SaveIngredientPriceRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        var storeExists = await db.Stores.AnyAsync(s => s.Id == request.StoreId, cancellationToken);
        if (!storeExists)
        {
            errors[nameof(request.StoreId)] = ["The selected store does not exist."];
        }

        if (request.Price < 0)
        {
            errors[nameof(request.Price)] = ["Price cannot be negative."];
        }

        if (request.PackageQuantity <= 0)
        {
            errors[nameof(request.PackageQuantity)] = ["Package quantity must be greater than zero."];
        }

        return errors.Count == 0 ? null : errors;
    }
}
