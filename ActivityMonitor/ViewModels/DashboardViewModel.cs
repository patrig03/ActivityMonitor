using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Backend;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Models;
using Backend.Report;
using Backend.Report.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Database.Manager;
using LiveChartsCore;
using LiveChartsCore.Measure;
using LiveChartsCore.SkiaSharpView;

namespace ActivityMonitor.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private const int DefaultUserId = 1;

    private static readonly string[] FocusCategoryKeywords =
    [
        "productivity",
        "programming",
        "office",
        "word",
        "spreadsheet",
        "presentation",
        "text editor",
        "database",
        "reference",
        "scientific"
    ];

    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private readonly ReportMaker _maker;

    private string _displayName = "Default user";
    private string _profileInitials = "DU";
    private string _dashboardSubtitle = "No tracked activity yet";
    private string _monitoringCadence = "Sampling interval unavailable";
    private string _lastRefreshLabel = "Not refreshed";
    private string _attentionBadge = "0";
    private string _snapshotSummary = "No categories active";
    private string _totalUsage = "--";
    private string _focusScore = "--";
    private string _topApplication = "No activity";
    private string _totalSessions = "0";
    private string _averageSession = "--";
    private string _interventionPulse = "No alerts";
    private ISeries[] _windowUsageSeries = Array.Empty<ISeries>();
    private Axis[] _windowXAxis = Array.Empty<Axis>();
    private ISeries[] _sessionTimelineSeries = Array.Empty<ISeries>();
    private Axis[] _sessionTimelineXAxis = Array.Empty<Axis>();
    private ISeries[] _processShareSeries = Array.Empty<ISeries>();
    private ISeries[] _categoryUsageSeries = Array.Empty<ISeries>();
    private Axis[] _categoryXAxis = Array.Empty<Axis>();

    public DashboardViewModel()
    {
        _maker = new ReportMaker(_db);
        RefreshCommand = new RelayCommand(_ => LoadDashboard());
        LoadDashboard();
    }

    public ICommand RefreshCommand { get; }

    public ObservableCollection<DashboardInsight> Insights { get; } = new();

    public ObservableCollection<DashboardBreakdownItem> CategoryBreakdown { get; } = new();

    public ObservableCollection<DashboardThresholdState> ThresholdStatuses { get; } = new();

    public ObservableCollection<DashboardDomainItem> TopDomains { get; } = new();

    public ObservableCollection<DashboardInterventionItem> RecentInterventions { get; } = new();

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string ProfileInitials
    {
        get => _profileInitials;
        set => SetProperty(ref _profileInitials, value);
    }

    public string DashboardSubtitle
    {
        get => _dashboardSubtitle;
        set => SetProperty(ref _dashboardSubtitle, value);
    }

    public string MonitoringCadence
    {
        get => _monitoringCadence;
        set => SetProperty(ref _monitoringCadence, value);
    }

    public string LastRefreshLabel
    {
        get => _lastRefreshLabel;
        set => SetProperty(ref _lastRefreshLabel, value);
    }

    public string AttentionBadge
    {
        get => _attentionBadge;
        set => SetProperty(ref _attentionBadge, value);
    }

    public string SnapshotSummary
    {
        get => _snapshotSummary;
        set => SetProperty(ref _snapshotSummary, value);
    }

    public string TotalUsage
    {
        get => _totalUsage;
        set => SetProperty(ref _totalUsage, value);
    }

    public string FocusScore
    {
        get => _focusScore;
        set => SetProperty(ref _focusScore, value);
    }

    public string TopApplication
    {
        get => _topApplication;
        set => SetProperty(ref _topApplication, value);
    }

    public string TotalSessions
    {
        get => _totalSessions;
        set => SetProperty(ref _totalSessions, value);
    }

    public string AverageSession
    {
        get => _averageSession;
        set => SetProperty(ref _averageSession, value);
    }

    public string InterventionPulse
    {
        get => _interventionPulse;
        set => SetProperty(ref _interventionPulse, value);
    }

    public ISeries[] WindowUsageSeries
    {
        get => _windowUsageSeries;
        set => SetProperty(ref _windowUsageSeries, value);
    }

    public Axis[] WindowXAxis
    {
        get => _windowXAxis;
        set => SetProperty(ref _windowXAxis, value);
    }

    public ISeries[] SessionTimelineSeries
    {
        get => _sessionTimelineSeries;
        set => SetProperty(ref _sessionTimelineSeries, value);
    }

    public Axis[] SessionTimelineXAxis
    {
        get => _sessionTimelineXAxis;
        set => SetProperty(ref _sessionTimelineXAxis, value);
    }

    public ISeries[] ProcessShareSeries
    {
        get => _processShareSeries;
        set => SetProperty(ref _processShareSeries, value);
    }

    public ISeries[] CategoryUsageSeries
    {
        get => _categoryUsageSeries;
        set => SetProperty(ref _categoryUsageSeries, value);
    }

    public Axis[] CategoryXAxis
    {
        get => _categoryXAxis;
        set => SetProperty(ref _categoryXAxis, value);
    }

    private void LoadDashboard()
    {
        var reports = _maker.MakeReportData().ToList();
        var settings = _db.GetSettings(DefaultUserId);
        var user = _db.GetUser(DefaultUserId);
        var categories = _db.GetAllCategories().Select(Category.FromDto).ToDictionary(c => c.Id);
        var applications = _db.GetAllApplications()
            .Select(ApplicationRecord.FromDto)
            .Where(a => a.Id.HasValue)
            .ToDictionary(a => a.Id!.Value);

        var categorySummaries = reports
            .Select(report => new CategoryDuration(report.Category, SumDuration(report.Applications.Select(app => app.TotalDuration))))
            .Where(summary => summary.Duration > TimeSpan.Zero)
            .OrderByDescending(summary => summary.Duration)
            .ToList();

        var sessions = reports
            .SelectMany(report => report.Applications)
            .SelectMany(app => app.Windows)
            .SelectMany(window => window.Sessions)
            .Where(session => session.ApplicationId.HasValue)
            .OrderBy(session => session.StartTime)
            .ToList();

        var processSummaries = reports
            .SelectMany(report => report.Applications)
            .GroupBy(app => app.ProcessName)
            .Select(group => new ProcessDuration(
                group.Key,
                SumDuration(group.Select(process => process.TotalDuration))))
            .OrderByDescending(summary => summary.Duration)
            .ToList();

        var windows = reports
            .SelectMany(report => report.Applications)
            .SelectMany(app => app.Windows)
            .GroupBy(window => window.WindowName)
            .Select(group => new WindowDuration(
                group.Key,
                SumDuration(group.Select(window => window.TotalDuration))))
            .OrderByDescending(summary => summary.Duration)
            .ToList();

        var thresholds = reports
            .SelectMany(report => report.Thresholds)
            .GroupBy(threshold => threshold.Id)
            .Select(group => group.First())
            .ToList();

        var interventions = reports
            .SelectMany(report => report.Interventions)
            .GroupBy(intervention => intervention.Id == 0
                ? $"{intervention.ThresholdId}:{intervention.TriggeredAt:o}:{intervention.Snoozed}"
                : intervention.Id.ToString())
            .Select(group => group.First())
            .OrderByDescending(intervention => intervention.TriggeredAt)
            .ToList();

        var browserRecords = reports
            .SelectMany(report => report.BrowserDetails)
            .GroupBy(record => record.Id == 0 ? record.Url : $"{record.Id}:{record.Url}")
            .Select(group => group.First())
            .ToList();

        DisplayName = string.IsNullOrWhiteSpace(user?.DisplayName) ? "Default user" : user.DisplayName!;
        ProfileInitials = BuildInitials(DisplayName);
        DashboardSubtitle = reports.Count == 0
            ? "No tracked sessions yet. Leave the monitor running to populate the dashboard."
            : $"{DateTime.Now:dddd, MMMM d} | {categorySummaries.Count} active categories | {browserRecords.Count} browser events";
        MonitoringCadence = settings == null
            ? "Sampling cadence unavailable"
            : $"Sampling every {settings.DeltaTimeSeconds}s";
        LastRefreshLabel = $"Updated {DateTime.Now:HH:mm}";

        var totalUsage = SumDuration(categorySummaries.Select(summary => summary.Duration));
        var productiveUsage = SumDuration(categorySummaries
            .Where(summary => MatchesFocusCategory(summary.Category.Name))
            .Select(summary => summary.Duration));
        var focusPercentage = totalUsage == TimeSpan.Zero
            ? 0
            : productiveUsage.TotalSeconds / totalUsage.TotalSeconds * 100d;

        TotalUsage = totalUsage == TimeSpan.Zero ? "--" : FormatDuration(totalUsage);
        FocusScore = totalUsage == TimeSpan.Zero ? "--" : $"{Math.Round(focusPercentage):0}%";
        TopApplication = processSummaries.FirstOrDefault()?.ProcessName ?? "No activity";
        TotalSessions = sessions.Count.ToString();
        AverageSession = sessions.Count == 0
            ? "--"
            : FormatDuration(TimeSpan.FromMinutes(sessions.Average(session => session.Duration.TotalMinutes)));
        InterventionPulse = interventions.Count == 0
            ? "Quiet week"
            : $"{interventions.Count(intervention => intervention.TriggeredAt >= DateTime.Now.AddDays(-7))} alerts in 7d";
        SnapshotSummary = categorySummaries.Count == 0
            ? "No thresholds or categories active yet"
            : $"{categorySummaries.First().Category.Name} leads with {FormatDuration(categorySummaries.First().Duration)}";

        BuildInsights(
            categorySummaries,
            processSummaries,
            sessions,
            thresholds,
            interventions,
            browserRecords,
            applications,
            categories);

        BuildCategoryBreakdown(categorySummaries, totalUsage);
        BuildTopDomains(browserRecords);
        BuildThresholdStatuses(thresholds, categorySummaries, sessions, categories, applications);
        BuildRecentInterventions(interventions, thresholds, categories, applications);
        BuildWindowUsageChart(windows);
        BuildCategoryUsageChart(categorySummaries);
        BuildSessionTimeline(sessions, applications);
        BuildProcessShareChart(processSummaries);

        var attentionCount = ThresholdStatuses.Count(status => status.RequiresAttention)
            + RecentInterventions.Count(item => item.IsRecent);
        AttentionBadge = attentionCount.ToString();
    }

    private void BuildInsights(
        IReadOnlyList<CategoryDuration> categorySummaries,
        IReadOnlyList<ProcessDuration> processSummaries,
        IReadOnlyList<SessionRecord> sessions,
        IReadOnlyList<Threshold> thresholds,
        IReadOnlyList<Intervention> interventions,
        IReadOnlyList<BrowserRecord> browserRecords,
        IReadOnlyDictionary<int, ApplicationRecord> applications,
        IReadOnlyDictionary<int, Category> categories)
    {
        Insights.Clear();

        if (categorySummaries.Count == 0)
        {
            Insights.Add(new DashboardInsight
            {
                Title = "Waiting for activity",
                Detail = "The monitor has not captured any sessions yet. Once app sessions land in the database, this page will show trends and limit pressure."
            });
            return;
        }

        var topCategory = categorySummaries[0];
        var topCategoryShare = SumDuration(categorySummaries.Select(item => item.Duration)) == TimeSpan.Zero
            ? 0
            : topCategory.Duration.TotalSeconds / SumDuration(categorySummaries.Select(item => item.Duration)).TotalSeconds * 100d;
        Insights.Add(new DashboardInsight
        {
            Title = "Primary workload",
            Detail = $"{topCategory.Category.Name} accounts for {Math.Round(topCategoryShare):0}% of tracked time."
        });

        var longestSession = sessions
            .OrderByDescending(session => session.Duration)
            .FirstOrDefault();
        if (longestSession != null)
        {
            var sessionTarget = longestSession.ApplicationId.HasValue && applications.TryGetValue(longestSession.ApplicationId.Value, out var app)
                ? app.ProcessName ?? app.WindowName ?? "Unknown app"
                : "Unknown app";

            Insights.Add(new DashboardInsight
            {
                Title = "Longest uninterrupted session",
                Detail = $"{sessionTarget} held focus for {FormatDuration(longestSession.Duration)}."
            });
        }

        var exceededThreshold = thresholds
            .Select(threshold => BuildThresholdState(threshold, categorySummaries, sessions, categories, applications))
            .FirstOrDefault(state => state.RequiresAttention);
        if (exceededThreshold != null)
        {
            Insights.Add(new DashboardInsight
            {
                Title = "Limit pressure",
                Detail = $"{exceededThreshold.TargetName} is {exceededThreshold.StateLabel.ToLowerInvariant()} at {exceededThreshold.UsageSummary}."
            });
        }
        else
        {
            Insights.Add(new DashboardInsight
            {
                Title = "Limits look healthy",
                Detail = thresholds.Count == 0
                    ? "No intervention thresholds are configured yet."
                    : "Configured thresholds are still below their warning range."
            });
        }

        var dominantDomain = browserRecords
            .Select(record => TryGetDomain(record.Url))
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .GroupBy(domain => domain!)
            .OrderByDescending(group => group.Count())
            .FirstOrDefault();
        if (dominantDomain != null)
        {
            Insights.Add(new DashboardInsight
            {
                Title = "Browser concentration",
                Detail = $"{dominantDomain.Key} appears in {dominantDomain.Count()} captured browser events."
            });
        }
        else if (processSummaries.Any(summary => string.Equals(summary.ProcessName, "browser", StringComparison.OrdinalIgnoreCase)))
        {
            Insights.Add(new DashboardInsight
            {
                Title = "Browser sessions detected",
                Detail = "Browser windows were active, but URL-level data has not been captured for the current sample set."
            });
        }

        if (interventions.Count > 0)
        {
            Insights.Add(new DashboardInsight
            {
                Title = "Intervention tempo",
                Detail = $"{interventions.Count(intervention => intervention.TriggeredAt >= DateTime.Now.AddDays(-1))} alerts fired in the last 24 hours."
            });
        }
    }

    private void BuildCategoryBreakdown(IReadOnlyList<CategoryDuration> categories, TimeSpan totalUsage)
    {
        CategoryBreakdown.Clear();

        foreach (var item in categories.Take(6))
        {
            var share = totalUsage == TimeSpan.Zero
                ? 0
                : item.Duration.TotalSeconds / totalUsage.TotalSeconds * 100d;
            CategoryBreakdown.Add(new DashboardBreakdownItem
            {
                Label = item.Category.Name,
                Duration = FormatDuration(item.Duration),
                Secondary = $"{Math.Round(share):0}% of tracked time"
            });
        }

        if (CategoryBreakdown.Count == 0)
        {
            CategoryBreakdown.Add(new DashboardBreakdownItem
            {
                Label = "No categories yet",
                Duration = "--",
                Secondary = "Tracked sessions will populate this list."
            });
        }
    }

    private void BuildTopDomains(IReadOnlyList<BrowserRecord> browserRecords)
    {
        TopDomains.Clear();

        var domainGroups = browserRecords
            .Select(record => TryGetDomain(record.Url))
            .Where(domain => !string.IsNullOrWhiteSpace(domain))
            .GroupBy(domain => domain!)
            .OrderByDescending(group => group.Count())
            .Take(5)
            .ToList();

        var total = domainGroups.Sum(group => group.Count());

        foreach (var group in domainGroups)
        {
            var share = total == 0 ? 0 : group.Count() / (double)total * 100d;
            TopDomains.Add(new DashboardDomainItem
            {
                Domain = group.Key,
                Visits = $"{group.Count()} records",
                Share = $"{Math.Round(share):0}% share"
            });
        }

        if (TopDomains.Count == 0)
        {
            TopDomains.Add(new DashboardDomainItem
            {
                Domain = "No browser detail",
                Visits = "0 records",
                Share = "URL tracking has not populated yet."
            });
        }
    }

    private void BuildThresholdStatuses(
        IReadOnlyList<Threshold> thresholds,
        IReadOnlyList<CategoryDuration> categorySummaries,
        IReadOnlyList<SessionRecord> sessions,
        IReadOnlyDictionary<int, Category> categories,
        IReadOnlyDictionary<int, ApplicationRecord> applications)
    {
        ThresholdStatuses.Clear();

        foreach (var state in thresholds
                     .Select(threshold => BuildThresholdState(threshold, categorySummaries, sessions, categories, applications))
                     .OrderByDescending(item => item.ProgressValue)
                     .Take(5))
        {
            ThresholdStatuses.Add(state);
        }

        if (ThresholdStatuses.Count == 0)
        {
            ThresholdStatuses.Add(new DashboardThresholdState
            {
                TargetName = "No thresholds configured",
                UsageSummary = "Add daily or session limits in Interventions to monitor pressure here.",
                LimitSummary = "No active guardrails",
                StateLabel = "Info",
                ProgressValue = 0,
                RequiresAttention = false
            });
        }
    }

    private DashboardThresholdState BuildThresholdState(
        Threshold threshold,
        IReadOnlyList<CategoryDuration> categorySummaries,
        IReadOnlyList<SessionRecord> sessions,
        IReadOnlyDictionary<int, Category> categories,
        IReadOnlyDictionary<int, ApplicationRecord> applications)
    {
        var sessionsWithApps = sessions
            .Where(session => session.ApplicationId.HasValue && applications.ContainsKey(session.ApplicationId.Value))
            .ToList();

        var categoryDurations = categorySummaries.ToDictionary(summary => summary.Category.Id, summary => summary.Duration);
        var categoryLongestSessions = sessionsWithApps
            .GroupBy(session => applications[session.ApplicationId!.Value].CategoryId ?? 0)
            .ToDictionary(group => group.Key, group => group.Max(session => session.Duration));
        var appDurations = sessionsWithApps
            .GroupBy(session => session.ApplicationId!.Value)
            .ToDictionary(group => group.Key, group => SumDuration(group.Select(session => session.Duration)));
        var appLongestSessions = sessionsWithApps
            .GroupBy(session => session.ApplicationId!.Value)
            .ToDictionary(group => group.Key, group => group.Max(session => session.Duration));

        var limit = threshold.Limit;
        TimeSpan currentUsage;
        string targetName;

        if (threshold.TargetType == Threshold.AppTargetType && threshold.AppId != 0)
        {
            applications.TryGetValue(threshold.AppId, out var app);
            targetName = app?.ProcessName ?? app?.WindowName ?? $"App {threshold.AppId}";
            currentUsage = threshold.LimitType == Threshold.SessionLimitType
                ? appLongestSessions.GetValueOrDefault(threshold.AppId)
                : appDurations.GetValueOrDefault(threshold.AppId);
        }
        else
        {
            targetName = categories.TryGetValue(threshold.CategoryId, out var category)
                ? category.Name
                : $"Category {threshold.CategoryId}";
            currentUsage = threshold.LimitType == Threshold.SessionLimitType
                ? categoryLongestSessions.GetValueOrDefault(threshold.CategoryId)
                : categoryDurations.GetValueOrDefault(threshold.CategoryId);
        }

        var progress = limit <= TimeSpan.Zero
            ? 0
            : Math.Clamp(currentUsage.TotalSeconds / limit.TotalSeconds * 100d, 0, 999);
        var state = !threshold.Active
            ? "Disabled"
            : progress >= 100
                ? "Exceeded"
                : progress >= 80
                    ? "Warning"
                    : currentUsage == TimeSpan.Zero
                        ? "Idle"
                        : "On track";

        return new DashboardThresholdState
        {
            TargetName = targetName,
            UsageSummary = $"{FormatDuration(currentUsage)} / {FormatDuration(limit)}",
            LimitSummary = $"{threshold.InterventionType} | {threshold.LimitType} limit",
            StateLabel = state,
            ProgressValue = Math.Clamp(progress, 0, 100),
            RequiresAttention = threshold.Active && progress >= 80
        };
    }

    private void BuildRecentInterventions(
        IReadOnlyList<Intervention> interventions,
        IReadOnlyList<Threshold> thresholds,
        IReadOnlyDictionary<int, Category> categories,
        IReadOnlyDictionary<int, ApplicationRecord> applications)
    {
        RecentInterventions.Clear();

        var thresholdLookup = thresholds.ToDictionary(threshold => threshold.Id);

        foreach (var intervention in interventions.Take(5))
        {
            thresholdLookup.TryGetValue(intervention.ThresholdId, out var threshold);
            var targetName = ResolveTargetName(threshold, categories, applications);
            RecentInterventions.Add(new DashboardInterventionItem
            {
                TargetName = targetName,
                Status = intervention.Snoozed ? "Snoozed" : "Triggered",
                TriggeredAt = FormatRelativeTime(intervention.TriggeredAt),
                Detail = threshold == null
                    ? "Threshold details unavailable"
                    : $"{threshold.InterventionType} | {threshold.LimitType} {FormatDuration(threshold.Limit)}",
                IsRecent = intervention.TriggeredAt >= DateTime.Now.AddDays(-1)
            });
        }

        if (RecentInterventions.Count == 0)
        {
            RecentInterventions.Add(new DashboardInterventionItem
            {
                TargetName = "No interventions yet",
                Status = "Clear",
                TriggeredAt = "No recent triggers",
                Detail = "Threshold events will appear here once usage crosses a configured limit.",
                IsRecent = false
            });
        }
    }

    private void BuildWindowUsageChart(IReadOnlyList<WindowDuration> windows)
    {
        var topWindows = windows.Take(8).ToList();

        WindowUsageSeries =
        [
            new ColumnSeries<double>
            {
                Values = topWindows.Select(window => window.Duration.TotalMinutes).ToArray()
            }
        ];

        WindowXAxis =
        [
            new Axis
            {
                Labels = topWindows.Select(window => window.WindowName).ToArray(),
                LabelsRotation = 18,
                MinStep = 1
            }
        ];
    }

    private void BuildCategoryUsageChart(IReadOnlyList<CategoryDuration> categories)
    {
        var topCategories = categories.Take(6).ToList();

        CategoryUsageSeries =
        [
            new ColumnSeries<double>
            {
                Values = topCategories.Select(category => category.Duration.TotalHours).ToArray()
            }
        ];

        CategoryXAxis =
        [
            new Axis
            {
                Labels = topCategories.Select(category => category.Category.Name).ToArray(),
                LabelsRotation = 12,
                MinStep = 1
            }
        ];
    }

    private void BuildSessionTimeline(
        IReadOnlyList<SessionRecord> sessions,
        IReadOnlyDictionary<int, ApplicationRecord> applications)
    {
        var recentSessions = sessions.TakeLast(12).ToList();

        SessionTimelineSeries =
        [
            new LineSeries<double>
            {
                Values = recentSessions.Select(session => session.Duration.TotalMinutes).ToArray(),
                GeometrySize = 10
            }
        ];

        SessionTimelineXAxis =
        [
            new Axis
            {
                Labels = recentSessions.Select(session =>
                {
                    if (!session.ApplicationId.HasValue || !applications.TryGetValue(session.ApplicationId.Value, out var app))
                    {
                        return session.StartTime.ToString("HH:mm");
                    }

                    var label = app.ProcessName ?? app.WindowName ?? "App";
                    return $"{label} {session.StartTime:HH:mm}";
                }).ToArray(),
                LabelsRotation = 16,
                MinStep = 1
            }
        ];
    }

    private void BuildProcessShareChart(IReadOnlyList<ProcessDuration> processes)
    {
        var topProcesses = processes.Take(5).ToList();
        var otherTotal = processes.Skip(5).Sum(process => process.Duration.TotalMinutes);

        var series = topProcesses
            .Select(process => new PieSeries<double>
            {
                Name = process.ProcessName,
                Values = new[] { process.Duration.TotalMinutes }
            })
            .Cast<ISeries>()
            .ToList();

        if (otherTotal > 0)
        {
            series.Add(new PieSeries<double>
            {
                Name = "Other",
                Values = new[] { otherTotal }
            });
        }

        ProcessShareSeries = series.ToArray();
    }

    private static TimeSpan SumDuration(IEnumerable<TimeSpan> durations)
    {
        return durations.Aggregate(TimeSpan.Zero, (current, next) => current + next);
    }

    private static bool MatchesFocusCategory(string categoryName)
    {
        var normalized = categoryName.ToLowerInvariant();
        return FocusCategoryKeywords.Any(keyword => normalized.Contains(keyword));
    }

    private static string ResolveTargetName(
        Threshold? threshold,
        IReadOnlyDictionary<int, Category> categories,
        IReadOnlyDictionary<int, ApplicationRecord> applications)
    {
        if (threshold == null)
        {
            return "Unknown target";
        }

        if (threshold.TargetType == Threshold.AppTargetType &&
            threshold.AppId != 0 &&
            applications.TryGetValue(threshold.AppId, out var app))
        {
            return app.ProcessName ?? app.WindowName ?? $"App {threshold.AppId}";
        }

        return categories.TryGetValue(threshold.CategoryId, out var category)
            ? category.Name
            : $"Category {threshold.CategoryId}";
    }

    private static string BuildInitials(string displayName)
    {
        var parts = displayName
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Take(2)
            .Select(part => char.ToUpperInvariant(part[0]));

        var initials = new string(parts.ToArray());
        return string.IsNullOrWhiteSpace(initials) ? "AM" : initials;
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

    private static string FormatRelativeTime(DateTime timestamp)
    {
        if (timestamp == default)
        {
            return "Unknown time";
        }

        var delta = DateTime.Now - timestamp;
        if (delta.TotalMinutes < 1)
        {
            return "Just now";
        }

        if (delta.TotalHours < 1)
        {
            return $"{Math.Floor(delta.TotalMinutes):0}m ago";
        }

        if (delta.TotalDays < 1)
        {
            return $"{Math.Floor(delta.TotalHours):0}h ago";
        }

        if (delta.TotalDays < 7)
        {
            return $"{Math.Floor(delta.TotalDays):0}d ago";
        }

        return timestamp.ToString("dd MMM");
    }

    private static string? TryGetDomain(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : null;
    }

    private sealed record CategoryDuration(Category Category, TimeSpan Duration);

    private sealed record ProcessDuration(string ProcessName, TimeSpan Duration);

    private sealed record WindowDuration(string WindowName, TimeSpan Duration);
}

public sealed class DashboardInsight
{
    public string Title { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
}

public sealed class DashboardBreakdownItem
{
    public string Label { get; init; } = string.Empty;
    public string Duration { get; init; } = string.Empty;
    public string Secondary { get; init; } = string.Empty;
}

public sealed class DashboardThresholdState
{
    public string TargetName { get; init; } = string.Empty;
    public string UsageSummary { get; init; } = string.Empty;
    public string LimitSummary { get; init; } = string.Empty;
    public string StateLabel { get; init; } = string.Empty;
    public double ProgressValue { get; init; }
    public bool RequiresAttention { get; init; }
}

public sealed class DashboardDomainItem
{
    public string Domain { get; init; } = string.Empty;
    public string Visits { get; init; } = string.Empty;
    public string Share { get; init; } = string.Empty;
}

public sealed class DashboardInterventionItem
{
    public string TargetName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string TriggeredAt { get; init; } = string.Empty;
    public string Detail { get; init; } = string.Empty;
    public bool IsRecent { get; init; }
}
