namespace Database.DTO;

public sealed class SessionDto
{
    public int? SessionId { get; set; }
    public int? AppId { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }
}