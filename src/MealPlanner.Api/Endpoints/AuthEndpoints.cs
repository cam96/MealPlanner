using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MealPlanner.Contracts.Auth;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using MealPlanner.ServiceDefaults.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;

namespace MealPlanner.Api.Endpoints;

/// <summary>Maps authentication-related endpoints (user provisioning and role resolution).</summary>
public static class AuthEndpoints
{
    /// <summary>Registers the auth endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/auth").WithTags("Auth").RequireAuthorization();

        group.MapPost("/ensure-user", EnsureUserAsync);

        return app;
    }

    /// <summary>
    /// Ensures the authenticated user exists in the database. Creates a new user record with the
    /// default "User" role on first login. Returns the user's current roles.
    /// </summary>
    private static async Task<Ok<UserRolesResponse>> EnsureUserAsync(
        ClaimsPrincipal user,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        var googleId = user.FindFirstValue(JwtRegisteredClaimNames.Sub)
            ?? user.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? "";
        var email = user.FindFirstValue(JwtRegisteredClaimNames.Email)
            ?? user.FindFirstValue(ClaimTypes.Email)
            ?? "";
        var name = user.FindFirstValue(JwtRegisteredClaimNames.Name)
            ?? user.FindFirstValue(ClaimTypes.Name)
            ?? "";

        var appUser = await db.AppUsers
            .Include(u => u.UserRoles)
            .FirstOrDefaultAsync(u => u.GoogleId == googleId, cancellationToken);

        if (appUser is null)
        {
            appUser = new AppUser
            {
                GoogleId = googleId,
                Email = email,
                Name = name,
                CreatedAt = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow,
            };
            appUser.UserRoles.Add(new AppUserRole { Role = AppRoles.User });
            db.AppUsers.Add(appUser);
        }
        else
        {
            appUser.LastLoginAt = DateTime.UtcNow;
            appUser.Name = name;
            appUser.Email = email;
        }

        await db.SaveChangesAsync(cancellationToken);

        var roles = appUser.UserRoles.Select(r => r.Role).ToList();
        return TypedResults.Ok(new UserRolesResponse(appUser.Id, roles));
    }
}
