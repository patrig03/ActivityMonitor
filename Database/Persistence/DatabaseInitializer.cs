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
        EnsureApplicationDeviceIdColumnExists();
        EnsureSessionDeviceIdColumnExists();
        EnsureBrowserActivityDeviceIdColumnExists();
        EnsureThresholdDeviceIdColumnExists();
        RemoveUsersTableConstraint();

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

        if (!context.Settings.Any())
        {
            context.Settings.Add(new SettingsEntity
            {
                SettingsId = 1,
                UserId = 1,
                RefreshTimeSeconds = 10,
                SyncServerAddress = null,
                SyncEmail = null,
                SyncAuthToken = null,
                SyncRemoteUserId = null,
                SyncDeviceId = null,
                SyncLastServerTimeUtc = null
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
                UNIQUE KEY ux_devices_user_fingerprint (user_id, fingerprint),
                KEY ix_devices_user_last_seen (user_id, last_seen_at)
            ) CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;
            """);
    }

    private void EnsureSettingsColumnsExist()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        EnsureSettingsColumn(connection, "sync_server_address", "ALTER TABLE settings ADD COLUMN sync_server_address VARCHAR(512) NULL;");
        EnsureSettingsColumn(connection, "sync_email", "ALTER TABLE settings ADD COLUMN sync_email VARCHAR(255) NULL;");
        EnsureSettingsColumn(connection, "sync_auth_token", "ALTER TABLE settings ADD COLUMN sync_auth_token TEXT NULL;");
        EnsureSettingsColumn(connection, "sync_remote_user_id", "ALTER TABLE settings ADD COLUMN sync_remote_user_id VARCHAR(64) NULL;");
        EnsureSettingsColumn(connection, "sync_device_id", "ALTER TABLE settings ADD COLUMN sync_device_id VARCHAR(64) NULL;");
        EnsureSettingsColumn(connection, "sync_last_server_time_utc", "ALTER TABLE settings ADD COLUMN sync_last_server_time_utc DATETIME(6) NULL;");
        EnsureSettingsColumn(connection, "report_path", "ALTER TABLE settings ADD COLUMN report_path VARCHAR(1024) NULL;");
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

    private void EnsureApplicationDeviceIdColumnExists()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        if (ColumnExists(connection, "applications", "device_id"))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText =
            "ALTER TABLE applications ADD COLUMN device_id INT NULL, DROP COLUMN IF EXISTS position_x, DROP COLUMN IF EXISTS position_y, DROP COLUMN IF EXISTS width, DROP COLUMN IF EXISTS height;";
        command.ExecuteNonQuery();
    }

    private void EnsureSessionDeviceIdColumnExists()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        if (ColumnExists(connection, "sessions", "device_id"))
        {
            return;
        }

        DropForeignKeyIfExists(connection, "sessions", "fk_sessions_users_user_id");
        DropForeignKeyIfExists(connection, "sessions", "FK_sessions_users_user_id");

        using var command = connection.CreateCommand();
        command.CommandText =
            "ALTER TABLE sessions ADD COLUMN device_id INT NULL, DROP COLUMN IF EXISTS user_id;";
        command.ExecuteNonQuery();
    }

    private void EnsureBrowserActivityDeviceIdColumnExists()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        if (ColumnExists(connection, "browser_activity", "device_id"))
        {
            return;
        }

        DropForeignKeyIfExists(connection, "browser_activity", "fk_browser_activity_users_user_id");
        DropForeignKeyIfExists(connection, "browser_activity", "FK_browser_activity_users_user_id");

        using var command = connection.CreateCommand();
        command.CommandText =
            "ALTER TABLE browser_activity ADD COLUMN device_id INT NULL, DROP COLUMN IF EXISTS user_id;";
        command.ExecuteNonQuery();
    }

    private void EnsureThresholdDeviceIdColumnExists()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        if (ColumnExists(connection, "thresholds", "device_id"))
        {
            return;
        }

        DropForeignKeyIfExists(connection, "thresholds", "fk_thresholds_users_user_id");
        DropForeignKeyIfExists(connection, "thresholds", "FK_thresholds_users_user_id");

        using var command = connection.CreateCommand();
        command.CommandText =
            "ALTER TABLE thresholds ADD COLUMN device_id INT NULL, DROP COLUMN IF EXISTS user_id;";
        command.ExecuteNonQuery();
    }

    private void DropForeignKeyIfExists(MySqlConnection connection, string tableName, string fkName)
    {
        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"ALTER TABLE {tableName} DROP FOREIGN KEY IF EXISTS {fkName};";
            command.ExecuteNonQuery();
        }
        catch (MySqlException)
        {
        }
    }

    private void RemoveUsersTableConstraint()
    {
        using var connection = new MySqlConnection(DatabaseConnectionFactory.BuildConnectionString(options));
        connection.Open();

        if (!TableExists(connection, "users"))
        {
            return;
        }

        DropForeignKeyIfExists(connection, "devices", "fk_devices_users_user_id");
        DropForeignKeyIfExists(connection, "devices", "FK_devices_users_user_id");
        DropForeignKeyIfExists(connection, "settings", "fk_settings_users_user_id");
        DropForeignKeyIfExists(connection, "settings", "FK_settings_users_user_id");

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "DROP TABLE IF EXISTS users;";
            command.ExecuteNonQuery();
        }
        catch (MySqlException)
        {
        }
    }

    private bool TableExists(MySqlConnection connection, string tableName)
    {
        using var command = connection.CreateCommand();
        command.CommandText =
            """
            SELECT COUNT(*)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_SCHEMA = @databaseName
              AND TABLE_NAME = @tableName;
            """;
        command.Parameters.AddWithValue("@databaseName", options.Database);
        command.Parameters.AddWithValue("@tableName", tableName);

        return Convert.ToInt32(command.ExecuteScalar()) > 0;
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

    private void EnsureSettingsColumn(MySqlConnection connection, string columnName, string alterSql)
    {
        if (ColumnExists(connection, "settings", columnName))
        {
            return;
        }

        using var command = connection.CreateCommand();
        command.CommandText = alterSql;
        command.ExecuteNonQuery();
    }
}
