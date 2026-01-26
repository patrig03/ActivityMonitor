namespace Database.DTO;

public sealed class ApplicationDto
{
    public int AppId { get; set; }
    public string Name { get; set; } = null!;
    public string? Class { get; set; }
    public string? ProcessName { get; set; }
    public string Type { get; set; } = null!;
    public int CategoryId { get; set; }
}