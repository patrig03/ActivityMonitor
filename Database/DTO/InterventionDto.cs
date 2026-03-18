namespace Database.DTO;

public sealed class InterventionDto
{
    public int Id { get; set; }
    public int ThresholdId { get; set; }
    public bool Snoozed { get; set; }
    public DateTime TriggeredAt { get; set; }
}