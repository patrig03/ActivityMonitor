namespace Database.DTO;

public sealed class ApplicationDto
{
    public int? AppId { get; set; }
    public string? WindowTitle { get; set; }
    public string? ClassName { get; set; }
    public string? ProcessName { get; set; }
    public int? CategoryId { get; set; }
}