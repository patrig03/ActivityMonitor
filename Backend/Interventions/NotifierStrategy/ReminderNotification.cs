using System.Diagnostics;

namespace Backend.Interventions.NotifierStrategy;

public class ReminderNotification
{
    public string Notify(string message)
    {
        var process = new Process();
        process.StartInfo.FileName = "/home/patri/Projects/Notifier/cmake-build-release/Notifier";
        process.StartInfo.Arguments = $"-n \"{message}\" Close Snooze";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        
        string response = process.StandardOutput.ReadToEnd();

        process.WaitForExit();
        
        return response;
    }
}