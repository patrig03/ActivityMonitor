using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Backend;
using Backend.Report;
using Backend.Report.Models;
using Database.Manager;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.Measure;

namespace ActivityMonitor.ViewModels;


public partial class DashboardViewModel : ObservableObject
{
    private string _totalUsage;
    
    public string TotalUsage
    {
        get => _totalUsage;
        set => SetProperty(ref _totalUsage, value);
    }

    private string _focusScore;
    public string FocusScore
    {
        get => _focusScore;
        set => SetProperty(ref _focusScore, value);
    }

    private string _topApplication;
    public string TopApplication
    {
        get => _topApplication;
        set => SetProperty(ref _topApplication, value);
    }
    private string _totalSessions;

    public string TotalSessions
    {
        get => _totalSessions;
        set => SetProperty(ref _totalSessions, value);
    }

    public ObservableCollection<string> Recommendations { get; set; } =
        new()
        {
            "Try enabling Focus Mode during work periods.",
            "You spent most time in productivity apps today.",
            "Consider scheduling short breaks to maintain focus."
        };
    
    private IDatabaseManager _db = new DatabaseManager(GetDatabasePath());
    private ReportMaker _maker;

    public ISeries[] ProcessPie { get; set; }


    public DashboardViewModel()
    {
        _maker = new ReportMaker(_db);

        TotalUsage = "4h 38m";
        FocusScore = "78%";
        TopApplication = "monitor";
        TotalSessions = "12";
        
        var report = _maker.MakeReportData();
        BuildProcessUsageChart(report);
        BuildWindowUsageChart(report);
        BuildSessionTimeline(report);
        BuildProcessShareChart(report);
    }
    
        
    public ISeries[] ProcessSeries { get; set; }
    public Axis[] XAxes { get; set; }

    public void BuildProcessChart(IEnumerable<ProcessUsage> processes)
    {
        ProcessSeries =
        [
            new ColumnSeries<double>
            {
                Values = processes
                    .Select(p => p.TotalDuration.TotalMinutes)
                    .ToArray()
            }
        ];

        XAxes =
        [
            new Axis
            {
                Labels = processes
                    .Select(p => p.ProcessName)
                    .ToArray(),
                LabelsRotation = 20
            }
        ];
        
                
        ProcessPie = processes
            .Select(p => new PieSeries<double>
            {
                Values = new[] { p.TotalDuration.TotalMinutes },
                Name = p.ProcessName
            })
            .ToArray();
    }
    
    public ISeries[] ProcessUsageSeries { get; set; }
    public Axis[] ProcessXAxis { get; set; }

    public void BuildProcessUsageChart(IEnumerable<ReportData> reports)
    {
        var processes = reports
            .SelectMany(r => r.Applications)
            .GroupBy(p => p.ProcessName)
            .Select(g => new
            {
                Process = g.Key,
                Duration = g.Aggregate(TimeSpan.Zero, (s, p) => s + p.TotalDuration)
            })
            .OrderByDescending(x => x.Duration)
            .ToList();

        ProcessUsageSeries =
        [
            new ColumnSeries<double>
            {
                Values = processes
                    .Select(p => p.Duration.TotalMinutes)
                    .ToArray()
            }
        ];

        ProcessXAxis =
        [
            new Axis
            {
                Labels = processes
                    .Select(p => p.Process)
                    .ToArray(),
                LabelsRotation = 20
            }
        ];
    }
    
    public ISeries[] WindowUsageSeries { get; set; }
    public Axis[] WindowXAxis { get; set; }

    public void BuildWindowUsageChart(IEnumerable<ReportData> reports)
    {
        var windows = reports
            .SelectMany(r => r.Applications)
            .SelectMany(p => p.Windows)
            .GroupBy(w => w.WindowName)
            .Select(g => new
            {
                Window = g.Key,
                Duration = g.Aggregate(TimeSpan.Zero, (s, w) => s + w.TotalDuration)
            })
            .OrderByDescending(x => x.Duration)
            .Take(10)
            .ToList();

        WindowUsageSeries =
        [
            new ColumnSeries<double>
            {
                Values = windows
                    .Select(w => w.Duration.TotalMinutes)
                    .ToArray()
            }
        ];

        WindowXAxis =
        [
            new Axis
            {
                Labels = windows
                    .Select(w => w.Window)
                    .ToArray(),
                LabelsRotation = 25
            }
        ];
    }    
    
    public ISeries[] SessionTimelineSeries { get; set; }
    public void BuildSessionTimeline(IEnumerable<ReportData> reports)
    {
        var sessions = reports
            .SelectMany(r => r.Applications)
            .SelectMany(p => p.Windows)
            .SelectMany(w => w.Sessions)
            .OrderBy(s => s.StartTime)
            .ToList();

        SessionTimelineSeries =
        [
            new LineSeries<double>
            {
                Values = sessions
                    .Select(s => (s.EndTime - s.StartTime).TotalMinutes)
                    .ToArray(),
            }
        ];
    }
    
    public ISeries[] ProcessShareSeries { get; set; }
    public void BuildProcessShareChart(IEnumerable<ReportData> reports)
    {
        var processes = reports
            .SelectMany(r => r.Applications)
            .GroupBy(p => p.ProcessName)
            .Select(g => new
            {
                Process = g.Key,
                Duration = g.Aggregate(TimeSpan.Zero, (s, p) => s + p.TotalDuration)
            })
            .OrderByDescending(x => x.Duration);

        ProcessShareSeries = processes
            .Select(p => new PieSeries<double>
            {
                Name = p.Process,
                Values = new[] { p.Duration.TotalMinutes }
            })
            .ToArray();
    }
    
    private static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        appDataPath = Path.Combine(appDataPath, "ActivityMonitor");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "database.db");
    }
}