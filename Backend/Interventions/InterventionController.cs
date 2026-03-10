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
                TriggerIntervention(t.InterventionType);
            }
        }
    }
    
    private void TriggerIntervention(int interventionType)
    {
        if (interventionType == 1)
        {
            _notifier.Notify("You have exceeded your daily limit.");
        }
    }
}