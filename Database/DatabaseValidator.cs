using Microsoft.Data.Sqlite;

namespace Database;

public static class DatabaseValidator
{
    
    private const string UsersTable = """
                                      CREATE TABLE IF NOT EXISTS users (
                                          user_id INTEGER PRIMARY KEY,
                                          display_name TEXT,
                                          pin_hash TEXT,
                                          sync_enabled INTEGER,
                                          created_at DATETIME
                                      );
                                      """;
    private const string SettingsTable = """
                                         CREATE TABLE IF NOT EXISTS settings (
                                             settings_id INTEGER PRIMARY KEY,
                                             user_id INTEGER,
                                             focus_mode_enabled INTEGER,
                                             notification_type TEXT,
                                             theme TEXT,
                                             FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
                                         );
                                         """;
    private const string CategoriesTable = """
                                           CREATE TABLE IF NOT EXISTS categories (
                                               category_id INTEGER PRIMARY KEY,
                                               name TEXT,
                                               description TEXT
                                           );
                                           """;
    private const string ApplicationsTable = """
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
                                             """;
    private const string SessionsTable = """
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
                                         """;
    private const string BrowserActivityTable = """
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
                                                """;
    private const string ThresholdsTable = """
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
                                           """;
    private const string InterventionsTable = """
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
                                              """;
    private const string ReportsAggregatedTable = """
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
    
    public static void EnsureDatabase(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);

        using var connection = new SqliteConnection($"Data Source={dbPath}");
        connection.Open();

        using var cmd = connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();

        CreateTable(cmd, "users");
        CreateTable(cmd, "settings");
        CreateTable(cmd, "categories");
        CreateTable(cmd, "applications");
        CreateTable(cmd, "sessions");
        CreateTable(cmd, "browser_activity");
        CreateTable(cmd, "thresholds");
        CreateTable(cmd, "interventions");
        CreateTable(cmd, "reports_aggregated");
        
        
    }
    private static void CreateTable(SqliteCommand cmd, string tableName)
    {
        var tableCode = GetCodeFromTableName(tableName);
        cmd.CommandText = tableCode;
        cmd.ExecuteNonQuery();
    }
    
    /// Verifies that table contains all expected columns and no extra columns are present
    /// returns -1 if there are columns missing, 1 if there are extra columns and 0 if is equal
    public static int VerifyTable(SqliteCommand cmd, string tableName)
    {
        var tableCode = GetCodeFromTableName(tableName);
        var requiredColumns = ExtractColumnNames(tableCode);
        
        cmd.CommandText = $"PRAGMA table_info({tableName});";

        var found = new HashSet<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            found.Add(r.GetString(1));
        }

        // Check if all required columns are present
        foreach (var col in requiredColumns)
        {
            if (!found.Contains(col))
            {
                return -1;
            }
        }

        // Ensure there are no extra columns beyond those specified
        var extraColumns = found.Except(requiredColumns);
        return extraColumns.Any() ? 1 : 0;
    }
    private static string[] ExtractColumnNames(string createTableCommand)
    {
        if (string.IsNullOrWhiteSpace(createTableCommand))
            return Array.Empty<string>();

        // Find the text inside the first pair of parentheses.
        var start = createTableCommand.IndexOf('(');
        var end   = createTableCommand.LastIndexOf(')');
        if (start < 0 || end <= start)
            throw new ArgumentException("Invalid CREATE TABLE syntax.", nameof(createTableCommand));

        var insideParens = createTableCommand.Substring(start + 1, end - start - 1);

        // Split by commas – each part represents a column definition or a constraint.
        var parts = insideParens.Split(',');

        var columns = new List<string>();

        foreach (var part in parts)
        {
            var trimmed = part.Trim();

            // Skip table constraints such as FOREIGN KEY, UNIQUE, CHECK, etc.
            if (trimmed.StartsWith("FOREIGN KEY", StringComparison.OrdinalIgnoreCase) ||
                trimmed.StartsWith("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)  ||
                trimmed.StartsWith("UNIQUE",      StringComparison.OrdinalIgnoreCase)  ||
                trimmed.StartsWith("CHECK",       StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // The first token is the column name.
            var tokens = trimmed.Split(
                new char[] { ' ', '\t' }, 
                StringSplitOptions.RemoveEmptyEntries);

            if (tokens.Length > 0)
                columns.Add(tokens[0]);
        }

        return columns.ToArray();
    }

    private static string GetCodeFromTableName(string tableName)
    {
        return tableName switch
        {
            "users" => UsersTable,
            "settings" => SettingsTable,
            "categories" => CategoriesTable,
            "applications" => ApplicationsTable,
            "sessions" => SessionsTable,
            "browser_activity" => BrowserActivityTable,
            "thresholds" => ThresholdsTable,
            "interventions" => InterventionsTable,
            "reports_aggregated" => ReportsAggregatedTable,
            _ => throw new ArgumentOutOfRangeException(nameof(tableName), tableName, null)
        };
    } 
}
