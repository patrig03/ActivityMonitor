using System.Text.Json;

namespace SyncServer;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseUrls(GetListenUrls());

        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });

        builder.Services.AddSingleton(_ =>
        {
            var dataDirectory = ResolveDataDirectory();
            var stateFilePath = Path.Combine(dataDirectory, "device-sync-state.json");
            return new DeviceSyncStore(stateFilePath);
        });

        var app = builder.Build();

        app.MapGet("/", (HttpContext httpContext) =>
        {
            var baseUrl = $"{httpContext.Request.Scheme}://{httpContext.Request.Host}";
            return Results.Ok(new
            {
                service = "ActivityMonitor Sync Server",
                endpoints = new[]
                {
                    $"{baseUrl}/healthz",
                    $"{baseUrl}/api/sync/devices",
                    $"{baseUrl}/api/sync/devices/{{userId}}"
                }
            });
        });

        app.MapGet("/healthz", () => Results.Ok(new
        {
            status = "ok",
            serverTimeUtc = DateTime.UtcNow
        }));

        app.MapGet("/api/sync/devices/{userId:int}", async (int userId, DeviceSyncStore store, CancellationToken cancellationToken) =>
        {
            if (userId <= 0)
            {
                return Results.BadRequest(new { message = "userId must be a positive integer." });
            }

            var devices = await store.GetDevicesAsync(userId, cancellationToken);
            return Results.Ok(new DeviceSyncResponse
            {
                Message = $"Au fost gasite {devices.Count} dispozitive pentru utilizatorul {userId}.",
                Devices = devices.ToList()
            });
        });

        app.MapPost("/api/sync/devices", async (
            DeviceSyncRequest? request,
            DeviceSyncStore store,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new { message = "Request body is required." });
            }

            if (request.UserId <= 0)
            {
                return Results.BadRequest(new { message = "userId must be a positive integer." });
            }

            if (request.Devices is null || request.Devices.Count == 0)
            {
                return Results.BadRequest(new { message = "At least one device must be provided." });
            }

            var logger = loggerFactory.CreateLogger("SyncServer");
            logger.LogInformation(
                "Sync request for user {UserId} with {DeviceCount} devices from fingerprint {Fingerprint}",
                request.UserId,
                request.Devices.Count,
                request.CurrentFingerprint);

            var response = await store.SyncDevicesAsync(request, cancellationToken);
            return Results.Ok(response);
        });

        app.Run();
    }

    private static string[] GetListenUrls()
    {
        var configuredUrls = Environment.GetEnvironmentVariable("SYNC_SERVER_URLS");
        if (string.IsNullOrWhiteSpace(configuredUrls))
        {
            configuredUrls = Environment.GetEnvironmentVariable("ASPNETCORE_URLS");
        }

        if (string.IsNullOrWhiteSpace(configuredUrls))
        {
            configuredUrls = "http://0.0.0.0:8080";
        }

        return configuredUrls
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string ResolveDataDirectory()
    {
        var configuredDirectory = Environment.GetEnvironmentVariable("ACTIVITY_MONITOR_SYNC_DATA_DIR");
        if (!string.IsNullOrWhiteSpace(configuredDirectory))
        {
            Directory.CreateDirectory(configuredDirectory);
            return configuredDirectory;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dataDirectory = Path.Combine(localAppData, "ActivityMonitor", "SyncServer");
        Directory.CreateDirectory(dataDirectory);
        return dataDirectory;
    }
}
