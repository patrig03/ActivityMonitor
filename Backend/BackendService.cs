using Database;
using Database.Manager;

namespace Backend;

using System.Threading;

public static class Program
{
    private static readonly TimeSpan DeltaTime = TimeSpan.FromSeconds(10);
    private const string MutexName = "Global\\MyBackgroundBackendSingleton";
    private const string DbPath = "./data/database.db";

    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool isNew);
        if (!isNew) { return; }

        DatabaseInitializer.EnsureDatabase(DbPath);
        IDatabaseManager dbManager = new DatabaseManager(DbPath);
        
        DataCollector collector = new DataCollector(DeltaTime, dbManager);
        
        while (true)
        {
            collector.CheckActivity();
            Thread.Sleep(DeltaTime);
        }
    }
}
