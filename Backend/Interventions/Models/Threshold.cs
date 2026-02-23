namespace Backend.Interventions.Models;

public class Threshold
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public TimeSpan? DailyLimit { get; set; }
    public TimeSpan? WeeklyLimit { get; set; }
}