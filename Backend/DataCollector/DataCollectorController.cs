using Backend.Classifier;
using Backend.DataCollector.Application;
using Backend.DataCollector.Browser;
using Backend.DataCollector.Models;
using Backend.Models;
using Database.Manager;

namespace Backend.DataCollector;

public sealed class DataCollectorController : IDisposable
{
    private static readonly HashSet<string> FirefoxBrowsers = new(StringComparer.OrdinalIgnoreCase) { "firefox", "librewolf", "navigator" };
    private static readonly HashSet<string> ChromiumBrowsers = new(StringComparer.OrdinalIgnoreCase) { "chrome", "chromium", "msedge", "brave", "opera", "vivaldi" };

    private SessionRecord _previousRecord = new();
    private readonly IClassifier _classifier = new RuleBasedClassifier();
    private readonly FirefoxCollector _firefoxCollector = new();
    private readonly ChromiumCollector _chromiumCollector = new();
    private readonly IApplicationDataCollector _appCollector = new WindowsAppCollector();
    private string? _lastBrowserProcessName;

    public ApplicationRecord? CheckActivity(IDatabaseManager db)
    {
        var app = _appCollector.GetActive();
        if (app is null) return null;

        app.CategoryId = _classifier.ClassifyAsync(app);
        var appId = db.UpsertApplication(app.ToDto());

        if (app.CategoryId == 2)
            return HandleBrowserActivity(app, appId, db);

        ClearBrowserState();
        return HandleSessionTransition(app, appId, db);
    }

    private ApplicationRecord HandleBrowserActivity(ApplicationRecord app, int appId, IDatabaseManager db)
    {
        var processName = app.ProcessName?.ToLower();
        var (browserCollector, browserType) = SelectBrowserCollector(processName);

        if (browserCollector is null)
        {
            Console.WriteLine($"DataCollector: Unknown browser type: {processName}");
            _lastBrowserProcessName = null;
            return app;
        }

        HandleBrowserSwitch(processName, browserType);

        var browserRecord = browserCollector.QueryTabs();
        if (browserRecord is null)
        {
            Console.WriteLine($"DataCollector: No browser record available from {browserType} collector");
            return app;
        }

        browserRecord.BrowserId = appId;
        browserRecord.CategoryId = _classifier.ClassifyAsync(browserRecord);
        var existingId = db.IsInDb(browserRecord.ToDto());

        if (existingId is null)
        {
            db.InsertBrowserActivity(browserRecord.ToDto());
            Console.WriteLine($"DataCollector: Inserted browser activity - URL: {browserRecord.Url}");
        }
        else
        {
            Console.WriteLine($"DataCollector: Browser activity already in DB - URL: {browserRecord.Url}");
        }

        return HandleSessionTransition(app, appId, db);
    }

    private (IBrowserDataCollector? collector, string? type) SelectBrowserCollector(string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return (null, null);

        if (ChromiumBrowsers.Any(b => processName.Contains(b)))
            return (_chromiumCollector, "chromium");

        if (FirefoxBrowsers.Any(b => processName.Contains(b)))
            return (_firefoxCollector, "firefox");

        return (null, null);
    }

    private void HandleBrowserSwitch(string? newBrowser, string? browserType)
    {
        if (_lastBrowserProcessName != null && _lastBrowserProcessName != newBrowser)
        {
            Console.WriteLine($"DataCollector: Browser switched from '{_lastBrowserProcessName}' to '{newBrowser}'");
            ClearBrowserState();
        }
        else if (_lastBrowserProcessName is null)
        {
            Console.WriteLine($"DataCollector: Browser detected: {browserType} ({newBrowser})");
        }
        _lastBrowserProcessName = newBrowser;
    }

    private void ClearBrowserState()
    {
        _firefoxCollector.ClearState();
        _chromiumCollector.ClearState();
    }

    private ApplicationRecord HandleSessionTransition(ApplicationRecord app, int appId, IDatabaseManager db)
    {
        if (_previousRecord is null || _previousRecord.ApplicationId != appId)
        {
            if (_previousRecord is not null)
            {
                _previousRecord.EndTime = DateTime.Now;
                db.UpdateSession(_previousRecord.ToDto());
            }

            _previousRecord = new SessionRecord
            {
                ApplicationId = appId,
                UserId = 1,
                StartTime = DateTime.Now,
                EndTime = DateTime.Now
            };
            _previousRecord.Id = db.InsertSession(_previousRecord.ToDto());
            return app;
        }

        _previousRecord.EndTime = DateTime.Now;
        db.UpdateSession(_previousRecord.ToDto());
        return app;
    }

    public void Dispose()
    {
        _firefoxCollector?.Dispose();
        _chromiumCollector?.Dispose();
    }
}
