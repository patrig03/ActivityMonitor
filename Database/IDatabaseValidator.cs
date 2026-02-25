using Microsoft.Data.Sqlite;

namespace Database;

public interface IDatabaseValidator
{
    void EnsureDatabase(string dbPath);
    int VerifyTable(SqliteCommand cmd, string tableName);
}