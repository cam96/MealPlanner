using MealPlanner.Api.Household;
using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.Planning;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using MealPlanner.Domain.Planning;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using MealPlanner.ServiceDefaults.Authorization;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps endpoints for monthly meal planning.</summary>
public static class PlannerEndpoints
{
    /// <summary>Registers the planner endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPlannerEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/plans").WithTags("Planner").RequireAuthorization(AuthorizationPolicies.User);

        group.MapGet("/{year:int}/{month:int}", GetMonthAsync);
        group.MapPut("/days/{dayId:int}", UpdateDayAsync);
        group.MapPost("/days/{dayId:int}/meals", AddMealAsync);
        group.MapPut("/meals/{mealId:int}", UpdateMealAsync);
        group.MapDelete("/meals/{mealId:int}", DeleteMealAsync);

        return app;
    }

    private static async Task<IResult> GetMonthAsync(
        int year,
        int month,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        if (month is < 1 or > 12 || year is < 1 or > 9999)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(month)] = ["A valid year and month are required."],
            });
        }

        var plan = await db.MealPlans
            .FirstOrDefaultAsync(p => p.Year == year && p.Month == month && p.HouseholdId == householdId, cancellationToken);

        if (plan is null)
        {
            plan = new MealPlan { Year = year, Month = month, HouseholdId = householdId };
            var days = DateTime.DaysInMonth(year, month);
            for (var day = 1; day <= days; day++)
            {
                plan.Days.Add(new DayPlan { Date = new DateOnly(year, month, day) });
            }

            db.MealPlans.Add(plan);
            await db.SaveChangesAsync(cancellationToken);
        }

        var loaded = await LoadPlanAsync(db, plan.Id, cancellationToken);
        var people = await db.People.AsNoTracking().Where(p => p.HouseholdId == householdId).ToListAsync(cancellationToken);

        return TypedResults.Ok(BuildDto(loaded, people));
    }

    private static async Task<IResult> UpdateDayAsync(
        int dayId,
        SaveDayRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var day = await db.DayPlans
            .Include(d => d.MealPlan)
            .FirstOrDefaultAsync(d => d.Id == dayId && d.MealPlan!.HouseholdId == householdId, cancellationToken);
        if (day is null)
        {
            return TypedResults.NotFound();
        }

        day.DayType = request.DayType.ToDomain();
        day.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();
        await db.SaveChangesAsync(cancellationToken);

        return await ReturnPlanAsync(db, day.MealPlanId, householdId, cancellationToken);
    }

    private static async Task<IResult> AddMealAsync(
        int dayId,
        SavePlannedMealRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var day = await db.DayPlans
            .Include(d => d.MealPlan)
            .FirstOrDefaultAsync(d => d.Id == dayId && d.MealPlan!.HouseholdId == householdId, cancellationToken);
        if (day is null)
        {
            return TypedResults.NotFound();
        }

        if (await ValidateMealAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var meal = new PlannedMeal { DayPlanId = dayId };
        meal.Apply(request);
        db.PlannedMeals.Add(meal);
        await db.SaveChangesAsync(cancellationToken);

        return await ReturnPlanAsync(db, day.MealPlanId, householdId, cancellationToken);
    }

    private static async Task<IResult> UpdateMealAsync(
        int mealId,
        SavePlannedMealRequest request,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var meal = await db.PlannedMeals
            .Include(m => m.DayPlan)
                .ThenInclude(d => d!.MealPlan)
            .FirstOrDefaultAsync(m => m.Id == mealId && m.DayPlan!.MealPlan!.HouseholdId == householdId, cancellationToken);
        if (meal is null)
        {
            return TypedResults.NotFound();
        }

        if (await ValidateMealAsync(request, db, cancellationToken) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        meal.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        return await ReturnPlanAsync(db, meal.DayPlan!.MealPlanId, householdId, cancellationToken);
    }

    private static async Task<IResult> DeleteMealAsync(
        int mealId,
        HouseholdContext context,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var (householdId, error) = await context.RequireHouseholdAsync(cancellationToken);
        if (error is not null) return error;

        var meal = await db.PlannedMeals
            .Include(m => m.DayPlan)
                .ThenInclude(d => d!.MealPlan)
            .FirstOrDefaultAsync(m => m.Id == mealId && m.DayPlan!.MealPlan!.HouseholdId == householdId, cancellationToken);
        if (meal is null)
        {
            return TypedResults.NotFound();
        }

        var planId = meal.DayPlan!.MealPlanId;
        db.PlannedMeals.Remove(meal);
        await db.SaveChangesAsync(cancellationToken);

        return await ReturnPlanAsync(db, planId, householdId, cancellationToken);
    }

    private static async Task<IResult> ReturnPlanAsync(
        MealPlannerDbContext db,
        int planId,
        int householdId,
        CancellationToken cancellationToken)
    {
        var loaded = await LoadPlanAsync(db, planId, cancellationToken);
        var people = await db.People.AsNoTracking().Where(p => p.HouseholdId == householdId).ToListAsync(cancellationToken);
        return TypedResults.Ok(BuildDto(loaded, people));
    }

    private static async Task<MealPlan> LoadPlanAsync(
        MealPlannerDbContext db,
        int planId,
        CancellationToken cancellationToken) =>
        await db.MealPlans
            .AsNoTracking()
            .Include(p => p.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.Recipe!)
                        .ThenInclude(r => r.Ingredients)
                            .ThenInclude(ri => ri.Ingredient)
            .Include(p => p.Days)
                .ThenInclude(d => d.Meals)
                    .ThenInclude(m => m.MealCombo)
            .FirstAsync(p => p.Id == planId, cancellationToken);

    private static MealPlanDto BuildDto(MealPlan plan, IReadOnlyList<Person> people)
    {
        var prepByDate = PrepTimeLoad.PerDay(plan).ToDictionary(l => l.Date, l => l.TotalMinutes);

        var days = plan.Days
            .OrderBy(d => d.Date)
            .Select(d => d.ToDto(prepByDate.TryGetValue(d.Date, out var minutes) ? minutes : 0))
            .ToList();

        var peopleById = people.ToDictionary(p => p.Id);
        var nutrition = PlanAggregator.PerPersonPerDay(plan, people)
            .Where(r => peopleById.ContainsKey(r.PersonId))
            .Select(r =>
            {
                var person = peopleById[r.PersonId];
                return new PersonDayNutritionDto(
                    r.PersonId,
                    person.Name,
                    r.Date,
                    r.Nutrition.Calories,
                    r.Nutrition.Protein,
                    r.Nutrition.Fiber,
                    r.Nutrition.Carbs,
                    r.Nutrition.Fat,
                    person.DailyCalorieGoal,
                    person.DailyProteinGoal,
                    person.DailyFiberGoal,
                    person.DailyCarbGoal,
                    person.DailyFatGoal,
                    r.Nutrition.IsEstimated);
            })
            .ToList();

        return new MealPlanDto(plan.Id, plan.Year, plan.Month, days, nutrition);
    }

    private static async Task<IDictionary<string, string[]>?> ValidateMealAsync(
        SavePlannedMealRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Servings < 1)
        {
            errors[nameof(request.Servings)] = ["Servings must be at least 1."];
        }

        if (request.RecipeId is null == request.MealComboId is null)
        {
            errors[nameof(request.RecipeId)] = ["Choose either a recipe or a meal combo."];
        }

        if (request.RecipeId is { } recipeId
            && (recipeId <= 0 || !await db.Recipes.AnyAsync(r => r.Id == recipeId, cancellationToken)))
        {
            errors[nameof(request.RecipeId)] = ["A valid recipe is required."];
        }

        if (request.MealComboId is { } comboId
            && (comboId <= 0 || !await db.MealCombos.AnyAsync(c => c.Id == comboId, cancellationToken)))
        {
            errors[nameof(request.MealComboId)] = ["A valid meal combo is required."];
        }

        return errors.Count == 0 ? null : errors;
    }
}
