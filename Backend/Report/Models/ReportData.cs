using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Models;

namespace Backend.Report.Models;

public class ReportData
{
    public required User User { get; set; }
    public required Category Category { get; set; }
    public required IEnumerable<ApplicationRecord> Applications { get; set; }
    public required IEnumerable<SessionRecord> SessionDetails { get; set; }
    public required IEnumerable<Intervention> Interventions { get; set; }
    public required IEnumerable<BrowserRecord> BrowserDetails { get; set; }
    public required IEnumerable<Threshold> Thresholds { get; set; }
}