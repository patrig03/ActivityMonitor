using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    public int InsertThreshold(ThresholdDto threshold)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "thresholds") != 0)
        {
            throw new Exception("Database exception in thresholds table");
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            INSERT INTO thresholds
            (user_id, category_id, app_id, is_active, target_type, intervention_type, duration_type, daily_limit_sec, session_limit_sec)
            VALUES
            ($user, $category, $app, $is_active, $target_type, $intervention_type, $duration_type, $daily, $session);
            SELECT last_insert_rowid();
            """;

        AddThresholdParameters(cmd, threshold);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public int UpdateThreshold(ThresholdDto threshold)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "thresholds") != 0)
        {
            throw new Exception("Database exception in thresholds table");
        }

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            UPDATE thresholds
            SET user_id = $user,
                category_id = $category,
                app_id = $app,
                is_active = $is_active,
                target_type = $target_type,
                intervention_type = $intervention_type,
                duration_type = $duration_type,
                daily_limit_sec = $daily,
                session_limit_sec = $session
            WHERE threshold_id = $id;
            """;

        AddThresholdParameters(cmd, threshold);
        cmd.Parameters.AddWithValue("$id", threshold.Id);

        return cmd.ExecuteNonQuery();
    }

    public int UpsertThreshold(ThresholdDto threshold)
    {
        if (threshold.Id > 0)
        {
            return UpdateThreshold(threshold);
        }

        return InsertThreshold(threshold);
    }

    public ThresholdDto? GetThreshold(int userId, int categoryId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT threshold_id, user_id, category_id, app_id, is_active, target_type, intervention_type, duration_type, daily_limit_sec, session_limit_sec
            FROM thresholds
            WHERE user_id = $user AND category_id = $category
            LIMIT 1;
            """;

        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$category", categoryId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return ReadThreshold(reader);
    }

    public IEnumerable<ThresholdDto?> GetAllThresholds()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT threshold_id, user_id, category_id, app_id, is_active, target_type, intervention_type, duration_type, daily_limit_sec, session_limit_sec
            FROM thresholds;
            """;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            yield return ReadThreshold(reader);
        }
    }

    public void DeleteThreshold(ThresholdDto threshold)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "DELETE FROM thresholds WHERE threshold_id = $id;";
        cmd.Parameters.AddWithValue("$id", threshold.Id);
        cmd.ExecuteNonQuery();
    }

    private static void AddThresholdParameters(Microsoft.Data.Sqlite.SqliteCommand cmd, ThresholdDto threshold)
    {
        cmd.Parameters.AddWithValue("$user", threshold.UserId);
        cmd.Parameters.AddWithValue("$category", DbIntOrNull(threshold.CategoryId));
        cmd.Parameters.AddWithValue("$app", DbIntOrNull(threshold.AppId));
        cmd.Parameters.AddWithValue("$is_active", threshold.Active);
        cmd.Parameters.AddWithValue("$target_type", threshold.TargetType);
        cmd.Parameters.AddWithValue("$intervention_type", threshold.InterventionType);
        cmd.Parameters.AddWithValue("$duration_type", threshold.DurationType);
        cmd.Parameters.AddWithValue("$daily", threshold.DailyLimitSec);
        cmd.Parameters.AddWithValue("$session", threshold.SessionLimitSec);
    }

    private static ThresholdDto ReadThreshold(Microsoft.Data.Sqlite.SqliteDataReader reader)
    {
        return new ThresholdDto
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CategoryId = reader.IsDBNull(2) ? 0 : reader.GetInt32(2),
            AppId = reader.IsDBNull(3) ? 0 : reader.GetInt32(3),
            Active = reader.GetBoolean(4),
            TargetType = reader.IsDBNull(5) ? "Category" : reader.GetString(5),
            InterventionType = reader.IsDBNull(6) ? "Notification" : reader.GetString(6),
            DurationType = reader.IsDBNull(7) ? "Daily" : reader.GetString(7),
            DailyLimitSec = reader.IsDBNull(8) ? 0 : reader.GetInt32(8),
            SessionLimitSec = reader.IsDBNull(9) ? 0 : reader.GetInt32(9),
        };
    }

    private static object DbIntOrNull(int value)
    {
        return value == 0 ? DBNull.Value : value;
    }
}
