using Backend.DataCollector.Models;

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
}