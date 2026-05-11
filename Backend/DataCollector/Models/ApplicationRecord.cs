using Database.DTO;

namespace Backend.DataCollector.Models;

public class ApplicationRecord
{
    public int? Id { get; set; }
    public int? DeviceId { get; set; }
    public int? CategoryId { get; set; }
    public string? ProcessName { get; set; }
    public string? WindowName { get; set; }
    public string? ClassName { get; set; }
    public long? WindowId { get; set; }
    
    public ApplicationDto ToDto()
    {
        return new ApplicationDto
        {
            Id = Id,
            DeviceId = DeviceId,
            WindowTitle = WindowName,
            ClassName = ClassName,
            ProcessName = ProcessName,
            CategoryId = CategoryId,
            WindowId = WindowId,
        };
    }

    public static ApplicationRecord FromDto(ApplicationDto dto)
    {
        return new ApplicationRecord
        {
            Id = dto.Id,
            DeviceId = dto.DeviceId,
            CategoryId = dto.CategoryId,
            ProcessName = dto.ProcessName,
            WindowName = dto.WindowTitle,
            ClassName = dto.ClassName,
            WindowId = dto.WindowId,
        };
    }
}
