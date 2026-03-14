using Database.DTO;

namespace Backend.Models;

public class SessionRecord
{
    public int? Id { get; set; }
    public int? ApplicationId { get; set; }
    public int? UserId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public SessionDto ToDto()
    {
        return new SessionDto
        {
            SessionId = Id,
            AppId = ApplicationId,
            UserId = UserId,
            StartTime = StartTime,
            EndTime = EndTime,
        };
    }

    public static SessionRecord FromDto(SessionDto dto)
    {
        return new SessionRecord
        {
            Id = dto.SessionId,
            ApplicationId = dto.AppId,
            UserId = dto.UserId,
            StartTime = dto.StartTime?? DateTime.MaxValue,
            EndTime = dto.EndTime?? DateTime.MaxValue,
        };
    }
}