namespace Database.DTO;

public sealed class BrowserActivityDto
{
    public int ActivityId { get; set; }
    public int UserId { get; set; }
    public int AppId { get; set; }
    public string? Url { get; set; }
    public string? Domain { get; set; }
    public string? Title { get; set; }
    public string? TabId { get; set; }
    public string? WindowId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int DurationSec { get; set; }
}