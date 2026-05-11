namespace Database.DTO;

public sealed class ApplicationDto
{
    public int? Id { get; set; }
    public int? DeviceId { get; set; }
    public string? WindowTitle { get; set; }
    public string? ClassName { get; set; }
    public string? ProcessName { get; set; }
    public int? CategoryId { get; set; }
    public long? WindowId { get; set; }
}
