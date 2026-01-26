namespace Database.DTO;

public sealed class SettingsDto
{
    public int SettingsId { get; set; }
    public int UserId { get; set; }
    public bool FocusModeEnabled { get; set; }
    public string? NotificationType { get; set; }
    public string? Theme { get; set; }
}