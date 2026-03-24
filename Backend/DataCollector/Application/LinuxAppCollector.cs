using System.Diagnostics;
using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.DataCollector.Application;

public class LinuxAppCollector : IApplicationDataCollector
{
    private const string WmctrlCmd  = "wmctrl";
    private const string XpropCmd   = "xprop";
    
    public ApplicationRecord? GetActive()
    {
        var wmctrlResult = ExecuteCommand(WmctrlCmd, "-lGpx");
        var app = ParseWindows(wmctrlResult);
        return app;
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
        var output = process.StandardOutput.ReadToEnd();
        process.WaitForExit();
        return output;
    }

    private ApplicationRecord? ParseWindows(string wmctrlResult)
    {
        var lines = wmctrlResult.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0) return null;

        var array = lines
            .Select(ParseWindowLine)
            .Where(w => w != null!)
            .ToArray();
        
        if (array.Length == 0) return null;
        return array.First();
    }

    private ApplicationRecord? ParseWindowLine(string line)
    {
        // wmctrl -lGpx output: <id> <desktop> <pid> <x> <y> <w> <h> <class> <host> <title>
        var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 8) return null;
                
        var windowId   = parts[0];
        if (!int.TryParse(parts[2], out var pid)) return null;

        var xpropOutput = ExecuteCommand(XpropCmd, $"-id {windowId}");
        var state = GetXPropValue(xpropOutput, "_NET_WM_STATE");

        if (state == null) return null;
        if (!state.Contains("_NET_WM_STATE_FOCUSED")) return null;
        
        return new ApplicationRecord
        {
            Id = null,
            CategoryId = null,
            ProcessName = GetProcessName(pid),
            WindowName = string.Join(' ', parts.Skip(9)),
            ClassName = parts[7],
            PositionX = int.Parse(parts[3]),
            PositionY = int.Parse(parts[4]),
            Width = int.Parse(parts[5]),
            Height = int.Parse(parts[6]),
            WindowId = Convert.ToInt64(parts[0], 16)
            
        };
    }

    private string? GetXPropValue(string output, string key)
    {
        // Each line: <key> = <value>
        var line = output
            .Split('\n')
            .FirstOrDefault(l => l.TrimStart().StartsWith(key));
        return line?.Split('=')?.Last()?.Trim();
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
    
}
