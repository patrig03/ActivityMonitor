using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    
    /// <summary>
    /// checks if the session is already in the database
    /// </summary>
    /// <param name="s"></param>
    /// <returns>id of the session if in the database, else null</returns>
    public int? IsInDb(SessionDto s)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM sessions WHERE app_id = $appid AND user_id = $userid AND start_time = $start";
        cmd.Parameters.AddWithValue("$appid", (object?)s.AppId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$userid", (object?)s.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("start", (object?)s.StartTime ?? DBNull.Value);

        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            return r.GetInt32(0);
        }
        return null;
    }
    
    public int InsertSession(SessionDto s)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "sessions") != 0)
        {
            throw new Exception("Database exception in sessions table");
        }
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO sessions (app_id, user_id, start_time, end_time)
            VALUES ($app, $user, $start, $end);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$app", (object?)s.AppId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$user", (object?)s.UserId ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$start", (object?)s.StartTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$end", (object?)s.EndTime ?? DBNull.Value);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int? UpdateSession(SessionDto s)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "applications") != 0)
        {
            throw new Exception("Database exception in applications table");
        }
        
        using var cmd = _connection.CreateCommand();
        
        var appId = IsInDb(s);
        if (appId == null) return null;
        
        using var updateCmd = _connection.CreateCommand();
        updateCmd.CommandText =
            "UPDATE sessions SET user_id = $userid, app_id = $appid, start_time = $start, end_time = $end WHERE session_id = $id";
        updateCmd.Parameters.AddWithValue("$userid", (object?)s.UserId ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$appid", (object?)s.AppId ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$start", (object?)s.StartTime ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$end", (object?)s.EndTime ?? DBNull.Value);
        updateCmd.Parameters.AddWithValue("$id", appId);

        updateCmd.ExecuteNonQuery();
        
        return appId;
    }
    
    public int UpsertSession(SessionDto s)
    {
        if (IsInDb(s) != null)
        {
            return UpdateSession(s) ?? throw new Exception("Could not update application");
        }

        return InsertSession(s);
    }
    
    public SessionDto? GetSession(int sessionId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT session_id, app_id, user_id, start_time, end_time
            FROM sessions
            WHERE session_id = $id;
            """;

        cmd.Parameters.AddWithValue("$id", sessionId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new SessionDto
        {
            SessionId = reader.GetInt32(0),
            AppId     = reader.GetInt32(1),
            UserId    = reader.GetInt32(2),
            StartTime = reader.GetDateTime(3),
            EndTime   = reader.IsDBNull(4) ? null : reader.GetDateTime(4)
        };
    }
    
    public IEnumerable<SessionDto> GetSessionsForUser(int userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM sessions WHERE user_id = $uid";
        cmd.Parameters.AddWithValue("$uid", userId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new SessionDto
            {
                SessionId = r.GetInt32(0),
                AppId = r.GetInt32(1),
                UserId = r.GetInt32(2),
                StartTime = r.GetDateTime(3),
                EndTime = r.GetDateTime(4),
            };
        }
    }

    public IEnumerable<SessionDto> GetSessionsByCategory(int categoryId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = """
                          SELECT s.*
                          FROM sessions s
                                   INNER JOIN applications a ON s.app_id = a.app_id
                          WHERE a.category_id = $category_id
                          """;
        cmd.Parameters.AddWithValue("$category_id", categoryId);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new SessionDto
            {
                SessionId = r.GetInt32(0),
                AppId = r.GetInt32(1),
                UserId = r.GetInt32(2),
                StartTime = r.GetDateTime(3),
                EndTime = r.GetDateTime(4),
            };
        }
    }

    public int GetSessionDurationForCategory(int categoryId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =         """
                                  SELECT SUM((julianday(s.end_time) - julianday(s.start_time)) * 86400.0) AS total_seconds
                                  FROM sessions s
                                  INNER JOIN applications a ON s.app_id = a.app_id
                                  WHERE a.category_id = $category_id;
                                  """;
        cmd.Parameters.AddWithValue("$category_id", categoryId);

        using var r = cmd.ExecuteReader();
        var duration = 0;
        if (r.Read())
        {
            duration = r.GetInt32(0) ;
        }
        return duration;
    }
    
}