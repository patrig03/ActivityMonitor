using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    /// <summary>
    /// Inserts an application into the database.
    /// </summary>
    /// <param name="a"></param>
    /// <returns>int: id of the newly inserted app</returns>
    /// <exception cref="Exception"></exception>
    public int InsertApplication(ApplicationDto a)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "applications") != 0)
        {
            throw new Exception("Database exception in applications table");
        }
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO applications
        (name, class, process_name, category_id, window_id)
        VALUES ($name, $class, $proc, $cat, $wid);
        SELECT last_insert_rowid();
        """;

        cmd.Parameters.AddWithValue("$name", (object?)a.WindowTitle ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$class", (object?)a.ClassName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$proc", (object?)a.ProcessName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$cat", (object?)a.CategoryId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$wid", (object?)a.WindowId ?? DBNull.Value);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    /// <summary>
    /// Inserts or updates an application in the database.
    /// </summary>
    /// <param name="a"></param>
    /// <returns>id of the inserted or updated row</returns>
    /// <exception cref="Exception"></exception>
    public int UpsertApplication(ApplicationDto a)
    {
        if (IsInDb(a) != null)
        {
            return UpdateApplication(a) ?? throw new Exception("Could not update application");
        }

        return InsertApplication(a);
    }
    
    public IEnumerable<int> InsertApplications(IEnumerable<ApplicationDto> apps)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "applications") != 0)
        {
            throw new Exception("Database exception in applications table");
        }

        // One transaction = much faster than a separate commit per row.
        using var tx   = _connection.BeginTransaction();
        using var cmd  = _connection.CreateCommand();

        cmd.CommandText =
            """
            INSERT INTO applications
            (name, class, process_name, category_id, window_id)
            VALUES ($name, $class, $proc, $cat, $wid);
            SELECT last_insert_rowid();
            """;

        // Create parameters once – they will be reused for every row.
        var nameParam  = cmd.CreateParameter(); nameParam.ParameterName  = "$name";
        var classParam = cmd.CreateParameter(); classParam.ParameterName = "$class";
        var procParam  = cmd.CreateParameter(); procParam.ParameterName  = "$proc";
        var catParam   = cmd.CreateParameter(); catParam.ParameterName   = "$cat";
        var widParam   = cmd.CreateParameter(); catParam.ParameterName   = "$wid";

        cmd.Parameters.Add(nameParam);
        cmd.Parameters.Add(classParam);
        cmd.Parameters.Add(procParam);
        cmd.Parameters.Add(catParam);
        cmd.Parameters.Add(widParam);

        foreach (var a in apps)
        {
            nameParam.Value  = (object?)a.WindowTitle ?? DBNull.Value;
            classParam.Value = (object?)a.ClassName ?? DBNull.Value;
            procParam.Value  = (object?)a.ProcessName ?? DBNull.Value;
            catParam.Value   = (object?)a.CategoryId ?? DBNull.Value;
            widParam.Value   = (object?)a.WindowId ?? DBNull.Value;

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
            Id = r.GetInt32(0),
            CategoryId = r.IsDBNull(1) ? null : r.GetInt32(1),
            WindowTitle = r.IsDBNull(2) ? null : r.GetString(2),
            ClassName = r.IsDBNull(3) ? null : r.GetString(3),
            ProcessName = r.IsDBNull(4) ? null : r.GetString(4),
            WindowId = r.IsDBNull(5) ? null : r.GetInt32(5),
        };
    }

    /// <summary>
    /// checks if the application is already in the database
    /// </summary>
    /// <param name="applicationDto"></param>
    /// <returns>id of the application if in database, else null</returns>
    public int? IsInDb(ApplicationDto applicationDto)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM applications WHERE name = $name AND class = $class AND process_name = $proc";
        cmd.Parameters.AddWithValue("name", applicationDto.WindowTitle);
        cmd.Parameters.AddWithValue("$class", applicationDto.ClassName);
        cmd.Parameters.AddWithValue("$proc", applicationDto.ProcessName);

        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return r.GetInt32(0);
        }
        return null;
    }
    
    public int? UpdateApplication(ApplicationDto app)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "applications") != 0)
        {
            throw new Exception("Database exception in applications table");
        }
        
        using var cmd = _connection.CreateCommand();
        
        var appId = IsInDb(app);
        if (appId == null) return null;
        
        using var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText =
            "UPDATE applications SET name = $name, process_name = $processName, class = $class, category_id = $cat, window_id = $wid WHERE app_id = $id";
        updateCmd.Parameters.AddWithValue("$name", (object?)app.WindowTitle ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$processName", (object?)app.ProcessName ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$class", (object?)app.ClassName ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$cat", (object?)app.CategoryId ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$wid", (object?)app.WindowId ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$id", appId);

        updateCmd.ExecuteNonQuery();
        
        return appId;
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
                Id = r.GetInt32(0),
                CategoryId = r.IsDBNull(1) ? null : r.GetInt32(1),
                WindowTitle = r.IsDBNull(2) ? null : r.GetString(2),
                ClassName = r.IsDBNull(3) ? null : r.GetString(3),
                ProcessName = r.IsDBNull(4) ? null : r.GetString(4),
                WindowId = r.IsDBNull(5) ? null : r.GetInt32(5),

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
                Id = r.GetInt32(0),
                CategoryId = r.IsDBNull(1) ? null : r.GetInt32(1),
                WindowTitle = r.IsDBNull(2) ? null : r.GetString(2),
                ClassName = r.IsDBNull(3) ? null : r.GetString(3),
                ProcessName = r.IsDBNull(4) ? null : r.GetString(4),
                WindowId = r.IsDBNull(5) ? null : r.GetInt32(5),
            };
        }
    }

}
