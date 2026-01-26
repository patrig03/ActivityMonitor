using Microsoft.Data.Sqlite;

namespace Database;

public static class TableValidator
{
    public static void EnsureTableExists(
        SqliteConnection connection,
        string tableName,
        params string[] requiredColumns)
    {
        using var cmd = connection.CreateCommand();
        cmd.CommandText = $"PRAGMA table_info({tableName});";

        var found = new HashSet<string>();
        using var r = cmd.ExecuteReader();
        while (r.Read())
            found.Add(r.GetString(1));

        foreach (var col in requiredColumns)
        {
            if (!found.Contains(col))
                throw new InvalidOperationException(
                    $"Table '{tableName}' missing column '{col}'");
        }
    }
}
