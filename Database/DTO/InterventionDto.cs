namespace Database.DTO;

public sealed class InterventionDto
{
    public int InterventionId { get; set; }
    public int UserId { get; set; }
    public int CategoryId { get; set; }
    public int SessionId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public string Type { get; set; } = null!;
    public int Intensity { get; set; }
}