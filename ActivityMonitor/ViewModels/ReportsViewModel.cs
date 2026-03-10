using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Avalonia.Threading;
using Backend.Report.Models;
using Database.DTO;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class ReportsViewModel
{
    public ObservableCollection<ReportDto> Reports { get; set; }
    private DatabaseManager _manager { get; }
    private static readonly string DbPath = GetDatabasePath();

    private Timer? _timer;
    
    public ReportsViewModel()
    {
        _manager = new (DbPath);
        Reports = new ();

        _timer = new Timer(_ =>
        {
            var updatedItems = _manager.GetActivityReport();
            

            Dispatcher.UIThread.Post(() =>
            {
                Reports.Clear();
                string? lastCategoryName = null;

                foreach (var app in updatedItems)
                {
                    if (app.CategoryName != lastCategoryName)
                    {
                        lastCategoryName = app.CategoryName;
                    }
                    else
                    {
                        app.CategoryName = string.Empty;
                    }
                    
                    Reports.Add(app);
                }
            });

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }
    
    private static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        appDataPath = Path.Combine(appDataPath, "ActivityMonitor");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "database.db");
    }
}