using Backend.Report.Models;

namespace Backend.Report.Writers;

public class PdfWriter
{
    private string OutputPath { get; set; }
    public PdfWriter(string outputPath)
    {
        OutputPath = outputPath;
    }
    public bool WriteToFile(IEnumerable<ReportData> data)
    {
        throw new NotImplementedException();
    }
}