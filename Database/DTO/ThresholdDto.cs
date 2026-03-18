namespace Database.DTO;

public sealed class ThresholdDto
{
    public int Id { get; set; }
    public int UserId { get; set; } = 1;
    public int CategoryId { get; set; }
    public int AppId { get; set; }
    public bool Active { get; set; }
    public string TargetType { get; set; } = "Category";
    public string InterventionType { get; set; } = "Notification";
    public string DurationType { get; set; } = "Daily";
    public int SessionLimitSec { get; set; }
    public int DailyLimitSec { get; set; }
}