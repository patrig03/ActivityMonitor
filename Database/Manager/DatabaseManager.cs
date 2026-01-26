namespace Database.Manager;

using Microsoft.Data.Sqlite;

public partial class DatabaseManager : IDatabaseManager
{
    private readonly SqliteConnection _connection;

    public DatabaseManager(string dbPath)
    {
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
    }

    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
