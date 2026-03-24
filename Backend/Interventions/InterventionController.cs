using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Database.Manager;

namespace Backend.Interventions;

public class InterventionController
{
    public void VerifyThresholds(IDatabaseManager db, ApplicationRecord lastRecord)
    {
        var thresholds = db.GetAllThresholds().Select(t => Threshold.FromDto(t));

        foreach (var t in thresholds)
        {
            if (!t.Active) continue;
            CheckThreshold(db, t, lastRecord); 
        }
    }
    
    private void CheckThreshold(IDatabaseManager db, Threshold t, ApplicationRecord lastRecord)
    {

        var duration = db.GetSessionDurationForCategory(t.CategoryId);
        if (!(TimeSpan.FromSeconds(duration) > t.Limit)){ return; }

        bool snooze = false;
        switch (t.InterventionType)
        {
            case Threshold.NotificationInterventionType:
                var response = Notifier.Notification($"Daily limit exceeded for {lastRecord.ProcessName}", 
                    new[] { "Dismiss", "Snooze"});
                if (response == "Snooze")
                {
                    t.Limit += TimeSpan.FromMinutes(10);
                    snooze = true;
                }
                else
                {
                    t.Active = false;
                }
                break;
            case Threshold.TypingLockInterventionType: 
                if (lastRecord.WindowId == null || lastRecord.WindowId == 0) return;
                Notifier.TypingLock("Type this message to unlock", lastRecord.WindowId.Value, "Type this message to unlock");
                t.Active = false;
                break;
            case Threshold.TimedLockInterventionType:
            case "TimerLock":
                if (lastRecord.WindowId == null || lastRecord.WindowId == 0) return;
                Notifier.TimedLock($"Daily limit exceeded for {lastRecord.ProcessName}", lastRecord.WindowId.Value, 20);
                t.Active = false;
                break;
        }
        db.UpdateThreshold(t.ToDto());


        var intervention = new Intervention
        {
            ThresholdId = t.Id,
            Snoozed = snooze,
            TriggeredAt = DateTime.Now,
        };

        db.InsertIntervention(intervention.ToDto());
    }
}
