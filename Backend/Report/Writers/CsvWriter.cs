using Backend.Report.Models;

namespace Backend.Report.Writers;

public class CsvWriter
{
    private string OutputPath { get; set; }
    public CsvWriter(string outputPath)
    {
        OutputPath = outputPath;
    }
    public bool WriteToFile(IEnumerable<ReportData> data)
    {
        throw new NotImplementedException();
    }
}