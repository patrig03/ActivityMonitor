using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public int InsertApplication(ApplicationDto a)
    {
        TableValidator.EnsureTableExists(_connection,
            "applications", 
            "app_id",
            "name",
            "class",
            "process_name",
            "type",
            "category_id"
        );
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO applications
        (name, class, process_name, type, category_id)
        VALUES ($name, $class, $proc, $type, $cat);
        SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$name", a.Name);
        cmd.Parameters.AddWithValue("$class", (object?)a.Class ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$proc", (object?)a.ProcessName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", a.Type);
        cmd.Parameters.AddWithValue("$cat", a.CategoryId);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public ApplicationDto? GetApplication(int appId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM applications WHERE app_id = $id";
        cmd.Parameters.AddWithValue("$id", appId);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new ApplicationDto
        {
            AppId = r.GetInt32(0),
            Name = r.GetString(1),
            Class = r.IsDBNull(2) ? null : r.GetString(2),
            ProcessName = r.IsDBNull(3) ? null : r.GetString(3),
            Type = r.GetString(4),
            CategoryId = r.GetInt32(5)
        };
    }

    public IEnumerable<ApplicationDto> GetApplicationsByCategory(int categoryId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            "SELECT * FROM applications WHERE category_id = $cat";
        cmd.Parameters.AddWithValue("$cat", categoryId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new ApplicationDto
            {
                AppId = r.GetInt32(0),
                Name = r.GetString(1),
                Class = r.IsDBNull(2) ? null : r.GetString(2),
                ProcessName = r.IsDBNull(3) ? null : r.GetString(3),
                Type = r.GetString(4),
                CategoryId = r.GetInt32(5)
            };
        }
    }
}