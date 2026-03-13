using Backend.DataCollector;
using Backend.Interventions;
using Backend.Interventions.NotifierStrategy;
using Database.DTO;
using Database.Manager;

namespace Backend;

using System.Threading;

public static class Program
{
    private static readonly TimeSpan DeltaTime = TimeSpan.FromSeconds(10);
    private const string MutexName = "Global\\ActivityMonitorBackgroundService";
    private static readonly string DbPath = GetDatabasePath();

    private static void Main()
    {
        if (!VerifyMutex()) { Console.WriteLine("Another instance is already running"); return; }
        
        var dbManager = new DatabaseManager(DbPath);
        dbManager.EnsureDatabase();
        
        DataCollectorController collector = new ();
        InterventionController intervener = new();

        dbManager.InsertThreshold(new ThresholdDto
        {
            UserId = 1,
            CategoryId = 5,
            Active = true,
            InterventionType = "Notification",
            DailyLimitSec = 20,
            WeeklyLimitSec = 100
        });
        
        while (true)
        {
            collector.CheckActivity(dbManager);
            intervener.VerifyThresholds(dbManager);
            Thread.Sleep(DeltaTime);
        }
    }
    
    /// <summary>
    /// makes sure that only one instance of the program is running at a time
    /// </summary>
    /// <returns>returns true if only one instance is running</returns>
    private static bool VerifyMutex()
    {
        using var mutex = new Mutex(true, MutexName, out var isNew);
        return isNew;
    }
    
    private static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        appDataPath = Path.Combine(appDataPath, "ActivityMonitor");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "database.db");
    }
}
