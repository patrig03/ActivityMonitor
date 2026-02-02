namespace Database;

using Microsoft.Data.Sqlite;

public static class DatabaseInitializer
{
    public static void EnsureDatabase(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        CreateSchema(cmd);
    }

    private static void CreateSchema(SqliteCommand cmd)
    {
        cmd.CommandText =
        """
        CREATE TABLE IF NOT EXISTS users (
            user_id INTEGER PRIMARY KEY,
            display_name TEXT,
            pin_hash TEXT,
            sync_enabled INTEGER,
            created_at DATETIME
        );

        CREATE TABLE IF NOT EXISTS settings (
            settings_id INTEGER PRIMARY KEY,
            user_id INTEGER,
            focus_mode_enabled INTEGER,
            notification_type TEXT,
            theme TEXT,
            FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
        );

        CREATE TABLE IF NOT EXISTS categories (
            category_id INTEGER PRIMARY KEY,
            name TEXT
        );

        CREATE TABLE IF NOT EXISTS applications (
            app_id INTEGER PRIMARY KEY,
            name TEXT,
            class TEXT,
            process_name TEXT,
            type TEXT,
            category_id INTEGER,
            category_confidence DECIMAL,
            FOREIGN KEY (category_id) REFERENCES categories(category_id)
        );

        CREATE TABLE IF NOT EXISTS sessions (
            session_id INTEGER PRIMARY KEY,
            app_id INTEGER,
            user_id INTEGER,
            start_time DATETIME,
            end_time DATETIME,
            duration_sec INTEGER,
            FOREIGN KEY (app_id) REFERENCES applications(app_id),
            FOREIGN KEY (user_id) REFERENCES users(user_id)
        );

        CREATE TABLE IF NOT EXISTS browser_activity (
            activity_id INTEGER PRIMARY KEY,
            user_id INTEGER,
            app_id INTEGER,
            url TEXT,
            domain TEXT,
            title TEXT,
            tab_id TEXT,
            window_id TEXT,
            start_time DATETIME,
            end_time DATETIME,
            duration_sec INTEGER,
            FOREIGN KEY (user_id) REFERENCES users(user_id),
            FOREIGN KEY (app_id) REFERENCES applications(app_id)
        );

        CREATE TABLE IF NOT EXISTS thresholds (
            threshold_id INTEGER PRIMARY KEY,
            user_id INTEGER,
            category_id INTEGER,
            daily_limit_sec INTEGER,
            weekly_limit_sec INTEGER,
            break_mode_enabled INTEGER,
            FOREIGN KEY (user_id) REFERENCES users(user_id),
            FOREIGN KEY (category_id) REFERENCES categories(category_id)
        );
        
        CREATE TABLE IF NOT EXISTS interventions (
            intervention_id INTEGER PRIMARY KEY,
            user_id INTEGER,
            category_id INTEGER,
            session_id INTEGER,
            triggered_at DATETIME,
            type TEXT,
            intensity INTEGER,
            FOREIGN KEY (user_id) REFERENCES users(user_id),
            FOREIGN KEY (category_id) REFERENCES categories(category_id),
            FOREIGN KEY (session_id) REFERENCES sessions(session_id)
        );
        
        CREATE TABLE IF NOT EXISTS reports_aggregated (
            report_id INTEGER PRIMARY KEY,
            user_id INTEGER,
            period_type TEXT,
            period_start DATETIME,
            period_end DATETIME,
            category_id INTEGER,
            total_duration_sec INTEGER,
            FOREIGN KEY (user_id) REFERENCES users(user_id),
            FOREIGN KEY (category_id) REFERENCES categories(category_id)
        );
        """;

        cmd.ExecuteNonQuery();
    }
}
