using Database.DTO;
using Database.Configuration;

namespace Backend.Models;

public class Settings
{
    public int Id { get; set; }
    public int UserId { get; set; } = 1;
    public TimeSpan DeltaTime { get; set; } = TimeSpan.FromSeconds(10);
    public string? SyncServerAddress { get; set; }

    public static string MutexName { get => "Global\\ActivityMonitorBackgroundService"; }

    public static string DatabaseConnectionString => DatabaseConnectionFactory.BuildConnectionString();

    public static string DatabaseEndpoint => DatabaseConnectionFactory.GetDisplayName();

    public static string DataDirectory
    {
        get
        {
            var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            appDataPath = Path.Combine(appDataPath, "ActivityMonitor");
            Directory.CreateDirectory(appDataPath);
            return appDataPath;
        }
    }

    public SettingsDto ToDto()
    {
        return new SettingsDto
        {
            Id = Id,
            UserId = UserId,
            DeltaTimeSeconds = (int)DeltaTime.TotalSeconds,
            SyncServerAddress = SyncServerAddress,
        };
    }
    public static Settings FromDto(SettingsDto dto)
    {
        return new Settings
        {
            Id = dto.Id,
            UserId = dto.UserId,
            DeltaTime = TimeSpan.FromSeconds(dto.DeltaTimeSeconds),
            SyncServerAddress = dto.SyncServerAddress,
        };
    }
}
