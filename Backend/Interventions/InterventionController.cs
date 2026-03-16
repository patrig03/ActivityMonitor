using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Interventions.NotifierStrategy;
using Database.Manager;

namespace Backend.Interventions;

public class InterventionController
{
    private ReminderNotification? _notifier;
    private SoftLock? _softLock;
    private HardLock? _hardLock;


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
                    response = HandleSoftLock("Type this message to unlock.", lastRecord.WindowId ?? 0, 
                        "Type this message to unlock.");
                }
                else if (t.InterventionType == "HardLock")
                {
                    response = HandleHardLock("Daily threshold exceeded.", lastRecord.WindowId ?? 0, 20);
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

    private string TriggerIntervention(string interventionType, int windowId, string message, int timeout,
        string password)
    {
        return interventionType switch
        {
            "Notification" => HandleNotification(message),
            "SoftLock" => HandleSoftLock(message, windowId, password),
            "HardLock" => HandleHardLock(message, windowId, timeout),
            _ => ""
        };
    }

    /// <summary>
    /// Handles a notification intervention. Returns the notifier's response.
    /// </summary>
    private string HandleNotification(string message)
    {
        _notifier ??= new();
        return _notifier.Notify(message);
    }

    /// <summary>
    /// Locks the UI softly, using a password. Does not return anything.
    /// </summary>
    private string HandleSoftLock(string message, int windowId, string password)
    {
        if (windowId == 0) return "";
        _softLock ??= new();
        _softLock.Lock(message, windowId, password);
        return "";
    }

    /// <summary>
    /// Locks the UI hard for a specified timeout. Does not return anything.
    /// </summary>
    private string HandleHardLock(string message, int windowId, int timeout)
    {
        if (windowId == 0) return "";
        _hardLock ??= new();
        _hardLock.Lock(message, windowId, timeout);
        return "";
    }
}