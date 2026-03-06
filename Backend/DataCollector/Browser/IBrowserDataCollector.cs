using Backend.Models;

namespace Backend.DataCollector.Browser;

public interface IBrowserDataCollector
{
    BrowserRecord QueryTabs();
}