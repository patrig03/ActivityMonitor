using System.Globalization;
using System.Text;
using Database.DTO;
using Microsoft.Data.Sqlite;

namespace Database.Manager;

public partial class DatabaseManager
{

    
    public List<ReportDto> GetActivityReport()
    {
        var result = new List<ReportDto>();

        using var cmd = _connection.CreateCommand();

        cmd.CommandText = """
                              SELECT 
                                  a.category_id,
                                  a.name,
                                  a.process_name,
                                  s.start_time,
                                  s.end_time
                              FROM sessions s
                              JOIN applications a ON s.app_id = a.app_id
                              ORDER BY a.category_id, a.name, s.start_time;
                          """;

        using var reader = cmd.ExecuteReader();

        var grouped = new Dictionary<(int? CategoryId, string AppName, string ProcessName), List<(DateTime Start, DateTime End)>>();

        while (reader.Read())
        {
            int? categoryId = reader.IsDBNull(0) ? null : reader.GetInt32(0);
            var appName = reader.IsDBNull(1) ? "" : reader.GetString(1);
            var processName = reader.IsDBNull(2) ? "" : reader.GetString(2);

            var start = DateTime.Parse(reader.GetString(3), CultureInfo.InvariantCulture);
            var end   = DateTime.Parse(reader.GetString(4), CultureInfo.InvariantCulture);

            var key = (categoryId, appName, processName);

            if (!grouped.ContainsKey(key))
                grouped[key] = new();

            grouped[key].Add((start, end));
        }

        foreach (var group in grouped)
        {
            double total = 0; // reset per group

            var (categoryId, appName, processName) = group.Key;
            var sessions = group.Value;

            var sb = new StringBuilder();

            foreach (var s in sessions)
            {
                var duration = (s.End - s.Start).TotalSeconds;
                total += duration;
                sb.AppendLine($"{s.Start:HH:mm:ss} → {s.End:HH:mm:ss}   ({duration:0.###}s)");
            }

            result.Add(new ReportDto
            {
                CategoryName = $"Category {categoryId}: {(categoryId == null ? "Unknown" : GetCategory(categoryId??0)?.Name)}",
                ApplicationName = $"Process: {processName}\n{appName}",
                SessionDetails = "Total: " + TimeSpan.FromSeconds(total).ToString(@"hh\:mm\:ss")
            });
        
        }

        return result;
    }
}