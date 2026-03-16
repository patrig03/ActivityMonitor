namespace Database.DTO;

public sealed class SettingsDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DeltaTimeSeconds { get; set; }
}