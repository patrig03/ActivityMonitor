using Backend.DataCollector;
using Backend.Interventions.NotifierStrategy;
using Database.Manager;

namespace Backend;

using System.Threading;

public static class Program
{
    private static readonly TimeSpan DeltaTime = TimeSpan.FromSeconds(10);
    private const string MutexName = "Global\\MyBackgroundBackendSingleton";
    private const string DbPath = "./data/database.db";

    private static void Main()
    {
        if (!VerifyMutex()) { Console.WriteLine("Another instance is already running"); return; }
        
        var dbManager = new DatabaseManager(DbPath);
        dbManager.EnsureDatabase();
        
        DataCollectorController collector = new ();

        var notifier = new ReminderNotification();
        notifier.Notify("You have exceeded the daily limit for this category");
        
        while (true)
        {
            collector.CheckActivity(dbManager);
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
}
