using Backend.Models;

namespace Backend.DataCollector.Browser;

public interface IBrowserDataCollector
{
    IEnumerable<BrowserRecord> QueryTabs();
}