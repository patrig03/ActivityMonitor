namespace Database.DTO;

public sealed class ReportDto
{
    // public int ReportId { get; set; }
    // public int UserId { get; set; }
    // public string PeriodType { get; set; } = null!;
    // public DateTime PeriodStart { get; set; }
    // public DateTime PeriodEnd { get; set; }
    // public int CategoryId { get; set; }
    // public int TotalDurationSec { get; set; }
    
    
    public string CategoryName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string SessionDetails { get; set; } = string.Empty;
}