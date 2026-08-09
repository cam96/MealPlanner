using System.Security.Claims;
using MealPlanner.Contracts.Reporting;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Reporting;
using MealPlanner.Domain.Shopping;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MealPlanner.ServiceDefaults.Authorization;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps the endpoint that produces an at-a-glance dashboard for a month's plan.</summary>
public static class DashboardEndpoints
{
    /// <summary>Registers the dashboard endpoint on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapGet("/api/plans/{year:int}/{month:int}/dashboard", GetAsync)
            .WithTags("Dashboard").RequireAuthorization(AuthorizationPolicies.User);

        return app;
    }

    private static async Task<Results<Ok<DashboardDto>, ValidationProblem>> GetAsync(
        int year,
        int month,
        ClaimsPrincipal user,
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

        var userId = user.GetAppUserId();
        var budget = await SettingsEndpoints.GetMonthlyBudgetAsync(db, cancellationToken);

        var people = await db.People
            .AsNoTracking()
            .Where(p => p.AppUserId == userId)
            .OrderBy(p => p.Id)
            .ToListAsync(cancellationToken);

        var plan = await db.MealPlans
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Recipe!)
                        .ThenInclude(r => r.Ingredients)
                            .ThenInclude(ri => ri.Ingredient)
            .FirstOrDefaultAsync(p => p.AppUserId == userId && p.Year == year && p.Month == month, cancellationToken)
            ?? new MealPlan { Year = year, Month = month };

        var pantry = await db.PantryItems
            .AsNoTracking()
            .Include(p => p.Ingredient)
            .Where(p => p.AppUserId == userId)
            .ToListAsync(cancellationToken);

        var prices = await db.IngredientPrices
            .AsNoTracking()
            .Include(p => p.Store)
            .ToListAsync(cancellationToken);

        var list = ShoppingListGenerator.Generate(plan, pantry, prices);

        var summary = DashboardBuilder.Build(
            plan,
            people,
            budget,
            list.EstimatedTotal,
            list.IsEstimated);

        return TypedResults.Ok(ToDto(summary));
    }

    private static DashboardDto ToDto(DashboardSummary summary) => new(
        summary.Year,
        summary.Month,
        summary.CountedDays,
        summary.PlannedMealCount,
        summary.People
            .Select(p => new PersonNutritionSummaryDto(
                p.PersonId,
                p.PersonName,
                p.AverageCalories,
                p.AverageProtein,
                p.AverageFiber,
                p.AverageCarbs,
                p.AverageFat,
                p.CalorieGoal,
                p.ProteinGoal,
                p.FiberGoal,
                p.CarbGoal,
                p.FatGoal,
                p.IsEstimated))
            .ToList(),
        new PrepSummaryDto(
            summary.Prep.TotalMinutes,
            summary.Prep.AverageMinutesPerCountedDay,
            summary.Prep.BusiestDate,
            summary.Prep.BusiestDayMinutes),
        summary.MonthlyBudget,
        summary.ProjectedSpend,
        summary.SpendIsEstimated,
        summary.IsOverBudget,
        summary.RemainingBudget,
        summary.Alerts
            .Select(a => new DashboardAlertDto(
                (MealPlanner.Contracts.Reporting.DashboardAlertLevel)(int)a.Level, a.Message))
            .ToList());
}
