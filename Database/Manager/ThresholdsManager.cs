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
            INSERT INTO thresholds (user_id, category_id, daily_limit_sec, weekly_limit_sec, break_mode_enabled)
            VALUES ($user, $category, $daily, $weekly, $break);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("$user", threshold.UserId);
        cmd.Parameters.AddWithValue("$category", threshold.CategoryId);
        cmd.Parameters.AddWithValue("$daily", threshold.DailyLimitSec);
        cmd.Parameters.AddWithValue("$weekly", threshold.WeeklyLimitSec);
        cmd.Parameters.AddWithValue("$break", threshold.BreakModeEnabled ? 1 : 0);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public ThresholdDto? GetThreshold(int userId, int categoryId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT threshold_id, user_id, category_id, daily_limit_sec, weekly_limit_sec, break_mode_enabled
            FROM thresholds
            WHERE user_id = $user AND category_id = $category;
            """;

        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$category", categoryId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new ThresholdDto
        {
            ThresholdId = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CategoryId = reader.GetInt32(2),
            DailyLimitSec = reader.GetInt32(3),
            WeeklyLimitSec = reader.GetInt32(4),
            BreakModeEnabled = reader.GetInt32(5) != 0
        };
    }
}