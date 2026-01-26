using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public int InsertUser(UserDto user)
    {
        TableValidator.EnsureTableExists(_connection,
            "users", 
            "user_id", 
            "display_name", 
            "pin_hash", 
            "sync_enabled", 
            "created_at"
        );

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO users (display_name, pin_hash, sync_enabled, created_at)
            VALUES ($display, $pin, $sync, $created);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$display", user.DisplayName);
        cmd.Parameters.AddWithValue("$pin", user.PinHash);
        cmd.Parameters.AddWithValue("$sync", user.SyncEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$created", user.CreatedAt);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public UserDto? GetUser(int userId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM users WHERE user_id = $id";
        cmd.Parameters.AddWithValue("$id", userId);

        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;

        return new UserDto
        {
            UserId = r.GetInt32(0),
            DisplayName = r.GetString(1),
            PinHash = r.GetString(2),
            SyncEnabled = r.GetInt32(3) == 1,
            CreatedAt = r.GetDateTime(4)
        };
    }
}