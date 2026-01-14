namespace BusinessLogic;

using System.IO.Pipes;
using System.Threading;

class Program
{
    static readonly string MutexName = "Global\\MyBackgroundBackendSingleton";

    static void Main()
    {
        using var mutex = new Mutex(true, MutexName, out bool isNew);

        if (!isNew)
            return; // already running → exit

        while (true)
        {
            DataCollector.CheckActivity();
            Console.WriteLine("writing to database..");
            System.Threading.Thread.Sleep(10000);
        }
    }
}
