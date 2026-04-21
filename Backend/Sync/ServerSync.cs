using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Backend.Sync;

public sealed class ServerSync
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

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

    public static string BuildHealthEndpointPreview(string normalizedAddress)
    {
        return string.IsNullOrWhiteSpace(normalizedAddress)
            ? "Serverul de sincronizare nu este configurat."
            : BuildHealthEndpointUri(normalizedAddress).AbsoluteUri;
    }

    public static string BuildSyncEndpointPreview(string normalizedAddress)
    {
        return string.IsNullOrWhiteSpace(normalizedAddress)
            ? "Serverul de sincronizare nu este configurat."
            : BuildSyncEndpointUri(normalizedAddress).AbsoluteUri;
    }

    public static string BuildDevicesEndpointPreview(string normalizedAddress)
    {
        return string.IsNullOrWhiteSpace(normalizedAddress)
            ? "Serverul de sincronizare nu este configurat."
            : BuildDevicesEndpointUri(normalizedAddress).AbsoluteUri;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        string serverAddress,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeServerAddress(serverAddress, out var normalizedAddress, out var validationMessage))
        {
            return HealthCheckResult.Failed(validationMessage);
        }

        var endpoints = BuildHealthEndpointCandidates(normalizedAddress);
        foreach (var endpoint in endpoints)
        {
            try
            {
                using var response = await _httpClient.GetAsync(endpoint, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    continue;
                }

                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
                if (string.IsNullOrWhiteSpace(responseBody))
                {
                    return HealthCheckResult.Succeeded("healthy", $"Server disponibil la {endpoint.AbsoluteUri}.");
                }

                var payload = JsonSerializer.Deserialize<HealthResponse>(responseBody, JsonOptions);
                return HealthCheckResult.Succeeded(
                    payload?.Status ?? "healthy",
                    $"Server disponibil la {endpoint.AbsoluteUri}.");
            }
            catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
            {
                if (endpoint == endpoints[^1])
                {
                    return HealthCheckResult.Failed($"Verificarea serverului a esuat: {ex.Message}");
                }
            }
        }

        return HealthCheckResult.Failed("Serverul nu a raspuns la endpoint-ul de health check asteptat.");
    }

    public Task<AuthResult> RegisterAsync(
        string serverAddress,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        return AuthenticateAsync(serverAddress, "/api/auth/register", email, password, "Cont creat", cancellationToken);
    }

    public Task<AuthResult> LoginAsync(
        string serverAddress,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        return AuthenticateAsync(serverAddress, "/api/auth/login", email, password, "Autentificare reusita", cancellationToken);
    }

    public async Task<DeviceRegistrationResult> EnsureDeviceAsync(
        string serverAddress,
        string bearerToken,
        string? existingDeviceId,
        string deviceName,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeServerAddress(serverAddress, out var normalizedAddress, out var validationMessage))
        {
            return DeviceRegistrationResult.Failed(validationMessage);
        }

        if (Guid.TryParse(existingDeviceId, out var existingGuid) && existingGuid != Guid.Empty)
        {
            var lookupResult = await GetDevicesAsync(normalizedAddress, bearerToken, cancellationToken);
            if (!lookupResult.Success)
            {
                return DeviceRegistrationResult.Failed(lookupResult.Message);
            }

            var existing = lookupResult.Devices.FirstOrDefault(device =>
                string.Equals(device.DeviceId, existingGuid.ToString(), StringComparison.OrdinalIgnoreCase));
            if (existing != null)
            {
                return DeviceRegistrationResult.Succeeded(existingGuid.ToString(), false, "Dispozitivul server este deja configurat.");
            }
        }

        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return DeviceRegistrationResult.Failed("Numele dispozitivului curent lipseste, deci nu poate fi creat pe server.");
        }

        try
        {
            using var createRequest = new HttpRequestMessage(HttpMethod.Post, BuildDevicesEndpointUri(normalizedAddress))
            {
                Content = JsonContent.Create(new CreateDeviceRequest
                {
                    Name = deviceName.Trim()
                }, options: JsonOptions)
            };
            createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var createResponse = await _httpClient.SendAsync(createRequest, cancellationToken);
            var createBody = await createResponse.Content.ReadAsStringAsync(cancellationToken);

            if (!createResponse.IsSuccessStatusCode)
            {
                return DeviceRegistrationResult.Failed(BuildErrorMessage(createResponse.StatusCode, createBody, "Crearea dispozitivului pe server a esuat"));
            }

            var createdDeviceId = TryExtractGuid(createBody, "deviceId")
                                  ?? TryExtractGuid(createBody, "id");
            if (!string.IsNullOrWhiteSpace(createdDeviceId))
            {
                return DeviceRegistrationResult.Succeeded(createdDeviceId, true, "Dispozitivul curent a fost inregistrat pe server.");
            }

            var lookupResult = await GetDeviceIdByNameAsync(normalizedAddress, bearerToken, deviceName.Trim(), cancellationToken);
            return lookupResult.Success
                ? lookupResult
                : DeviceRegistrationResult.Failed("Serverul a acceptat crearea dispozitivului, dar nu a returnat un deviceId utilizabil.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return DeviceRegistrationResult.Failed($"Inregistrarea dispozitivului pe server a esuat: {ex.Message}");
        }
    }

    public async Task<SyncOperationResult> SyncDataAsync(
        string serverAddress,
        string bearerToken,
        SyncRequest payload,
        CancellationToken cancellationToken = default)
    {
        if (!TryNormalizeServerAddress(serverAddress, out var normalizedAddress, out var validationMessage))
        {
            return SyncOperationResult.Failed(validationMessage);
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, BuildSyncEndpointUri(normalizedAddress))
            {
                Content = JsonContent.Create(payload, options: JsonOptions)
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return SyncOperationResult.Failed(BuildErrorMessage(response.StatusCode, responseBody, "Sincronizarea cu serverul a esuat"));
            }

            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return SyncOperationResult.Succeeded(new SyncResponse(), "Serverul a confirmat sincronizarea.");
            }

            var syncResponse = JsonSerializer.Deserialize<SyncResponse>(responseBody, JsonOptions) ?? new SyncResponse();
            return SyncOperationResult.Succeeded(syncResponse, "Sincronizarea cu serverul a fost finalizata.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return SyncOperationResult.Failed($"Sincronizarea cu serverul a esuat: {ex.Message}");
        }
    }

    private async Task<AuthResult> AuthenticateAsync(
        string serverAddress,
        string path,
        string email,
        string password,
        string successMessage,
        CancellationToken cancellationToken)
    {
        if (!TryNormalizeServerAddress(serverAddress, out var normalizedAddress, out var validationMessage))
        {
            return AuthResult.Failed(validationMessage);
        }

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return AuthResult.Failed("Emailul si parola sunt obligatorii.");
        }

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                BuildEndpointUri(normalizedAddress, path),
                new AuthRequest
                {
                    Email = email.Trim(),
                    Password = password
                },
                JsonOptions,
                cancellationToken);

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return AuthResult.Failed(BuildErrorMessage(response.StatusCode, responseBody, "Autentificarea la server a esuat"));
            }

            var payload = JsonSerializer.Deserialize<AuthResponse>(responseBody, JsonOptions);
            if (payload == null || string.IsNullOrWhiteSpace(payload.Token))
            {
                return AuthResult.Failed("Serverul nu a returnat un token valid.");
            }

            return AuthResult.Succeeded(
                payload.Token,
                payload.UserId,
                string.IsNullOrWhiteSpace(payload.Email) ? email.Trim() : payload.Email,
                successMessage);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return AuthResult.Failed($"Autentificarea la server a esuat: {ex.Message}");
        }
    }

    private async Task<DeviceRegistrationResult> GetDeviceIdByNameAsync(
        string normalizedAddress,
        string bearerToken,
        string deviceName,
        CancellationToken cancellationToken)
    {
        var lookupResult = await GetDevicesAsync(normalizedAddress, bearerToken, cancellationToken);
        if (!lookupResult.Success)
        {
            return DeviceRegistrationResult.Failed(lookupResult.Message);
        }

        var devices = lookupResult.Devices;
        var match = devices.LastOrDefault(device =>
            string.Equals(device.Name, deviceName, StringComparison.OrdinalIgnoreCase) &&
            Guid.TryParse(device.DeviceId, out _));

        return match == null
            ? DeviceRegistrationResult.Failed("Serverul nu a returnat un deviceId pentru dispozitivul nou creat.")
            : DeviceRegistrationResult.Succeeded(match.DeviceId, true, "Dispozitivul curent a fost inregistrat pe server.");
    }

    private async Task<DeviceLookupResult> GetDevicesAsync(
        string normalizedAddress,
        string bearerToken,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildDevicesEndpointUri(normalizedAddress));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            return DeviceLookupResult.Failed(BuildErrorMessage(response.StatusCode, responseBody, "Citirea dispozitivelor de pe server a esuat"));
        }

        return DeviceLookupResult.Succeeded(ParseDevices(responseBody));
    }

    private static IReadOnlyList<Uri> BuildHealthEndpointCandidates(string normalizedAddress)
    {
        return
        [
            BuildEndpointUri(normalizedAddress, "/health"),
            BuildEndpointUri(normalizedAddress, "/api/health")
        ];
    }

    private static Uri BuildHealthEndpointUri(string normalizedAddress) => BuildEndpointUri(normalizedAddress, "/health");

    private static Uri BuildSyncEndpointUri(string normalizedAddress) => BuildEndpointUri(normalizedAddress, "/api/sync");

    private static Uri BuildDevicesEndpointUri(string normalizedAddress) => BuildEndpointUri(normalizedAddress, "/api/devices");

    private static Uri BuildEndpointUri(string normalizedAddress, string relativePath)
    {
        var baseUri = new Uri(normalizedAddress, UriKind.Absolute);
        var builder = new UriBuilder(baseUri)
        {
            Query = string.Empty,
            Fragment = string.Empty
        };

        var basePath = builder.Path.TrimEnd('/');
        builder.Path = string.IsNullOrWhiteSpace(basePath) || basePath == "/"
            ? relativePath
            : $"{basePath}{relativePath}";

        return builder.Uri;
    }

    private static string BuildErrorMessage(System.Net.HttpStatusCode statusCode, string responseBody, string prefix)
    {
        return string.IsNullOrWhiteSpace(responseBody)
            ? $"{prefix}: {(int)statusCode}."
            : $"{prefix}: {(int)statusCode} - {responseBody.Trim()}";
    }

    private static string? TryExtractGuid(string? json, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        using var document = JsonDocument.Parse(json);
        return TryExtractGuid(document.RootElement, propertyName);
    }

    private static string? TryExtractGuid(JsonElement element, string propertyName)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                        property.Value.ValueKind == JsonValueKind.String)
                    {
                        var value = property.Value.GetString();
                        if (Guid.TryParse(value, out var guid) && guid != Guid.Empty)
                        {
                            return guid.ToString();
                        }
                    }

                    var nested = TryExtractGuid(property.Value, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    var nested = TryExtractGuid(item, propertyName);
                    if (!string.IsNullOrWhiteSpace(nested))
                    {
                        return nested;
                    }
                }

                break;
        }

        return null;
    }

    private static IReadOnlyList<ServerDeviceDescriptor> ParseDevices(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        using var document = JsonDocument.Parse(json);
        var result = new List<ServerDeviceDescriptor>();
        CollectDevices(document.RootElement, result);
        return result;
    }

    private static void CollectDevices(JsonElement element, ICollection<ServerDeviceDescriptor> devices)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var parsed = TryParseDevice(element);
                if (parsed != null)
                {
                    devices.Add(parsed);
                }

                foreach (var property in element.EnumerateObject())
                {
                    CollectDevices(property.Value, devices);
                }

                break;
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                {
                    CollectDevices(item, devices);
                }

                break;
        }
    }

    private static ServerDeviceDescriptor? TryParseDevice(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var id = TryExtractGuid(element, "deviceId") ?? TryExtractGuid(element, "id");
        var name = TryExtractString(element, "name");

        return string.IsNullOrWhiteSpace(id)
            ? null
            : new ServerDeviceDescriptor(id, name ?? string.Empty);
    }

    private static string? TryExtractString(JsonElement element, string propertyName)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, propertyName, StringComparison.OrdinalIgnoreCase) &&
                property.Value.ValueKind == JsonValueKind.String)
            {
                return property.Value.GetString();
            }
        }

        return null;
    }
}

