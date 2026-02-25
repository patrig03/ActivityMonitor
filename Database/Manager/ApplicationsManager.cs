using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
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
            (name, class, process_name, type, category_id)
            VALUES ($name, $class, $proc, $type, $cat);
            SELECT last_insert_rowid();
            """;

        // Create parameters once – they will be reused for every row.
        var nameParam  = cmd.CreateParameter(); nameParam.ParameterName  = "$name";
        var classParam = cmd.CreateParameter(); classParam.ParameterName = "$class";
        var procParam  = cmd.CreateParameter(); procParam.ParameterName  = "$proc";
        var typeParam  = cmd.CreateParameter(); typeParam.ParameterName  = "$type";
        var catParam   = cmd.CreateParameter(); catParam.ParameterName   = "$cat";

        cmd.Parameters.Add(nameParam);
        cmd.Parameters.Add(classParam);
        cmd.Parameters.Add(procParam);
        cmd.Parameters.Add(typeParam);
        cmd.Parameters.Add(catParam);

        foreach (var a in apps)
        {
            nameParam.Value  = a.Name;
            classParam.Value = (object?)a.Class ?? DBNull.Value;
            procParam.Value  = (object?)a.ProcessName ?? DBNull.Value;
            typeParam.Value  = a.Type;
            catParam.Value   = a.CategoryId;

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


    public bool IsInDb(ApplicationDto applicationDto)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM applications WHERE class = $class AND process_name = $proc";
        cmd.Parameters.AddWithValue("$class", applicationDto.Class);
        cmd.Parameters.AddWithValue("$proc", applicationDto.ProcessName);

        using var r = cmd.ExecuteReader();
        return r.Read();
    }
    
    public int UpdateOrInsertApplication(ApplicationDto app)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "applications") != 0)
        {
            throw new Exception("Database exception in applications table");
        }
        
        using var cmd = _connection.CreateCommand();
        
        // Check if the application already exists in the database.
        cmd.CommandText = "SELECT * FROM applications WHERE class = $class AND process_name = $proc";
        cmd.Parameters.AddWithValue("$class", app.Class);
        cmd.Parameters.AddWithValue("$proc", app.ProcessName);

        using var reader = cmd.ExecuteReader();

        if (reader.Read())
        {
            // Application already exists, update it
            int appId = reader.GetInt32(0);  // Assuming the first column is the ID of the application

            // Update the existing record with new data.
            using var updateCmd = _connection.CreateCommand();
            updateCmd.CommandText =
                "UPDATE applications SET name = $name, type = $type, category_id = $cat WHERE app_id = $id";
            updateCmd.Parameters.AddWithValue("$name", app.Name);
            updateCmd.Parameters.AddWithValue("$type", app.Type);
            updateCmd.Parameters.AddWithValue("$cat", app.CategoryId);
            updateCmd.Parameters.AddWithValue("$id", appId);

            updateCmd.ExecuteNonQuery();
            
            return appId;
        }

        return InsertApplication(app);
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