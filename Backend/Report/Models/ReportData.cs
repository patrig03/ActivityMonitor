using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Models;

namespace Backend.Report.Models;


public class ProcessUsage
{
    public string ProcessName { get; set; } = "";
    public TimeSpan TotalDuration { get; set; }
    public IEnumerable<WindowUsage> Windows { get; set; } = Enumerable.Empty<WindowUsage>();
}

public class WindowUsage
{
    public string WindowName { get; set; } = "";
    public string ClassName { get; set; } = "";
    public TimeSpan TotalDuration { get; set; }
    public IEnumerable<SessionRecord> Sessions { get; set; } = Enumerable.Empty<SessionRecord>();
}

public class ReportData
{
    public required User User { get; set; }
    public required Category Category { get; set; }

    public required IEnumerable<ProcessUsage> Applications { get; set; }

    public required IEnumerable<Intervention> Interventions { get; set; }
    public required IEnumerable<BrowserRecord> BrowserDetails { get; set; }
    public required IEnumerable<Threshold> Thresholds { get; set; }
}


// public class ReportData
// {
//     public required User User { get; set; }
//     public required Category Category { get; set; }
//     public required IEnumerable<ApplicationRecord> Applications { get; set; }
//     public required IEnumerable<SessionRecord> SessionDetails { get; set; }
//     public required IEnumerable<Intervention> Interventions { get; set; }
//     public required IEnumerable<BrowserRecord> BrowserDetails { get; set; }
//     public required IEnumerable<Threshold> Thresholds { get; set; }
// }