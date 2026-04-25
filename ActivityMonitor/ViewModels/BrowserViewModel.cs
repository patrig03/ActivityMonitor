using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using Backend.Classifier;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Models;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class BrowserViewModel : ViewModelBase
{
    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private readonly IClassifier _classifier = new RuleBasedClassifier();

    private string _browserStatus = "Se încarcă activitatea browserului";
    private string _totalEvents = "0";
    private string _uniqueDomains = "0";
    private string _trackedBrowsers = "0";
    private string _topDomain = "Nu există înregistrări browser";

    public BrowserViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        Load();
    }

    public ICommand RefreshCommand { get; }

    public ObservableCollection<BrowserDomainSummary> TopDomains { get; } = new();

    public ObservableCollection<BrowserAppSummary> BrowserApps { get; } = new();

    public ObservableCollection<BrowserActivityRow> RecentActivity { get; } = new();

    public string BrowserStatus
    {
        get => _browserStatus;
        set => SetProperty(ref _browserStatus, value);
    }

    public string TotalEvents
    {
        get => _totalEvents;
        set => SetProperty(ref _totalEvents, value);
    }

    public string UniqueDomains
    {
        get => _uniqueDomains;
        set => SetProperty(ref _uniqueDomains, value);
    }

    public string TrackedBrowsers
    {
        get => _trackedBrowsers;
        set => SetProperty(ref _trackedBrowsers, value);
    }

    public string TopDomain
    {
        get => _topDomain;
        set => SetProperty(ref _topDomain, value);
    }

    private void Load()
    {
        var records = _db.GetAllBrowserActivity()
            .Select(BrowserRecord.FromDto)
            .Select(EnsureBrowserCategory)
            .ToList();

        var apps = _db.GetAllApplications()
            .Select(ApplicationRecord.FromDto)
            .Where(app => app.Id.HasValue)
            .ToDictionary(app => app.Id!.Value);

        var categories = _db.GetAllCategories()
            .Select(Category.FromDto)
            .ToDictionary(category => category.Id);

        var domainGroups = records
            .Select(record => new { Record = record, Domain = TryGetDomain(record.Url) })
            .Where(item => !string.IsNullOrWhiteSpace(item.Domain))
            .GroupBy(item => item.Domain!)
            .OrderByDescending(group => group.Count())
            .ToList();

        TopDomains.Clear();
        foreach (var group in domainGroups.Take(6))
        {
            var firstRecord = group.First().Record;
            TopDomains.Add(new BrowserDomainSummary
            {
                Domain = group.Key,
                RecordCount = $"{group.Count()} inregistrari",
                Share = records.Count == 0 ? "0%" : $"{Math.Round(group.Count() / (double)records.Count * 100d):0}% din activitatea browserului",
                SampleUrl = firstRecord.Url
            });
        }

        BrowserApps.Clear();
        foreach (var group in records
                     .GroupBy(record => record.BrowserId)
                     .OrderByDescending(group => group.Count())
                     .Take(6))
        {
            apps.TryGetValue(group.Key, out var app);
            var mostFrequentCategoryId = group
                .Select(record => ResolveCategoryId(record, app))
                .Where(categoryId => categoryId.HasValue)
                .GroupBy(categoryId => categoryId!.Value)
                .OrderByDescending(categoryGroup => categoryGroup.Count())
                .ThenBy(categoryGroup => categoryGroup.Key)
                .Select(categoryGroup => (int?)categoryGroup.Key)
                .FirstOrDefault();

            var categoryName = ResolveCategoryName(mostFrequentCategoryId, categories);

            BrowserApps.Add(new BrowserAppSummary
            {
                ProcessName = app?.ProcessName ?? app?.WindowName ?? $"Aplicatia {group.Key}",
                CategoryName = categoryName,
                RecordCount = $"{group.Count()} URL-uri capturate"
            });
        }

        RecentActivity.Clear();
        foreach (var record in records.OrderByDescending(item => item.Id).Take(30))
        {
            apps.TryGetValue(record.BrowserId, out var app);
            var categoryName = ResolveCategoryName(ResolveCategoryId(record, app), categories);

            RecentActivity.Add(new BrowserActivityRow
            {
                ActivityId = record.Id,
                Domain = TryGetDomain(record.Url) ?? "URL invalid",
                Url = record.Url,
                BrowserProcess = app?.ProcessName ?? app?.WindowName ?? $"Aplicatia {record.BrowserId}",
                CategoryName = categoryName,
                UserId = "Utilizator 1"
            });
        }

        TotalEvents = records.Count.ToString();
        UniqueDomains = domainGroups.Count.ToString();
        TrackedBrowsers = records.Select(record => record.BrowserId).Distinct().Count().ToString();
        TopDomain = domainGroups.FirstOrDefault()?.Key ?? "Nu există înregistrări browser";
        BrowserStatus = records.Count == 0
            ? "Nu a fost stocată încă activitate de browser."
            : $"Au fost încărcate {records.Count} înregistrări de activitate browser din MySQL.";
    }

    private static string? TryGetDomain(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            ? uri.Host
            : null;
    }

    private BrowserRecord EnsureBrowserCategory(BrowserRecord record)
    {
        record.CategoryId ??= _classifier.ClassifyAsync(record);
        return record;
    }

    private static int? ResolveCategoryId(BrowserRecord record, ApplicationRecord? application)
    {
        return record.CategoryId ?? application?.CategoryId;
    }

    private static string ResolveCategoryName(int? categoryId, IReadOnlyDictionary<int, Category> categories)
    {
        return categoryId.HasValue && categories.TryGetValue(categoryId.Value, out var category)
            ? category.Name
            : "Neclasificat";
    }
}

public sealed class BrowserDomainSummary
{
    public string Domain { get; init; } = string.Empty;
    public string RecordCount { get; init; } = string.Empty;
    public string Share { get; init; } = string.Empty;
    public string SampleUrl { get; init; } = string.Empty;
}

public sealed class BrowserAppSummary
{
    public string ProcessName { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string RecordCount { get; init; } = string.Empty;
}

public sealed class BrowserActivityRow
{
    public int ActivityId { get; init; }
    public string Domain { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
    public string BrowserProcess { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
}