public static class SyncIdentity
{
    public static string Create(string scope, params object?[] parts)
    {
        var seed = $"{scope}:{string.Join("|", parts.Select(NormalizePart))}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50);
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80);
        var guidBytes = hash.Take(16).ToArray();
        return new Guid(guidBytes).ToString();
    }

    private static string NormalizePart(object? value)
    {
        return value switch
        {
            null => string.Empty,
            DateTime dateTime => dateTime.ToUniversalTime().ToString("O"),
            DateTimeOffset dateTimeOffset => dateTimeOffset.ToUniversalTime().ToString("O"),
            _ => value.ToString()?.Trim() ?? string.Empty
        };
    }
}

public sealed record HealthCheckResult(bool Success, string Status, string Message)
{
    public static HealthCheckResult Failed(string message) => new(false, "unknown", message);

    public static HealthCheckResult Succeeded(string status, string message) => new(true, status, message);
}

public sealed record AuthResult(bool Success, string? Token, string? UserId, string? Email, string Message)
{
    public static AuthResult Failed(string message) => new(false, null, null, null, message);

    public static AuthResult Succeeded(string token, string? userId, string? email, string message) =>
        new(true, token, userId, email, message);
}

public sealed record DeviceRegistrationResult(bool Success, string? DeviceId, bool Created, string Message)
{
    public static DeviceRegistrationResult Failed(string message) => new(false, null, false, message);

