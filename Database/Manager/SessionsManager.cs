using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    
    public int InsertSession(SessionDto s)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "sessions") != 0)
        {
            throw new Exception("Database exception in sessions table");
        }
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO sessions (app_id, user_id, start_time, end_time, duration_sec)
            VALUES ($app, $user, $start, $end, $dur);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$app", s.AppId);
        cmd.Parameters.AddWithValue("$user", s.UserId);
        cmd.Parameters.AddWithValue("$start", s.StartTime);
        cmd.Parameters.AddWithValue("$end", (object?)s.EndTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$dur", s.DurationSec);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public SessionDto? GetSession(int sessionId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT session_id, app_id, user_id, start_time, end_time, duration_sec
            FROM sessions
            WHERE session_id = $id;
            """;

        cmd.Parameters.AddWithValue("$id", sessionId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new SessionDto
        {
            SessionId = reader.GetInt32(0),
            AppId = reader.GetInt32(1),
            UserId = reader.GetInt32(2),
            StartTime = reader.GetDateTime(3),
            EndTime = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
            DurationSec = reader.GetInt32(5)
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
                EndTime = r.IsDBNull(4) ? null : r.GetDateTime(4),
                DurationSec = r.GetInt32(5)
            };
        }
    }
    
}