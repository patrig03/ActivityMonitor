namespace Database.DTO;

public sealed class BrowserActivityDto
{
    public int ActivityId { get; set; }
    public int UserId { get; set; }
    public int AppId { get; set; }
    public int? CategoryId { get; set; }
    public string? Url { get; set; }
}
