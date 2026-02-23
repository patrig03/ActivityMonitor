using Backend.Classifier.Models;
using Backend.Models;

namespace Backend.Classifier;

public class MlClassifier : IClassifier
{
    public Category ClassifyAsync(SessionRecord record)
    {
        throw new NotImplementedException();
    }

    public IEnumerable<Category> ClassifyAsync(IEnumerable<SessionRecord> records)
    {
        throw new NotImplementedException();
    }
}