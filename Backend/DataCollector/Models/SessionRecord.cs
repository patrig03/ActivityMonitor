namespace Backend.Models;

public class SessionRecord
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public TimeSpan Duration => StartTime - EndTime;
}