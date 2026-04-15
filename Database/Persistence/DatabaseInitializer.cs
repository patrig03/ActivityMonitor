using Database.Configuration;
using Microsoft.EntityFrameworkCore;
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
        EnsureDevicesTableExists(context);
        EnsureSettingsColumnsExist();
        EnsureBrowserActivityCategoryColumnExists();

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
                RefreshTimeSeconds = 10,
                SyncServerAddress = null
            });
        }

        context.SaveChanges();
    }

    private static void EnsureDevicesTableExists(ActivityMonitorDbContext context)
    {
        context.Database.ExecuteSqlRaw(
            """
            CREATE TABLE IF NOT EXISTS devices (
                device_id INT NOT NULL AUTO_INCREMENT,
                user_id INT NOT NULL,
                name VARCHAR(255) NOT NULL,
                device_type VARCHAR(64) NOT NULL,
                platform VARCHAR(128) NOT NULL,
                fingerprint VARCHAR(255) NOT NULL,
                status VARCHAR(32) NOT NULL,
                app_version VARCHAR(64) NULL,
                is_trusted TINYINT(1) NOT NULL,
                is_current_device TINYINT(1) NOT NULL,
                created_at DATETIME(6) NOT NULL,
                last_seen_at DATETIME(6) NOT NULL,
                revoked_at DATETIME(6) NULL,
                PRIMARY KEY (device_id),
                CONSTRAINT fk_devices_users_user_id FOREIGN KEY (user_id) REFERENCES users (user_id) ON DELETE CASCADE,
                UNIQUE KEY ux_devices_user_fingerprint (user_id, fingerprint),
                KEY ix_devices_user_last_seen (user_id, last_seen_at)
            ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
            """);
    }

    private void EnsureSettingsColumnsExist()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        if (ColumnExists(connection, "settings", "sync_server_address"))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText =
            "ALTER TABLE settings ADD COLUMN sync_server_address VARCHAR(512) NULL;";
        command.ExecuteNonQuery();
    }

    private void EnsureBrowserActivityCategoryColumnExists()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        if (ColumnExists(connection, "browser_activity", "category_id"))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText =
            "ALTER TABLE browser_activity ADD COLUMN category_id INT NULL;";
        command.ExecuteNonQuery();
    }

    private bool ColumnExists(MySqlConnection connection, string tableName, string columnName)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.COLUMNS
            WHERE TABLE_SCHEMA = @databaseName
              AND TABLE_NAME = @tableName
              AND COLUMN_NAME = @columnName;
            """;
        command.Parameters.AddWithValue("@databaseName", options.Database);
        command.Parameters.AddWithValue("@tableName", tableName);
        command.Parameters.AddWithValue("@columnName", columnName);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
