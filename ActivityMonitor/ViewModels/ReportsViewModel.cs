using System;
using System.Collections.ObjectModel;
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
    private const string DbPath = "../../../../Backend/bin/Debug/net9.0/data/database.db";

    private Timer? _timer;
    
    public ReportsViewModel()
    {
        _manager = new (DbPath);
        Reports = new ();

            Console.WriteLine("check");
        _timer = new Timer(_ =>
        {
            var updatedItems = _manager.GetActivityReport();
            

            Dispatcher.UIThread.Post(() =>
            {
                Reports.Clear();

                foreach (var app in updatedItems)
                {
                    Reports.Add(app);
                }
            });

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }
}