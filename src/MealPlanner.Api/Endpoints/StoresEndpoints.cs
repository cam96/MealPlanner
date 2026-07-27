using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Stores;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps CRUD endpoints for grocery stores.</summary>
public static class StoresEndpoints
{
    /// <summary>Registers the stores endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapStoresEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/stores").WithTags("Stores");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<StoreDto>>> GetAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var stores = await db.Stores
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => s.ToDto())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<StoreDto>>(stores);
    }

    private static async Task<Results<Ok<StoreDto>, NotFound>> GetByIdAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var store = await db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return store is null ? TypedResults.NotFound() : TypedResults.Ok(store.ToDto());
    }

    private static async Task<Results<Created<StoreDto>, ValidationProblem>> CreateAsync(
        SaveStoreRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var store = new Store { Name = request.Name.Trim() };

        db.Stores.Add(store);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/stores/{store.Id}", store.ToDto());
    }

    private static async Task<Results<Ok<StoreDto>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        SaveStoreRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (store is null)
        {
            return TypedResults.NotFound();
        }

        store.Name = request.Name.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(store.ToDto());
    }

    private static async Task<Results<NoContent, NotFound, ValidationProblem>> DeleteAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (store is null)
        {
            return TypedResults.NotFound();
        }

        var hasPrices = await db.IngredientPrices.AnyAsync(p => p.StoreId == id, cancellationToken);
        if (hasPrices)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(id)] = ["Cannot delete a store that has recorded prices."],
            });
        }

        db.Stores.Remove(store);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static IDictionary<string, string[]>? Validate(SaveStoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return new Dictionary<string, string[]>
            {
                [nameof(request.Name)] = ["Name is required."],
            };
        }

        return null;
    }
}
