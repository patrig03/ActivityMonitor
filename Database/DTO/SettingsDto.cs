namespace Database.DTO;

public sealed class SettingsDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public int DeltaTimeSeconds { get; set; }
    public string? SyncServerAddress { get; set; }
    public string? SyncEmail { get; set; }
    public string? SyncAuthToken { get; set; }
    public string? SyncRemoteUserId { get; set; }
    public string? SyncDeviceId { get; set; }
    public DateTime? SyncLastServerTimeUtc { get; set; }
}
