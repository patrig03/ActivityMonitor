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
        
        var dbManager = new DatabaseManager(Settings.DatabaseConnectionString);
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
    
    private static bool VerifyMutex()
    {
        using var mutex = new Mutex(true, Settings.MutexName, out var isNew);
        return isNew;
    }
    
}
