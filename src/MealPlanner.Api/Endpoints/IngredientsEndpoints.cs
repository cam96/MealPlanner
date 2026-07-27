using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Combos;
using MealPlanner.Contracts.Ingredients;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps CRUD endpoints for ingredients and their nutrition values.</summary>
public static class IngredientsEndpoints
{
    /// <summary>Registers the ingredients endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapIngredientsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/ingredients").WithTags("Ingredients");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapPut("/{id:int}/category", SetCategoryAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<IngredientDto>>> GetAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var ingredients = await db.Ingredients
            .AsNoTracking()
            .OrderBy(i => i.Name)
            .Select(i => i.ToDto())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<IngredientDto>>(ingredients);
    }

    private static async Task<Results<Ok<IngredientDto>, NotFound>> GetByIdAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var ingredient = await db.Ingredients
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        return ingredient is null ? TypedResults.NotFound() : TypedResults.Ok(ingredient.ToDto());
    }

    private static async Task<Results<Created<IngredientDto>, ValidationProblem>> CreateAsync(
        SaveIngredientRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var ingredient = new Ingredient { Name = request.Name.Trim() };
        ingredient.Apply(request);

        db.Ingredients.Add(ingredient);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/ingredients/{ingredient.Id}", ingredient.ToDto());
    }

    private static async Task<Results<Ok<IngredientDto>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        SaveIngredientRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (ingredient is null)
        {
            return TypedResults.NotFound();
        }

        ingredient.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ingredient.ToDto());
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (ingredient is null)
        {
            return TypedResults.NotFound();
        }

        db.Ingredients.Remove(ingredient);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Results<Ok<IngredientDto>, NotFound>> SetCategoryAsync(
        int id,
        SetIngredientCategoryRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var ingredient = await db.Ingredients.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (ingredient is null)
        {
            return TypedResults.NotFound();
        }

        ingredient.Category = request.Category.ToDomain();
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(ingredient.ToDto());
    }

    private static IDictionary<string, string[]>? Validate(SaveIngredientRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }

        if (request.CaloriesPer100 < 0)
        {
            errors[nameof(request.CaloriesPer100)] = ["Calories cannot be negative."];
        }

        if (request.ProteinPer100 < 0)
        {
            errors[nameof(request.ProteinPer100)] = ["Protein cannot be negative."];
        }

        if (request.FiberPer100 < 0)
        {
            errors[nameof(request.FiberPer100)] = ["Fibre cannot be negative."];
        }

        if (request.CarbsPer100 < 0)
        {
            errors[nameof(request.CarbsPer100)] = ["Carbohydrates cannot be negative."];
        }

        if (request.FatPer100 < 0)
        {
            errors[nameof(request.FatPer100)] = ["Fat cannot be negative."];
        }

        if (request.ServingWeightG is < 0)
        {
            errors[nameof(request.ServingWeightG)] = ["Serving weight cannot be negative."];
        }

        return errors.Count == 0 ? null : errors;
    }
}
