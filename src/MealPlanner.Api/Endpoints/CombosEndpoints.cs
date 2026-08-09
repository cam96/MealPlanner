using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Combos;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MealPlanner.ServiceDefaults.Authorization;

namespace MealPlanner.Api.Endpoints;

/// <summary>
/// Maps endpoints for the meal-building category board (protein / carbohydrate / vegetable) and the
/// informal meal combos assembled from those categories.
/// </summary>
public static class CombosEndpoints
{
    /// <summary>Registers the combos endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapCombosEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/combos").WithTags("Combos").RequireAuthorization(AuthorizationPolicies.User);

        group.MapGet("/board", GetBoardAsync);
        group.MapGet("/", GetAllAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<Ok<CategoryBoardDto>> GetBoardAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var ingredients = await db.Ingredients
            .AsNoTracking()
            .Where(i => i.Category != FoodCategory.None)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        var ids = ingredients.Select(i => i.Id).ToList();
        var stock = await db.PantryItems
            .AsNoTracking()
            .Where(p => ids.Contains(p.IngredientId))
            .ToListAsync(cancellationToken);

        var stockByIngredient = stock
            .GroupBy(p => p.IngredientId)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<CategoryStockDto>)g
                    .OrderBy(p => p.Location)
                    .Select(p => new CategoryStockDto(p.Location.ToContract(), p.QuantityOnHand, p.Unit.ToContract()))
                    .ToList());

        IReadOnlyList<CategoryIngredientDto> Column(FoodCategory category) => ingredients
            .Where(i => i.Category == category)
            .Select(i => new CategoryIngredientDto(
                i.Id,
                i.Name,
                i.BaseUnit.ToContract(),
                stockByIngredient.GetValueOrDefault(i.Id, [])))
            .ToList();

        var board = new CategoryBoardDto(
            Column(FoodCategory.Protein),
            Column(FoodCategory.Carbohydrate),
            Column(FoodCategory.Vegetable));

        return TypedResults.Ok(board);
    }

    private static async Task<Ok<IReadOnlyList<MealComboDto>>> GetAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var combos = await db.MealCombos
            .AsNoTracking()
            .Include(c => c.ProteinIngredient)
            .Include(c => c.CarbohydrateIngredient)
            .Include(c => c.VegetableIngredient)
            .OrderBy(c => c.Name)
            .Select(c => c.ToDto())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<MealComboDto>>(combos);
    }

    private static async Task<Results<Created<MealComboDto>, ValidationProblem>> CreateAsync(
        SaveMealComboRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var combo = new MealCombo { Name = request.Name.Trim() };
        combo.Apply(request);
        db.MealCombos.Add(combo);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(db, combo.Id, cancellationToken);
        return TypedResults.Created($"/api/combos/{combo.Id}", saved.ToDto());
    }

    private static async Task<Results<Ok<MealComboDto>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        SaveMealComboRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var combo = await db.MealCombos.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (combo is null)
        {
            return TypedResults.NotFound();
        }

        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        combo.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(db, combo.Id, cancellationToken);
        return TypedResults.Ok(saved.ToDto());
    }

    private static async Task<Results<NoContent, NotFound, BadRequest<string>>> DeleteAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var combo = await db.MealCombos.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (combo is null)
        {
            return TypedResults.NotFound();
        }

        if (await db.PlannedMeals.AnyAsync(m => m.MealComboId == id, cancellationToken))
        {
            return TypedResults.BadRequest("This combo is used in a meal plan and cannot be deleted.");
        }

        db.MealCombos.Remove(combo);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<MealCombo> ReloadAsync(
        MealPlannerDbContext db,
        int id,
        CancellationToken cancellationToken) =>
        await db.MealCombos
            .AsNoTracking()
            .Include(c => c.ProteinIngredient)
            .Include(c => c.CarbohydrateIngredient)
            .Include(c => c.VegetableIngredient)
            .FirstAsync(c => c.Id == id, cancellationToken);

    private static async Task<IDictionary<string, string[]>?> ValidateAsync(
        SaveMealComboRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }

        if (request.ProteinIngredientId is null
            && request.CarbohydrateIngredientId is null
            && request.VegetableIngredientId is null)
        {
            errors[nameof(request.Name)] = ["Choose at least one ingredient for the combo."];
        }

        await ValidateIngredientAsync(
            request.ProteinIngredientId, FoodCategory.Protein, nameof(request.ProteinIngredientId));
        await ValidateIngredientAsync(
            request.CarbohydrateIngredientId, FoodCategory.Carbohydrate, nameof(request.CarbohydrateIngredientId));
        await ValidateIngredientAsync(
            request.VegetableIngredientId, FoodCategory.Vegetable, nameof(request.VegetableIngredientId));

        return errors.Count == 0 ? null : errors;

        async Task ValidateIngredientAsync(int? ingredientId, FoodCategory category, string field)
        {
            if (ingredientId is not { } value)
            {
                return;
            }

            var ingredient = await db.Ingredients
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Id == value, cancellationToken);

            if (ingredient is null)
            {
                errors[field] = ["A valid ingredient is required."];
            }
            else if (ingredient.Category != category)
            {
                errors[field] = [$"'{ingredient.Name}' is not categorised as a {category.ToString().ToLowerInvariant()}."];
            }
        }
    }
}
