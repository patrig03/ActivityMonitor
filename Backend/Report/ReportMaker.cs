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
    
        // Load shared data once instead of per category
        var interventions = Manager.GetInterventionsForUser(1);
        var browserRecords = Manager.GetAllBrowserActivity();
        var thresholds = Manager.GetAllThresholds();
    
        // Materialize LINQ queries to avoid multiple enumeration
        var interventionsList = interventions.Select(i => Intervention.FromDto(i)).ToList();
        var browserDetailsList = browserRecords.Select(b => BrowserRecord.FromDto(b)).ToList();
        var thresholdsList = thresholds.Select(t => Threshold.FromDto(t)).ToList();
    
        return categories
            .Select(category => new
            {
                Category = category,
                Sessions = Manager.GetSessionsByCategory(category.CategoryId)
            })
            .Where(x => x.Sessions.Any())
            .Select(x => new ReportData
            {
                User = new User(),
                Category = Category.FromDto(x.Category),
                Applications = Manager.GetApplicationsByCategory(x.Category.CategoryId)
                    .Select(a => ApplicationRecord.FromDto(a)),
                SessionDetails = x.Sessions.Select(s => SessionRecord.FromDto(s)),
                Interventions = interventionsList,
                BrowserDetails = browserDetailsList,
                Thresholds = thresholdsList
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