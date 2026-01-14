using System.Reflection;
using BusinessLogic.DTO;
using Microsoft.Data.Sqlite;

namespace BusinessLogic;

public static class DatabaseManager
{
    static readonly string DbPath = "/home/patri/Projects/ActivityMonitor/activity.db";
    
    public static void EnsureDatabase(string tableName)
    {
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        // Check if table exists
        var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT name 
            FROM sqlite_master 
            WHERE type='table' AND name=$table;
            """;
        cmd.Parameters.AddWithValue("$table", tableName);
        var result = cmd.ExecuteScalar();
        if (result is not null) { return; }
        
        // If table doesn't exist, create it
        var createCmd = conn.CreateCommand();
        createCmd.CommandText =
            $"""
             CREATE TABLE {tableName} (
                 Id INTEGER PRIMARY KEY AUTOINCREMENT,
                 WmClass TEXT,
                 Title TEXT,
                 VisibleFor TIME,
                 ActiveFor TIME,
                 LastVisible TIMESTAMP,
                 LastActive TIMESTAMP,
                 UNIQUE(WmClass, Title)
             );
             """;
        createCmd.ExecuteNonQuery();
    }

    public static void InsertOrUpdate(WindowDto win)
    {
        EnsureDatabase("Activity");
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Activity (WmClass, Title, VisibleFor, ActiveFor, LastActive, LastVisible)
            VALUES ($class, $title, $visibleFor, $activeFor, $lastActive, $lastVisible)
            ON CONFLICT(WmClass, Title)
            DO UPDATE SET 
               VisibleFor = $visibleFor,
               ActiveFor = $activeFor,
               LastVisible = $lastVisible,
               LastActive = $lastActive;
        """;
        cmd.Parameters.AddWithValue("$class", win.WmClass);
        cmd.Parameters.AddWithValue("$title", win.Title);
        cmd.Parameters.AddWithValue("$visibleFor", win.VisibleFor);
        cmd.Parameters.AddWithValue("$activeFor", win.ActiveFor);
        cmd.Parameters.AddWithValue("$lastVisible", win.LastVisible);
        cmd.Parameters.AddWithValue("$lastActive", win.LastActive);

        cmd.ExecuteNonQuery();
    }



    public static List<WindowDto> GetAll()
    {
        EnsureDatabase("Activity");
        var list = new List<WindowDto>();

        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        using var cmd = conn.CreateCommand();
        cmd.CommandText =
            """
            SELECT
                WmClass,
                Title,
                VisibleFor,
                ActiveFor,
                LastVisible,
                LastActive
            FROM Activity
            """;

        using var reader = cmd.ExecuteReader();

        while (reader.Read())
        {
            var dto = new WindowDto(
                reader.GetString(0),
                reader.GetString(1),     
                reader.GetTimeSpan(2),
                reader.GetTimeSpan(3),
                reader.GetDateTime(4),
                reader.GetDateTime(5)
            );
            
            list.Add(dto);
        }

        return list;
    }
    
    public static WindowDto? GetWindowEntry(string wmClass, string title)
    {
        EnsureDatabase("Activity");
        using var conn = new SqliteConnection($"Data Source={DbPath}");
        conn.Open();

        var cmd = conn.CreateCommand();
        cmd.CommandText = 
          """
          SELECT
              WmClass,
              Title,
              VisibleFor,
              ActiveFor,
              LastVisible,
              LastActive
          FROM Activity
          WHERE WmClass = $wmClass AND Title = $title
          LIMIT 1;
          """;
        
        cmd.Parameters.AddWithValue("$wmClass", wmClass);
        cmd.Parameters.AddWithValue("$title", title);

        using var reader = cmd.ExecuteReader();
        if (reader.Read())
        {
            return new WindowDto(
                reader.GetString(0),
                reader.GetString(1),     
                reader.GetTimeSpan(2),
                reader.GetTimeSpan(3),
                reader.GetDateTime(4),
                reader.GetDateTime(5)
            );
        }

        return null;
    }

}