using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Pantry;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps CRUD endpoints for pantry and freezer inventory.</summary>
public static class PantryEndpoints
{
    /// <summary>Registers the pantry endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPantryEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/pantry").WithTags("Pantry");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<PantryItemDto>>> GetAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var items = await db.PantryItems
            .AsNoTracking()
            .Include(p => p.Ingredient)
            .OrderBy(p => p.Location)
            .ThenBy(p => p.Ingredient!.Name)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<PantryItemDto>>(items);
    }

    private static async Task<Results<Ok<PantryItemDto>, NotFound>> GetByIdAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.PantryItems
            .AsNoTracking()
            .Include(p => p.Ingredient)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return item is null ? TypedResults.NotFound() : TypedResults.Ok(item.ToDto());
    }

    private static async Task<Results<Created<PantryItemDto>, ValidationProblem>> CreateAsync(
        SavePantryItemRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var item = new PantryItem();
        item.Apply(request);

        db.PantryItems.Add(item);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(db, item.Id, cancellationToken);
        return TypedResults.Created($"/api/pantry/{item.Id}", saved.ToDto());
    }

    private static async Task<Results<Ok<PantryItemDto>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        SavePantryItemRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.PantryItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        item.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(db, item.Id, cancellationToken);
        return TypedResults.Ok(saved.ToDto());
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var item = await db.PantryItems.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (item is null)
        {
            return TypedResults.NotFound();
        }

        db.PantryItems.Remove(item);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<PantryItem> ReloadAsync(
        MealPlannerDbContext db,
        int id,
        CancellationToken cancellationToken) =>
        await db.PantryItems
            .AsNoTracking()
            .Include(p => p.Ingredient)
            .FirstAsync(p => p.Id == id, cancellationToken);

    private static async Task<IDictionary<string, string[]>?> ValidateAsync(
        SavePantryItemRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.QuantityOnHand < 0)
        {
            errors[nameof(request.QuantityOnHand)] = ["Quantity on hand cannot be negative."];
        }

        if (request.IngredientId <= 0
            || !await db.Ingredients.AnyAsync(i => i.Id == request.IngredientId, cancellationToken))
        {
            errors[nameof(request.IngredientId)] = ["A valid ingredient is required."];
        }

        return errors.Count == 0 ? null : errors;
    }
}
