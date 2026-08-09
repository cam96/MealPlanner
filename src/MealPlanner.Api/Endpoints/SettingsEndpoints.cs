using System.Globalization;
using MealPlanner.Api.Household;
using MealPlanner.Contracts.Settings;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MealPlanner.ServiceDefaults.Authorization;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps endpoints for reading and updating application settings.</summary>
public static class SettingsEndpoints
{
    /// <summary>The setting key for the household's monthly grocery budget.</summary>
    public const string MonthlyBudgetKey = "MonthlyBudget";

    /// <summary>Registers the settings endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapSettingsEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/settings").WithTags("Settings").RequireAuthorization(AuthorizationPolicies.User);

        group.MapGet("/", GetAsync);
        group.MapPut("/", UpdateAsync);

        return app;
    }

    /// <summary>Reads the configured monthly budget, defaulting to zero when unset.</summary>
    /// <param name="db">The database context.</param>
    /// <param name="householdId">The household to retrieve the budget for.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The monthly budget in Canadian dollars.</returns>
    public static async Task<decimal> GetMonthlyBudgetAsync(
        MealPlannerDbContext db,
        int householdId,
        CancellationToken cancellationToken)
    {
        var setting = await db.AppSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.HouseholdId == householdId && s.Key == MonthlyBudgetKey, cancellationToken);

        return setting is not null
            && decimal.TryParse(setting.Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var budget)
            ? budget
            : 0m;
    }

    private static async Task<IResult> GetAsync(
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var budget = await GetMonthlyBudgetAsync(db, householdId, cancellationToken);
        return TypedResults.Ok(new AppSettingsDto(budget));
    }

    private static async Task<IResult> UpdateAsync(
        SaveSettingsRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        if (request.MonthlyBudget < 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.MonthlyBudget)] = ["The monthly budget cannot be negative."],
            });
        }

        var setting = await db.AppSettings
            .FirstOrDefaultAsync(s => s.HouseholdId == householdId && s.Key == MonthlyBudgetKey, cancellationToken);

        if (setting is null)
        {
            setting = new AppSetting { HouseholdId = householdId, Key = MonthlyBudgetKey };
            db.AppSettings.Add(setting);
        }

        setting.Value = request.MonthlyBudget.ToString(CultureInfo.InvariantCulture);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new AppSettingsDto(request.MonthlyBudget));
    }
}
