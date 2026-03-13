namespace Database.DTO;

public sealed class ThresholdDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public bool Active { get; set; }
    public string? InterventionType { get; set; }
    public int? DailyLimitSec { get; set; }
    public int? WeeklyLimitSec { get; set; }
}