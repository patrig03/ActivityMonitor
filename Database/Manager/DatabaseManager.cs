using Database.Configuration;
using Database.DTO;
using Database.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Database.Manager;

public sealed class DatabaseManager : IDatabaseManager
{
    private readonly IActivityMonitorDbContextFactory _contextFactory;
    private readonly IDatabaseInitializer _initializer;

    public DatabaseManager(string connectionString)
    {
        var options = DatabaseConnectionFactory.Parse(connectionString);
        _contextFactory = new ActivityMonitorDbContextFactory(connectionString);
        _initializer = new MySqlDatabaseInitializer(options, _contextFactory);
        _initializer.EnsureDatabase();
    }

    public void EnsureDatabase() => _initializer.EnsureDatabase();
    public void Dispose() { }

    private ActivityMonitorDbContext CreateContext() => _contextFactory.CreateDbContext();

    private static ApplicationEntity? FindApplication(ActivityMonitorDbContext context, ApplicationDto app)
        => context.Applications.FirstOrDefault(e =>
            e.DeviceId == app.DeviceId &&
            e.Name == app.WindowTitle &&
            e.Class == app.ClassName &&
            e.ProcessName == app.ProcessName);

    private static int? NullIfZero(int value) => value == 0 ? null : value;

    #region Device

    public int InsertDevice(DeviceDto device)
    {
        using var context = CreateContext();
        var entity = device.ToEntity();
        context.Devices.Add(entity);
        context.SaveChanges();
        return entity.DeviceId;
    }

    public int UpdateDevice(DeviceDto device)
    {
        using var context = CreateContext();
        var entity = context.Devices.FirstOrDefault(e => e.DeviceId == device.DeviceId);
        if (entity is null) return 0;
        entity.UpdateFrom(device);
        return context.SaveChanges();
    }

    public int UpsertDevice(DeviceDto device)
    {
        using var context = CreateContext();
        var entity = device.DeviceId > 0
            ? context.Devices.FirstOrDefault(e => e.DeviceId == device.DeviceId)
            : context.Devices.FirstOrDefault(e => e.UserId == device.UserId && e.Fingerprint == device.Fingerprint);

        if (entity is null)
        {
            entity = device.ToEntity();
            context.Devices.Add(entity);
            context.SaveChanges();
            return entity.DeviceId;
        }
        entity.UpdateFrom(device);
        context.SaveChanges();
        return entity.DeviceId;
    }

    public IEnumerable<DeviceDto> GetDevicesForUser(int userId)
    {
        using var context = CreateContext();
        return context.Devices
            .AsNoTracking()
            .Where(e => e.UserId == userId)
            .OrderByDescending(e => e.IsCurrentDevice)
            .ThenByDescending(e => e.LastSeenAt)
            .Select(e => e.ToDto())
            .ToArray();
    }

    public DeviceDto? GetDevice(int deviceId)
    {
        using var context = CreateContext();
        return context.Devices
            .AsNoTracking()
            .FirstOrDefault(e => e.DeviceId == deviceId)?
            .ToDto();
    }

    #endregion

    #region Settings

    public int InsertSettings(SettingsDto settings)
    {
        using var context = CreateContext();
        var entity = new SettingsEntity
        {
            UserId = settings.UserId,
            RefreshTimeSeconds = settings.DeltaTimeSeconds,
            SyncServerAddress = settings.SyncServerAddress,
            SyncEmail = settings.SyncEmail,
            SyncAuthToken = settings.SyncAuthToken,
            SyncRemoteUserId = settings.SyncRemoteUserId,
            SyncDeviceId = settings.SyncDeviceId,
            SyncLastServerTimeUtc = settings.SyncLastServerTimeUtc,
            ReportPath = settings.ReportPath
        };
        context.Settings.Add(entity);
        context.SaveChanges();
        return entity.SettingsId;
    }

    public int UpdateSettings(SettingsDto settings)
    {
        using var context = CreateContext();
        var entity = context.Settings.FirstOrDefault(e => e.SettingsId == settings.Id);
        if (entity is null) return 0;
        entity.UserId = settings.UserId;
        entity.RefreshTimeSeconds = settings.DeltaTimeSeconds;
        entity.SyncServerAddress = settings.SyncServerAddress;
        entity.SyncEmail = settings.SyncEmail;
        entity.SyncAuthToken = settings.SyncAuthToken;
        entity.SyncRemoteUserId = settings.SyncRemoteUserId;
        entity.SyncDeviceId = settings.SyncDeviceId;
        entity.SyncLastServerTimeUtc = settings.SyncLastServerTimeUtc;
        entity.ReportPath = settings.ReportPath;
        return context.SaveChanges();
    }

