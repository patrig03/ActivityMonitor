using Backend.DataCollector;
using Backend.Interventions;
using Backend.Models;
using Database.Manager;

namespace Backend;

using System.Threading;

public static class Program
{
    private static Mutex? _singleInstanceMutex;

    private static void Main()
    {
        if (!TryAcquireSingleInstanceMutex())
        {
            Console.WriteLine("Another instance is already running");
            return;
        }

        var dbManager = new DatabaseManager(Settings.DatabaseConnectionString);
        dbManager.EnsureDatabase();

        using var collector = new DataCollectorController();
        InterventionController intervener = new();

        while (true)
        {
            var app = collector.CheckActivity(dbManager);
            if (app != null)
            {
                intervener.VerifyThresholds(dbManager, app);
            }

            var settings = dbManager.GetSettings(1);
            if (settings == null) throw new Exception("settings not found");
            var deltaTime = TimeSpan.FromSeconds(settings.DeltaTimeSeconds);

            Thread.Sleep(deltaTime);
        }
    }
    
    private static bool TryAcquireSingleInstanceMutex()
    {
        _singleInstanceMutex = new Mutex(true, Settings.MutexName, out var isNew);
        if (!isNew)
        {
            _singleInstanceMutex.Dispose();
            _singleInstanceMutex = null;
        }

        return isNew;
    }
    
}
