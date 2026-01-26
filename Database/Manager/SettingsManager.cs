using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public int InsertSettings(SettingsDto s)
    {
        TableValidator.EnsureTableExists(_connection,
            "settings", 
            "settings_id",
            "user_id",
            "focus_mode_enabled",
            "notification_type",
            "theme"
        );
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO settings
            (user_id, focus_mode_enabled, notification_type, theme)
            VALUES ($user, $focus, $notif, $theme);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$user", s.UserId);
        cmd.Parameters.AddWithValue("$focus", s.FocusModeEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$notif", s.NotificationType);
        cmd.Parameters.AddWithValue("$theme", s.Theme);

        return Convert.ToInt32(cmd.ExecuteScalar());
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
            SettingsId = r.GetInt32(0),
            UserId = r.GetInt32(1),
            FocusModeEnabled = r.GetInt32(2) == 1,
            NotificationType = r.IsDBNull(3) ? null : r.GetString(3),
            Theme = r.IsDBNull(4) ? null : r.GetString(4)
        };
    }
}