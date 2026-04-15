using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using Database.DTO;

namespace Backend.Sync;

public sealed class ServerSync
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _httpClient;

    public ServerSync(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }

    public static bool TryNormalizeServerAddress(
        string? serverAddress,
        out string normalizedAddress,
        out string validationMessage)
    {
        normalizedAddress = string.Empty;
        validationMessage = "Introdu un IP sau URL valid pentru serverul de sincronizare.";

        if (string.IsNullOrWhiteSpace(serverAddress))
        {
            validationMessage = string.Empty;
            return true;
        }

        var candidate = serverAddress.Trim();
        if (!candidate.Contains("://", StringComparison.Ordinal))
        {
            candidate = $"http://{candidate}";
        }

        if (!Uri.TryCreate(candidate, UriKind.Absolute, out var uri))
        {
            return false;
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            validationMessage = "Serverul de sincronizare trebuie sa foloseasca http sau https.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(uri.Host))
        {
            return false;
        }

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        normalizedAddress = builder.Uri.AbsoluteUri.TrimEnd('/');
        validationMessage = string.Empty;
        return true;
    }

    public static string BuildDevicesEndpointPreview(string normalizedAddress)
    {
        if (string.IsNullOrWhiteSpace(normalizedAddress))
        {
            return "Serverul de sincronizare nu este configurat.";
        }

        return BuildDevicesEndpointUri(normalizedAddress).AbsoluteUri;
    }

    public async Task<DeviceSyncResult> SyncDevicesAsync(
        string serverAddress,
        int userId,
        IEnumerable<DeviceDto> devices,
        DeviceDto currentDevice,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeServerAddress(serverAddress, out var normalizedAddress, out var validationMessage))
        {
            return DeviceSyncResult.Failed(validationMessage);
        }

        var payload = new DeviceSyncRequest
        {
            UserId = userId,
            RequestedAtUtc = DateTime.UtcNow,
            CurrentFingerprint = currentDevice.Fingerprint,
            Devices = devices
                .Where(device => !string.IsNullOrWhiteSpace(device.Fingerprint))
                .Select(CloneForTransport)
                .ToList()
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                BuildDevicesEndpointUri(normalizedAddress),
                payload,
                JsonOptions,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                var message = string.IsNullOrWhiteSpace(errorBody)
                    ? $"Serverul de sincronizare a raspuns cu {(int)response.StatusCode}."
                    : $"Serverul de sincronizare a raspuns cu {(int)response.StatusCode}: {errorBody.Trim()}";
                return DeviceSyncResult.Failed(message);
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return DeviceSyncResult.Succeeded([], "Sincronizarea a fost acceptata de server.");
            }

            var syncResponse = JsonSerializer.Deserialize<DeviceSyncResponse>(responseBody, JsonOptions);
            var remoteDevices = syncResponse?.Devices?
                .Where(device => !string.IsNullOrWhiteSpace(device.Fingerprint))
                .Select(CloneForTransport)
                .ToList() ?? [];

            return DeviceSyncResult.Succeeded(
                remoteDevices,
                string.IsNullOrWhiteSpace(syncResponse?.Message)
                    ? $"Sincronizare finalizata cu {normalizedAddress}."
                    : syncResponse!.Message!);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return DeviceSyncResult.Failed($"Sincronizarea cu serverul a esuat: {ex.Message}");
        }
    }

    private static Uri BuildDevicesEndpointUri(string normalizedAddress)
    {
        var baseUri = new Uri(normalizedAddress, UriKind.Absolute);
        var builder = new UriBuilder(baseUri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        var basePath = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrWhiteSpace(basePath) || basePath == "/"
            ? "/api/sync/devices"
            : $"{basePath}/api/sync/devices";

        return builder.Uri;
    }

    private static DeviceDto CloneForTransport(DeviceDto device)
    {
        return new DeviceDto
        {
            DeviceId = 0,
            UserId = device.UserId,
            Name = device.Name,
            DeviceType = device.DeviceType,
            Platform = device.Platform,
            Fingerprint = device.Fingerprint,
            Status = device.Status,
            AppVersion = device.AppVersion,
            IsTrusted = device.IsTrusted,
            IsCurrentDevice = device.IsCurrentDevice,
            CreatedAt = device.CreatedAt,
            LastSeenAt = device.LastSeenAt,
            RevokedAt = device.RevokedAt
        };
    }
}

public sealed class DeviceSyncResult
{
    private DeviceSyncResult(bool success, IReadOnlyList<DeviceDto> devices, string message)
    {
        Success = success;
        Devices = devices;
        Message = message;
    }

    public bool Success { get; }

    public IReadOnlyList<DeviceDto> Devices { get; }

    public string Message { get; }

    public static DeviceSyncResult Failed(string message) => new(false, [], message);

    public static DeviceSyncResult Succeeded(IReadOnlyList<DeviceDto> devices, string message) => new(true, devices, message);
}

public sealed class DeviceSyncResponse
{
    public string? Message { get; set; }
    public List<DeviceDto>? Devices { get; set; }
}

internal sealed class DeviceSyncRequest
{
    public int UserId { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public string CurrentFingerprint { get; set; } = string.Empty;
    public List<DeviceDto> Devices { get; set; } = [];
}
