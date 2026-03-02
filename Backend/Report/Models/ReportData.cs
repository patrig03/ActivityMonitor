using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Models;

namespace Backend.Report.Models;

public class ReportData
{
    // public required User User { get; set; }
    // public IEnumerable<SessionRecord>? Records { get; set; }
    // public IEnumerable<Intervention>? Interventions { get; set; }
    // public IEnumerable<Threshold>? Thresholds { get; set; }
    // public IEnumerable<ApplicationRecord>? ApplicationData { get; set; }
    // public IEnumerable<BrowserRecord>? BrowserData { get; set; }
    
    public string CategoryName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string SessionDetails { get; set; } = string.Empty;
}