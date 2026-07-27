using MealPlanner.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace MealPlanner.Tests.Data;

/// <summary>
/// Guard-clause tests for <see cref="DatabaseMaintenance"/> startup safety routine.
/// </summary>
[TestFixture]
public class DatabaseMaintenanceTests
{
    [Test]
    public void BackupThenMigrateAsync_NullContext_ThrowsArgumentNullException()
    {
        Assert.ThrowsAsync<ArgumentNullException>(() =>
            DatabaseMaintenance.BackupThenMigrateAsync(null!, backupDirectory: null, NullLogger.Instance));
    }

    [Test]
    public void BackupThenMigrateAsync_NullLogger_ThrowsArgumentNullException()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var context = new MealPlannerDbContext(options);

        Assert.ThrowsAsync<ArgumentNullException>(() =>
            DatabaseMaintenance.BackupThenMigrateAsync(context, backupDirectory: null, logger: null!));
    }
}
