using Backend.Models;

namespace Backend.DataCollector.Application;

public interface IApplicationDataCollector
{
    IEnumerable<ApplicationRecord> QueryApplications();
}