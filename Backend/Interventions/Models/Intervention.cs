namespace Backend.Interventions.Models;

public class Intervention
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int ThresholdId { get; set; }
    public DateTime TriggeredAt { get; set; }
}