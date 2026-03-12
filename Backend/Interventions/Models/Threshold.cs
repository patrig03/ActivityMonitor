using Database.DTO;

namespace Backend.Interventions.Models;

public class Threshold
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public bool Active { get; set; }
    public int InterventionType { get; set; }
    public TimeSpan? DailyLimit { get; set; }
    public TimeSpan? WeeklyLimit { get; set; }
    
    public ThresholdDto ToDto()
    {
        return new ThresholdDto
        {
            Id = Id,
            UserId = UserId,
            CategoryId = CategoryId,
            Active = Active,
            InterventionType = InterventionType,
            DailyLimitSec = DailyLimit.HasValue ? (int)DailyLimit.Value.TotalSeconds : null,
            WeeklyLimitSec = WeeklyLimit.HasValue ? (int)WeeklyLimit.Value.TotalSeconds : null,
        };
    }

    public static Threshold FromDto(ThresholdDto dto)
    {
        return new Threshold
        {
            Id = dto.Id,
            UserId = dto.UserId,
            CategoryId = dto.CategoryId,
            Active = dto.Active,
            InterventionType = dto.InterventionType,
            DailyLimit = TimeSpan.FromSeconds(dto.DailyLimitSec.GetValueOrDefault()),
            WeeklyLimit = TimeSpan.FromSeconds(dto.WeeklyLimitSec.GetValueOrDefault()),
        };
    }
    
}