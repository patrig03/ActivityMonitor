using Backend.Models;

namespace Backend.Report;

public class ReportMaker
{
    private string _outputPath { get; set; }

    public ReportMaker(string outputPath)
    {
        _outputPath = outputPath;
    }

    public bool MakeReport(Settings settings)
    {
        throw new NotImplementedException();
    }
}