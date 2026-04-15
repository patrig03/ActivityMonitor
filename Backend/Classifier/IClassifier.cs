using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Models;

namespace Backend.Classifier;

public interface IClassifier
{
    int? ClassifyAsync(ApplicationRecord record);
    IEnumerable<int?> ClassifyAsync(IEnumerable<ApplicationRecord> records);
    int? ClassifyAsync(BrowserRecord record);
    IEnumerable<int?> ClassifyAsync(IEnumerable<BrowserRecord> records);
}
