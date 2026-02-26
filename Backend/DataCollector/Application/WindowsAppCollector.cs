using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.DataCollector.Application;

public class WindowsAppCollector : IApplicationDataCollector
{
    public IEnumerable<ApplicationRecord> QueryApplications()
    {
        throw new NotImplementedException();
    }
}
