using Database.DTO;

namespace Backend.Interventions.Models;

public class Threshold
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int AppId { get; set; }
    public bool Active { get; set; }
    public string TargetType { get; set; } = "Category";
    public string InterventionType { get; set; } = "Notification";
    public string LimitType { get; set; } = "Daily";
    public TimeSpan SessionLimit { get; set; }
    public TimeSpan DailyLimit { get; set; }
    public TimeSpan Limit
    {
        get
        {
            switch (LimitType)
            {
                case "Daily": return DailyLimit;
                case "Session": return SessionLimit;
                default: throw new ArgumentOutOfRangeException();
            }
        } 
        set
        {
            switch (LimitType)
            {
                case "Daily": DailyLimit = value; break;
                case "Session": SessionLimit = value; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }
    
    public ThresholdDto ToDto()
    {
        return new ThresholdDto
        {
            Id = Id,
            UserId = UserId,
            CategoryId = CategoryId,
            AppId = AppId,
            Active = Active,
            TargetType = TargetType,
            InterventionType = InterventionType,
            DurationType = LimitType,
            SessionLimitSec = SessionLimit.Seconds,
            DailyLimitSec = DailyLimit.Seconds,
        };
    }

    public static Threshold FromDto(ThresholdDto dto)
    {
        return new Threshold
        {
            Id = dto.Id,
            UserId = dto.UserId,
            CategoryId = dto.CategoryId,
            AppId = dto.AppId,
            Active = dto.Active,
            TargetType = dto.TargetType,
            InterventionType = dto.InterventionType,
            LimitType = dto.DurationType,
            SessionLimit = TimeSpan.FromSeconds(dto.SessionLimitSec),
            DailyLimit = TimeSpan.FromSeconds(dto.DailyLimitSec),
        };
    }
    
}