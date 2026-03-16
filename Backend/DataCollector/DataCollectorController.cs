using Backend.Classifier;
using Backend.DataCollector.Application;
using Backend.DataCollector.Browser;
using Backend.DataCollector.Models;
using Backend.Models;
using Database.Manager;

namespace Backend.DataCollector;

public class DataCollectorController
{
    private SessionRecord _previousRecord = new ();
    
    private readonly IClassifier _classifier = new RuleBasedClassifier();
    private readonly IBrowserDataCollector _browserCollector = new FirefoxCollector();
    private readonly IApplicationDataCollector _appCollector = new LinuxAppCollector();
    
    public ApplicationRecord CheckActivity(IDatabaseManager db)
    {
        var app = _appCollector.GetActive();
        if (app == null) throw new Exception("No active window found");

        app.CategoryId = _classifier.ClassifyAsync(app);
        var dto = app.ToDto();
        var appid = db.UpsertApplication(dto);

        if (app.CategoryId == 2)
        {
            var browserRecord = _browserCollector.QueryTabs();
            if (browserRecord != null)
            {
                browserRecord.BrowserId = appid;
                db.InsertBrowserActivity(browserRecord.ToDto());
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
}