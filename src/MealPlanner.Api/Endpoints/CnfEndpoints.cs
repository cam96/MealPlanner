using MealPlanner.Contracts.Cnf;
using MealPlanner.Data.Cnf;
using Microsoft.AspNetCore.Http.HttpResults;

namespace MealPlanner.Api.Endpoints;

/// <summary>
/// Maps read-only endpoints that search the Canadian Nutrient File (CNF) so ingredients can be
/// populated with trusted nutrition. Attribution ("Canadian Nutrient File, Health Canada, 2015")
/// is shown in the UI.
/// </summary>
public static class CnfEndpoints
{
    private const int MaxSearchResults = 50;

    /// <summary>Registers the CNF lookup endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapCnfEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/cnf").WithTags("Canadian Nutrient File");

        group.MapGet("/status", GetStatus);
        group.MapGet("/foods", SearchFoods);
        group.MapGet("/foods/{foodCode:int}", GetFood);

        return app;
    }

    private static Ok<CnfStatusDto> GetStatus(ICnfFoodRepository cnf) =>
        TypedResults.Ok(new CnfStatusDto(cnf.IsAvailable));

    private static Ok<IReadOnlyList<CnfFoodSummaryDto>> SearchFoods(string? query, ICnfFoodRepository cnf)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Trim().Length < 2)
        {
            return TypedResults.Ok<IReadOnlyList<CnfFoodSummaryDto>>([]);
        }

        var results = cnf.Search(query, MaxSearchResults)
            .Select(f => new CnfFoodSummaryDto(f.FoodCode, f.Description))
            .ToList();

        return TypedResults.Ok<IReadOnlyList<CnfFoodSummaryDto>>(results);
    }

    private static Results<Ok<CnfFoodNutritionDto>, NotFound> GetFood(int foodCode, ICnfFoodRepository cnf)
    {
        var food = cnf.GetByFoodCode(foodCode);
        return food is null
            ? TypedResults.NotFound()
            : TypedResults.Ok(new CnfFoodNutritionDto(
                food.FoodCode,
                food.Description,
                food.CaloriesPer100,
                food.ProteinPer100,
                food.FiberPer100,
                food.CarbsPer100,
                food.FatPer100));
    }
}
