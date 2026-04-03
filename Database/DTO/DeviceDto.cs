namespace Database.DTO;

public sealed class DeviceDto
{
    public int DeviceId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Desktop";
    public string Platform { get; set; } = string.Empty;
    public string Fingerprint { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? AppVersion { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsCurrentDevice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? RevokedAt { get; set; }
}
