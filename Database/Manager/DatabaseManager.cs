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

    public void Dispose()
    {
    }

    public int InsertUser(UserDto user)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = new UserEntity
        {
            DisplayName = user.DisplayName,
            PinHash = user.PinHash,
            SyncEnabled = user.SyncEnabled,
            CreatedAt = user.CreatedAt
        };

        context.Users.Add(entity);
        context.SaveChanges();
        return entity.UserId;
    }

    public UserDto? GetUser(int userId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Users
            .AsNoTracking()
            .Where(entity => entity.UserId == userId)
            .AsEnumerable()
            .Select(ToDto)
            .SingleOrDefault();
    }

    public int InsertDevice(DeviceDto device)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = ToEntity(device);
        context.Devices.Add(entity);
        context.SaveChanges();
        return entity.DeviceId;
    }

    public int UpdateDevice(DeviceDto device)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Devices.SingleOrDefault(item => item.DeviceId == device.DeviceId);
        if (entity is null)
        {
            return 0;
        }

        UpdateEntity(entity, device);
        return context.SaveChanges();
    }

    public int UpsertDevice(DeviceDto device)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = device.DeviceId > 0
            ? context.Devices.SingleOrDefault(item => item.DeviceId == device.DeviceId)
            : context.Devices.SingleOrDefault(item =>
                item.UserId == device.UserId &&
                item.Fingerprint == device.Fingerprint);

        if (entity is null)
        {
            entity = ToEntity(device);
            context.Devices.Add(entity);
            context.SaveChanges();
            return entity.DeviceId;
        }

        UpdateEntity(entity, device);
        context.SaveChanges();
        return entity.DeviceId;
    }

    public IEnumerable<DeviceDto> GetDevicesForUser(int userId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Devices
            .AsNoTracking()
            .Where(entity => entity.UserId == userId)
            .OrderByDescending(entity => entity.IsCurrentDevice)
            .ThenByDescending(entity => entity.LastSeenAt)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public int InsertSettings(SettingsDto settings)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = new SettingsEntity
        {
            UserId = settings.UserId,
            RefreshTimeSeconds = settings.DeltaTimeSeconds,
            SyncServerAddress = settings.SyncServerAddress,
            SyncEmail = settings.SyncEmail,
            SyncAuthToken = settings.SyncAuthToken,
            SyncRemoteUserId = settings.SyncRemoteUserId,
            SyncDeviceId = settings.SyncDeviceId,
            SyncLastServerTimeUtc = settings.SyncLastServerTimeUtc
        };

        context.Settings.Add(entity);
        context.SaveChanges();
        return entity.SettingsId;
    }

    public int UpdateSettings(SettingsDto settings)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Settings.SingleOrDefault(item => item.SettingsId == settings.Id);
        if (entity is null)
        {
            return 0;
        }

        entity.UserId = settings.UserId;
        entity.RefreshTimeSeconds = settings.DeltaTimeSeconds;
        entity.SyncServerAddress = settings.SyncServerAddress;
        entity.SyncEmail = settings.SyncEmail;
        entity.SyncAuthToken = settings.SyncAuthToken;
        entity.SyncRemoteUserId = settings.SyncRemoteUserId;
        entity.SyncDeviceId = settings.SyncDeviceId;
        entity.SyncLastServerTimeUtc = settings.SyncLastServerTimeUtc;
        return context.SaveChanges();
    }

    public SettingsDto? GetSettings(int userId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Settings
            .AsNoTracking()
            .Where(entity => entity.UserId == userId)
            .AsEnumerable()
            .Select(ToDto)
            .SingleOrDefault();
    }

    public int InsertCategory(CategoryDto category)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = new CategoryEntity
        {
            Name = category.Name,
            Description = category.Description
        };

        context.Categories.Add(entity);
        context.SaveChanges();
        return entity.CategoryId;
    }

    public CategoryDto? GetCategory(int categoryId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories
            .AsNoTracking()
            .Where(entity => entity.CategoryId == categoryId)
            .AsEnumerable()
            .Select(ToDto)
            .SingleOrDefault();
    }

    public IEnumerable<CategoryDto> GetAllCategories()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Categories
            .AsNoTracking()
            .OrderBy(entity => entity.CategoryId)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public int DeleteCategory(int categoryId)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Categories.SingleOrDefault(item => item.CategoryId == categoryId);
        if (entity is null)
        {
            return 0;
        }

        context.Categories.Remove(entity);
        return context.SaveChanges();
    }

    public int InsertApplication(ApplicationDto app)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = ToEntity(app);
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
        using var context = _contextFactory.CreateDbContext();
        var entity = FindApplication(context, app);
        if (entity is null)
        {
            return null;
        }

        entity.Name = app.WindowTitle;
        entity.Class = app.ClassName;
        entity.ProcessName = app.ProcessName;
        entity.CategoryId = app.CategoryId;
        entity.PositionX = app.PositionX;
        entity.PositionY = app.PositionY;
        entity.Width = app.Width;
        entity.Height = app.Height;
        entity.WindowId = app.WindowId;

        context.SaveChanges();
        return entity.AppId;
    }

    public int UpdateApplicationCategory(int appId, int? categoryId)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Applications.SingleOrDefault(item => item.AppId == appId);
        if (entity is null)
        {
            return 0;
        }

        entity.CategoryId = categoryId;
        return context.SaveChanges();
    }

    public IEnumerable<int> InsertApplications(IEnumerable<ApplicationDto> apps)
    {
        using var context = _contextFactory.CreateDbContext();
        var entities = apps.Select(ToEntity).ToList();
        context.Applications.AddRange(entities);
        context.SaveChanges();
        return entities.Select(entity => entity.AppId).ToList();
    }

    public ApplicationDto? GetApplication(int appId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Applications
            .AsNoTracking()
            .Where(entity => entity.AppId == appId)
            .AsEnumerable()
            .Select(ToDto)
            .SingleOrDefault();
    }

    public IEnumerable<ApplicationDto> GetApplicationsByCategory(int categoryId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Applications
            .AsNoTracking()
            .Where(entity => entity.CategoryId == categoryId)
            .OrderBy(entity => entity.AppId)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public IEnumerable<ApplicationDto> GetAllApplications()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Applications
            .AsNoTracking()
            .OrderBy(entity => entity.AppId)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public int? IsInDb(ApplicationDto applicationDto)
    {
        using var context = _contextFactory.CreateDbContext();
        return FindApplication(context, applicationDto)?.AppId;
    }

    public int? IsInDb(SessionDto session)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Sessions
            .AsNoTracking()
            .Where(entity =>
                entity.AppId == session.AppId &&
                entity.UserId == session.UserId &&
                entity.StartTime == session.StartTime)
            .Select(entity => (int?)entity.SessionId)
            .SingleOrDefault();
    }

    public int InsertSession(SessionDto session)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = new SessionEntity
        {
            AppId = session.AppId,
            UserId = session.UserId,
            StartTime = session.StartTime,
            EndTime = session.EndTime
        };

        context.Sessions.Add(entity);
        context.SaveChanges();
        return entity.SessionId;
    }

    public int? UpdateSession(SessionDto session)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Sessions.SingleOrDefault(item =>
            item.AppId == session.AppId &&
            item.UserId == session.UserId &&
            item.StartTime == session.StartTime);

        if (entity is null)
        {
            return null;
        }

        entity.AppId = session.AppId;
        entity.UserId = session.UserId;
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
        using var context = _contextFactory.CreateDbContext();
        return context.Sessions
            .AsNoTracking()
            .Where(entity => entity.SessionId == sessionId)
            .AsEnumerable()
            .Select(ToDto)
            .SingleOrDefault();
    }

    public IEnumerable<SessionDto> GetSessionsForUser(int userId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Sessions
            .AsNoTracking()
            .Where(entity => entity.UserId == userId)
            .OrderBy(entity => entity.StartTime)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public IEnumerable<SessionDto> GetSessionsByCategory(int categoryId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Sessions
            .AsNoTracking()
            .Where(entity => entity.Application != null && entity.Application.CategoryId == categoryId)
            .OrderBy(entity => entity.StartTime)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public int GetSessionDurationForCategory(int categoryId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Sessions
            .AsNoTracking()
            .Where(entity => entity.Application != null && entity.Application.CategoryId == categoryId)
            .ToList()
            .Sum(entity =>
            {
                if (!entity.StartTime.HasValue || !entity.EndTime.HasValue)
                {
                    return 0;
                }

                return Math.Max(0, (int)(entity.EndTime.Value - entity.StartTime.Value).TotalSeconds);
            });
    }

    public void InsertBrowserActivity(BrowserActivityDto activity)
    {
        using var context = _contextFactory.CreateDbContext();
        context.BrowserActivities.Add(new BrowserActivityEntity
        {
            UserId = activity.UserId,
            AppId = activity.AppId,
            CategoryId = activity.CategoryId,
            Url = activity.Url
        });
        context.SaveChanges();
    }

    public IEnumerable<BrowserActivityDto> GetBrowserActivityForSession(int sessionId)
    {
        using var context = _contextFactory.CreateDbContext();
        var session = context.Sessions
            .AsNoTracking()
            .SingleOrDefault(entity => entity.SessionId == sessionId);

        if (session is null || !session.AppId.HasValue || !session.UserId.HasValue)
        {
            return [];
        }

        return context.BrowserActivities
            .AsNoTracking()
            .Where(entity => entity.AppId == session.AppId.Value && entity.UserId == session.UserId.Value)
            .OrderByDescending(entity => entity.ActivityId)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public IEnumerable<BrowserActivityDto> GetAllBrowserActivity()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.BrowserActivities
            .AsNoTracking()
            .OrderBy(entity => entity.ActivityId)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public int? IsInDb(BrowserActivityDto activity)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.BrowserActivities
            .AsNoTracking()
            .Where(entity =>
                entity.UserId == activity.UserId &&
                entity.AppId == activity.AppId &&
                entity.Url == activity.Url)
            .Select(entity => (int?)entity.ActivityId)
            .SingleOrDefault();
    }

    public int InsertThreshold(ThresholdDto threshold)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = ToEntity(threshold);
        context.Thresholds.Add(entity);
        context.SaveChanges();
        return entity.ThresholdId;
    }

    public ThresholdDto? GetThreshold(int userId, int categoryId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Thresholds
            .AsNoTracking()
            .Where(entity => entity.UserId == userId && entity.CategoryId == categoryId)
            .OrderBy(entity => entity.ThresholdId)
            .AsEnumerable()
            .Select(ToDto)
            .FirstOrDefault();
    }

    public IEnumerable<ThresholdDto?> GetAllThresholds()
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Thresholds
            .AsNoTracking()
            .OrderBy(entity => entity.ThresholdId)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    public void DeleteThreshold(ThresholdDto threshold)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Thresholds.SingleOrDefault(item => item.ThresholdId == threshold.Id);
        if (entity is null)
        {
            return;
        }

        context.Thresholds.Remove(entity);
        context.SaveChanges();
    }

    public int UpdateThreshold(ThresholdDto threshold)
    {
        using var context = _contextFactory.CreateDbContext();
        var entity = context.Thresholds.SingleOrDefault(item => item.ThresholdId == threshold.Id);
        if (entity is null)
        {
            return 0;
        }

        entity.UserId = threshold.UserId;
        entity.CategoryId = NullIfZero(threshold.CategoryId);
        entity.AppId = NullIfZero(threshold.AppId);
        entity.IsActive = threshold.Active;
        entity.TargetType = threshold.TargetType;
        entity.InterventionType = threshold.InterventionType;
        entity.DurationType = threshold.DurationType;
        entity.DailyLimitSec = threshold.DailyLimitSec;
        entity.SessionLimitSec = threshold.SessionLimitSec;

        return context.SaveChanges();
    }

    public int UpsertThreshold(ThresholdDto threshold)
    {
        return threshold.Id > 0 ? UpdateThreshold(threshold) : InsertThreshold(threshold);
    }

    public int InsertIntervention(InterventionDto intervention)
    {
        using var context = _contextFactory.CreateDbContext();
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

    public IEnumerable<InterventionDto> GetInterventionsForUser(int userId)
    {
        using var context = _contextFactory.CreateDbContext();
        return context.Interventions
            .AsNoTracking()
            .Where(entity => entity.Threshold != null && entity.Threshold.UserId == userId)
            .OrderByDescending(entity => entity.TriggeredAt)
            .AsEnumerable()
            .Select(ToDto)
            .ToList();
    }

    private static ApplicationEntity? FindApplication(ActivityMonitorDbContext context, ApplicationDto app)
    {
        return context.Applications.SingleOrDefault(entity =>
            entity.Name == app.WindowTitle &&
            entity.Class == app.ClassName &&
            entity.ProcessName == app.ProcessName);
    }

    private static DeviceEntity ToEntity(DeviceDto dto)
    {
        var entity = new DeviceEntity();
        UpdateEntity(entity, dto);
        return entity;
    }

    private static void UpdateEntity(DeviceEntity entity, DeviceDto dto)
    {
        entity.UserId = dto.UserId;
        entity.Name = dto.Name;
        entity.DeviceType = dto.DeviceType;
        entity.Platform = dto.Platform;
        entity.Fingerprint = dto.Fingerprint;
        entity.Status = dto.Status;
        entity.AppVersion = dto.AppVersion;
        entity.IsTrusted = dto.IsTrusted;
        entity.IsCurrentDevice = dto.IsCurrentDevice;
        entity.CreatedAt = dto.CreatedAt;
        entity.LastSeenAt = dto.LastSeenAt;
        entity.RevokedAt = dto.RevokedAt;
    }

    private static ApplicationEntity ToEntity(ApplicationDto dto)
    {
        return new ApplicationEntity
        {
            CategoryId = dto.CategoryId,
            Name = dto.WindowTitle,
            Class = dto.ClassName,
            ProcessName = dto.ProcessName,
            PositionX = dto.PositionX,
            PositionY = dto.PositionY,
            Width = dto.Width,
            Height = dto.Height,
            WindowId = dto.WindowId
        };
    }

    private static ThresholdEntity ToEntity(ThresholdDto dto)
    {
        return new ThresholdEntity
        {
            UserId = dto.UserId,
            CategoryId = NullIfZero(dto.CategoryId),
            AppId = NullIfZero(dto.AppId),
            IsActive = dto.Active,
            TargetType = dto.TargetType,
            InterventionType = dto.InterventionType,
            DurationType = dto.DurationType,
            DailyLimitSec = dto.DailyLimitSec,
            SessionLimitSec = dto.SessionLimitSec
        };
    }

    private static int? NullIfZero(int value) => value == 0 ? null : value;

    private static UserDto ToDto(UserEntity entity)
    {
        return new UserDto
        {
            UserId = entity.UserId,
            DisplayName = entity.DisplayName,
            PinHash = entity.PinHash,
            SyncEnabled = entity.SyncEnabled,
            CreatedAt = entity.CreatedAt
        };
    }

    private static DeviceDto ToDto(DeviceEntity entity)
    {
        return new DeviceDto
        {
            DeviceId = entity.DeviceId,
            UserId = entity.UserId,
            Name = entity.Name,
            DeviceType = entity.DeviceType,
            Platform = entity.Platform,
            Fingerprint = entity.Fingerprint,
            Status = entity.Status,
            AppVersion = entity.AppVersion,
            IsTrusted = entity.IsTrusted,
            IsCurrentDevice = entity.IsCurrentDevice,
            CreatedAt = entity.CreatedAt,
            LastSeenAt = entity.LastSeenAt,
            RevokedAt = entity.RevokedAt
        };
    }

    private static SettingsDto ToDto(SettingsEntity entity)
    {
        return new SettingsDto
        {
            Id = entity.SettingsId,
            UserId = entity.UserId,
            DeltaTimeSeconds = entity.RefreshTimeSeconds,
            SyncServerAddress = entity.SyncServerAddress,
            SyncEmail = entity.SyncEmail,
            SyncAuthToken = entity.SyncAuthToken,
            SyncRemoteUserId = entity.SyncRemoteUserId,
            SyncDeviceId = entity.SyncDeviceId,
            SyncLastServerTimeUtc = entity.SyncLastServerTimeUtc
        };
    }

    private static CategoryDto ToDto(CategoryEntity entity)
    {
        return new CategoryDto
        {
            CategoryId = entity.CategoryId,
            Name = entity.Name,
            Description = entity.Description
        };
    }

    private static ApplicationDto ToDto(ApplicationEntity entity)
    {
        return new ApplicationDto
        {
            Id = entity.AppId,
            CategoryId = entity.CategoryId,
            WindowTitle = entity.Name,
            ClassName = entity.Class,
            ProcessName = entity.ProcessName,
            PositionX = entity.PositionX,
            PositionY = entity.PositionY,
            Width = entity.Width,
            Height = entity.Height,
            WindowId = entity.WindowId
        };
    }

    private static SessionDto ToDto(SessionEntity entity)
    {
        return new SessionDto
        {
            SessionId = entity.SessionId,
            AppId = entity.AppId,
            UserId = entity.UserId,
            StartTime = entity.StartTime,
            EndTime = entity.EndTime
        };
    }

    private static BrowserActivityDto ToDto(BrowserActivityEntity entity)
    {
        return new BrowserActivityDto
        {
            ActivityId = entity.ActivityId,
            UserId = entity.UserId,
            AppId = entity.AppId,
            CategoryId = entity.CategoryId,
            Url = entity.Url
        };
    }

    private static ThresholdDto ToDto(ThresholdEntity entity)
    {
        return new ThresholdDto
        {
            Id = entity.ThresholdId,
            UserId = entity.UserId,
            CategoryId = entity.CategoryId ?? 0,
            AppId = entity.AppId ?? 0,
            Active = entity.IsActive,
            TargetType = entity.TargetType,
            InterventionType = entity.InterventionType,
            DurationType = entity.DurationType,
            DailyLimitSec = entity.DailyLimitSec,
            SessionLimitSec = entity.SessionLimitSec
        };
    }

    private static InterventionDto ToDto(InterventionEntity entity)
    {
        return new InterventionDto
        {
            Id = entity.InterventionId,
            ThresholdId = entity.ThresholdId,
            Snoozed = entity.Snoozed,
            TriggeredAt = entity.TriggeredAt
        };
    }
}