    public static DeviceRegistrationResult Succeeded(string deviceId, bool created, string message) =>
        new(true, deviceId, created, message);
}

public sealed record SyncOperationResult(bool Success, SyncResponse Data, string Message)
{
    public static SyncOperationResult Failed(string message) => new(false, new SyncResponse(), message);

    public static SyncOperationResult Succeeded(SyncResponse data, string message) => new(true, data, message);
}

public sealed class SyncRequest
{
    public DateTime LastSyncAt { get; set; }
    public string DeviceId { get; set; } = string.Empty;
    public List<SyncSessionRecord> Sessions { get; set; } = [];
    public List<SyncActivityRecord> Activities { get; set; } = [];
    public List<SyncThresholdRecord> Thresholds { get; set; } = [];
    public List<SyncSettingRecord> Settings { get; set; } = [];
    public List<SyncCategoryRecord> Categories { get; set; } = [];
    public List<SyncApplicationRecord> Applications { get; set; } = [];
}

public sealed class SyncResponse
{
    public List<SyncSessionRecord> Sessions { get; set; } = [];
    public List<SyncActivityRecord> Activities { get; set; } = [];
    public List<SyncThresholdRecord> Thresholds { get; set; } = [];
    public List<SyncSettingRecord> Settings { get; set; } = [];
    public List<SyncCategoryRecord> Categories { get; set; } = [];
    public List<SyncApplicationRecord> Applications { get; set; } = [];
    public DateTime? ServerTime { get; set; }
}