    public SettingsDto? GetSettings(int userId)
    {
        using var context = CreateContext();
        return context.Settings.AsNoTracking().FirstOrDefault(e => e.UserId == userId)?.ToDto();
    }

    #endregion

    #region Category

    public int InsertCategory(CategoryDto category)
    {
        using var context = CreateContext();
        var entity = new CategoryEntity { Name = category.Name, Description = category.Description };
        context.Categories.Add(entity);
        context.SaveChanges();
        return entity.CategoryId;
    }

    public int UpdateCategory(CategoryDto category)
    {
        using var context = CreateContext();
        var entity = context.Categories.FirstOrDefault(e => e.CategoryId == category.CategoryId);
        if (entity is null) return 0;
        entity.Name = category.Name;
        entity.Description = category.Description;
        return context.SaveChanges();
    }

    public CategoryDto? GetCategory(int categoryId)
    {
        using var context = CreateContext();
        return context.Categories.AsNoTracking().FirstOrDefault(e => e.CategoryId == categoryId)?.ToDto();
    }

    public IEnumerable<CategoryDto> GetAllCategories()
    {
        using var context = CreateContext();
        return context.Categories.AsNoTracking().OrderBy(e => e.CategoryId).Select(e => e.ToDto()).ToArray();
    }

    public int DeleteCategory(int categoryId)
    {
        using var context = CreateContext();
        var entity = context.Categories.FirstOrDefault(e => e.CategoryId == categoryId);
        if (entity is null) return 0;
        context.Categories.Remove(entity);
        return context.SaveChanges();
    }

    #endregion

    #region Application

    public int InsertApplication(ApplicationDto app)
    {
        using var context = CreateContext();
        var entity = app.ToEntity();
        context.Applications.Add(entity);
        context.SaveChanges();
        return entity.AppId;
    }

    public int UpsertApplication(ApplicationDto app)
    {
        var existingId = IsInDb(app);
        return existingId.HasValue
            ? UpdateApplication(app) ?? throw new InvalidOperationException("Could not update application.")
            : InsertApplication(app);
    }

    public int? UpdateApplication(ApplicationDto app)
    {
        using var context = CreateContext();
        var entity = FindApplication(context, app);
        if (entity is null) return null;
        entity.Name = app.WindowTitle;
        entity.Class = app.ClassName;
        entity.ProcessName = app.ProcessName;
        entity.CategoryId = app.CategoryId;
        entity.DeviceId = app.DeviceId;
        entity.WindowId = app.WindowId;
        context.SaveChanges();
        return entity.AppId;
    }

    public int UpdateApplicationCategory(int appId, int? categoryId)
    {
        using var context = CreateContext();
        var entity = context.Applications.FirstOrDefault(e => e.AppId == appId);
        if (entity is null) return 0;
        entity.CategoryId = categoryId;
        return context.SaveChanges();
    }

    public IEnumerable<int> InsertApplications(IEnumerable<ApplicationDto> apps)
    {
        using var context = CreateContext();
        var entities = apps.Select(app => app.ToEntity()).ToArray();
        context.Applications.AddRange(entities);
        context.SaveChanges();
        return entities.Select(e => e.AppId).ToArray();
    }

    public ApplicationDto? GetApplication(int appId)
    {
        using var context = CreateContext();
        return context.Applications.AsNoTracking().FirstOrDefault(e => e.AppId == appId)?.ToDto();
    }

    public IEnumerable<ApplicationDto> GetApplicationsByCategory(int categoryId)
    {
        using var context = CreateContext();
        return context.Applications
            .AsNoTracking()
            .Where(e => e.CategoryId == categoryId)
            .OrderBy(e => e.AppId)
            .Select(e => e.ToDto())
            .ToArray();
    }

    public IEnumerable<ApplicationDto> GetAllApplications()
    {
        using var context = CreateContext();
        return context.Applications.AsNoTracking().OrderBy(e => e.AppId).Select(e => e.ToDto()).ToArray();
    }

