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
            INSERT INTO thresholds (threshold_id, user_id, category_id, is_active, intervention_type, daily_limit_sec, weekly_limit_sec)
            VALUES ($id, $user, $category, $is_active, $intervention_type, $daily, $weekly);
            SELECT last_insert_rowid();
            """;

        cmd.Parameters.AddWithValue("id", threshold.Id);
        cmd.Parameters.AddWithValue("$user", threshold.UserId);
        cmd.Parameters.AddWithValue("$category", threshold.CategoryId);
        cmd.Parameters.AddWithValue("is_active", threshold.Active);
        cmd.Parameters.AddWithValue("intervention_type", threshold.InterventionType);
        cmd.Parameters.AddWithValue("$daily", threshold.DailyLimitSec);
        cmd.Parameters.AddWithValue("$weekly", threshold.WeeklyLimitSec);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public ThresholdDto? GetThreshold(int userId, int categoryId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT threshold_id, user_id, category_id, is_active, intervention_type, daily_limit_sec, weekly_limit_sec
            FROM thresholds
            WHERE user_id = $user AND category_id = $category;
            """;

        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$category", categoryId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read()) return null;

        return new ThresholdDto
        {
            Id = reader.GetInt32(0),
            UserId = reader.GetInt32(1),
            CategoryId = reader.GetInt32(2),
            Active = reader.GetBoolean(3),
            InterventionType = reader.GetInt32(4),
            DailyLimitSec = reader.GetInt32(5),
            WeeklyLimitSec = reader.GetInt32(6),
        };
    }

    public IEnumerable<ThresholdDto?> GetAllThresholds()
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText = "SELECT * FROM thresholds";

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            yield return new ThresholdDto
            {
                Id = r.GetInt32(0),
                UserId = r.GetInt32(1),
                CategoryId = r.GetInt32(2),
                Active = r.GetBoolean(3),
                InterventionType = r.GetInt32(4),
                DailyLimitSec = r.GetInt32(5),
                WeeklyLimitSec = r.GetInt32(6),
            };
        }
    }
}