using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.DataCollector.Application;

public interface IApplicationDataCollector
{
    ApplicationRecord? GetActive();
}