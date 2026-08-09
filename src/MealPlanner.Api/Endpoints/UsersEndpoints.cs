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
        group.MapGet("/pending", GetPendingAsync);
        group.MapPut("/{id:int}/roles", UpdateRolesAsync);
        group.MapPost("/{id:int}/approve", ApproveAsync);
        group.MapPost("/{id:int}/reject", RejectAsync);
        group.MapPost("/approve-all", ApproveAllAsync);
        group.MapPost("/reject-all", RejectAllAsync);

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

    /// <summary>Gets all users with the UserPending role, optionally filtered by search term.</summary>
    private static async Task<Ok<IReadOnlyList<AppUserDto>>> GetPendingAsync(
        MealPlannerDbContext db,
        string? search,
        CancellationToken cancellationToken)
    {
        var query = db.AppUsers
            .AsNoTracking()
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(r => r.Role == AppRoles.UserPending));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            query = query.Where(u =>
                u.Name.ToLower().Contains(term) ||
                u.Email.ToLower().Contains(term));
        }

        var users = await query
            .OrderBy(u => u.CreatedAt)
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

    /// <summary>Approves a pending user by replacing UserPending with User role.</summary>
    private static async Task<Results<Ok<AppUserDto>, NotFound>> ApproveAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.AppUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        var pendingRole = user.UserRoles.FirstOrDefault(r => r.Role == AppRoles.UserPending);
        if (pendingRole is not null)
        {
            user.UserRoles.Remove(pendingRole);
            user.UserRoles.Add(new AppUserRole { Role = AppRoles.User });
            await db.SaveChangesAsync(cancellationToken);
        }

        return TypedResults.Ok(new AppUserDto(
            user.Id, user.Email, user.Name,
            user.UserRoles.Select(r => r.Role).ToList(),
            user.CreatedAt, user.LastLoginAt));
    }

    /// <summary>Rejects a pending user by deleting them from the database.</summary>
    private static async Task<Results<Ok, NotFound>> RejectAsync(
        int id,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var user = await db.AppUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

        if (user is null)
        {
            return TypedResults.NotFound();
        }

        db.AppUsers.Remove(user);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok();
    }

    /// <summary>Approves all pending users by replacing UserPending with User role.</summary>
    private static async Task<Ok<int>> ApproveAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var pendingUsers = await db.AppUsers
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(r => r.Role == AppRoles.UserPending))
            .ToListAsync(cancellationToken);

        foreach (var user in pendingUsers)
        {
            var pendingRole = user.UserRoles.FirstOrDefault(r => r.Role == AppRoles.UserPending);
            if (pendingRole is not null)
            {
                user.UserRoles.Remove(pendingRole);
                user.UserRoles.Add(new AppUserRole { Role = AppRoles.User });
            }
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(pendingUsers.Count);
    }

    /// <summary>Rejects all pending users by deleting them from the database.</summary>
    private static async Task<Ok<int>> RejectAllAsync(
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var pendingUsers = await db.AppUsers
            .Include(u => u.UserRoles)
            .Where(u => u.UserRoles.Any(r => r.Role == AppRoles.UserPending))
            .ToListAsync(cancellationToken);

        db.AppUsers.RemoveRange(pendingUsers);
        await db.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(pendingUsers.Count);
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
        var validRoles = new HashSet<string>
        {
            AppRoles.User, AppRoles.Viewer, AppRoles.Admin, AppRoles.UserPending,
        };
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
