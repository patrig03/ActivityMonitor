using System.Collections.ObjectModel;
using ActivityMonitor.Converters;
using Backend.Models;
using Backend.Report;
using Backend.Report.Models;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class ReportsViewModel
{
    public ObservableCollection<ReportData> Report { get; set; }
    private ReportMaker _maker = new(new DatabaseManager(Settings.DbPath));
    
    public ReportsViewModel()
    {
        Report = new();
        var reportData = _maker.MakeReportData();
        
        foreach (var d in reportData)
        {
            Report.Add(d);
        }

    }
}