using System.Diagnostics;
using Database.DTO;
using Database.Manager;

namespace Backend;

public class DataCollectorController
{
    private const string WmctrlCmd  = "wmctrl";
    private const string XpropCmd   = "xprop";
    
    public IEnumerable<ApplicationDto> CheckActivity()
    {
        var windowsOutput = ExecuteCommand(WmctrlCmd, "-lGpx");
        return ParseWindows(windowsOutput);
    }

    private string ExecuteCommand(string file, string args)
    {
        using var process = new Process
        {
            StartInfo =
            {
                FileName           = file,
                Arguments          = args,
                RedirectStandardOutput = true,
                UseShellExecute    = false,
                CreateNoWindow     = true
            }
        };
        process.Start();
        string output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private ApplicationDto[] ParseWindows(string wmctrlResult)
    {
        var lines = wmctrlResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        return lines
            .Select(ParseWindowLine)
            .Where(w => w != null!)
            .ToArray()!;
    }

    private ApplicationDto? ParseWindowLine(string line)
    {
        // wmctrl -lGpx output: <id> <desktop> <x> <y> <w> <h> <pid> <class> <title…>
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8) return null;

        string windowId   = parts[0];
        string pidStr     = parts[2];
        string wmClass    = parts[7];
        string title      = string.Join(' ', parts.Skip(8));

        if (!int.TryParse(pidStr, out int pid)) return null;

        var xpropOutput = ExecuteCommand(XpropCmd, $"-id {windowId}");
        string windowType = GetXPropValue(xpropOutput, "_NET_WM_WINDOW_TYPE");

        return BuildDto(pid, title, wmClass, windowType);
    }

    private string? GetXPropValue(string output, string key)
    {
        // Each line: <key> = <value>
        var line = output
            .Split('\n')
            .FirstOrDefault(l => l.TrimStart().StartsWith(key));
        return line?.Split('=')?.Last()?.Trim();
    }

    private ApplicationDto BuildDto(int pid, string title, string wmClass, string? windowType)
    {
        var processName = GetProcessName(pid);

        return new ApplicationDto
        {
            AppId        = pid,
            Name         = title,
            Class        = wmClass,
            ProcessName  = processName,
            Type         = NormalizeType(windowType),
            CategoryId   = 1
        };
    }

    private string? GetProcessName(int pid)
    {
        try
        {
            return File.ReadAllText($"/proc/{pid}/comm").Trim();
        }
        catch
        {
            return null;
        }
    }

    private string NormalizeType(string? raw)
    {
        if (raw == null) return "Unknown";

        return raw switch
        {
            var s when s.Contains("DIALOG") => "Dialog",
            var s when s.Contains("UTILITY") => "Utility",
            var s when s.Contains("NORMAL")  => "ApplicationRecord",
            _                               => "Unknown"
        };
    }
    
}