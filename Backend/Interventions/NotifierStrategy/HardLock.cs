using System.Diagnostics;

namespace Backend.Interventions.NotifierStrategy;

public class HardLock
{
    public void Lock(string message, int windowId, int seconds)
    {
        var process = new Process();
        process.StartInfo.FileName = "/home/patri/Projects/ActivityMonitor/Backend/NotifierBuild/build/cmake-build-release/Notifier";
        process.StartInfo.Arguments = $"-s \"{message}\" \"{windowId}\" \"{seconds}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        
        string output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();
        Console.WriteLine(output);
    }
}