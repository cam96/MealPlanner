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
    /// <summary>The AppSettings key used to store the bootstrap admin email.</summary>
    internal const string BootstrapAdminEmailKey = "BootstrapAdminEmail";

    /// <summary>Registers the auth endpoints on the given route builder.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same route builder, for chaining.</returns>
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var group = app.MapGroup("/api/auth").WithTags("Auth").RequireAuthorization();

        group.MapPost("/ensure-user", EnsureUserAsync);

        // Bootstrap endpoint is anonymous — it only works when no admin exists.
        app.MapPost("/api/admin/bootstrap", BootstrapAdminAsync)
            .WithTags("Auth")
            .AllowAnonymous();

        return app;
    }

    /// <summary>
    /// Ensures the authenticated user exists in the database. Creates a new user record with the
    /// "UserPending" role on first login (unless the email matches the bootstrap admin setting).
    /// Returns the user's current roles.
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

            // Check if this email is the bootstrap admin
            var bootstrapSetting = await db.AppSettings
                .FirstOrDefaultAsync(s => s.Key == BootstrapAdminEmailKey, cancellationToken);

            if (bootstrapSetting is not null &&
                string.Equals(bootstrapSetting.Value, email, StringComparison.OrdinalIgnoreCase))
            {
                // Promote to Admin + User and clear the bootstrap setting
                appUser.UserRoles.Add(new AppUserRole { Role = AppRoles.Admin });
                appUser.UserRoles.Add(new AppUserRole { Role = AppRoles.User });
                db.AppSettings.Remove(bootstrapSetting);
            }
            else
            {
                appUser.UserRoles.Add(new AppUserRole { Role = AppRoles.UserPending });
            }

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

    /// <summary>
    /// One-time seed command to designate an admin email. Only succeeds when no admin users exist
    /// in the database. The specified email will receive Admin + User roles on first login.
    /// </summary>
    private static async Task<Results<Ok, Conflict<string>, ValidationProblem>> BootstrapAdminAsync(
        BootstrapAdminRequest request,
        MealPlannerDbContext db,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(request.Email)] = ["Email is required."],
            });
        }

        // Only allow bootstrap if no admin users exist
        var adminExists = await db.AppUserRoles
            .AnyAsync(r => r.Role == AppRoles.Admin, cancellationToken);

        if (adminExists)
        {
            return TypedResults.Conflict("An admin user already exists. Bootstrap is no longer available.");
        }

        // Store or update the bootstrap email in AppSettings
        var existing = await db.AppSettings
            .FirstOrDefaultAsync(s => s.Key == BootstrapAdminEmailKey, cancellationToken);

        if (existing is not null)
        {
            existing.Value = request.Email.Trim();
        }
        else
        {
            db.AppSettings.Add(new AppSetting
            {
                Key = BootstrapAdminEmailKey,
                Value = request.Email.Trim(),
            });
        }

        await db.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok();
    }
}
