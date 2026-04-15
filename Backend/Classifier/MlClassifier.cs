using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.Classifier;

public class MlClassifier : IClassifier
{
    public int? ClassifyAsync(ApplicationRecord record)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<int?> ClassifyAsync(IEnumerable<ApplicationRecord> records)
    {
        throw new NotImplementedException();
    }

    public int? ClassifyAsync(BrowserRecord record)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<int?> ClassifyAsync(IEnumerable<BrowserRecord> records)
    {
        throw new NotImplementedException();
    }
}
