using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Shopping;
using MealPlanner.Data;
using MealPlanner.Domain.Shopping;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps the endpoint that generates a shopping list from a month's meal plan.</summary>
public static class ShoppingEndpoints
{
    /// <summary>Registers the shopping-list endpoint on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapShoppingEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/plans/{year:int}/{month:int}/shopping-list", GetAsync)
            .WithTags("Shopping");

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

        if (plan is null)
        {
            var empty = new ShoppingListDto(year, month, [], 0m, false, budget, false, budget);
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
                l.PercentBelowAverage))
            .ToList();

        var dto = new ShoppingListDto(
            year,
            month,
            lines,
            list.EstimatedTotal,
            list.IsEstimated,
            budget,
            budget > 0 && list.EstimatedTotal > budget,
            budget - list.EstimatedTotal);

        return TypedResults.Ok(dto);
    }
}
