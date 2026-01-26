using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    
    public void InsertBrowserActivity(BrowserActivityDto a)
    {
        TableValidator.EnsureTableExists(_connection,
            "browser_activity", 
            "activity_id",
            "user_id",
            "app_id",
            "url",
            "domain",
            "title",
            "tab_id",
            "window_id",
            "start_time",
            "end_time",
            "duration_sec"
        );
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO browser_activity
        (user_id, app_id, url, domain, title, tab_id, window_id, start_time, end_time, duration_sec)
        VALUES
        ($user, $app, $url, $domain, $title, $tab, $window, $start, $end, $dur);
        """;

        cmd.Parameters.AddWithValue("$user", a.UserId);
        cmd.Parameters.AddWithValue("$app", a.AppId);
        cmd.Parameters.AddWithValue("$url", a.Url);
        cmd.Parameters.AddWithValue("$domain", a.Domain);
        cmd.Parameters.AddWithValue("$title", a.Title);
        cmd.Parameters.AddWithValue("$tab", a.TabId);
        cmd.Parameters.AddWithValue("$window", a.WindowId);
        cmd.Parameters.AddWithValue("$start", a.StartTime);
        cmd.Parameters.AddWithValue("$end", (object?)a.EndTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$dur", a.DurationSec);

        cmd.ExecuteNonQuery();
    }

    public IEnumerable<BrowserActivityDto> GetBrowserActivityForSession(int sessionId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT activity_id, user_id, app_id, url, domain, title, tab_id, window_id, start_time, end_time, duration_sec
            FROM browser_activity
            WHERE session_id = $sessionId;
            """;

        cmd.Parameters.AddWithValue("$sessionId", sessionId);

        using var reader = cmd.ExecuteReader();
        var activities = new List<BrowserActivityDto>();

        while (reader.Read())
        {
            activities.Add(new BrowserActivityDto
            {
                ActivityId = reader.GetInt32(0),
                UserId = reader.GetInt32(1),
                AppId = reader.GetInt32(2),
                Url = reader.GetString(3),
                Domain = reader.GetString(4),
                Title = reader.GetString(5),
                TabId = reader.GetString(6),
                WindowId = reader.GetString(7),
                StartTime = reader.GetDateTime(8),
                EndTime = reader.IsDBNull(9) ? null : reader.GetDateTime(9),
                DurationSec = reader.GetInt32(10)
            });
        }

        return activities;
    }
    
}