public sealed class SyncSessionRecord
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public int Duration { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SyncActivityRecord
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string ApplicationId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public string? Url { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SyncThresholdRecord
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public string? ApplicationId { get; set; }
    public bool Active { get; set; }
    public string TargetType { get; set; } = "Category";
    public string InterventionType { get; set; } = "Notification";
    public string DurationType { get; set; } = "Daily";
    public int SessionLimitSec { get; set; }
    public int DailyLimitSec { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class SyncSettingRecord
{
    public string Id { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public int DeltaTimeSeconds { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class SyncCategoryRecord
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class SyncApplicationRecord
{
    public string Id { get; set; } = string.Empty;
    public string? CategoryId { get; set; }
    public string? WindowTitle { get; set; }
    public string? ClassName { get; set; }
    public string? ProcessName { get; set; }
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? WindowId { get; set; }
}

internal sealed class AuthRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

internal sealed class AuthResponse
{
    public string? Token { get; set; }
    public string? UserId { get; set; }
    public string? Email { get; set; }
}

internal sealed class HealthResponse
{
    public string? Status { get; set; }
}

internal sealed class CreateDeviceRequest
{
    public string Name { get; set; } = string.Empty;
}

internal sealed record ServerDeviceDescriptor(string DeviceId, string Name);

internal sealed record DeviceLookupResult(bool Success, IReadOnlyList<ServerDeviceDescriptor> Devices, string Message)
{
    public static DeviceLookupResult Failed(string message) => new(false, [], message);

    public static DeviceLookupResult Succeeded(IReadOnlyList<ServerDeviceDescriptor> devices) => new(true, devices, string.Empty);
}
