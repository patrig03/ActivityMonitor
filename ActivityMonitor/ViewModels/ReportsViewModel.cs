using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;
using Backend.Models;
using Backend.Report;
using Backend.Report.Models;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class ReportsViewModel : ViewModelBase
{
    private readonly ReportMaker _maker = new(new DatabaseManager(Settings.DatabaseConnectionString));

    private string _reportStatus = "Se pregateste raportul de activitate";
    private string _lastGenerated = "Nu a fost generat inca";
    private string _exportDirectory = BuildExportDirectory();
    private string _categoryCount = "0";
    private string _totalTrackedTime = "--";
    private string _applicationCount = "0";
    private string _interventionCount = "0";
    private string _browserEventCount = "0";

    public ReportsViewModel()
    {
        RefreshCommand = new RelayCommand(_ => LoadReports());
        ExportCsvCommand = new RelayCommand(_ => ExportCsv());
        ExportPdfCommand = new RelayCommand(_ => ExportPdf());
        LoadReports();
    }

    public ICommand RefreshCommand { get; }

    public ICommand ExportCsvCommand { get; }

    public ICommand ExportPdfCommand { get; }

    public ObservableCollection<ReportCategoryCard> Categories { get; } = new();

    public string ReportStatus
    {
        get => _reportStatus;
        set => SetProperty(ref _reportStatus, value);
    }

    public string LastGenerated
    {
        get => _lastGenerated;
        set => SetProperty(ref _lastGenerated, value);
    }

    public string ExportDirectory
    {
        get => _exportDirectory;
        set => SetProperty(ref _exportDirectory, value);
    }

    public string CategoryCount
    {
        get => _categoryCount;
        set => SetProperty(ref _categoryCount, value);
    }

    public string TotalTrackedTime
    {
        get => _totalTrackedTime;
        set => SetProperty(ref _totalTrackedTime, value);
    }

    public string ApplicationCount
    {
        get => _applicationCount;
        set => SetProperty(ref _applicationCount, value);
    }

    public string InterventionCount
    {
        get => _interventionCount;
        set => SetProperty(ref _interventionCount, value);
    }

    public string BrowserEventCount
    {
        get => _browserEventCount;
        set => SetProperty(ref _browserEventCount, value);
    }

    private void LoadReports()
    {
        var reportData = _maker.MakeReportData().ToList();
        Categories.Clear();

        var allDurations = new List<TimeSpan>();
        var uniqueProcesses = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var uniqueInterventions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var browserEventCount = reportData
            .SelectMany(report => report.BrowserDetails)
            .Select(record => record.Id == 0 ? record.Url : $"{record.Id}:{record.Url}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        foreach (var report in reportData.OrderByDescending(GetCategoryDuration))
        {
            var categoryDuration = GetCategoryDuration(report);
            var topProcesses = report.Applications
                .OrderByDescending(app => app.TotalDuration)
                .Take(4)
                .Select(app => new ReportProcessSummary
                {
                    ProcessName = string.IsNullOrWhiteSpace(app.ProcessName) ? "Proces necunoscut" : app.ProcessName,
                    Duration = FormatDuration(app.TotalDuration),
                    WindowCount = $"{app.Windows.Count()} ferestre"
                })
                .ToList();

            var thresholdIds = report.Thresholds.Select(threshold => threshold.Id).ToHashSet();
            var interventionsForCategory = report.Interventions
                .Where(intervention => thresholdIds.Contains(intervention.ThresholdId))
                .ToList();

            foreach (var process in report.Applications)
            {
                if (!string.IsNullOrWhiteSpace(process.ProcessName))
                {
                    uniqueProcesses.Add(process.ProcessName);
                }

                allDurations.Add(process.TotalDuration);
            }

            foreach (var intervention in interventionsForCategory)
            {
                uniqueInterventions.Add(intervention.Id == 0
                    ? $"{intervention.ThresholdId}:{intervention.TriggeredAt:o}:{intervention.Snoozed}"
                    : intervention.Id.ToString());
            }

            Categories.Add(new ReportCategoryCard
            {
                CategoryName = report.Category.Name,
                Description = string.IsNullOrWhiteSpace(report.Category.Description)
                    ? "Nu exista descriere pentru categorie."
                    : report.Category.Description!,
                TotalDuration = FormatDuration(categoryDuration),
                ApplicationCount = $"{report.Applications.Count()} procese monitorizate",
                WindowCount = $"{report.Applications.Sum(app => app.Windows.Count())} ferestre capturate",
                ThresholdCount = $"{report.Thresholds.Count()} praguri asociate",
                InterventionCount = $"{interventionsForCategory.Count} interventii asociate",
                BrowserActivityCount = report.BrowserDetails.Any()
                    ? $"{report.BrowserDetails.Count()} inregistrari browser asociate"
                    : "Fara activitate browser asociata",
                TopProcesses = new ObservableCollection<ReportProcessSummary>(topProcesses),
                Highlight = topProcesses.FirstOrDefault()?.ProcessName ?? "Nu exista activitate de proces"
            });
        }

        CategoryCount = reportData.Count.ToString();
        TotalTrackedTime = allDurations.Count == 0
            ? "--"
            : FormatDuration(allDurations.Aggregate(TimeSpan.Zero, (current, next) => current + next));
        ApplicationCount = uniqueProcesses.Count.ToString();
        InterventionCount = uniqueInterventions.Count.ToString();
        BrowserEventCount = browserEventCount.ToString();
        LastGenerated = $"Generat la {DateTime.Now:HH:mm}";
        ReportStatus = reportData.Count == 0
            ? "Nu exista inca activitate raportabila. Lasa monitorul sa ruleze pentru a captura mai intai sesiuni."
            : $"Au fost incarcate {reportData.Count} rapoarte de categorie din datele de activitate MySQL.";
    }

    private void ExportCsv()
    {
        Directory.CreateDirectory(ExportDirectory);
        var success = _maker.WriteCsvReport(EnsureTrailingSeparator(ExportDirectory));
        ReportStatus = success
            ? $"Raportul CSV a fost scris in {Path.Combine(ExportDirectory, "report.csv")}"
            : "Exportul CSV a esuat.";
    }

    private void ExportPdf()
    {
        Directory.CreateDirectory(ExportDirectory);
        var success = _maker.WritePdfReport(EnsureTrailingSeparator(ExportDirectory));
        ReportStatus = success
            ? $"Raportul PDF a fost scris in {Path.Combine(ExportDirectory, "report.pdf")}"
            : "Exportul PDF a esuat.";
    }

    private static TimeSpan GetCategoryDuration(ReportData report)
    {
        return report.Applications.Aggregate(TimeSpan.Zero, (current, next) => current + next.TotalDuration);
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration <= TimeSpan.Zero)
        {
            return "0m";
        }

        if (duration.TotalHours >= 1)
        {
            return $"{(int)duration.TotalHours}h {duration.Minutes}m";
        }

        return $"{Math.Max(1, (int)Math.Round(duration.TotalMinutes))}m";
    }

    private static string BuildExportDirectory()
    {
        return Path.Combine(Settings.DataDirectory, "reports");
    }

    private static string EnsureTrailingSeparator(string path)
    {
        return path.EndsWith(Path.DirectorySeparatorChar)
            ? path
            : path + Path.DirectorySeparatorChar;
    }
}

public sealed class ReportCategoryCard
{
    public string CategoryName { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public string TotalDuration { get; init; } = string.Empty;
    public string ApplicationCount { get; init; } = string.Empty;
    public string WindowCount { get; init; } = string.Empty;
    public string ThresholdCount { get; init; } = string.Empty;
    public string InterventionCount { get; init; } = string.Empty;
    public string BrowserActivityCount { get; init; } = string.Empty;
    public string Highlight { get; init; } = string.Empty;
    public ObservableCollection<ReportProcessSummary> TopProcesses { get; init; } = new();
}

public sealed class ReportProcessSummary
{
    public string ProcessName { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string WindowCount { get; init; } = string.Empty;
}
