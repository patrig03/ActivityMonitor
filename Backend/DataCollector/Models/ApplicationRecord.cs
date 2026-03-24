using Database.DTO;

namespace Backend.DataCollector.Models;

public class ApplicationRecord
{
    public int? Id { get; set; }
    public int? CategoryId { get; set; }
    public string? ProcessName { get; set; }
    public string? WindowName { get; set; }
    public string? ClassName { get; set; }
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public long? WindowId { get; set; }
    
    public ApplicationDto ToDto()
    {
        return new ApplicationDto
        {
            Id = Id,
            WindowTitle = WindowName,
            ClassName = ClassName,
            ProcessName = ProcessName,
            CategoryId = CategoryId,
            WindowId = WindowId,
            PositionX = PositionX,
            PositionY = PositionY,
            Width = Width,
            Height = Height,
        };
    }

    public static ApplicationRecord FromDto(ApplicationDto dto)
    {
        return new ApplicationRecord
        {
            Id = dto.Id,
            CategoryId = dto.CategoryId,
            ProcessName = dto.ProcessName,
            WindowName = dto.WindowTitle,
            ClassName = dto.ClassName,
            WindowId = dto.WindowId,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            Width = dto.Width,
            Height = dto.Height,
        };
    }
}
