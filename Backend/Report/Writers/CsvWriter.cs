using System.Text;
using Backend.Interventions.Models;
using Backend.Models;
using Backend.Report.Models;

public class CsvWriter
{
    private string OutputPath { get; set; }
    
    public CsvWriter(string outputPath)
    {
        OutputPath = outputPath;
    }
    
    public bool WriteToFile(IEnumerable<ReportData> data)
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(OutputPath, false, Encoding.UTF8))
            {
                // Write CSV header
                writer.WriteLine(GetCsvHeader());
                
                // Write each report data row
                foreach (var report in data)
                {
                    WriteReportData(writer, report);
                }
            }
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
    
    private string GetCsvHeader()
    {
        var headers = new[]
        {
            "CategoryId",
            "CategoryName",
            "ProcessName",
            "WindowName",
            "ClassName",
            "WindowDurationMinutes",
            "ProcessTotalDurationMinutes",
            "InterventionId",
            "InterventionType",
            "InterventionTriggeredAt",
            "ThresholdId",
            "ThresholdActive",
            "ThresholdInterventionType",
            "ThresholdDailyLimitMinutes",
            "ThresholdWeeklyLimitMinutes",
            "BrowserRecordId",
            "BrowserId",
            "BrowserUrl",
            "BrowserDomain"
        };
        
        return string.Join(",", headers);
    }
    
    private void WriteReportData(StreamWriter writer, ReportData report)
    {
        // Process each application's windows
        foreach (var process in report.Applications)
        {
            foreach (var window in process.Windows)
            {
                WriteWindowData(writer, report, process, window);
            }
        }
        
        // If there are no windows/applications, still write category info with empty values
        if (!report.Applications.Any())
        {
            WriteWindowData(writer, report, null, null);
        }
    }
    
    private void WriteWindowData(StreamWriter writer, ReportData report, ProcessUsage process, WindowUsage window)
    {
        // Get interventions for this category
        var interventions = report.Interventions?.ToList() ?? new List<Intervention>();
        var thresholds = report.Thresholds?.ToList() ?? new List<Threshold>();
        var browserRecords = report.BrowserDetails?.ToList() ?? new List<BrowserRecord>();
        
        // Determine max rows needed (based on max count of interventions, thresholds, and browser records)
        int maxRows = Math.Max(
            Math.Max(
                interventions.Any() ? interventions.Count : 1,
                thresholds.Any() ? thresholds.Count : 1
            ),
            browserRecords.Any() ? browserRecords.Count : 1
        );
        
        for (int i = 0; i < maxRows; i++)
        {
            var fields = new List<string>();
            
            // Category information (always present)
            fields.Add(EscapeCsvField(report.Category?.Id.ToString() ?? ""));
            fields.Add(EscapeCsvField(report.Category?.Name ?? ""));
            
            // Process/Window information (may be null if no applications)
            fields.Add(EscapeCsvField(process?.ProcessName ?? ""));
            fields.Add(EscapeCsvField(window?.WindowName ?? ""));
            fields.Add(EscapeCsvField(window?.ClassName ?? ""));
            fields.Add(EscapeCsvField(window?.TotalDuration.TotalMinutes.ToString("F2") ?? ""));
            fields.Add(EscapeCsvField(process?.TotalDuration.TotalMinutes.ToString("F2") ?? ""));
            
            // Intervention information (cycle through interventions)
            if (i < interventions.Count)
            {
                var intervention = interventions[i];
                fields.Add(EscapeCsvField(intervention.Id.ToString()));
                fields.Add(EscapeCsvField(intervention.Type));
                fields.Add(EscapeCsvField(intervention.TriggeredAt.ToString("yyyy-MM-dd HH:mm:ss")));
            }
            else
            {
                fields.Add(""); // InterventionId
                fields.Add(""); // InterventionType
                fields.Add(""); // InterventionTriggeredAt
            }
            
            // Threshold information (cycle through thresholds)
            if (i < thresholds.Count)
            {
                var threshold = thresholds[i];
                fields.Add(EscapeCsvField(threshold.Id.ToString()));
                fields.Add(EscapeCsvField(threshold.Active.ToString()));
                fields.Add(EscapeCsvField(threshold.InterventionType ?? ""));
                fields.Add(EscapeCsvField(threshold.DailyLimit?.TotalMinutes.ToString("F2") ?? ""));
                fields.Add(EscapeCsvField(threshold.WeeklyLimit?.TotalMinutes.ToString("F2") ?? ""));
            }
            else
            {
                fields.Add(""); // ThresholdId
                fields.Add(""); // ThresholdActive
                fields.Add(""); // ThresholdInterventionType
                fields.Add(""); // ThresholdDailyLimitMinutes
                fields.Add(""); // ThresholdWeeklyLimitMinutes
            }
            
            // Browser record information (cycle through browser records)
            if (i < browserRecords.Count)
            {
                var browser = browserRecords[i];
                fields.Add(EscapeCsvField(browser.Id.ToString()));
                fields.Add(EscapeCsvField(browser.BrowserId.ToString()));
                fields.Add(EscapeCsvField(browser.Url));
                fields.Add(EscapeCsvField(browser.Domain));
            }
            else
            {
                fields.Add(""); // BrowserRecordId
                fields.Add(""); // BrowserId
                fields.Add(""); // BrowserUrl
                fields.Add(""); // BrowserDomain
            }
            
            writer.WriteLine(string.Join(",", fields));
        }
    }
    
    private string EscapeCsvField(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";
        
        // If the field contains comma, newline, or double quote, enclose in double quotes
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            // Escape double quotes by doubling them
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }
        
        return field;
    }
}
