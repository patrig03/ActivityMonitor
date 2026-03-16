using Backend.DataCollector;
using Backend.Interventions;
using Backend.Models;
using Database.Manager;

namespace Backend;

using System.Threading;

public static class Program
{
    private static void Main()
    {
        if (!VerifyMutex()) { Console.WriteLine("Another instance is already running"); return; }
        
        var dbManager = new DatabaseManager(Settings.DbPath);
        dbManager.EnsureDatabase();
        
        DataCollectorController collector = new();
        InterventionController intervener = new();
        
        while (true)
        {
            var app = collector.CheckActivity(dbManager);
            intervener.VerifyThresholds(dbManager, app);
            
            var settings = dbManager.GetSettings(1);
            if (settings == null) throw new Exception("settings not found");
            var deltaTime = TimeSpan.FromSeconds(settings.DeltaTimeSeconds);

            Thread.Sleep(deltaTime);
        }
    }
    
    /// <summary>
    /// makes sure that only one instance of the program is running at a time
    /// </summary>
    /// <returns>returns true if only one instance is running</returns>
    private static bool VerifyMutex()
    {
        using var mutex = new Mutex(true, Settings.MutexName, out var isNew);
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
