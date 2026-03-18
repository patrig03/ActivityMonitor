using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Models;
using Backend.Report.Models;
using Backend.Report.Writers;
using Database.Manager;

namespace Backend.Report;

public class ReportMaker
{
    private IDatabaseManager Manager { get; }
    
    private CsvWriter CsvWriter { get; set; }
    private PdfWriter PdfWriter { get; set; }

    public ReportMaker(IDatabaseManager manager)
    {
        Manager = manager;
    }

    public IEnumerable<ReportData> MakeReportData()
    {
        var categories = Manager.GetAllCategories();

        var interventions = Manager.GetInterventionsForUser(1);
        var browserRecords = Manager.GetAllBrowserActivity();
        var thresholds = Manager.GetAllThresholds();

        var interventionsList = interventions.Select(Intervention.FromDto).ToList();
        var browserDetailsList = browserRecords.Select(BrowserRecord.FromDto).ToList();
        var thresholdsList = thresholds.Select(Threshold.FromDto).ToList();

        return categories.Select(category => {
            var apps = Manager.GetApplicationsByCategory(category.CategoryId)
                .Select(ApplicationRecord.FromDto)
                .ToDictionary(a => a.Id);

            var sessions = Manager.GetSessionsByCategory(category.CategoryId)
                .Select(SessionRecord.FromDto)
                .ToList();

            return new { category, apps, sessions };
        })
        .Where(x => x.sessions.Any())
        .Select(x =>
        {
            var processes =
                x.sessions
                .Where(s => x.apps.ContainsKey(s.ApplicationId))
                .GroupBy(s => x.apps[s.ApplicationId].ProcessName)
                .Select(processGroup =>
                {
                    var windows =
                        processGroup
                        .GroupBy(s => x.apps[s.ApplicationId].WindowName)
                        .Select(windowGroup =>
                        {
                            var app = x.apps[windowGroup.First().ApplicationId];

                            var duration = windowGroup
                                .Aggregate(TimeSpan.Zero,
                                    (sum, s) => sum + s.Duration);

                            return new WindowUsage
                            {
                                WindowName = app.WindowName,
                                ClassName = app.ClassName,
                                Sessions = windowGroup,
                                TotalDuration = duration
                            };
                        })
                        .ToList();

                    var processDuration = windows
                        .Aggregate(TimeSpan.Zero, (sum, w) => sum + w.TotalDuration);

                    return new ProcessUsage
                    {
                        ProcessName = processGroup.Key,
                        Windows = windows,
                        TotalDuration = processDuration
                    };
                })
                .OrderByDescending(p => p.TotalDuration)
                .ToList();

            return new ReportData
            {
                User = new User(),
                Category = Category.FromDto(x.category),
                Applications = processes,
                Interventions = interventionsList,
                BrowserDetails = browserDetailsList,
                Thresholds = thresholdsList.Where(t => t.CategoryId == x.category.CategoryId)
            };
        });
    }

    public bool WriteCsvReport(string outputPath)
    {
        CsvWriter = new(outputPath + "report.csv");
        var data = MakeReportData();
        return CsvWriter.WriteToFile(data);
    }
    
    public bool WritePdfReport(string outputPath)
    {
        PdfWriter = new(outputPath + "report.pdf");
        var data = MakeReportData();
        return PdfWriter.WriteToFile(data);
    }
}