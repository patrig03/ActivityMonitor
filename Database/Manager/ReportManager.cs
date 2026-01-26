using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    
    public void InsertAggregatedReport(ReportAggregatedDto r)
    {
        TableValidator.EnsureTableExists(_connection,
            "reports_aggregated", 
            "report_id",
            "user_id",
            "period_type",
            "period_start",
            "period_end",
            "category_id",
            "total_duration_sec"
        );
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO reports_aggregated
        (user_id, period_type, period_start, period_end, category_id, total_duration_sec)
        VALUES ($user, $type, $start, $end, $cat, $total);
        """;

        cmd.Parameters.AddWithValue("$user", r.UserId);
        cmd.Parameters.AddWithValue("$type", r.PeriodType);
        cmd.Parameters.AddWithValue("$start", r.PeriodStart);
        cmd.Parameters.AddWithValue("$end", r.PeriodEnd);
        cmd.Parameters.AddWithValue("$cat", r.CategoryId);
        cmd.Parameters.AddWithValue("$total", r.TotalDurationSec);

        cmd.ExecuteNonQuery();
    }

    public IEnumerable<ReportAggregatedDto> GetReports(
        int userId, string periodType, DateTime periodStart, DateTime periodEnd)
    {

        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT report_id, user_id, period_type, period_start, period_end, category_id, total_duration_sec
            FROM reports_aggregated
            WHERE user_id = $user
              AND period_type = $type
              AND period_start >= $start
              AND period_end <= $end;
            """;

        cmd.Parameters.AddWithValue("$user", userId);
        cmd.Parameters.AddWithValue("$type", periodType);
        cmd.Parameters.AddWithValue("$start", periodStart);
        cmd.Parameters.AddWithValue("$end", periodEnd);

        using var reader = cmd.ExecuteReader();
        var list = new List<ReportAggregatedDto>();

        while (reader.Read())
        {
            list.Add(new ReportAggregatedDto
            {
                ReportId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                PeriodType = reader.GetString(2),
                PeriodStart = reader.GetDateTime(3),
                PeriodEnd = reader.GetDateTime(4),
                CategoryId = reader.GetInt32(5),
                TotalDurationSec = reader.GetInt32(6)
            });
        }

        return list;
    }
}