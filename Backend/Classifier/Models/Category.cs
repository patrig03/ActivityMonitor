using Database.DTO;

namespace Backend.Classifier.Models;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    
    
    public CategoryDto ToDto()
    {
        return new CategoryDto
        {
            CategoryId = Id,
            Name = Name,
            Description = Description,
        };
    }

    public static Category FromDto(CategoryDto dto)
    {
        return new Category
        {
            Id = dto.CategoryId,
            Name = dto.Name,
            Description = dto.Description,
        };
    }

}