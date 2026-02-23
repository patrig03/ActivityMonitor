using Backend.Models;

namespace Backend.DataCollector.Browser;

public class FirefoxCollector : IBrowserDataCollector
{
    public IEnumerable<BrowserRecord> QueryTabs()
    {
        throw new NotImplementedException();
    }
}