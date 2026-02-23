using Backend.Models;

namespace Backend.DataCollector.Browser;

public class ChromiumCollector : IBrowserDataCollector
{
    public IEnumerable<BrowserRecord> QueryTabs()
    {
        throw new NotImplementedException();
    }
}