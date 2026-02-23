using Backend.Classifier.Models;
using Backend.Models;

namespace Backend.Classifier;

public interface IClassifier
{
    Category ClassifyAsync(SessionRecord record);
    IEnumerable<Category> ClassifyAsync(IEnumerable<SessionRecord> records);
}