    public int? IsInDb(ApplicationDto app)
    {
        using var context = CreateContext();
        return FindApplication(context, app)?.AppId;
    }

    #endregion

    #region Session

    public int? IsInDb(SessionDto session)
    {
        using var context = CreateContext();
        return context.Sessions
            .AsNoTracking()
            .FirstOrDefault(e => e.AppId == session.AppId && e.DeviceId == session.DeviceId && e.StartTime == session.StartTime)?
            .SessionId;
    }

    public int InsertSession(SessionDto session)
    {
        using var context = CreateContext();
        var entity = new SessionEntity
        {
            AppId = session.AppId,
            DeviceId = session.DeviceId,
            StartTime = session.StartTime,
            EndTime = session.EndTime
        };
        context.Sessions.Add(entity);
        context.SaveChanges();
        return entity.SessionId;
    }

    public int? UpdateSession(SessionDto session)
    {
        using var context = CreateContext();
        var entity = context.Sessions.FirstOrDefault(e =>
            e.AppId == session.AppId && e.DeviceId == session.DeviceId && e.StartTime == session.StartTime);
        if (entity is null) return null;
        entity.AppId = session.AppId;
        entity.DeviceId = session.DeviceId;
        entity.StartTime = session.StartTime;
        entity.EndTime = session.EndTime;
        context.SaveChanges();
        return entity.SessionId;
    }

    public int UpsertSession(SessionDto session)
    {
        var existingId = IsInDb(session);
        return existingId.HasValue
            ? UpdateSession(session) ?? throw new InvalidOperationException("Could not update session.")
            : InsertSession(session);
    }

    public SessionDto? GetSession(int sessionId)
    {
        using var context = CreateContext();
        return context.Sessions.AsNoTracking().FirstOrDefault(e => e.SessionId == sessionId)?.ToDto();
    }

    public IEnumerable<SessionDto> GetSessionsForDevice(int deviceId)
    {
        using var context = CreateContext();
        return context.Sessions
            .AsNoTracking()
            .Where(e => e.DeviceId == deviceId)
            .OrderBy(e => e.StartTime)
            .Select(e => e.ToDto())
            .ToArray();
    }

    public IEnumerable<SessionDto> GetSessionsByCategory(int categoryId)
    {
        using var context = CreateContext();
        return context.Sessions
            .AsNoTracking()
            .Where(e => e.Application != null && e.Application.CategoryId == categoryId)
            .OrderBy(e => e.StartTime)
            .Select(e => e.ToDto())
            .ToArray();
    }

    public int GetSessionDurationForCategory(int categoryId)
    {
        using var context = CreateContext();
        var sessions = context.Sessions
            .AsNoTracking()
            .Where(e => e.Application != null && e.Application.CategoryId == categoryId)
            .ToArray();
        return sessions.Sum(e => !e.StartTime.HasValue || !e.EndTime.HasValue ? 0 : Math.Max(0, (int)(e.EndTime.Value - e.StartTime.Value).TotalSeconds));
    }

    public int GetSessionDurationForCategorySince(int categoryId, DateTime since)
    {
        using var context = CreateContext();
        var sessions = context.Sessions
            .AsNoTracking()
            .Where(e => e.Application != null && e.Application.CategoryId == categoryId)
            .Where(e => e.EndTime > since)
            .ToArray();
        var total = 0;
        foreach (var session in sessions)
        {
            if (!session.StartTime.HasValue || !session.EndTime.HasValue) continue;
            var effectiveStart = session.StartTime.Value > since ? session.StartTime.Value : since;
            var effectiveEnd = session.EndTime.Value;
            total += Math.Max(0, (int)(effectiveEnd - effectiveStart).TotalSeconds);
        }
        return total;
    }

    #endregion

    #region BrowserActivity

    public void InsertBrowserActivity(BrowserActivityDto activity)
    {
        using var context = CreateContext();
        context.BrowserActivities.Add(new BrowserActivityEntity
        {
            DeviceId = activity.DeviceId,
            AppId = activity.AppId,
            CategoryId = activity.CategoryId,
            Url = activity.Url
        });
        context.SaveChanges();
    }

