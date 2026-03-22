using Database.Configuration;
using MySqlConnector;

namespace Database.Persistence;

public interface IDatabaseInitializer
{
    void EnsureDatabase();
}

public sealed class MySqlDatabaseInitializer(
    ActivityMonitorDatabaseOptions options,
    IActivityMonitorDbContextFactory contextFactory) : IDatabaseInitializer
{
    public void EnsureDatabase()
    {
        EnsureCatalogExists();

        using var context = contextFactory.CreateDbContext();
        context.Database.EnsureCreated();

        SeedDefaults(context);
    }

    private void EnsureCatalogExists()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildServerConnectionString(options));
        connection.Open();

        using var command = connection.CreateCommand();
        command.CommandText =
            $"CREATE DATABASE IF NOT EXISTS {DatabaseConnectionFactory.EscapeDatabaseName(options.Database)} CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
        command.ExecuteNonQuery();
    }

    private static void SeedDefaults(ActivityMonitorDbContext context)
    {
        if (!context.Categories.Any())
        {
            context.Categories.AddRange(DefaultSeedData.Categories);
        }

        if (!context.Users.Any())
        {
            context.Users.Add(new UserEntity
            {
                UserId = 1,
                DisplayName = "Default user",
                PinHash = string.Empty,
                SyncEnabled = false,
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!context.Settings.Any())
        {
            context.Settings.Add(new SettingsEntity
            {
                SettingsId = 1,
                UserId = 1,
                RefreshTimeSeconds = 10
            });
        }

        context.SaveChanges();
    }
}
