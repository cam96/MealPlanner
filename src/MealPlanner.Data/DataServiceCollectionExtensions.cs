using MealPlanner.Data.Cnf;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.Data;

/// <summary>
/// Registration helpers that wire the MealPlanner data layer into an application's DI container.
/// Only the API composition root should call these; the Web (UI) project never references data.
/// </summary>
public static class DataServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="MealPlannerDbContext"/> backed by SQLite using the supplied connection string.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="connectionString">The SQLite connection string (for example <c>Data Source=data/mealplanner.db</c>).</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is null or whitespace.</exception>
    public static IServiceCollection AddMealPlannerData(this IServiceCollection services, string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<MealPlannerDbContext>(options => options.UseSqlite(connectionString));

        return services;
    }

    /// <summary>
    /// Registers the Canadian Nutrient File (CNF) food lookup as a singleton. The dataset is read
    /// lazily from <paramref name="cnfDirectory"/> and cached; when the files are absent the lookup
    /// reports itself unavailable rather than failing.
    /// </summary>
    /// <param name="services">The service collection to add registrations to.</param>
    /// <param name="cnfDirectory">The directory containing the extracted CNF CSV files.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="cnfDirectory"/> is null or whitespace.</exception>
    public static IServiceCollection AddCnfFoodLookup(this IServiceCollection services, string cnfDirectory)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(cnfDirectory);

        services.AddSingleton<ICnfFoodRepository>(new CnfFoodRepository(new CnfOptions { Directory = cnfDirectory }));

        return services;
    }
}
