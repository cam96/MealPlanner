using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Recipes;
using MealPlanner.Data;
using MealPlanner.Domain.Costing;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Nutrition;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps CRUD endpoints for recipes, including computed nutrition and cost.</summary>
public static class RecipesEndpoints
{
    /// <summary>Registers the recipe endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapRecipesEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/recipes").WithTags("Recipes");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<RecipeSummaryDto>>> GetAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var recipes = await db.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .ThenInclude(ri => ri.Ingredient)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var prices = await LoadPricesAsync(db, recipes, cancellationToken);

        var summaries = recipes
            .Select(r => r.ToSummaryDto(NutritionCalculator.PerServing(r), CostCalculator.ForRecipe(r, prices)))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<RecipeSummaryDto>>(summaries);
    }

    private static async Task<Results<Ok<RecipeDto>, NotFound>> GetByIdAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        var dto = await ToDetailDtoAsync(db, recipe, cancellationToken);
        return TypedResults.Ok(dto);
    }

    private static async Task<Results<Created<RecipeDto>, ValidationProblem>> CreateAsync(
        SaveRecipeRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var recipe = new Recipe { Name = request.Name.Trim() };
        recipe.Apply(request);

        db.Recipes.Add(recipe);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(db, recipe.Id, cancellationToken);
        var dto = await ToDetailDtoAsync(db, saved, cancellationToken);
        return TypedResults.Created($"/api/recipes/{recipe.Id}", dto);
    }

    private static async Task<Results<Ok<RecipeDto>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        SaveRecipeRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes
            .Include(r => r.Ingredients)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        if (await ValidateAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        recipe.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        var saved = await ReloadAsync(db, recipe.Id, cancellationToken);
        var dto = await ToDetailDtoAsync(db, saved, cancellationToken);
        return TypedResults.Ok(dto);
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var recipe = await db.Recipes.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (recipe is null)
        {
            return TypedResults.NotFound();
        }

        db.Recipes.Remove(recipe);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Recipe> ReloadAsync(
        MealPlannerDbContext db,
        int id,
        CancellationToken cancellationToken) =>
        await db.Recipes
            .AsNoTracking()
            .Include(r => r.Ingredients)
            .ThenInclude(ri => ri.Ingredient)
            .FirstAsync(r => r.Id == id, cancellationToken);

    private static async Task<RecipeDto> ToDetailDtoAsync(
        MealPlannerDbContext db,
        Recipe recipe,
        CancellationToken cancellationToken)
    {
        var prices = await LoadPricesAsync(db, [recipe], cancellationToken);
        var nutrition = NutritionCalculator.PerServing(recipe);
        var cost = CostCalculator.ForRecipe(recipe, prices);
        return recipe.ToDto(nutrition, cost);
    }

    private static async Task<List<IngredientPrice>> LoadPricesAsync(
        MealPlannerDbContext db,
        IEnumerable<Recipe> recipes,
        CancellationToken cancellationToken)
    {
        var ingredientIds = recipes
            .SelectMany(r => r.Ingredients)
            .Select(i => i.IngredientId)
            .Distinct()
            .ToList();

        if (ingredientIds.Count == 0)
        {
            return [];
        }

        return await db.IngredientPrices
            .AsNoTracking()
            .Where(p => ingredientIds.Contains(p.IngredientId))
            .ToListAsync(cancellationToken);
    }

    private static async Task<IDictionary<string, string[]>?> ValidateAsync(
        SaveRecipeRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }

        if (request.Servings < 1)
        {
            errors[nameof(request.Servings)] = ["Servings must be at least one."];
        }

        if (request.PrepMinutes < 0 || request.CookMinutes < 0)
        {
            errors[nameof(request.PrepMinutes)] = ["Times cannot be negative."];
        }

        if (request.Ingredients.Any(i => i.Quantity <= 0))
        {
            errors[nameof(request.Ingredients)] = ["Every ingredient line needs a quantity greater than zero."];
        }

        var requestedIds = request.Ingredients.Select(i => i.IngredientId).Distinct().ToList();
        if (requestedIds.Count > 0)
        {
            var existingIds = await db.Ingredients
                .Where(i => requestedIds.Contains(i.Id))
                .Select(i => i.Id)
                .ToListAsync(cancellationToken);

            if (existingIds.Count != requestedIds.Count)
            {
                errors[nameof(request.Ingredients)] = ["One or more ingredients do not exist."];
            }
        }

        return errors.Count == 0 ? null : errors;
    }
}
