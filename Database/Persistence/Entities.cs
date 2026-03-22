namespace Database.Persistence;

public sealed class UserEntity
{
    public int UserId { get; set; }
    public string? DisplayName { get; set; }
    public string? PinHash { get; set; }
    public bool SyncEnabled { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<SettingsEntity> Settings { get; set; } = new List<SettingsEntity>();
    public ICollection<SessionEntity> Sessions { get; set; } = new List<SessionEntity>();
    public ICollection<BrowserActivityEntity> BrowserActivities { get; set; } = new List<BrowserActivityEntity>();
    public ICollection<ThresholdEntity> Thresholds { get; set; } = new List<ThresholdEntity>();
}

public sealed class SettingsEntity
{
    public int SettingsId { get; set; }
    public int UserId { get; set; }
    public int RefreshTimeSeconds { get; set; }

    public UserEntity? User { get; set; }
}

public sealed class CategoryEntity
{
    public int CategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    public ICollection<ApplicationEntity> Applications { get; set; } = new List<ApplicationEntity>();
    public ICollection<ThresholdEntity> Thresholds { get; set; } = new List<ThresholdEntity>();
}

public sealed class ApplicationEntity
{
    public int AppId { get; set; }
    public int? CategoryId { get; set; }
    public string? Name { get; set; }
    public string? Class { get; set; }
    public string? ProcessName { get; set; }
    public int? PositionX { get; set; }
    public int? PositionY { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? WindowId { get; set; }

    public CategoryEntity? Category { get; set; }
    public ICollection<SessionEntity> Sessions { get; set; } = new List<SessionEntity>();
    public ICollection<BrowserActivityEntity> BrowserActivities { get; set; } = new List<BrowserActivityEntity>();
    public ICollection<ThresholdEntity> Thresholds { get; set; } = new List<ThresholdEntity>();
}

public sealed class SessionEntity
{
    public int SessionId { get; set; }
    public int? AppId { get; set; }
    public int? UserId { get; set; }
    public DateTime? StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    public ApplicationEntity? Application { get; set; }
    public UserEntity? User { get; set; }
}

public sealed class BrowserActivityEntity
{
    public int ActivityId { get; set; }
    public int UserId { get; set; }
    public int AppId { get; set; }
    public string? Url { get; set; }

    public UserEntity? User { get; set; }
    public ApplicationEntity? Application { get; set; }
}

public sealed class ThresholdEntity
{
    public int ThresholdId { get; set; }
    public int UserId { get; set; }
    public int? CategoryId { get; set; }
    public int? AppId { get; set; }
    public bool IsActive { get; set; }
    public string TargetType { get; set; } = "Category";
    public string InterventionType { get; set; } = "Notification";
    public string DurationType { get; set; } = "Daily";
    public int DailyLimitSec { get; set; }
    public int SessionLimitSec { get; set; }

    public UserEntity? User { get; set; }
    public CategoryEntity? Category { get; set; }
    public ApplicationEntity? Application { get; set; }
    public ICollection<InterventionEntity> Interventions { get; set; } = new List<InterventionEntity>();
}

public sealed class InterventionEntity
{
    public int InterventionId { get; set; }
    public int ThresholdId { get; set; }
    public DateTime TriggeredAt { get; set; }
    public bool Snoozed { get; set; }

    public ThresholdEntity? Threshold { get; set; }
}
