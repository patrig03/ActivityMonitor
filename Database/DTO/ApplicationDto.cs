namespace Database.DTO;

public sealed class ApplicationDto
{
    public int? Id { get; set; }
    public string? WindowTitle { get; set; }
    public string? ClassName { get; set; }
    public string? ProcessName { get; set; }
    public int? CategoryId { get; set; }
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? WindowId { get; set; }
}