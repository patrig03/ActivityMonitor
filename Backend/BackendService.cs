using Database;
using Database.DTO;
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

        DatabaseInitializer.EnsureDatabase(DbPath);
        DatabaseManager dbManager = new DatabaseManager(DbPath);
        
        // CategoryDto categoryDto = new CategoryDto
        // {
        //     CategoryId = 1,
        //     Name = "name" 
        // };
        // Console.WriteLine(dbManager.InsertCategory(categoryDto));
        
        DataCollector collector = new ();
        
        while (true)
        {
            var apps = collector.CheckActivity();
            var appIds = dbManager.InsertApplications(apps);
            foreach (var appId in appIds)
            {
                Console.WriteLine($"Inserted application with ID: {appId}");
            }
            Thread.Sleep(DeltaTime);
        }
    }
}
