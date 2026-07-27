using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MealPlanner.Data;

/// <summary>
/// Design-time factory used by the EF Core tools (for example <c>dotnet ef migrations add</c>) to
/// create a <see cref="MealPlannerDbContext"/> without starting the API host. It points at a local
/// SQLite file so migrations can be authored from the <c>MealPlanner.Data</c> project directly.
/// </summary>
public sealed class MealPlannerDbContextFactory : IDesignTimeDbContextFactory<MealPlannerDbContext>
{
    /// <summary>Creates a context instance for design-time tooling.</summary>
    /// <param name="args">Arguments passed by the EF Core tools (unused).</param>
    /// <returns>A configured <see cref="MealPlannerDbContext"/>.</returns>
    public MealPlannerDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseSqlite("Data Source=mealplanner.design.db")
            .Options;

        return new MealPlannerDbContext(options);
    }
}
