using Database.DTO;

namespace SyncServer;

public sealed class DeviceSyncRequest
{
    public int UserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string CurrentFingerprint { get; set; } = string.Empty;
    public List<DeviceDto> Devices { get; set; } = [];
}

public sealed class DeviceSyncResponse
{
    public string Message { get; set; } = string.Empty;
    public List<DeviceDto> Devices { get; set; } = [];
}

internal sealed class SyncStoreState
{
    public List<UserDeviceBucket> Users { get; set; } = [];
}

internal sealed class UserDeviceBucket
{
    public int UserId { get; set; }
    public List<DeviceRecord> Devices { get; set; } = [];
}

internal sealed class DeviceRecord
{
    public string Fingerprint { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DeviceType { get; set; } = "Desktop";
    public string Platform { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public string? AppVersion { get; set; }
    public bool IsTrusted { get; set; }
    public bool IsCurrentDevice { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime LastSynchronizedAtUtc { get; set; }

    public DeviceDto ToDto(int userId)
    {
        return new DeviceDto
        {
            DeviceId = 0,
            UserId = userId,
            Name = Name,
            DeviceType = DeviceType,
            Platform = Platform,
            Fingerprint = Fingerprint,
            Status = Status,
            AppVersion = AppVersion,
            IsTrusted = IsTrusted,
            IsCurrentDevice = IsCurrentDevice,
            CreatedAt = CreatedAt,
            LastSeenAt = LastSeenAt,
            RevokedAt = RevokedAt
        };
    }
}