    public IEnumerable<BrowserActivityDto> GetBrowserActivityForSession(int sessionId)
    {
        using var context = CreateContext();
        var session = context.Sessions.AsNoTracking().FirstOrDefault(e => e.SessionId == sessionId);
        if (session is null || !session.AppId.HasValue || !session.DeviceId.HasValue) return [];
        return context.BrowserActivities
            .AsNoTracking()
            .Where(e => e.AppId == session.AppId.Value && e.DeviceId == session.DeviceId.Value)
            .OrderByDescending(e => e.ActivityId)
            .Select(e => e.ToDto())
            .ToArray();
    }

    public IEnumerable<BrowserActivityDto> GetAllBrowserActivity()
    {
        using var context = CreateContext();
        return context.BrowserActivities.AsNoTracking().OrderBy(e => e.ActivityId).Select(e => e.ToDto()).ToArray();
    }

    public int? IsInDb(BrowserActivityDto activity)
    {
        using var context = CreateContext();
        return context.BrowserActivities
            .AsNoTracking()
            .FirstOrDefault(e => e.DeviceId == activity.DeviceId && e.AppId == activity.AppId && e.Url == activity.Url)?
            .ActivityId;
    }

    #endregion

    #region Threshold

    public int InsertThreshold(ThresholdDto threshold)
    {
        using var context = CreateContext();
        var entity = threshold.ToEntity();
        context.Thresholds.Add(entity);
        context.SaveChanges();
        return entity.ThresholdId;
    }

    public ThresholdDto? GetThreshold(int deviceId, int categoryId)
    {
        using var context = CreateContext();
        return context.Thresholds
            .AsNoTracking()
            .FirstOrDefault(e => e.DeviceId == deviceId && e.CategoryId == categoryId)?
            .ToDto();
    }

    public IEnumerable<ThresholdDto?> GetAllThresholds()
    {
        using var context = CreateContext();
        return context.Thresholds.AsNoTracking().OrderBy(e => e.ThresholdId).Select(e => e.ToDto()).ToArray();
    }

    public void DeleteThreshold(ThresholdDto threshold)
    {
        using var context = CreateContext();
        var entity = context.Thresholds.FirstOrDefault(e => e.ThresholdId == threshold.Id);
        if (entity is null) return;
        context.Thresholds.Remove(entity);
        context.SaveChanges();
    }

    public int UpdateThreshold(ThresholdDto threshold)
    {
        using var context = CreateContext();
        var entity = context.Thresholds.FirstOrDefault(e => e.ThresholdId == threshold.Id);
        if (entity is null) return 0;
        entity.DeviceId = threshold.DeviceId;
        entity.CategoryId = NullIfZero(threshold.CategoryId);
        entity.AppId = NullIfZero(threshold.AppId);
        entity.IsActive = threshold.Active;
        entity.TargetType = threshold.TargetType;
        entity.InterventionType = threshold.InterventionType;
        entity.DurationType = threshold.DurationType;
        entity.DailyLimitSec = threshold.DailyLimitSec;
        entity.SessionLimitSec = threshold.SessionLimitSec;
        entity.ActivatedAt = threshold.ActivatedAt;
        return context.SaveChanges();
    }

    public int UpsertThreshold(ThresholdDto threshold)
        => threshold.Id > 0 ? UpdateThreshold(threshold) : InsertThreshold(threshold);

    #endregion

    #region Intervention

    public int InsertIntervention(InterventionDto intervention)
    {
        using var context = CreateContext();
        var entity = new InterventionEntity
        {
            ThresholdId = intervention.ThresholdId,
            TriggeredAt = intervention.TriggeredAt,
            Snoozed = intervention.Snoozed
        };
        context.Interventions.Add(entity);
        context.SaveChanges();
        return entity.InterventionId;
    }

    public IEnumerable<InterventionDto> GetInterventionsForDevice(int deviceId)
    {
        using var context = CreateContext();
        return context.Interventions
            .AsNoTracking()
            .Where(e => e.Threshold != null && e.Threshold.DeviceId == deviceId)
            .OrderByDescending(e => e.TriggeredAt)
            .Select(e => e.ToDto())
            .ToArray();
    }

    #endregion
}

#region Extension Methods

