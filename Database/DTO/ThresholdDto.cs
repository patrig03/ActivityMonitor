namespace Database.DTO;

public sealed class ThresholdDto
{
    public int ThresholdId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int? DailyLimitSec { get; set; }
    public int? WeeklyLimitSec { get; set; }
    public bool BreakModeEnabled { get; set; }
}