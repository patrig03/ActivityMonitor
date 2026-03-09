using System.Diagnostics;

namespace Backend.Interventions.NotifierStrategy;

public class SoftLock
{
    public void Lock(string message, int windowId, string password)
    {
        var process = new Process();
        process.StartInfo.FileName = "/home/patri/Projects/ActivityMonitor/Backend/NotifierBuild/build/cmake-build-release/Notifier";
        process.StartInfo.Arguments = $"-s \"{message}\" \"{windowId}\" \"{password}\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.RedirectStandardOutput = true;
        process.Start();
        
        string output = process.StandardOutput.ReadToEnd();

        process.WaitForExit();
        Console.WriteLine(output);
    }
}