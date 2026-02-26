using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.DataCollector.Application;

public class LinuxAppCollector : IApplicationDataCollector
{
    public IEnumerable<ApplicationRecord> QueryApplications()
    {
        throw new NotImplementedException();
    }
}