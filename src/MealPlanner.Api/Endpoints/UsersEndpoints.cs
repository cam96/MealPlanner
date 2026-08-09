using MealPlanner.Contracts.Auth;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using MealPlanner.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps admin user-management endpoints.</summary>
public static class UsersEndpoints
{
    /// <summary>Registers the user management endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapUsersEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .RequireAuthorization(AuthorizationPolicies.Admin);

        group.MapGet("/", GetAllAsync);
        group.MapPut("/{id:int}/roles", UpdateRolesAsync);

        return app;
    }

    private static async Task<Ok<IReadOnlyList<AppUserDto>>> GetAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var users = await db.AppUsers
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .OrderBy(u => u.Name)
            .Select(u => new AppUserDto(
                u.Id,
                u.Email,
                u.Name,
                u.UserRoles.Select(r => r.Role).ToList(),
                u.CreatedAt,
                u.LastLoginAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<AppUserDto>>(users);
    }

    private static async Task<Results<Ok<AppUserDto>, NotFound, ValidationProblem>> UpdateRolesAsync(
        int id,
        UpdateUserRolesRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (request.Roles is null || request.Roles.Count == 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Roles)] = ["At least one role is required."],
            });
        }

        // Validate that all requested roles are known.
        var validRoles = new HashSet<string> { AppRoles.User, AppRoles.Viewer, AppRoles.Admin };
        var invalidRoles = request.Roles.Where(r => !validRoles.Contains(r)).ToList();
        if (invalidRoles.Count > 0)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Roles)] = [$"Unknown roles: {string.Join(", ", invalidRoles)}"],
            });
        }

        var user = await db.AppUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        // Replace existing roles with the new set.
        user.UserRoles.Clear();
        foreach (var role in request.Roles.Distinct())
        {
            user.UserRoles.Add(new AppUserRole { Role = role });
        }

        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(new AppUserDto(
            user.Id,
            user.Email,
            user.Name,
            user.UserRoles.Select(r => r.Role).ToList(),
            user.CreatedAt,
            user.LastLoginAt));
    }
}
