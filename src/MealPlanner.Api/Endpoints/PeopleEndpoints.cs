using MealPlanner.Api.Mapping;
using MealPlanner.Contracts.People;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps CRUD endpoints for household members and their nutrition goals.</summary>
public static class PeopleEndpoints
{
    /// <summary>Registers the people endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapPeopleEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/people").WithTags("People");

        group.MapGet("/", GetAllAsync);
        group.MapGet("/{id:int}", GetByIdAsync);
        group.MapPost("/", CreateAsync);
        group.MapPut("/{id:int}", UpdateAsync);
        group.MapDelete("/{id:int}", DeleteAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<PersonDto>>> GetAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var people = await db.People
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => p.ToDto())
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<PersonDto>>(people);
    }

    private static async Task<Results<Ok<PersonDto>, NotFound>> GetByIdAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var person = await db.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        return person is null ? TypedResults.NotFound() : TypedResults.Ok(person.ToDto());
    }

    private static async Task<Results<Created<PersonDto>, ValidationProblem>> CreateAsync(
        SavePersonRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var person = new Person { Name = request.Name.Trim() };
        person.Apply(request);

        db.People.Add(person);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Created($"/api/people/{person.Id}", person.ToDto());
    }

    private static async Task<Results<Ok<PersonDto>, NotFound, ValidationProblem>> UpdateAsync(
        int id,
        SavePersonRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (Validate(request) is { } errors)
        {
            return TypedResults.ValidationProblem(errors);
        }

        var person = await db.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
        {
            return TypedResults.NotFound();
        }

        person.Apply(request);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(person.ToDto());
    }

    private static async Task<Results<NoContent, NotFound>> DeleteAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var person = await db.People.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (person is null)
        {
            return TypedResults.NotFound();
        }

        db.People.Remove(person);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static IDictionary<string, string[]>? Validate(SavePersonRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            errors[nameof(request.Name)] = ["Name is required."];
        }

        if (request.DailyCalorieGoal < 0)
        {
            errors[nameof(request.DailyCalorieGoal)] = ["Calorie goal cannot be negative."];
        }

        if (request.DailyProteinGoal < 0)
        {
            errors[nameof(request.DailyProteinGoal)] = ["Protein goal cannot be negative."];
        }

        if (request.DailyFiberGoal < 0)
        {
            errors[nameof(request.DailyFiberGoal)] = ["Fibre goal cannot be negative."];
        }

        if (request.DailyCarbGoal < 0)
        {
            errors[nameof(request.DailyCarbGoal)] = ["Carbohydrate goal cannot be negative."];
        }

        if (request.DailyFatGoal < 0)
        {
            errors[nameof(request.DailyFatGoal)] = ["Fat goal cannot be negative."];
        }

        return errors.Count == 0 ? null : errors;
    }
}
