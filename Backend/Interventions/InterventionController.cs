using Backend.Interventions.Models;
using Backend.Interventions.NotifierStrategy;
using Database.Manager;

namespace Backend.Interventions;

public class InterventionController
{
    ReminderNotification _notifier = new ();
    SoftLock _softLock = new ();
    HardLock _hardLock = new ();
    
    public void VerifyThresholds(IDatabaseManager db)
    {
        var thresholds = db.GetAllThresholds();

        foreach (var t in thresholds)
        {
            if (!t.Active) continue;
            
            var duration = db.GetSessionDurationForCategory(t.CategoryId);

            if (duration > t.DailyLimitSec)
            {
                TriggerIntervention(t.InterventionType?? "");
                var intervention = new Intervention
                {
                    UserId = t.UserId,
                    CategoryId = t.Id,
                    ThresholdId = t.Id,
                    TriggeredAt = DateTime.Now
                };
                db.InsertIntervention(intervention.ToDto());
            }
        }
    }
    
    private void TriggerIntervention(string interventionType)
    {
        switch (interventionType)
        {
            case "Notification":
                _notifier.Notify("You have exceeded your daily limit.");
                break;
            case "SoftLock":
                break;
            case "HardLock":
                break;
        }

    }
}