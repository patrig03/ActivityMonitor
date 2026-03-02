namespace Database.Manager;

using Microsoft.Data.Sqlite;

public partial class DatabaseManager : IDatabaseManager
{
    private readonly SqliteConnection _connection;
    private readonly IDatabaseValidator _validator = new DatabaseValidator();
    private readonly string _dbPath;

    public DatabaseManager(string dbPath)
    {
        _validator.EnsureDatabase(dbPath);
        _dbPath = dbPath;
        _connection = new SqliteConnection($"Data Source={dbPath}");
        _connection.Open();
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
    }
    
    public void EnsureDatabase() => _validator.EnsureDatabase(_dbPath);
    
    public void Dispose()
    {
        _connection.Close();
        _connection.Dispose();
    }
}
