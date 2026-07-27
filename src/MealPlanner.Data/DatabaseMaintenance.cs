using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MealPlanner.Data;

/// <summary>
/// Startup database maintenance: takes a safety backup of the SQLite file before applying any
/// pending EF Core migrations, and enables Write-Ahead Logging (WAL) for safer concurrent access.
/// This protects the household's data across container updates and schema changes.
/// </summary>
public static class DatabaseMaintenance
{
    /// <summary>
    /// Backs up the existing SQLite database file (if any pending migrations exist), then applies
    /// all pending migrations, then enables WAL journal mode. Safe to call on every startup: when
    /// there are no pending migrations nothing is backed up and migration is a no-op.
    /// </summary>
    /// <param name="context">The database context to migrate.</param>
    /// <param name="backupDirectory">
    /// Directory where a timestamped pre-migration backup copy is written. When null or empty,
    /// backups are skipped (for example in tests using an in-memory or throwaway database).
    /// </param>
    /// <param name="logger">Logger for diagnostics.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    public static async Task BackupThenMigrateAsync(
        MealPlannerDbContext context,
        string? backupDirectory,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(logger);

        EnsureDatabaseDirectoryExists(context);

        var pending = (await context.Database.GetPendingMigrationsAsync(cancellationToken)).ToList();

        if (pending.Count > 0)
        {
            TryBackup(context, backupDirectory, logger);
            logger.LogInformation("Applying {Count} pending database migration(s): {Migrations}",
                pending.Count, string.Join(", ", pending));
        }

        await context.Database.MigrateAsync(cancellationToken);
        await EnableWalModeAsync(context, cancellationToken);
    }

    private static void EnsureDatabaseDirectoryExists(MealPlannerDbContext context)
    {
        var dbPath = GetDatabaseFilePath(context);
        if (string.IsNullOrWhiteSpace(dbPath))
        {
            return;
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(dbPath));
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
    }

    private static void TryBackup(MealPlannerDbContext context, string? backupDirectory, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(backupDirectory))
        {
            return;
        }

        var dbPath = GetDatabaseFilePath(context);
        if (dbPath is null || !File.Exists(dbPath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(backupDirectory);
            var stamp = DateTimeOffset.UtcNow.ToString("yyyyMMdd-HHmmss");
            var target = Path.Combine(backupDirectory, $"mealplanner-{stamp}.db");
            File.Copy(dbPath, target, overwrite: false);
            logger.LogInformation("Pre-migration backup written to {BackupPath}", target);
        }
        catch (Exception ex)
        {
            // A failed backup must not prevent the app from starting, but it must be visible.
            logger.LogError(ex, "Pre-migration database backup failed; continuing with migration.");
        }
    }

    private static async Task EnableWalModeAsync(MealPlannerDbContext context, CancellationToken cancellationToken)
    {
        // WAL improves read/write concurrency and crash durability for SQLite.
        await context.Database.ExecuteSqlRawAsync("PRAGMA journal_mode=WAL;", cancellationToken);
    }

    private static string? GetDatabaseFilePath(MealPlannerDbContext context)
    {
        var connectionString = context.Database.GetConnectionString();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return null;
        }

        var builder = new SqliteConnectionStringBuilder(connectionString);
        return string.IsNullOrWhiteSpace(builder.DataSource) ? null : builder.DataSource;
    }
}
