using Database.DTO;

namespace Backend.Interventions.Models;

public class Intervention
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int ThresholdId { get; set; }
    public string Type { get; set; }
    public DateTime TriggeredAt { get; set; }
    
    public InterventionDto ToDto()
    {
        return new InterventionDto
        {
            InterventionId = Id,
            UserId = UserId,
            CategoryId = CategoryId,
            TriggeredAt = TriggeredAt,
            Type = Type,
        };
    }

    public static Intervention FromDto(InterventionDto dto)
    {
        return new Intervention
        {
            Id = dto.InterventionId,
            UserId = dto.UserId,
            CategoryId = dto.CategoryId,
            TriggeredAt = dto.TriggeredAt,
            Type = dto.Type,
        };
    }
}