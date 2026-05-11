using Database.DTO;

namespace Backend.Models;

public class SessionRecord
{
    public int? Id { get; set; }
    public int? ApplicationId { get; set; }
    public int? DeviceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => EndTime - StartTime;

    public SessionDto ToDto()
    {
        return new SessionDto
        {
            SessionId = Id,
            AppId = ApplicationId,
            DeviceId = DeviceId,
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
            DeviceId = dto.DeviceId,
            StartTime = dto.StartTime?? DateTime.MaxValue,
            EndTime = dto.EndTime?? DateTime.MaxValue,
        };
    }
}