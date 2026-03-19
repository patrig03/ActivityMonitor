using Database.DTO;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Backend.Interventions.Models;

public class Threshold : INotifyPropertyChanged
{
    public const string CategoryTargetType = "Category";
    public const string AppTargetType = "App";
    public const string NotificationInterventionType = "Notification";
    public const string TypingLockInterventionType = "TypingLock";
    public const string TimedLockInterventionType = "TimedLock";
    public const string DailyLimitType = "Daily";
    public const string SessionLimitType = "Session";

    private int _id;
    private int _userId = 1;
    private int _categoryId;
    private int _appId;
    private bool _active = true;
    private string _targetType = CategoryTargetType;
    private string _interventionType = NotificationInterventionType;
    private string _limitType = DailyLimitType;
    private TimeSpan _sessionLimit = TimeSpan.FromMinutes(30);
    private TimeSpan _dailyLimit = TimeSpan.FromHours(1);

    public int Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public int UserId
    {
        get => _userId;
        set => SetField(ref _userId, value);
    }

    public int CategoryId
    {
        get => _categoryId;
        set => SetField(ref _categoryId, value);
    }

    public int AppId
    {
        get => _appId;
        set => SetField(ref _appId, value);
    }

    public bool Active
    {
        get => _active;
        set => SetField(ref _active, value);
    }

    public string TargetType
    {
        get => _targetType;
        set => SetField(ref _targetType, NormalizeTargetType(value));
    }

    public string InterventionType
    {
        get => _interventionType;
        set => SetField(ref _interventionType, NormalizeInterventionType(value));
    }

    public string LimitType
    {
        get => _limitType;
        set
        {
            if (!SetField(ref _limitType, NormalizeLimitType(value)))
            {
                return;
            }

            OnPropertyChanged(nameof(Limit));
        }
    }

    public TimeSpan SessionLimit
    {
        get => _sessionLimit;
        set
        {
            if (!SetField(ref _sessionLimit, value))
            {
                return;
            }

            if (LimitType == SessionLimitType)
            {
                OnPropertyChanged(nameof(Limit));
            }
        }
    }

    public TimeSpan DailyLimit
    {
        get => _dailyLimit;
        set
        {
            if (!SetField(ref _dailyLimit, value))
            {
                return;
            }

            if (LimitType == DailyLimitType)
            {
                OnPropertyChanged(nameof(Limit));
            }
        }
    }

    public TimeSpan Limit
    {
        get
        {
            switch (LimitType)
            {
                case DailyLimitType: return DailyLimit;
                case SessionLimitType: return SessionLimit;
                default: throw new ArgumentOutOfRangeException();
            }
        } 
        set
        {
            switch (LimitType)
            {
                case DailyLimitType: DailyLimit = value; break;
                case SessionLimitType: SessionLimit = value; break;
                default: throw new ArgumentOutOfRangeException();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    
    public ThresholdDto ToDto()
    {
        return new ThresholdDto
        {
            Id = Id,
            UserId = UserId,
            CategoryId = CategoryId,
            AppId = AppId,
            Active = Active,
            TargetType = TargetType,
            InterventionType = InterventionType,
            DurationType = LimitType,
            SessionLimitSec = (int)SessionLimit.TotalSeconds,
            DailyLimitSec = (int)DailyLimit.TotalSeconds,
        };
    }

    public static Threshold FromDto(ThresholdDto dto)
    {
        return new Threshold
        {
            Id = dto.Id,
            UserId = dto.UserId,
            CategoryId = dto.CategoryId,
            AppId = dto.AppId,
            Active = dto.Active,
            TargetType = NormalizeTargetType(dto.TargetType),
            InterventionType = NormalizeInterventionType(dto.InterventionType),
            LimitType = NormalizeLimitType(dto.DurationType),
            SessionLimit = TimeSpan.FromSeconds(dto.SessionLimitSec),
            DailyLimit = TimeSpan.FromSeconds(dto.DailyLimitSec),
        };
    }

    public Threshold Clone()
    {
        return new Threshold
        {
            Id = Id,
            UserId = UserId,
            CategoryId = CategoryId,
            AppId = AppId,
            Active = Active,
            TargetType = TargetType,
            InterventionType = InterventionType,
            LimitType = LimitType,
            SessionLimit = SessionLimit,
            DailyLimit = DailyLimit,
        };
    }

    private static string NormalizeTargetType(string? value)
    {
        return string.Equals(value, AppTargetType, StringComparison.OrdinalIgnoreCase)
            ? AppTargetType
            : CategoryTargetType;
    }

    private static string NormalizeInterventionType(string? value)
    {
        if (string.Equals(value, TypingLockInterventionType, StringComparison.OrdinalIgnoreCase))
        {
            return TypingLockInterventionType;
        }

        if (string.Equals(value, TimedLockInterventionType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "TimerLock", StringComparison.OrdinalIgnoreCase))
        {
            return TimedLockInterventionType;
        }

        return NotificationInterventionType;
    }

    private static string NormalizeLimitType(string? value)
    {
        if (string.Equals(value, SessionLimitType, StringComparison.OrdinalIgnoreCase) ||
            string.Equals(value, "This session", StringComparison.OrdinalIgnoreCase))
        {
            return SessionLimitType;
        }

        return DailyLimitType;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
