using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Backend.Report;
using Backend.Report.Models;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class ReportsViewModel
{
    public ObservableCollection<ReportData> Report { get; set; }

    private ReportMaker _maker = new(new DatabaseManager(GetDatabasePath()));
    
    private Timer? _timer;
    
    public ReportsViewModel()
    {
        Report = new();
        var reportData = _maker.MakeReportData();
        
        foreach (var d in reportData)
        {
            Report.Add(d);
        }

    }
    
    private static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        appDataPath = Path.Combine(appDataPath, "ActivityMonitor");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "database.db");
    }
}