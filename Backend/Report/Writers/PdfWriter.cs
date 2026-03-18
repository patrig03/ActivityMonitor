using Backend.Classifier.Models;
using Backend.Interventions.Models;
using Backend.Models;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Backend.Report.Models;

namespace Backend.Report.Writers;

public class PdfWriter
{
    private string OutputPath { get; set; }
    
    // PDF styling constants
    private static readonly Font TitleFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 18, BaseColor.DARK_GRAY);
    private static readonly Font HeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 12, BaseColor.WHITE);
    private static readonly Font SubHeaderFont = FontFactory.GetFont(FontFactory.HELVETICA_BOLD, 14, BaseColor.BLACK);
    private static readonly Font NormalFont = FontFactory.GetFont(FontFactory.HELVETICA, 10, BaseColor.BLACK);
    private static readonly Font SmallFont = FontFactory.GetFont(FontFactory.HELVETICA, 8, BaseColor.GRAY);
    
    private static readonly BaseColor HeaderBackgroundColor = new BaseColor(52, 73, 94); // Dark blue-gray
    private static readonly BaseColor AlternateRowColor = new BaseColor(245, 245, 245); // Light gray

    public PdfWriter(string outputPath)
    {
        OutputPath = outputPath;
    }

    public bool WriteToFile(IEnumerable<ReportData> data)
    {
        try
        {
            // Ensure the directory exists
            var directory = Path.GetDirectoryName(OutputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var fs = new FileStream(OutputPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // Create PDF document with A4 page size
                using (var document = new Document(PageSize.A4, 25, 25, 30, 30))
                {
                    document.Open();

                    // Add title
                    AddTitle(document, "Usage Report");
                    
                    // Add generation date
                    AddGenerationDate(document);
                    
                    // Add metadata
                    document.AddAuthor("Backend Report System");
                    document.AddTitle($"Usage Report - {DateTime.Now:yyyy-MM-dd HH:mm}");
                    
                    // Process each category's data
                    var reportList = data.ToList();
                    for (int i = 0; i < reportList.Count; i++)
                    {
                        AddCategoryReport(document, reportList[i]);
                        
                        // Add page break between categories (except for the last one)
                        if (i < reportList.Count - 1)
                        {
                            document.NewPage();
                        }
                    }
                    
                    document.Close();
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            // Log exception here if you have logging
            Console.WriteLine($"Error generating PDF: {ex.Message}");
            return false;
        }
    }

    private void AddTitle(Document document, string title)
    {
        var titleParagraph = new Paragraph(title, TitleFont)
        {
            Alignment = Element.ALIGN_CENTER,
            SpacingAfter = 20f
        };
        document.Add(titleParagraph);
    }

    private void AddGenerationDate(Document document)
    {
        var dateParagraph = new Paragraph($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}", SmallFont)
        {
            Alignment = Element.ALIGN_RIGHT,
            SpacingAfter = 20f
        };
        document.Add(dateParagraph);
    }

    private void AddCategoryReport(Document document, ReportData report)
    {
        if (report.Category == null) return;

        // Category header
        var categoryHeader = new Paragraph($"Category: {report.Category.Name}", SubHeaderFont)
        {
            SpacingBefore = 15f,
            SpacingAfter = 10f
        };
        document.Add(categoryHeader);

        // Category details
        AddCategoryDetails(document, report.Category);

        // Applications usage
        if (report.Applications != null && report.Applications.Any())
        {
            AddApplicationsTable(document, report.Applications);
        }
        else
        {
            var noApps = new Paragraph("No application usage data available.", NormalFont)
            {
                SpacingBefore = 10f,
                SpacingAfter = 10f,
                Alignment = Element.ALIGN_CENTER
            };
            document.Add(noApps);
        }

        // Browser activity
        if (report.BrowserDetails != null && report.BrowserDetails.Any())
        {
            document.NewPage();
            AddBrowserActivity(document, report.BrowserDetails);
        }

        // Interventions and Thresholds
        var hasInterventions = report.Interventions != null && report.Interventions.Any();
        var hasThresholds = report.Thresholds != null && report.Thresholds.Any();

        if (hasInterventions || hasThresholds)
        {
            if (hasInterventions)
            {
                AddInterventionsTable(document, report.Interventions);
            }

            if (hasThresholds)
            {
                AddThresholdsTable(document, report.Thresholds);
            }
        }
    }

    private void AddCategoryDetails(Document document, Category category)
    {
        var detailsTable = new PdfPTable(2)
        {
            WidthPercentage = 50,
            HorizontalAlignment = Element.ALIGN_LEFT,
            SpacingBefore = 5f,
            SpacingAfter = 15f
        };
        
        // Set column widths
        detailsTable.SetWidths(new float[] { 1f, 2f });

        AddDetailRow(detailsTable, "Category ID:", category.Id.ToString());

        document.Add(detailsTable);
    }

    private void AddDetailRow(PdfPTable table, string label, string value)
    {
        var labelCell = new PdfPCell(new Phrase(label, NormalFont))
        {
            Border = Rectangle.NO_BORDER,
            PaddingTop = 2f,
            PaddingBottom = 2f
        };
        
        var valueCell = new PdfPCell(new Phrase(value, NormalFont))
        {
            Border = Rectangle.NO_BORDER,
            PaddingTop = 2f,
            PaddingBottom = 2f
        };

        table.AddCell(labelCell);
        table.AddCell(valueCell);
    }

    private void AddApplicationsTable(Document document, IEnumerable<ProcessUsage> processes)
    {
        var processList = processes.ToList();
        var totalDuration = TimeSpan.FromTicks(processList.Sum(p => p.TotalDuration.Ticks));

        // Summary
        var summary = new Paragraph($"Total Usage: {FormatTimeSpan(totalDuration)}", NormalFont)
        {
            SpacingBefore = 5f,
            SpacingAfter = 10f
        };
        document.Add(summary);

        // Create main table for applications
        var table = new PdfPTable(5)
        {
            WidthPercentage = 100,
            SpacingBefore = 10f,
            SpacingAfter = 20f
        };

        // Set column widths
        table.SetWidths(new float[] { 2f, 2f, 1.5f, 1.5f, 1f });

        // Add headers
        AddTableHeader(table, "Process", "Window", "Class", "Duration", "% of Total");

        // Add data rows
        bool alternate = false;
        foreach (var process in processList)
        {
            foreach (var window in process.Windows)
            {
                var percentage = totalDuration.TotalMinutes > 0 
                    ? (window.TotalDuration.TotalMinutes / totalDuration.TotalMinutes * 100) 
                    : 0;

                AddTableCell(table, process.ProcessName ?? "N/A", alternate);
                AddTableCell(table, window.WindowName ?? "N/A", alternate);
                AddTableCell(table, window.ClassName ?? "N/A", alternate);
                AddTableCell(table, FormatTimeSpan(window.TotalDuration), alternate);
                AddTableCell(table, $"{percentage:F1}%", alternate);

                alternate = !alternate;
            }

            // Add process total row
            var processPercentage = totalDuration.TotalMinutes > 0 
                ? (process.TotalDuration.TotalMinutes / totalDuration.TotalMinutes * 100) 
                : 0;

            var totalCell = new PdfPCell(new Phrase($"Total for {process.ProcessName}", NormalFont))
            {
                Colspan = 4,
                BackgroundColor = new BaseColor(240, 240, 240),
                PaddingTop = 5f,
                PaddingBottom = 5f
            };
            table.AddCell(totalCell);

            var totalValueCell = new PdfPCell(new Phrase($"{FormatTimeSpan(process.TotalDuration)} ({processPercentage:F1}%)", NormalFont))
            {
                BackgroundColor = new BaseColor(240, 240, 240),
                PaddingTop = 5f,
                PaddingBottom = 5f
            };
            table.AddCell(totalValueCell);
        }

        document.Add(table);
    }

    private void AddBrowserActivity(Document document, IEnumerable<BrowserRecord> browserRecords)
    {
        var browserList = browserRecords.ToList();
        
        var header = new Paragraph("Browser Activity", SubHeaderFont)
        {
            SpacingBefore = 15f,
            SpacingAfter = 10f
        };
        document.Add(header);

        var table = new PdfPTable(4)
        {
            WidthPercentage = 100,
            SpacingBefore = 5f,
            SpacingAfter = 20f
        };

        table.SetWidths(new float[] { 0.5f, 0.5f, 3f, 2f });

        // Headers
        AddTableHeader(table, "ID", "Browser ID", "URL", "Domain");

        // Data rows
        bool alternate = false;
        foreach (var browser in browserList.Take(50)) // Limit to 50 records to avoid overwhelming the PDF
        {
            AddTableCell(table, browser.Id.ToString(), alternate);
            AddTableCell(table, browser.BrowserId.ToString(), alternate);
            
            // Truncate long URLs
            var url = browser.Url ?? "";
            if (url.Length > 50)
            {
                url = url.Substring(0, 47) + "...";
            }
            AddTableCell(table, url, alternate);
            
            AddTableCell(table, browser.Domain ?? "N/A", alternate);

            alternate = !alternate;
        }

        if (browserList.Count > 50)
        {
            var noteCell = new PdfPCell(new Phrase($"... and {browserList.Count - 50} more records", SmallFont))
            {
                Colspan = 4,
                HorizontalAlignment = Element.ALIGN_CENTER,
                PaddingTop = 5f
            };
            table.AddCell(noteCell);
        }

        document.Add(table);
    }

    private void AddInterventionsTable(Document document, IEnumerable<Intervention> interventions)
    {
        var interventionList = interventions.ToList();
        
        var header = new Paragraph("Interventions", SubHeaderFont)
        {
            SpacingBefore = 15f,
            SpacingAfter = 10f
        };
        document.Add(header);

        var table = new PdfPTable(4)
        {
            WidthPercentage = 100,
            SpacingBefore = 5f,
            SpacingAfter = 20f
        };

        table.SetWidths(new float[] { 0.5f, 1f, 1.5f, 2f });

        // Headers
        AddTableHeader(table, "ID", "Type", "Threshold ID", "Triggered At");

        // Data rows
        bool alternate = false;
        foreach (var intervention in interventionList)
        {
            AddTableCell(table, intervention.Id.ToString(), alternate);
            AddTableCell(table, intervention.ThresholdId.ToString(), alternate);
            AddTableCell(table, intervention.TriggeredAt.ToString("yyyy-MM-dd HH:mm"), alternate);

            alternate = !alternate;
        }

        document.Add(table);
    }

    private void AddThresholdsTable(Document document, IEnumerable<Threshold> thresholds)
    {
        var thresholdList = thresholds.ToList();
        
        var header = new Paragraph("Thresholds", SubHeaderFont)
        {
            SpacingBefore = 15f,
            SpacingAfter = 10f
        };
        document.Add(header);

        var table = new PdfPTable(6)
        {
            WidthPercentage = 100,
            SpacingBefore = 5f,
            SpacingAfter = 20f
        };

        table.SetWidths(new float[] { 0.5f, 0.8f, 1f, 1f, 1f, 1f });

        // Headers
        AddTableHeader(table, "ID", "Active", "Intervention Type", "Daily Limit", "Weekly Limit", "Status");

        // Data rows
        bool alternate = false;
        foreach (var threshold in thresholdList)
        {
            AddTableCell(table, threshold.Id.ToString(), alternate);
            AddTableCell(table, threshold.Active ? "Yes" : "No", alternate);
            AddTableCell(table, threshold.InterventionType ?? "N/A", alternate);
            AddTableCell(table, FormatTimeSpan(threshold.DailyLimit), alternate);
            
            // Status column
            var status = threshold.Active ? "Active" : "Inactive";
            if (threshold.Active)
            {
                status = "Active with limits";
            }
            AddTableCell(table, status, alternate);

            alternate = !alternate;
        }

        document.Add(table);
    }

    private void AddTableHeader(PdfPTable table, params string[] headers)
    {
        foreach (var header in headers)
        {
            var cell = new PdfPCell(new Phrase(header, HeaderFont))
            {
                BackgroundColor = HeaderBackgroundColor,
                HorizontalAlignment = Element.ALIGN_CENTER,
                Padding = 5f
            };
            table.AddCell(cell);
        }
    }

    private void AddTableCell(PdfPTable table, string text, bool alternate)
    {
        var cell = new PdfPCell(new Phrase(text, NormalFont))
        {
            Padding = 3f,
            BackgroundColor = alternate ? AlternateRowColor : BaseColor.WHITE
        };
        table.AddCell(cell);
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalHours >= 1)
        {
            return $"{(int)ts.TotalHours}h {ts.Minutes}m";
        }
        else if (ts.TotalMinutes >= 1)
        {
            return $"{ts.Minutes}m {ts.Seconds}s";
        }
        else
        {
            return $"{ts.Seconds}s";
        }
    }
}
