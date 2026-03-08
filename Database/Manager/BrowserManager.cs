using Database.DTO;

namespace Database.Manager;

public partial class DatabaseManager
{
    
    public void InsertBrowserActivity(BrowserActivityDto a)
    {
        if (_validator.VerifyTable(_connection.CreateCommand(), "browser_activity") != 0)
        {
            throw new Exception("Database exception in browser_activity table");
        }
        
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
        """
        INSERT INTO browser_activity
        (user_id, app_id, url)
        VALUES
        ($user, $app, $url);
        """;

        cmd.Parameters.AddWithValue("$user", a.UserId);
        cmd.Parameters.AddWithValue("$app", a.AppId);
        cmd.Parameters.AddWithValue("$url", a.Url);

        cmd.ExecuteNonQuery();
    }

    public IEnumerable<BrowserActivityDto> GetBrowserActivityForSession(int sessionId)
    {
        using var cmd = _connection.CreateCommand();
        cmd.CommandText =
            """
            SELECT activity_id, user_id, app_id, url, title
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

            });
        }

        return activities;
    }
    
}