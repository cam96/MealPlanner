using MealPlanner.Api.Household;
using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Stores;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MealPlanner.ServiceDefaults.Authorization;

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

        var group = app.MapGroup("/api/stores").WithTags("Stores").RequireAuthorization(AuthorizationPolicies.User);

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<IResult> GetAllAsync(
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var stores = await db.Stores
            .AsNoTracking()
            .Where(s => s.HouseholdId == householdId)
            .OrderBy(s => s.Name)
            .Select(s => s.ToDto())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<StoreDto>>(stores);
    }

    private static async Task<IResult> GetByIdAsync(
        int id,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var store = await db.Stores
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id && s.HouseholdId == householdId, cancellationToken);

        return store is null ? TypedResults.NotFound() : TypedResults.Ok(store.ToDto());
    }

    private static async Task<IResult> CreateAsync(
        SaveStoreRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var store = new Store { Name = request.Name.Trim(), HouseholdId = householdId };

        db.Stores.Add(store);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/stores/{store.Id}", store.ToDto());
    }

    private static async Task<IResult> UpdateAsync(
        int id,
        SaveStoreRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id && s.HouseholdId == householdId, cancellationToken);
        if (store is null)
        {
            return TypedResults.NotFound();
        }

        store.Name = request.Name.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(store.ToDto());
    }

    private static async Task<IResult> DeleteAsync(
        int id,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var store = await db.Stores.FirstOrDefaultAsync(s => s.Id == id && s.HouseholdId == householdId, cancellationToken);
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
