using Database.DTO;

namespace Backend.DataCollector.Models;

public class ApplicationRecord
{
    public int? Id { get; set; }
    public int? CategoryId { get; set; }
    public string? ProcessName { get; set; }
    public string? WindowName { get; set; }
    public string? ClassName { get; set; }
    
    
    public ApplicationDto ToDto()
    {
        return new ApplicationDto
        {
            AppId = Id,
            WindowTitle = WindowName,
            ClassName = ClassName,
            ProcessName = ProcessName,
            CategoryId = CategoryId
        };
    }

    public static ApplicationRecord FromDto(ApplicationDto dto)
    {
        return new ApplicationRecord
        {
            Id = dto.AppId,
            CategoryId = dto.CategoryId,
            ProcessName = dto.ProcessName,
            WindowName = dto.WindowTitle,
            ClassName = dto.ClassName
        };
    }
}