internal static class EntityDtoExtensions
{
    internal static DeviceDto ToDto(this DeviceEntity e) => new() { DeviceId = e.DeviceId, UserId = e.UserId, Name = e.Name, DeviceType = e.DeviceType, Platform = e.Platform, Fingerprint = e.Fingerprint, Status = e.Status, AppVersion = e.AppVersion, IsTrusted = e.IsTrusted, IsCurrentDevice = e.IsCurrentDevice, CreatedAt = e.CreatedAt, LastSeenAt = e.LastSeenAt, RevokedAt = e.RevokedAt };
    internal static void UpdateFrom(this DeviceEntity e, DeviceDto dto) { e.UserId = dto.UserId; e.Name = dto.Name; e.DeviceType = dto.DeviceType; e.Platform = dto.Platform; e.Fingerprint = dto.Fingerprint; e.Status = dto.Status; e.AppVersion = dto.AppVersion; e.IsTrusted = dto.IsTrusted; e.IsCurrentDevice = dto.IsCurrentDevice; e.CreatedAt = dto.CreatedAt; e.LastSeenAt = dto.LastSeenAt; e.RevokedAt = dto.RevokedAt; }
    internal static DeviceEntity ToEntity(this DeviceDto dto) { var e = new DeviceEntity(); e.UpdateFrom(dto); return e; }
    internal static SettingsDto ToDto(this SettingsEntity e) => new() { Id = e.SettingsId, UserId = e.UserId, DeltaTimeSeconds = e.RefreshTimeSeconds, SyncServerAddress = e.SyncServerAddress, SyncEmail = e.SyncEmail, SyncAuthToken = e.SyncAuthToken, SyncRemoteUserId = e.SyncRemoteUserId, SyncDeviceId = e.SyncDeviceId, SyncLastServerTimeUtc = e.SyncLastServerTimeUtc, ReportPath = e.ReportPath };
    internal static CategoryDto ToDto(this CategoryEntity e) => new() { CategoryId = e.CategoryId, Name = e.Name, Description = e.Description };
    internal static ApplicationDto ToDto(this ApplicationEntity e) => new() { Id = e.AppId, DeviceId = e.DeviceId, CategoryId = e.CategoryId, WindowTitle = e.Name, ClassName = e.Class, ProcessName = e.ProcessName, WindowId = e.WindowId };
    internal static ApplicationEntity ToEntity(this ApplicationDto dto) => new() { DeviceId = dto.DeviceId, CategoryId = dto.CategoryId, Name = dto.WindowTitle, Class = dto.ClassName, ProcessName = dto.ProcessName, WindowId = dto.WindowId };
    internal static SessionDto ToDto(this SessionEntity e) => new() { SessionId = e.SessionId, AppId = e.AppId, DeviceId = e.DeviceId, StartTime = e.StartTime, EndTime = e.EndTime };
    internal static BrowserActivityDto ToDto(this BrowserActivityEntity e) => new() { ActivityId = e.ActivityId, DeviceId = e.DeviceId, AppId = e.AppId, CategoryId = e.CategoryId, Url = e.Url };
    internal static ThresholdDto ToDto(this ThresholdEntity e) => new() { Id = e.ThresholdId, DeviceId = e.DeviceId, CategoryId = e.CategoryId ?? 0, AppId = e.AppId ?? 0, Active = e.IsActive, TargetType = e.TargetType, InterventionType = e.InterventionType, DurationType = e.DurationType, DailyLimitSec = e.DailyLimitSec, SessionLimitSec = e.SessionLimitSec, ActivatedAt = e.ActivatedAt };
    internal static ThresholdEntity ToEntity(this ThresholdDto dto) => new() { DeviceId = dto.DeviceId, CategoryId = dto.CategoryId == 0 ? null : dto.CategoryId, AppId = dto.AppId == 0 ? null : dto.AppId, IsActive = dto.Active, TargetType = dto.TargetType, InterventionType = dto.InterventionType, DurationType = dto.DurationType, DailyLimitSec = dto.DailyLimitSec, SessionLimitSec = dto.SessionLimitSec, ActivatedAt = dto.ActivatedAt };
    internal static InterventionDto ToDto(this InterventionEntity e) => new() { Id = e.InterventionId, ThresholdId = e.ThresholdId, Snoozed = e.Snoozed, TriggeredAt = e.TriggeredAt };
}

#endregion
