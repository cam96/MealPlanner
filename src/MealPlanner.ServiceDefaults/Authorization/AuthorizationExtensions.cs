using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.ServiceDefaults.Authorization;

/// <summary>
/// Extension methods for registering MealPlanner authorization policies.
/// </summary>
public static class AuthorizationExtensions
{
    /// <summary>
    /// Registers the standard MealPlanner authorization policies. Call from both the Api and Web
    /// projects to keep policy definitions in sync.
    /// </summary>
    public static IServiceCollection AddMealPlannerAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // User policy: requires the User role (all household members).
            options.AddPolicy(AuthorizationPolicies.User, policy =>
                policy.RequireRole(AppRoles.User, AppRoles.Admin));

            // Viewer policy: read-only access — any of these roles satisfies it.
            options.AddPolicy(AuthorizationPolicies.Viewer, policy =>
                policy.RequireRole(AppRoles.Viewer, AppRoles.User, AppRoles.Admin));

            // Admin policy: requires the Admin role specifically.
            options.AddPolicy(AuthorizationPolicies.Admin, policy =>
                policy.RequireRole(AppRoles.Admin));
        });

        return services;
    }
}
