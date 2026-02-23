using Backend.Report.Models;

namespace Backend.Report.Writers;

public class PdfWriter
{
    private string OutputPath { get; set; }
    PdfWriter(string outputPath)
    {
        OutputPath = outputPath;
    }
    public bool WriteToFile(ReportData data)
    {
        throw new NotImplementedException();
    }
}