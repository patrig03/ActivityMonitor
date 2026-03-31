using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Backend.Models;

namespace ActivityMonitor.Services;

public sealed class BackendProcessController
{
    private readonly object _syncRoot = new();
    private int? _launchedProcessId;

    public bool IsSupported =>
        OperatingSystem.IsWindows() ||
        OperatingSystem.IsLinux() ||
        OperatingSystem.IsMacOS();

    public bool IsRunning()
    {
        if (HasRunningMutex())
        {
            return true;
        }

        var runningProcesses = FindCandidateProcesses();
        foreach (var process in runningProcesses)
        {
            process.Dispose();
        }

        return runningProcesses.Count > 0;
    }

    public string? Start()
    {
        if (!IsSupported)
        {
            return "Controlul backend-ului este disponibil doar pe desktop.";
        }

        lock (_syncRoot)
        {
            if (IsRunning())
            {
                return null;
            }

            var backendPath = ResolveBackendExecutablePath();
            if (backendPath == null)
            {
                return "Executabilul backend nu a fost găsit în directorul aplicației.";
            }

            try
            {
                var startInfo = CreateDetachedStartInfo(backendPath);

                using var process = Process.Start(startInfo);
                if (process == null)
                {
                    return "Pornirea backend-ului a eșuat.";
                }

                _launchedProcessId = process.Id;
                return null;
            }
            catch (Exception ex)
            {
                return $"Pornirea backend-ului a eșuat: {ex.Message}";
            }
        }
    }

    public string? Stop()
    {
        if (!IsSupported)
        {
            return "Controlul backend-ului este disponibil doar pe desktop.";
        }

        var processes = FindCandidateProcesses();
        if (processes.Count == 0)
        {
            return IsRunning()
                ? "Backend-ul rulează, dar procesul nu a putut fi identificat."
                : null;
        }

        int? launchedProcessId;
        lock (_syncRoot)
        {
            launchedProcessId = _launchedProcessId;
        }

        List<string>? failures = null;
        foreach (var process in processes)
        {
            try
            {
                if (process.HasExited)
                {
                    continue;
                }

                process.Kill(entireProcessTree: true);
                process.WaitForExit(5000);
            }
            catch (Exception ex)
            {
                failures ??= new List<string>();
                failures.Add(ex.Message);
            }
            finally
            {
                process.Dispose();
            }
        }

        lock (_syncRoot)
        {
            if (_launchedProcessId == launchedProcessId)
            {
                _launchedProcessId = null;
            }
        }

        if (HasRunningMutex())
        {
            return "Backend-ul nu s-a oprit complet.";
        }

        var remainingProcesses = FindCandidateProcesses();
        foreach (var process in remainingProcesses)
        {
            process.Dispose();
        }

        if (remainingProcesses.Count > 0)
        {
            return "Backend-ul nu s-a oprit complet.";
        }

        return failures is { Count: > 0 }
            ? $"Oprirea backend-ului a întâmpinat erori: {string.Join("; ", failures)}"
            : null;
    }

    private List<Process> FindCandidateProcesses()
    {
        var candidates = new List<Process>();
        var candidateIds = new HashSet<int>();
        int? launchedProcessId;

        lock (_syncRoot)
        {
            launchedProcessId = _launchedProcessId;
        }

        if (launchedProcessId.HasValue && TryGetProcessById(launchedProcessId.Value) is { } launchedProcess)
        {
            if (!launchedProcess.HasExited && candidateIds.Add(launchedProcess.Id))
            {
                candidates.Add(launchedProcess);
            }
            else
            {
                launchedProcess.Dispose();
            }
        }

        var processName = GetBackendProcessName();

        foreach (var process in Process.GetProcessesByName(processName))
        {
            if (process.Id == Environment.ProcessId || !candidateIds.Add(process.Id))
            {
                process.Dispose();
                continue;
            }

            candidates.Add(process);
        }

        return candidates;
    }

    private static bool HasRunningMutex()
    {
        try
        {
            if (!Mutex.TryOpenExisting(Settings.MutexName, out var mutex))
            {
                return false;
            }

            mutex.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static Process? TryGetProcessById(int processId)
    {
        try
        {
            return Process.GetProcessById(processId);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    private static string GetBackendProcessName()
    {
        var backendPath = ResolveBackendExecutablePath();
        return Path.GetFileNameWithoutExtension(backendPath ?? "Backend");
    }

    private static ProcessStartInfo CreateDetachedStartInfo(string backendPath)
    {
        if (OperatingSystem.IsWindows())
        {
            return new ProcessStartInfo
            {
                FileName = backendPath,
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };
        }

        if (File.Exists("/usr/bin/setsid"))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/setsid",
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add(backendPath);
            return startInfo;
        }

        if (File.Exists("/usr/bin/nohup"))
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = "/usr/bin/nohup",
                WorkingDirectory = AppContext.BaseDirectory,
                UseShellExecute = false,
            };
            startInfo.ArgumentList.Add(backendPath);
            return startInfo;
        }

        return new ProcessStartInfo
        {
            FileName = backendPath,
            WorkingDirectory = AppContext.BaseDirectory,
            UseShellExecute = false,
        };
    }

    private static string? ResolveBackendExecutablePath()
    {
        var fileName = OperatingSystem.IsWindows() ? "Backend.exe" : "Backend";
        var candidate = Path.Combine(AppContext.BaseDirectory, fileName);
        return File.Exists(candidate) ? candidate : null;
    }
}
