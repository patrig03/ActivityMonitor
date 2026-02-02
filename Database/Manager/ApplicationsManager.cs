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
            "category_id",
            "category_confidence"
        );
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO applications
        (name, class, process_name, type, category_id, category_confidence)
        VALUES ($name, $class, $proc, $type, $cat, $conf);
        SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$name", a.Name);
        cmd.Parameters.AddWithValue("$class", (object?)a.Class ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$proc", (object?)a.ProcessName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$type", a.Type);
        cmd.Parameters.AddWithValue("$cat", a.CategoryId);
        cmd.Parameters.AddWithValue("conf", a.CategoryConfidence);


        return Convert.ToInt32(cmd.ExecuteScalar());
    }
    
    public IEnumerable<int> InsertApplications(IEnumerable<ApplicationDto> apps)
    {
        // Ensure the table exists once – no need to repeat it for every row.
        TableValidator.EnsureTableExists(
            _connection,
            "applications",
            "app_id",
            "name",
            "class",
            "process_name",
            "type",
            "category_id"
        );

        // One transaction = much faster than a separate commit per row.
        using var tx   = _connection.BeginTransaction();
        using var cmd  = _connection.CreateCommand();

        cmd.CommandText =
            """
            INSERT INTO applications
            (name, class, process_name, type, category_id,  category_confidence)
            VALUES ($name, $class, $proc, $type, $cat, $conf);
            SELECT last_insert_rowid();
            """;

        // Create parameters once – they will be reused for every row.
        var nameParam  = cmd.CreateParameter(); nameParam.ParameterName  = "$name";
        var classParam = cmd.CreateParameter(); classParam.ParameterName = "$class";
        var procParam  = cmd.CreateParameter(); procParam.ParameterName  = "$proc";
        var typeParam  = cmd.CreateParameter(); typeParam.ParameterName  = "$type";
        var catParam   = cmd.CreateParameter(); catParam.ParameterName   = "$cat";
        var confParam  = cmd.CreateParameter(); confParam.ParameterName  = "$conf";

        cmd.Parameters.Add(nameParam);
        cmd.Parameters.Add(classParam);
        cmd.Parameters.Add(procParam);
        cmd.Parameters.Add(typeParam);
        cmd.Parameters.Add(catParam);
        cmd.Parameters.Add(confParam);

        foreach (var a in apps)
        {
            nameParam.Value  = a.Name;
            classParam.Value = (object?)a.Class ?? DBNull.Value;
            procParam.Value  = (object?)a.ProcessName ?? DBNull.Value;
            typeParam.Value  = a.Type;
            catParam.Value   = a.CategoryId;
            confParam.Value  = 0;

            yield return Convert.ToInt32(cmd.ExecuteScalar());
        }

        tx.Commit();
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
            CategoryId = r.GetInt32(5),
            CategoryConfidence = r.GetInt32(6)
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
                CategoryId = r.GetInt32(5),
                CategoryConfidence = r.GetInt32(6)
            };
        }
    }
    public IEnumerable<ApplicationDto> GetAllApplications()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM applications";

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
                CategoryId = r.GetInt32(5),
                CategoryConfidence = r.GetInt32(6)
            };
        }
    }

}