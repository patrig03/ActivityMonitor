using System.Diagnostics;
using BusinessLogic.DTO;

namespace BusinessLogic;

public class DataCollector
{

    public static void CheckActivity()
    {        
        var wmctrl = Process.Start(new ProcessStartInfo
        {
            FileName = "wmctrl",
            Arguments = "-lGpx",
            RedirectStandardOutput = true
        });

        string list = wmctrl.StandardOutput.ReadToEnd();
        wmctrl.WaitForExit();

        var lines = list.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var line in lines)
        {
            var parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8) 
                continue;
            
            var win = DatabaseManager.GetWindowEntry("Activity", parts[7], string.Join(' ', parts.Skip(8)));

            if (win == null) {
                win = new WindowDto(
                    parts[7],
                    string.Join(' ', parts.Skip(8)),
                    TimeSpan.FromSeconds(10)
                );
            }
            else
            {
                win.VisibleFor += TimeSpan.FromSeconds(10);
            }

            // var xprop = Process.Start(new ProcessStartInfo
            // {
            //     FileName = "xprop",
            //     Arguments = $"-id {parts[0]}",
            //     RedirectStandardOutput = true
            // });
            //
            // string xp = xprop.StandardOutput.ReadToEnd();
            // xprop.WaitForExit();
            //
            // string Get(string key) =>
            //     xp.Split('\n')
            //         .FirstOrDefault(l => l.TrimStart().StartsWith(key))?
            //         .Split('=')?
            //         .Last()
            //         .Trim() ?? "unknown";
            
            // win.Machine    = Get("WM_CLIENT_MACHINE");
            // win.WindowType = Get("_NET_WM_WINDOW_TYPE");
            // win.State      = Get("_NET_WM_STATE");
            // win.NetName    = Get("_NET_WM_NAME");

            DatabaseManager.InsertOrUpdate(win);
        }

    }
}