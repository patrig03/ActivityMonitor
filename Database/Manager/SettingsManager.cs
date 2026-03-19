using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public int InsertSettings(SettingsDto s)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "settings") != 0)
        {
            throw new Exception("Database exception in settings table");
        }
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO settings
            (user_id, refresh_time_seconds)
            VALUES ($user, $time);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$user", s.UserId);
        cmd.Parameters.AddWithValue("$time", s.DeltaTimeSeconds);
        
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int UpdateSettings(SettingsDto s)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "settings") != 0)
        {
            throw new Exception("Database exception in settings table");
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            UPDATE settings
            SET user_id = $user,
                refresh_time_seconds = $time
            WHERE settings_id = $id;
            """;

        cmd.Parameters.AddWithValue("$id", s.Id);
        cmd.Parameters.AddWithValue("$user", s.UserId);
        cmd.Parameters.AddWithValue("$time", s.DeltaTimeSeconds);

        return cmd.ExecuteNonQuery();
    }

    public SettingsDto? GetSettings(int userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM settings WHERE user_id = $uid";
        cmd.Parameters.AddWithValue("$uid", userId);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new SettingsDto
        {
            Id = r.GetInt32(0),
            UserId = r.GetInt32(1),
            DeltaTimeSeconds = r.GetInt32(2),
        };
    }
}
