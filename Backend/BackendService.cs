using Database;
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
        using var mutex = new Mutex(true, MutexName, out var isNew);
        if (!isNew) { return; }

        DatabaseValidator.EnsureDatabase(DbPath);
        var dbManager = new DatabaseManager(DbPath);

        // TODO: find good way to populate categories on database creation
        // dbManager.InsertDefaultCategories();
        
        DataCollector collector = new ();
        
        while (true)
        {
            var apps = collector.CheckActivity();
            foreach (var app in apps)
            {
                dbManager.UpdateOrInsertApplication(app);
            }
            Thread.Sleep(DeltaTime);
        }
    }
}
