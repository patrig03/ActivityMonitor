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
        Console.SetOut(TextWriter.Null);
        Console.SetError(TextWriter.Null);

        if (!TryAcquireSingleInstanceMutex())
        {
            WriteLog("[INFO] Another instance is already running");
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            WriteLog($"[FATAL] Unhandled exception. Terminating: {args.IsTerminating}{Environment.NewLine}{ex}");
        };

        try
        {
            RunMainLoop();
        }
        catch (Exception ex)
        {
            WriteLog($"[CRASH] Backend service crashed: {ex}");
        }
        finally
        {
            ReleaseSingleInstanceMutex();
        }
    }

    private static void RunMainLoop()
    {
        var dbManager = new DatabaseManager(Settings.DatabaseConnectionString);
        dbManager.EnsureDatabase();

        using var collector = new DataCollectorController(1);
        InterventionController intervener = new();

        while (true)
        {
            try
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
            catch (ThreadInterruptedException)
            {
                return;
            }
            catch (Exception ex)
            {
                WriteLog($"[ERROR] Backend loop iteration failed: {ex}");
                Thread.Sleep(TimeSpan.FromSeconds(10));
            }
        }
    }

    private static void WriteLog(string message)
    {
        try { File.AppendAllText(GetLogPath(), message + Environment.NewLine); } catch { }
    }

    private static string GetLogPath()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "logs");
        try { Directory.CreateDirectory(dir); } catch { }
        return Path.Combine(dir, "backend-crash.log");
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

    private static void ReleaseSingleInstanceMutex()
    {
        if (_singleInstanceMutex != null)
        {
            try
            {
                _singleInstanceMutex.ReleaseMutex();
                _singleInstanceMutex.Dispose();
            }
            catch { }
            _singleInstanceMutex = null;
        }
    }

}
