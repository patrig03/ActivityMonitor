using System.Text.Json;
using Database.DTO;

namespace SyncServer;

public sealed class DeviceSyncStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _stateFilePath;

    public DeviceSyncStore(string stateFilePath)
    {
        _stateFilePath = stateFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(_stateFilePath)!);
    }

    public async Task<DeviceSyncResponse> SyncDevicesAsync(DeviceSyncRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(cancellationToken);
            var bucket = GetOrCreateBucket(state, request.UserId);
            var synchronizedAt = DateTime.UtcNow;
            var currentFingerprint = request.CurrentFingerprint.Trim();

            foreach (var incoming in request.Devices.Where(IsValidIncomingDevice))
            {
                var normalized = NormalizeIncomingDevice(incoming, request.UserId, currentFingerprint, synchronizedAt);
                var existing = bucket.Devices.SingleOrDefault(device =>
                    string.Equals(device.Fingerprint, normalized.Fingerprint, StringComparison.OrdinalIgnoreCase));

                if (existing is null)
                {
                    bucket.Devices.Add(ToRecord(normalized, synchronizedAt));
                    continue;
                }

                MergeInto(existing, normalized, synchronizedAt);
            }

            bucket.Devices = bucket.Devices
                .OrderByDescending(device => device.LastSeenAt)
                .ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            await SaveStateAsync(state, cancellationToken);

            return new DeviceSyncResponse
            {
                Message = $"Serverul a sincronizat {request.Devices.Count} dispozitive pentru utilizatorul {request.UserId}.",
                Devices = bucket.Devices.Select(device => device.ToDto(request.UserId)).ToList()
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<DeviceDto>> GetDevicesAsync(int userId, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var state = await LoadStateAsync(cancellationToken);
            var bucket = state.Users.SingleOrDefault(item => item.UserId == userId);
            if (bucket is null)
            {
                return [];
            }

            return bucket.Devices
                .OrderByDescending(device => device.LastSeenAt)
                .ThenBy(device => device.Name, StringComparer.OrdinalIgnoreCase)
                .Select(device => device.ToDto(userId))
                .ToList();
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<SyncStoreState> LoadStateAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_stateFilePath))
        {
            return new SyncStoreState();
        }

        await using var stream = File.OpenRead(_stateFilePath);
        var state = await JsonSerializer.DeserializeAsync<SyncStoreState>(stream, JsonOptions, cancellationToken);
        return state ?? new SyncStoreState();
    }

    private async Task SaveStateAsync(SyncStoreState state, CancellationToken cancellationToken)
    {
        var tempFilePath = $"{_stateFilePath}.tmp";
        await using (var stream = File.Create(tempFilePath))
        {
            await JsonSerializer.SerializeAsync(stream, state, JsonOptions, cancellationToken);
        }

        File.Move(tempFilePath, _stateFilePath, overwrite: true);
    }

    private static UserDeviceBucket GetOrCreateBucket(SyncStoreState state, int userId)
    {
        var bucket = state.Users.SingleOrDefault(item => item.UserId == userId);
        if (bucket is not null)
        {
            return bucket;
        }

        bucket = new UserDeviceBucket { UserId = userId };
        state.Users.Add(bucket);
        return bucket;
    }

    private static bool IsValidIncomingDevice(DeviceDto? device)
    {
        return device is not null && !string.IsNullOrWhiteSpace(device.Fingerprint);
    }

    private static DeviceDto NormalizeIncomingDevice(
        DeviceDto device,
        int userId,
        string currentFingerprint,
        DateTime synchronizedAt)
    {
        var fingerprint = device.Fingerprint.Trim();
        var createdAt = device.CreatedAt == default ? synchronizedAt : EnsureUtc(device.CreatedAt);
        var lastSeenAt = device.LastSeenAt == default ? synchronizedAt : EnsureUtc(device.LastSeenAt);
        var isCurrentDevice = string.Equals(fingerprint, currentFingerprint, StringComparison.OrdinalIgnoreCase) || device.IsCurrentDevice;
        var status = string.IsNullOrWhiteSpace(device.Status) ? "Active" : device.Status.Trim();

        if (isCurrentDevice)
        {
            status = "Active";
        }

        return new DeviceDto
        {
            DeviceId = 0,
            UserId = userId,
            Name = string.IsNullOrWhiteSpace(device.Name) ? "Unnamed device" : device.Name.Trim(),
            DeviceType = string.IsNullOrWhiteSpace(device.DeviceType) ? "Desktop" : device.DeviceType.Trim(),
            Platform = string.IsNullOrWhiteSpace(device.Platform) ? "Unknown" : device.Platform.Trim(),
            Fingerprint = fingerprint,
            Status = status,
            AppVersion = NormalizeOptional(device.AppVersion),
            IsTrusted = device.IsTrusted,
            IsCurrentDevice = isCurrentDevice,
            CreatedAt = createdAt,
            LastSeenAt = lastSeenAt,
            RevokedAt = isCurrentDevice ? null : NormalizeNullableUtc(device.RevokedAt)
        };
    }

    private static DeviceRecord ToRecord(DeviceDto device, DateTime synchronizedAt)
    {
        return new DeviceRecord
        {
            Fingerprint = device.Fingerprint,
            Name = device.Name,
            DeviceType = device.DeviceType,
            Platform = device.Platform,
            Status = device.Status,
            AppVersion = device.AppVersion,
            IsTrusted = device.IsTrusted,
            IsCurrentDevice = device.IsCurrentDevice,
            CreatedAt = device.CreatedAt,
            LastSeenAt = device.LastSeenAt,
            RevokedAt = device.RevokedAt,
            LastSynchronizedAtUtc = synchronizedAt
        };
    }

    private static void MergeInto(DeviceRecord existing, DeviceDto incoming, DateTime synchronizedAt)
    {
        existing.Name = PickPreferred(existing.Name, incoming.Name);
        existing.DeviceType = PickPreferred(existing.DeviceType, incoming.DeviceType);
        existing.Platform = PickPreferred(existing.Platform, incoming.Platform);
        existing.AppVersion = NormalizeOptional(PickPreferred(existing.AppVersion, incoming.AppVersion));
        existing.IsTrusted = incoming.IsTrusted;
        existing.IsCurrentDevice = incoming.IsCurrentDevice;
        existing.CreatedAt = Min(existing.CreatedAt, incoming.CreatedAt);
        existing.LastSeenAt = Max(existing.LastSeenAt, incoming.LastSeenAt);
        existing.Status = incoming.IsCurrentDevice ? "Active" : PickPreferred(existing.Status, incoming.Status);
        existing.RevokedAt = incoming.IsCurrentDevice
            ? null
            : MaxNullable(existing.RevokedAt, incoming.RevokedAt);
        existing.LastSynchronizedAtUtc = synchronizedAt;
    }

    private static DateTime EnsureUtc(DateTime timestamp)
    {
        return timestamp.Kind switch
        {
            DateTimeKind.Utc => timestamp,
            DateTimeKind.Local => timestamp.ToUniversalTime(),
            _ => DateTime.SpecifyKind(timestamp, DateTimeKind.Utc)
        };
    }

    private static DateTime? NormalizeNullableUtc(DateTime? timestamp)
    {
        return timestamp.HasValue ? EnsureUtc(timestamp.Value) : null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string PickPreferred(string? existingValue, string? incomingValue)
    {
        return string.IsNullOrWhiteSpace(incomingValue)
            ? existingValue ?? string.Empty
            : incomingValue.Trim();
    }

    private static DateTime Min(DateTime left, DateTime right) => left <= right ? left : right;

    private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;

    private static DateTime? MaxNullable(DateTime? left, DateTime? right)
    {
        return left.HasValue && right.HasValue
            ? Max(left.Value, right.Value)
            : left ?? right;
    }
}
