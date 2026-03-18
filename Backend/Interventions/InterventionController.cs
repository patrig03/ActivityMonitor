using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Database.Manager;

namespace Backend.Interventions;

public class InterventionController
{
    public void VerifyThresholds(IDatabaseManager db, ApplicationRecord lastRecord)
    {
        var thresholds = db.GetAllThresholds();

        foreach (var t in thresholds)
        {
            if (!t.Active) continue;

            var duration = db.GetSessionDurationForCategory(t.CategoryId);

            if (duration > t.DailyLimitSec)
            {
                var response = "";
                if (t.InterventionType == "Notification")
                {
                    response = HandleNotification("Daily threshold exceeded.");
                }
                else if (t.InterventionType == "SoftLock")
                {
                    HandleSoftLock("Type this message to unlock", lastRecord.WindowId ?? 0, 
                        "Type this message to unlock");
                }
                else if (t.InterventionType == "HardLock")
                {
                    HandleHardLock("Threshold exceeded.", lastRecord.WindowId ?? 0, 20);
                }

                var intervention = new Intervention
                {
                    UserId = t.UserId,
                    CategoryId = t.CategoryId,
                    ThresholdId = t.Id,
                    Type = t.InterventionType ?? "",
                    TriggeredAt = DateTime.Now
                };
                db.InsertIntervention(intervention.ToDto());
                t.Active = false;
                if (response == "Snooze")
                {
                    t.DailyLimitSec += TimeSpan.FromMinutes(1).Seconds;
                }

                db.UpdateThreshold(t);
            }
            else if (duration > t.WeeklyLimitSec)
            {
                HandleNotification("Weekly threshold exceeded.");
                var intervention = new Intervention
                {
                    UserId = t.UserId,
                    CategoryId = t.CategoryId,
                    ThresholdId = t.Id,
                    Type = "Notification",
                    TriggeredAt = DateTime.Now
                };
                db.InsertIntervention(intervention.ToDto());
                t.Active = false;
                db.UpdateThreshold(t);
            }
        }
    }

    private string HandleNotification(string message, string[]? buttons = null)
    {
        return Notifier.Notification(message, buttons ?? new[] { "Dismiss" });
    }
    
    private void HandleSoftLock(string message, int windowId, string password)
    {
        if (windowId == 0) return;
        Notifier.TypingLock(message, (ulong)windowId, password);
    }

    private void HandleHardLock(string message, int windowId, int timeout)
    {
        if (windowId == 0) return;
        Notifier.TimedLock(message, (ulong)windowId, timeout);
    }
}