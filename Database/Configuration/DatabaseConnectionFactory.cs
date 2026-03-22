using System.Text.RegularExpressions;
using MySqlConnector;

namespace Database.Configuration;

public sealed record ActivityMonitorDatabaseOptions(
    string Host,
    uint Port,
    string Database,
    string Username,
    string Password,
    string SslMode)
{
    public static ActivityMonitorDatabaseOptions LoadFromEnvironment()
    {
        return new ActivityMonitorDatabaseOptions(
            Host: Read("ACTIVITY_MONITOR_DB_HOST", "127.0.0.1"),
            Port: ParsePort(Read("ACTIVITY_MONITOR_DB_PORT", "3306")),
            Database: Read("ACTIVITY_MONITOR_DB_NAME", "activitymonitor"),
            Username: Read("ACTIVITY_MONITOR_DB_USER", "activitymonitor"),
            Password: Environment.GetEnvironmentVariable("ACTIVITY_MONITOR_DB_PASSWORD") ?? string.Empty,
            SslMode: Read("ACTIVITY_MONITOR_DB_SSLMODE", "Preferred"));
    }

    private static string Read(string key, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(key);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    private static uint ParsePort(string rawPort)
    {
        return uint.TryParse(rawPort, out var port) && port > 0 ? port : 3306;
    }
}

public static class DatabaseConnectionFactory
{
    private static readonly Regex DatabaseNamePattern = new("^[A-Za-z0-9_]+$", RegexOptions.Compiled);

    public static string BuildConnectionString()
    {
        return BuildConnectionString(ActivityMonitorDatabaseOptions.LoadFromEnvironment());
    }

    public static string BuildConnectionString(ActivityMonitorDatabaseOptions options)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = options.Host,
            Port = options.Port,
            Database = options.Database,
            UserID = options.Username,
            Password = options.Password,
            PersistSecurityInfo = false,
            Pooling = true,
            AllowUserVariables = false,
            SslMode = ParseSslMode(options.SslMode),
            TreatTinyAsBoolean = true
        };

        return builder.ConnectionString;
    }

    public static ActivityMonitorDatabaseOptions Parse(string connectionString)
    {
        var builder = new MySqlConnectionStringBuilder(connectionString);
        return new ActivityMonitorDatabaseOptions(
            Host: builder.Server,
            Port: builder.Port,
            Database: builder.Database,
            Username: builder.UserID,
            Password: builder.Password,
            SslMode: builder.SslMode.ToString());
    }

    public static string BuildServerConnectionString(ActivityMonitorDatabaseOptions options)
    {
        var builder = new MySqlConnectionStringBuilder(BuildConnectionString(options))
        {
            Database = string.Empty
        };

        return builder.ConnectionString;
    }

    public static string GetDisplayName()
    {
        var options = ActivityMonitorDatabaseOptions.LoadFromEnvironment();
        return $"{options.Host}:{options.Port}/{options.Database}";
    }

    public static string EscapeDatabaseName(string databaseName)
    {
        if (!DatabaseNamePattern.IsMatch(databaseName))
        {
            throw new InvalidOperationException(
                "ACTIVITY_MONITOR_DB_NAME may only contain letters, digits, and underscores.");
        }

        return $"`{databaseName}`";
    }

    private static MySqlSslMode ParseSslMode(string rawValue)
    {
        return Enum.TryParse<MySqlSslMode>(rawValue, ignoreCase: true, out var sslMode)
            ? sslMode
            : MySqlSslMode.Preferred;
    }
}
