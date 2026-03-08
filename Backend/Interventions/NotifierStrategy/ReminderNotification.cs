using System.Diagnostics;

namespace Backend.Interventions.NotifierStrategy;

public class ReminderNotification : INotifierStrategy
{
    public void Notify(string message)
    {
        var process = new Process();
        process.StartInfo.FileName = "/home/patri/Projects/ActivityMonitor/Backend/NotifierBuild/build/cmake-build-release/Notifier";
        process.StartInfo.Arguments = $"\"{message}\" Close Snooze";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        
        string output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();
        Console.WriteLine(output);
    }
}