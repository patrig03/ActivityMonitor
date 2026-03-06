using System.Diagnostics;
using Backend.Classifier;
using Backend.DataCollector.Browser;
using Backend.DataCollector.Models;
using Backend.Models;
using Database.Manager;

namespace Backend.DataCollector;

public class DataCollectorController
{
    private const string WmctrlCmd  = "wmctrl";
    private const string XpropCmd   = "xprop";
    private SessionRecord previousRecord = new ();
    
    private IClassifier _classifier = new RuleBasedClassifier();
    private IBrowserDataCollector _browserCollector = new FirefoxCollector();
    
    public void CheckActivity(IDatabaseManager db)
    {
        var windowsOutput = ExecuteCommand(WmctrlCmd, "-lGpx");
        
        var app = ParseWindows(windowsOutput);
        if (app == null) throw new Exception("No active window found");

        app.CategoryId = _classifier.ClassifyAsync(app);
        var dto = app.ToDto();
        var appid = db.UpsertApplication(dto);

        if (app.CategoryId == 2)
        {
            var browserRecord = _browserCollector.QueryTabs();
            browserRecord.BrowserId = appid;
            db.InsertBrowserActivity(browserRecord.ToDto());
        }

        var session = new SessionRecord
        {
            ApplicationId = appid,
            UserId = 1,
            StartTime = previousRecord.StartTime,
        };
        var sessionId = db.IsInDb(session.ToDto());
        
        if (sessionId == null)
        {
            session.StartTime = DateTime.Now;
            session.EndTime = DateTime.Now;
            previousRecord = session;
            previousRecord.Id = db.InsertSession(session.ToDto());
            return;
        }

        var sdto = db.GetSession(sessionId.Value);
        if (sdto == null) throw new Exception("Session not found");
        session = new SessionRecord().FromDto(sdto);
        if (previousRecord.Id == sessionId) 
        {
            session.EndTime = DateTime.Now;
            db.UpdateSession(session.ToDto());
        }
        else
        {
            previousRecord = session;
            db.InsertSession(session.ToDto());
        }
        
    }

    private string ExecuteCommand(string file, string args)
    {
        using var process = new Process
        {
            StartInfo =
            {
                FileName           = file,
                Arguments          = args,
                RedirectStandardOutput = true,
                UseShellExecute    = false,
                CreateNoWindow     = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private ApplicationRecord? ParseWindows(string wmctrlResult)
    {
        var lines = wmctrlResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return lines
            .Select(ParseWindowLine)
            .Where(w => w != null!)
            .ToArray()
            .First();
    }

    private ApplicationRecord? ParseWindowLine(string line)
    {
        // wmctrl -lGpx output: <id> <desktop> <pid> <x> <y> <w> <h> <class> <host> <title>
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8) return null;

        var windowId   = parts[0];
        var pidStr     = parts[2];
        var wmClass    = parts[7];
        var title      = string.Join(' ', parts.Skip(9));

        if (!int.TryParse(pidStr, out var pid)) return null;

        var xpropOutput = ExecuteCommand(XpropCmd, $"-id {windowId}");
        var state = GetXPropValue(xpropOutput, "_NET_WM_STATE");
        var process = GetProcessName(pid);

        if (state == null) return null;
        if (!state.Contains("_NET_WM_STATE_FOCUSED")) return null;
        if (process == null) return null;
        
        return new ApplicationRecord
        {
            Id = null,
            CategoryId = null,
            ProcessName = process,
            WindowName = title,
            ClassName = wmClass
        };
    }

    private string? GetXPropValue(string output, string key)
    {
        // Each line: <key> = <value>
        var line = output
            .Split('\n')
            .FirstOrDefault(l => l.TrimStart().StartsWith(key));
        return line?.Split('=')?.Last()?.Trim();
    }

    private string? GetProcessName(int pid)
    {
        try
        {
            return File.ReadAllText($"/proc/{pid}/comm").Trim();
        }
        catch
        {
            return null;
        }
    }
}