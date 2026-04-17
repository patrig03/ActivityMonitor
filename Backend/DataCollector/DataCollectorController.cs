using Backend.Classifier;
using Backend.DataCollector.Application;
using Backend.DataCollector.Browser;
using Backend.DataCollector.Models;
using Backend.Models;
using Database.Manager;

namespace Backend.DataCollector;

public class DataCollectorController : IDisposable
{
    private SessionRecord _previousRecord = new ();

    private readonly IClassifier _classifier = new RuleBasedClassifier();
    private readonly FirefoxCollector _firefoxCollector = new();
    private readonly ChromiumCollector _chromiumCollector = new();
    private readonly IApplicationDataCollector _appCollector = new WindowsAppCollector();

    private string? _lastBrowserProcessName = null;

    public ApplicationRecord? CheckActivity(IDatabaseManager db)
    {
        var app = _appCollector.GetActive();
        if (app == null) { return null; }

        app.CategoryId = _classifier.ClassifyAsync(app);
        var dto = app.ToDto();
        var appid = db.UpsertApplication(dto);

        if (app.CategoryId == 2)
        {
            var currentBrowserProcessName = app.ProcessName?.ToLower();

            // Detect browser type and select appropriate collector
            IBrowserDataCollector? browserCollector = null;
            string? browserType = null;

            if (IsFirefox(currentBrowserProcessName))
            {
                browserCollector = _firefoxCollector;
                browserType = "firefox";
            }
            else if (IsChromiumBased(currentBrowserProcessName))
            {
                browserCollector = _chromiumCollector;
                browserType = "chromium";
            }

            if (browserCollector == null)
            {
                Console.WriteLine($"DataCollector: Unknown browser type: {currentBrowserProcessName}");
                _lastBrowserProcessName = null;
                return app;
            }

            // If we switched browsers, clear the previous browser state
            if (_lastBrowserProcessName != null && _lastBrowserProcessName != currentBrowserProcessName)
            {
                Console.WriteLine($"DataCollector: Browser switched from '{_lastBrowserProcessName}' to '{currentBrowserProcessName}'");

                // Clear state from the old browser collector
                if (IsFirefox(_lastBrowserProcessName))
                {
                    _firefoxCollector.ClearState();
                }
                else if (IsChromiumBased(_lastBrowserProcessName))
                {
                    _chromiumCollector.ClearState();
                }
            }
            else if (_lastBrowserProcessName == null)
            {
                Console.WriteLine($"DataCollector: Browser detected: {browserType} ({currentBrowserProcessName})");
            }

            _lastBrowserProcessName = currentBrowserProcessName;

            var browserRecord = browserCollector.QueryTabs();
            if (browserRecord != null)
            {
                browserRecord.BrowserId = appid;
                browserRecord.CategoryId = _classifier.ClassifyAsync(browserRecord);
                var existingId = db.IsInDb(browserRecord.ToDto());
                if (existingId == null)
                {
                    db.InsertBrowserActivity(browserRecord.ToDto());
                    Console.WriteLine($"DataCollector: Inserted browser activity - URL: {browserRecord.Url}");
                }
                else
                {
                    Console.WriteLine($"DataCollector: Browser activity already in DB - URL: {browserRecord.Url}");
                }
            }
            else
            {
                Console.WriteLine($"DataCollector: No browser record available from {browserType} collector");
            }
        }
        else
        {
            // Not a browser, clear the last browser process name
            if (_lastBrowserProcessName != null)
            {
                Console.WriteLine($"DataCollector: Switched from browser to non-browser app");
                _lastBrowserProcessName = null;
            }
        }

        var session = new SessionRecord
        {
            ApplicationId = appid,
            UserId = 1,
            StartTime = _previousRecord.StartTime,
        };
        var sessionId = db.IsInDb(session.ToDto());

        if (sessionId == null)
        {
            session.StartTime = DateTime.Now;
            session.EndTime = DateTime.Now + TimeSpan.FromSeconds(1);
            _previousRecord = session;
            _previousRecord.Id = db.InsertSession(session.ToDto());
            return app;
        }

        var sdto = db.GetSession(sessionId.Value);
        if (sdto == null) throw new Exception("Session not found");
        session = SessionRecord.FromDto(sdto);
        if (_previousRecord.Id == sessionId)
        {
            session.EndTime = DateTime.Now;
            db.UpdateSession(session.ToDto());
        }
        else
        {
            _previousRecord = session;
            db.InsertSession(session.ToDto());
        }
        return app;
    }

    private bool IsFirefox(string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        return processName.Contains("firefox") || processName.Contains("librewolf") || processName.Contains("navigator");
    }

    private bool IsChromiumBased(string? processName)
    {
        if (string.IsNullOrEmpty(processName)) return false;
        return processName.Contains("chrome") ||
               processName.Contains("chromium") ||
               processName.Contains("msedge") ||
               processName.Contains("brave") ||
               processName.Contains("opera") ||
               processName.Contains("vivaldi");
    }

    public void Dispose()
    {
        _firefoxCollector?.Dispose();
        _chromiumCollector?.Dispose();
    }
}
