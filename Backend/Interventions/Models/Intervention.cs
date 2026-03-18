using Database.DTO;

namespace Backend.Interventions.Models;

public class Intervention
{
    public int Id { get; set; }
    public int ThresholdId { get; set; }
    public bool Snoozed { get; set; }
    public DateTime TriggeredAt { get; set; }
    
    public InterventionDto ToDto()
    {
        return new InterventionDto
        {
            Id = Id,
            ThresholdId = ThresholdId,
            Snoozed = Snoozed,
            TriggeredAt = TriggeredAt,
        };
    }

    public static Intervention FromDto(InterventionDto dto)
    {
        return new Intervention
        {
            Id = dto.Id,
            ThresholdId = dto.ThresholdId,
            Snoozed = dto.Snoozed,
            TriggeredAt = dto.TriggeredAt,
        };
    }
}