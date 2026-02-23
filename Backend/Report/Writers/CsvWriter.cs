using Backend.Report.Models;

namespace Backend.Report.Writers;

public class CsvWriter
{
    private string OutputPath { get; set; }
    CsvWriter(string outputPath)
    {
        OutputPath = outputPath;
    }
    public bool WriteToFile(ReportData data)
    {
        throw new NotImplementedException();
    }
}