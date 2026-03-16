using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Interventions.NotifierStrategy;
using Database.Manager;

namespace Backend.Interventions;

public class InterventionController
{
    ReminderNotification _notifier;
    SoftLock _softLock;
    HardLock _hardLock;

    private string _password = "zxcvb";
    
    public void VerifyThresholds(IDatabaseManager db, ApplicationRecord lastRecord)
    {
        var thresholds = db.GetAllThresholds();

        foreach (var t in thresholds)
        {
            if (!t.Active) continue;
            
            var duration = db.GetSessionDurationForCategory(t.CategoryId);

            if (duration > t.DailyLimitSec)
            {
                var response = TriggerIntervention(t.InterventionType ?? "", lastRecord.WindowId ?? 0);
                var intervention = new Intervention
                {
                    UserId = t.UserId,
                    CategoryId = t.CategoryId,
                    ThresholdId = t.Id,
                    Type = t.InterventionType?? "",
                    TriggeredAt = DateTime.Now
                };
                db.InsertIntervention(intervention.ToDto());
                t.Active = false;
                if (response == "Snooze")
                {
                    t.DailyLimitSec += TimeSpan.FromMinutes(10).Seconds;
                }
                db.UpdateThreshold(t);
            }
        }
    }
    
    private string TriggerIntervention(string interventionType, int windowId)
    {
        switch (interventionType)
        {
            case "Notification":
                _notifier ??= new ();
                var response = _notifier.Notify("You have exceeded your daily limit.");
                return response;
            case "SoftLock":
                _softLock ??= new ();
                _softLock.Lock("You have exceeded your daily limit.", windowId, _password);
                break;
            case "HardLock":
                _hardLock ??= new ();
                _hardLock.Lock("You have exceeded your daily limit.", windowId, 30);
                break;
        }
        return "";
    }
}