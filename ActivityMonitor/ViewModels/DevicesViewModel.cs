using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Input;
using Backend.Models;
using Backend.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using Database.DTO;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public sealed class DevicesViewModel : ObservableObject
{
    private const int DefaultUserId = 1;

    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private readonly string _currentFingerprint = BuildCurrentFingerprint();
    private readonly ServerSync _serverSync = new();

    private int _selectedDeviceId;
    private string _pageSubtitle = "Se incarca inventarul dispozitivelor...";
    private string _deviceStatus = "Sincronizare in curs";
    private string _lastRefreshLabel = "Actualizare in curs";
    private string _accountLabel = "Cont local";
    private string _currentDeviceLabel = "--";
    private string _currentDeviceDetail = "Detectam dispozitivul curent.";
    private string _totalDevices = "0";
    private string _activeDevices = "0";
    private string _trustedDevices = "0";
    private string _revokedDevices = "0";
    private string _selectedDeviceName = string.Empty;
    private string _selectedDeviceType = DeviceTypeOptions[0];
    private string _selectedDevicePlatform = string.Empty;
    private string _selectedDeviceVersion = string.Empty;
    private string _selectedDeviceFingerprint = string.Empty;
    private string _selectedDeviceState = "Selecteaza un dispozitiv";
    private string _selectedDeviceTimeline = "Detaliile de activitate vor aparea aici.";
    private string _selectedDeviceTrust = "Alege un dispozitiv pentru editare.";
    private bool _hasSelectedDevice;
    private string _newDeviceName = string.Empty;
    private string _newDeviceType = DeviceTypeOptions[0];
    private string _newDevicePlatform = string.Empty;
    private string _newDeviceVersion = string.Empty;
    private string _syncServerLabel = "Server sincronizare neconfigurat";
    private string _lastServerSyncLabel = "Nicio sincronizare remota";

    public DevicesViewModel()
    {
        RefreshCommand = new RelayCommand(_ => Load());
        RegisterCurrentDeviceCommand = new RelayCommand(_ => RegisterCurrentDevice());
        SyncWithServerCommand = new RelayCommand(_ => SyncWithServerAsync());
        AddDeviceCommand = new RelayCommand(_ => AddDevice());
        SelectDeviceCommand = new RelayCommand(SelectDevice);
        SaveSelectedDeviceCommand = new RelayCommand(_ => SaveSelectedDevice());
        ToggleTrustedCommand = new RelayCommand(ToggleTrusted);
        ToggleDeviceStatusCommand = new RelayCommand(ToggleDeviceStatus);
        ClearSelectionCommand = new RelayCommand(_ => ClearSelection());

        var user = _db.GetUser(DefaultUserId);
        AccountLabel = string.IsNullOrWhiteSpace(user?.DisplayName) ? "Cont local" : user!.DisplayName!;
        NewDevicePlatform = DetectPlatformLabel();

        Load();
    }

    public static IReadOnlyList<string> DeviceTypeOptions { get; } =
        ["Desktop", "Laptop", "Telefon", "Tableta", "Browser", "TV"];

    public IReadOnlyList<string> AvailableDeviceTypes => DeviceTypeOptions;

    public ObservableCollection<AccountDeviceRow> Devices { get; } = [];

    public ICommand RefreshCommand { get; }

    public ICommand RegisterCurrentDeviceCommand { get; }

    public ICommand SyncWithServerCommand { get; }

    public ICommand AddDeviceCommand { get; }

    public ICommand SelectDeviceCommand { get; }

    public ICommand SaveSelectedDeviceCommand { get; }

    public ICommand ToggleTrustedCommand { get; }

    public ICommand ToggleDeviceStatusCommand { get; }

    public ICommand ClearSelectionCommand { get; }

    public string PageSubtitle
    {
        get => _pageSubtitle;
        set => SetProperty(ref _pageSubtitle, value);
    }

    public string DeviceStatus
    {
        get => _deviceStatus;
        set => SetProperty(ref _deviceStatus, value);
    }

    public string LastRefreshLabel
    {
        get => _lastRefreshLabel;
        set => SetProperty(ref _lastRefreshLabel, value);
    }

    public string AccountLabel
    {
        get => _accountLabel;
        set => SetProperty(ref _accountLabel, value);
    }

    public string CurrentDeviceLabel
    {
        get => _currentDeviceLabel;
        set => SetProperty(ref _currentDeviceLabel, value);
    }

    public string CurrentDeviceDetail
    {
        get => _currentDeviceDetail;
        set => SetProperty(ref _currentDeviceDetail, value);
    }

    public string TotalDevices
    {
        get => _totalDevices;
        set => SetProperty(ref _totalDevices, value);
    }

    public string ActiveDevices
    {
        get => _activeDevices;
        set => SetProperty(ref _activeDevices, value);
    }

    public string TrustedDevices
    {
        get => _trustedDevices;
        set => SetProperty(ref _trustedDevices, value);
    }

    public string RevokedDevices
    {
        get => _revokedDevices;
        set => SetProperty(ref _revokedDevices, value);
    }

    public string SelectedDeviceName
    {
        get => _selectedDeviceName;
        set => SetProperty(ref _selectedDeviceName, value);
    }

    public string SelectedDeviceType
    {
        get => _selectedDeviceType;
        set => SetProperty(ref _selectedDeviceType, value);
    }

    public string SelectedDevicePlatform
    {
        get => _selectedDevicePlatform;
        set => SetProperty(ref _selectedDevicePlatform, value);
    }

    public string SelectedDeviceVersion
    {
        get => _selectedDeviceVersion;
        set => SetProperty(ref _selectedDeviceVersion, value);
    }

    public string SelectedDeviceFingerprint
    {
        get => _selectedDeviceFingerprint;
        set => SetProperty(ref _selectedDeviceFingerprint, value);
    }

    public string SelectedDeviceState
    {
        get => _selectedDeviceState;
        set => SetProperty(ref _selectedDeviceState, value);
    }

    public string SelectedDeviceTimeline
    {
        get => _selectedDeviceTimeline;
        set => SetProperty(ref _selectedDeviceTimeline, value);
    }

    public string SelectedDeviceTrust
    {
        get => _selectedDeviceTrust;
        set => SetProperty(ref _selectedDeviceTrust, value);
    }

    public bool HasSelectedDevice
    {
        get => _hasSelectedDevice;
        set
        {
            if (!SetProperty(ref _hasSelectedDevice, value))
            {
                return;
            }

            OnPropertyChanged(nameof(NoSelectedDevice));
            OnPropertyChanged(nameof(SelectedTrustActionLabel));
            OnPropertyChanged(nameof(SelectedStatusActionLabel));
        }
    }

    public bool NoSelectedDevice => !HasSelectedDevice;

    public string NewDeviceName
    {
        get => _newDeviceName;
        set => SetProperty(ref _newDeviceName, value);
    }

    public string NewDeviceType
    {
        get => _newDeviceType;
        set => SetProperty(ref _newDeviceType, value);
    }

    public string NewDevicePlatform
    {
        get => _newDevicePlatform;
        set => SetProperty(ref _newDevicePlatform, value);
    }

    public string NewDeviceVersion
    {
        get => _newDeviceVersion;
        set => SetProperty(ref _newDeviceVersion, value);
    }

    public string SelectedTrustActionLabel
    {
        get
        {
            var device = GetSelectedRow();
            return device == null
                ? "Marcheaza ca sigur"
                : device.IsTrusted ? "Scoate din lista de incredere" : "Marcheaza ca sigur";
        }
    }

    public string SelectedStatusActionLabel
    {
        get
        {
            var device = GetSelectedRow();
            return device == null
                ? "Revoca"
                : device.IsActive ? "Revoca accesul" : "Reactiveaza accesul";
        }
    }

    public string SyncServerLabel
    {
        get => _syncServerLabel;
        set => SetProperty(ref _syncServerLabel, value);
    }

    public string LastServerSyncLabel
    {
        get => _lastServerSyncLabel;
        set => SetProperty(ref _lastServerSyncLabel, value);
    }

    private void Load(int? preferredSelectionId = null)
    {
        RegisterCurrentDevice();
        RefreshSyncServerStatus();

        var devices = _db.GetDevicesForUser(DefaultUserId).ToList();

        Devices.Clear();
        foreach (var device in devices.Select(ToRow))
        {
            Devices.Add(device);
        }

        var activeCount = Devices.Count(device => device.IsActive);
        var trustedCount = Devices.Count(device => device.IsTrusted);
        var revokedCount = Devices.Count(device => !device.IsActive);
        var currentDevice = Devices.FirstOrDefault(device => device.IsCurrentDevice);
        var newest = Devices.FirstOrDefault();

        TotalDevices = Devices.Count.ToString();
        ActiveDevices = activeCount.ToString();
        TrustedDevices = trustedCount.ToString();
        RevokedDevices = revokedCount.ToString();
        PageSubtitle =
            $"Ai {Devices.Count} dispozitive inregistrate pentru contul local, dintre care {activeCount} active si {trustedCount} marcate ca sigure.";
        DeviceStatus = revokedCount == 0
            ? "Toate dispozitivele active sunt in stare buna"
            : $"{revokedCount} dispozitive au accesul revocat";
        LastRefreshLabel = newest == null
            ? "Fara activitate recenta"
            : $"Actualizat {FormatRelativeTime(newest.LastSeenAt)}";
        CurrentDeviceLabel = currentDevice?.Name ?? DetectCurrentDeviceName();
        CurrentDeviceDetail = currentDevice == null
            ? DetectPlatformLabel()
            : $"{currentDevice.DeviceType} · {currentDevice.Platform} · vazut {currentDevice.LastSeenLabel}";

        var selectionId = preferredSelectionId ?? _selectedDeviceId;
        var selectedRow = Devices.FirstOrDefault(device => device.DeviceId == selectionId)
                          ?? currentDevice;

        if (selectedRow == null)
        {
            ClearSelection();
            return;
        }

        PopulateSelection(selectedRow);
    }

    private void RegisterCurrentDevice()
    {
        var now = DateTime.UtcNow;
        var devices = _db.GetDevicesForUser(DefaultUserId).ToList();

        foreach (var staleCurrent in devices.Where(device => device.IsCurrentDevice && device.Fingerprint != _currentFingerprint))
        {
            staleCurrent.IsCurrentDevice = false;
            _db.UpdateDevice(staleCurrent);
        }

        var current = devices.FirstOrDefault(device => device.Fingerprint == _currentFingerprint);
        var currentName = DetectCurrentDeviceName();
        var currentPlatform = DetectPlatformLabel();
        var version = DetectAppVersion();

        if (current == null)
        {
            _db.InsertDevice(new DeviceDto
            {
                UserId = DefaultUserId,
                Name = currentName,
                DeviceType = DetectCurrentDeviceType(),
                Platform = currentPlatform,
                Fingerprint = _currentFingerprint,
                Status = "Active",
                AppVersion = version,
                IsTrusted = true,
                IsCurrentDevice = true,
                CreatedAt = now,
                LastSeenAt = now
            });
            return;
        }

        current.Name = string.IsNullOrWhiteSpace(current.Name) ? currentName : current.Name;
        current.DeviceType = string.IsNullOrWhiteSpace(current.DeviceType) ? DetectCurrentDeviceType() : current.DeviceType;
        current.Platform = currentPlatform;
        current.Status = "Active";
        current.AppVersion = version;
        current.IsCurrentDevice = true;
        current.LastSeenAt = now;
        current.RevokedAt = null;
        _db.UpdateDevice(current);
    }

    private async void SyncWithServerAsync()
    {
        RegisterCurrentDevice();
        var settings = EnsureSettingsRecord();

        if (!TryValidateSyncConfiguration(settings, out var normalizedAddress, out var bearerToken, out var error))
        {
            DeviceStatus = error;
            RefreshSyncServerStatus();
            return;
        }

        var devices = _db.GetDevicesForUser(DefaultUserId).ToList();
        var currentDevice = devices.FirstOrDefault(device => device.Fingerprint == _currentFingerprint);
        if (currentDevice == null)
        {
            DeviceStatus = "Dispozitivul curent nu a putut fi inregistrat local inainte de sincronizare.";
            return;
        }

        DeviceStatus = $"Se inregistreaza dispozitivul curent pe {normalizedAddress}...";
        var deviceRegistration = await _serverSync.EnsureDeviceAsync(
            normalizedAddress,
            bearerToken,
            settings.SyncDeviceId,
            currentDevice.Name);

        if (!deviceRegistration.Success || string.IsNullOrWhiteSpace(deviceRegistration.DeviceId))
        {
            DeviceStatus = deviceRegistration.Message;
            LastServerSyncLabel = "Ultima sincronizare a esuat";
            return;
        }

        settings.SyncDeviceId = deviceRegistration.DeviceId;
        SaveSettings(settings);

        DeviceStatus = $"Se sincronizeaza datele locale cu {normalizedAddress}...";
        var payload = BuildSyncRequest(settings, deviceRegistration.DeviceId);
        var result = await _serverSync.SyncDataAsync(normalizedAddress, bearerToken, payload);
        if (!result.Success)
        {
            DeviceStatus = result.Message;
            LastServerSyncLabel = "Ultima sincronizare a esuat";
            RefreshSyncServerStatus();
            return;
        }

        MergeSyncResponse(result.Data);

        settings.SyncLastServerTimeUtc = result.Data.ServerTime?.ToUniversalTime() ?? DateTime.UtcNow;
        SaveSettings(settings);

        Load(currentDevice.DeviceId);
        LastServerSyncLabel = FormatLastSyncLabel(settings.SyncLastServerTimeUtc);
        DeviceStatus = BuildSyncSummary(result.Data, deviceRegistration.Created, result.Message);
    }

    private void AddDevice()
    {
        if (string.IsNullOrWhiteSpace(NewDeviceName) || string.IsNullOrWhiteSpace(NewDevicePlatform))
        {
            DeviceStatus = "Completeaza numele si platforma pentru dispozitivul nou.";
            return;
        }

        var now = DateTime.UtcNow;
        var deviceId = _db.InsertDevice(new DeviceDto
        {
            UserId = DefaultUserId,
            Name = NewDeviceName.Trim(),
            DeviceType = NewDeviceType,
            Platform = NewDevicePlatform.Trim(),
            Fingerprint = $"manual:{Guid.NewGuid():N}",
            Status = "Active",
            AppVersion = NormalizeOptionalValue(NewDeviceVersion),
            IsTrusted = false,
            IsCurrentDevice = false,
            CreatedAt = now,
            LastSeenAt = now
        });

        DeviceStatus = $"Dispozitivul \"{NewDeviceName.Trim()}\" a fost adaugat.";
        NewDeviceName = string.Empty;
        NewDeviceType = DeviceTypeOptions[0];
        NewDevicePlatform = DetectPlatformLabel();
        NewDeviceVersion = string.Empty;

        Load(deviceId);
    }

    private void SaveSelectedDevice()
    {
        var device = GetSelectedDto();
        if (device == null)
        {
            DeviceStatus = "Selecteaza un dispozitiv inainte sa salvezi.";
            return;
        }

        if (string.IsNullOrWhiteSpace(SelectedDeviceName) || string.IsNullOrWhiteSpace(SelectedDevicePlatform))
        {
            DeviceStatus = "Numele si platforma dispozitivului sunt obligatorii.";
            return;
        }

        device.Name = SelectedDeviceName.Trim();
        device.DeviceType = SelectedDeviceType;
        device.Platform = SelectedDevicePlatform.Trim();
        device.AppVersion = NormalizeOptionalValue(SelectedDeviceVersion);
        _db.UpdateDevice(device);

        DeviceStatus = $"Modificarile pentru \"{device.Name}\" au fost salvate.";
        Load(device.DeviceId);
    }

    private void ToggleTrusted(object? parameter)
    {
        var device = ResolveDevice(parameter);
        if (device == null)
        {
            DeviceStatus = "Nu exista un dispozitiv selectat pentru schimbarea nivelului de incredere.";
            return;
        }

        device.IsTrusted = !device.IsTrusted;
        _db.UpdateDevice(device);

        DeviceStatus = device.IsTrusted
            ? $"\"{device.Name}\" este acum marcat ca sigur."
            : $"\"{device.Name}\" necesita reverificare.";
        Load(device.DeviceId);
    }

    private void ToggleDeviceStatus(object? parameter)
    {
        var device = ResolveDevice(parameter);
        if (device == null)
        {
            DeviceStatus = "Nu exista un dispozitiv selectat pentru actualizarea accesului.";
            return;
        }

        if (device.Fingerprint == _currentFingerprint &&
            string.Equals(device.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            DeviceStatus = "Dispozitivul curent nu poate fi revocat din sesiunea activa.";
            Load(device.DeviceId);
            return;
        }

        var activate = !string.Equals(device.Status, "Active", StringComparison.OrdinalIgnoreCase);
        device.Status = activate ? "Active" : "Revoked";
        device.RevokedAt = activate ? null : DateTime.UtcNow;
        device.LastSeenAt = activate ? DateTime.UtcNow : device.LastSeenAt;
        device.IsCurrentDevice = activate && device.Fingerprint == _currentFingerprint;
        _db.UpdateDevice(device);

        DeviceStatus = activate
            ? $"Accesul pentru \"{device.Name}\" a fost reactivat."
            : $"Accesul pentru \"{device.Name}\" a fost revocat.";
        Load(device.DeviceId);
    }

    private void SelectDevice(object? parameter)
    {
        if (parameter is not AccountDeviceRow row)
        {
            return;
        }

        PopulateSelection(row);
    }

    private void PopulateSelection(AccountDeviceRow row)
    {
        _selectedDeviceId = row.DeviceId;
        SelectedDeviceName = row.Name;
        SelectedDeviceType = row.DeviceType;
        SelectedDevicePlatform = row.Platform;
        SelectedDeviceVersion = row.AppVersion;
        SelectedDeviceFingerprint = row.Fingerprint;
        SelectedDeviceState = row.StatusSummary;
        SelectedDeviceTimeline = $"{row.ActivitySummary} · inregistrat {row.CreatedLabel}";
        SelectedDeviceTrust = row.TrustSummary;
        HasSelectedDevice = true;
        OnPropertyChanged(nameof(SelectedTrustActionLabel));
        OnPropertyChanged(nameof(SelectedStatusActionLabel));
    }

    private void ClearSelection()
    {
        _selectedDeviceId = 0;
        SelectedDeviceName = string.Empty;
        SelectedDeviceType = DeviceTypeOptions[0];
        SelectedDevicePlatform = string.Empty;
        SelectedDeviceVersion = string.Empty;
        SelectedDeviceFingerprint = string.Empty;
        SelectedDeviceState = "Selecteaza un dispozitiv";
        SelectedDeviceTimeline = "Alege din lista din stanga pentru a edita numele, platforma si increderea.";
        SelectedDeviceTrust = "Nu este selectat niciun dispozitiv.";
        HasSelectedDevice = false;
        OnPropertyChanged(nameof(SelectedTrustActionLabel));
        OnPropertyChanged(nameof(SelectedStatusActionLabel));
    }

    private DeviceDto? ResolveDevice(object? parameter)
    {
        if (parameter is AccountDeviceRow row)
        {
            return _db.GetDevicesForUser(DefaultUserId).SingleOrDefault(device => device.DeviceId == row.DeviceId);
        }

        return GetSelectedDto();
    }

    private DeviceDto? GetSelectedDto()
    {
        return _selectedDeviceId == 0
            ? null
            : _db.GetDevicesForUser(DefaultUserId).SingleOrDefault(device => device.DeviceId == _selectedDeviceId);
    }

    private AccountDeviceRow? GetSelectedRow()
    {
        return _selectedDeviceId == 0
            ? null
            : Devices.FirstOrDefault(device => device.DeviceId == _selectedDeviceId);
    }

    private static AccountDeviceRow ToRow(DeviceDto device)
    {
        return new AccountDeviceRow
        {
            DeviceId = device.DeviceId,
            Name = device.Name,
            DeviceType = device.DeviceType,
            Platform = device.Platform,
            Fingerprint = device.Fingerprint,
            Status = device.Status,
            AppVersion = device.AppVersion ?? string.Empty,
            IsTrusted = device.IsTrusted,
            IsCurrentDevice = device.IsCurrentDevice,
            CreatedAt = device.CreatedAt,
            LastSeenAt = device.LastSeenAt,
            RevokedAt = device.RevokedAt
        };
    }

    private static string DetectCurrentDeviceName()
    {
        return string.IsNullOrWhiteSpace(Environment.MachineName) ? "Desktop local" : Environment.MachineName;
    }

    private static string DetectCurrentDeviceType()
    {
        return OperatingSystem.IsAndroid() ? "Telefon" : "Desktop";
    }

    private static string DetectPlatformLabel()
    {
        if (OperatingSystem.IsWindows())
        {
            return "Windows";
        }

        if (OperatingSystem.IsLinux())
        {
            return "Linux";
        }

        if (OperatingSystem.IsMacOS())
        {
            return "macOS";
        }

        if (OperatingSystem.IsAndroid())
        {
            return "Android";
        }

        return RuntimeInformation.OSDescription.Trim();
    }

    private static string DetectAppVersion()
    {
        var version = typeof(DevicesViewModel).Assembly.GetName().Version;
        return version == null ? "dev-build" : $"v{version.Major}.{version.Minor}.{version.Build}";
    }

    private void RefreshSyncServerStatus()
    {
        var settings = _db.GetSettings(DefaultUserId);
        var configuredAddress = settings?.SyncServerAddress;

        if (string.IsNullOrWhiteSpace(configuredAddress))
        {
            SyncServerLabel = "Server sincronizare neconfigurat";
            LastServerSyncLabel = "Nicio sincronizare remota";
            return;
        }

        if (!ServerSync.TryNormalizeServerAddress(configuredAddress, out var normalizedAddress, out _))
        {
            SyncServerLabel = $"Server sincronizare invalid: {configuredAddress}";
            LastServerSyncLabel = "Configuratie invalida";
            return;
        }

        var authState = string.IsNullOrWhiteSpace(settings?.SyncAuthToken)
            ? "neautentificat"
            : $"autentificat ca {settings?.SyncEmail ?? "cont sincronizat"}";
        var deviceState = string.IsNullOrWhiteSpace(settings?.SyncDeviceId)
            ? "device server lipsa"
            : $"device {settings!.SyncDeviceId![..Math.Min(8, settings.SyncDeviceId.Length)]}";

        SyncServerLabel = $"{ServerSync.BuildSyncEndpointPreview(normalizedAddress)} · {authState} · {deviceState}";
        LastServerSyncLabel = FormatLastSyncLabel(settings?.SyncLastServerTimeUtc);
    }

    private SettingsDto EnsureSettingsRecord()
    {
        return _db.GetSettings(DefaultUserId) ?? new SettingsDto
        {
            UserId = DefaultUserId,
            DeltaTimeSeconds = 10
        };
    }

    private bool TryValidateSyncConfiguration(
        SettingsDto settings,
        out string normalizedAddress,
        out string bearerToken,
        out string error)
    {
        normalizedAddress = string.Empty;
        bearerToken = string.Empty;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(settings.SyncServerAddress))
        {
            error = "Configureaza mai intai serverul de sincronizare din pagina Setari.";
            return false;
        }

        if (!ServerSync.TryNormalizeServerAddress(settings.SyncServerAddress, out normalizedAddress, out error))
        {
            return false;
        }

        bearerToken = settings.SyncAuthToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            error = "Autentifica-te in pagina Setari inainte sa pornesti sincronizarea.";
            return false;
        }

        return true;
    }

    private SyncRequest BuildSyncRequest(SettingsDto settings, string deviceId)
    {
        var categories = _db.GetAllCategories().ToList();
        var applications = _db.GetAllApplications().ToList();
        var sessions = _db.GetSessionsForUser(DefaultUserId).ToList();
        var activities = _db.GetAllBrowserActivity()
            .Where(activity => activity.UserId == DefaultUserId)
            .ToList();
        var thresholds = _db.GetAllThresholds()
            .Where(threshold => threshold != null)
            .Select(threshold => threshold!)
            .ToList();

        var categoryIds = categories.ToDictionary(category => category.CategoryId, GetCategorySyncId);
        var applicationIds = applications
            .Where(app => app.Id.HasValue)
            .ToDictionary(app => app.Id!.Value, GetApplicationSyncId);

        return new SyncRequest
        {
            LastSyncAt = settings.SyncLastServerTimeUtc ?? DateTime.UnixEpoch,
            DeviceId = deviceId,
            Categories = categories
                .Select(category => new SyncCategoryRecord
                {
                    Id = GetCategorySyncId(category),
                    Name = category.Name,
                    Description = NormalizeOptionalValue(category.Description)
                })
                .ToList(),
            Applications = applications
                .Select(app => new SyncApplicationRecord
                {
                    Id = GetApplicationSyncId(app),
                    CategoryId = app.CategoryId.HasValue && app.CategoryId.Value > 0 && categoryIds.TryGetValue(app.CategoryId.Value, out var categoryId)
                        ? categoryId
                        : null,
                    WindowTitle = NormalizeOptionalValue(app.WindowTitle),
                    ClassName = NormalizeOptionalValue(app.ClassName),
                    ProcessName = NormalizeOptionalValue(app.ProcessName),
                    PositionX = app.PositionX,
                    PositionY = app.PositionY,
                    Width = app.Width,
                    Height = app.Height,
                    WindowId = app.WindowId
                })
                .ToList(),
            Sessions = sessions
                .Where(session => session.StartTime.HasValue && session.AppId.HasValue && applicationIds.ContainsKey(session.AppId.Value))
                .Select(session => new SyncSessionRecord
                {
                    Id = GetSessionSyncId(session),
                    DeviceId = deviceId,
                    ApplicationId = applicationIds[session.AppId!.Value],
                    StartTime = session.StartTime!.Value.ToUniversalTime(),
                    EndTime = session.EndTime?.ToUniversalTime(),
                    Duration = CalculateDurationSeconds(session.StartTime, session.EndTime),
                    CreatedAt = (session.EndTime ?? session.StartTime ?? DateTime.UtcNow).ToUniversalTime()
                })
                .ToList(),
            Activities = activities
                .Where(activity => applicationIds.ContainsKey(activity.AppId))
                .Select(activity => new SyncActivityRecord
                {
                    Id = GetActivitySyncId(activity),
                    DeviceId = deviceId,
                    ApplicationId = applicationIds[activity.AppId],
                    CategoryId = activity.CategoryId.HasValue && activity.CategoryId.Value > 0 && categoryIds.TryGetValue(activity.CategoryId.Value, out var categoryId)
                        ? categoryId
                        : null,
                    Url = NormalizeOptionalValue(activity.Url),
                    CreatedAt = DateTime.UnixEpoch.AddSeconds(Math.Max(1, activity.ActivityId)).ToUniversalTime()
                })
                .ToList(),
            Thresholds = thresholds
                .Select(threshold => new SyncThresholdRecord
                {
                    Id = GetThresholdSyncId(threshold),
                    DeviceId = deviceId,
                    CategoryId = threshold.CategoryId > 0 && categoryIds.TryGetValue(threshold.CategoryId, out var categoryId)
                        ? categoryId
                        : null,
                    ApplicationId = threshold.AppId > 0 && applicationIds.TryGetValue(threshold.AppId, out var applicationId)
                        ? applicationId
                        : null,
                    Active = threshold.Active,
                    TargetType = threshold.TargetType,
                    InterventionType = threshold.InterventionType,
                    DurationType = threshold.DurationType,
                    SessionLimitSec = threshold.SessionLimitSec,
                    DailyLimitSec = threshold.DailyLimitSec,
                    CreatedAt = DateTime.UnixEpoch.AddSeconds(Math.Max(1, threshold.Id)).ToUniversalTime()
                })
                .ToList(),
            Settings =
            [
                new SyncSettingRecord
                {
                    Id = GetSettingsSyncId(settings),
                    DeviceId = deviceId,
                    DeltaTimeSeconds = settings.DeltaTimeSeconds,
                    UpdatedAt = (settings.SyncLastServerTimeUtc ?? DateTime.UtcNow).ToUniversalTime()
                }
            ]
        };
    }

    private void MergeSyncResponse(SyncResponse response)
    {
        var categoryMap = MergeCategories(response.Categories);
        var applicationMap = MergeApplications(response.Applications, categoryMap);
        MergeThresholds(response.Thresholds, categoryMap, applicationMap);
        MergeSessions(response.Sessions, applicationMap);
        MergeActivities(response.Activities, categoryMap, applicationMap);
        MergeSettings(response.Settings);
    }

    private Dictionary<string, int> MergeCategories(IEnumerable<SyncCategoryRecord> remoteCategories)
    {
        var localCategories = _db.GetAllCategories().ToList();
        var map = localCategories.ToDictionary(GetCategorySyncId, category => category.CategoryId, StringComparer.OrdinalIgnoreCase);

        foreach (var remoteCategory in remoteCategories.Where(category => !string.IsNullOrWhiteSpace(category.Id) && !string.IsNullOrWhiteSpace(category.Name)))
        {
            if (map.ContainsKey(remoteCategory.Id))
            {
                continue;
            }

            var existing = localCategories.FirstOrDefault(category =>
                string.Equals(category.Name, remoteCategory.Name.Trim(), StringComparison.OrdinalIgnoreCase));

            if (existing == null)
            {
                var categoryId = _db.InsertCategory(new CategoryDto
                {
                    Name = remoteCategory.Name.Trim(),
                    Description = NormalizeOptionalValue(remoteCategory.Description)
                });

                existing = new CategoryDto
                {
                    CategoryId = categoryId,
                    Name = remoteCategory.Name.Trim(),
                    Description = NormalizeOptionalValue(remoteCategory.Description)
                };
                localCategories.Add(existing);
            }

            map[remoteCategory.Id] = existing.CategoryId;
        }

        return map;
    }

    private Dictionary<string, int> MergeApplications(
        IEnumerable<SyncApplicationRecord> remoteApplications,
        IReadOnlyDictionary<string, int> categoryMap)
    {
        var localApplications = _db.GetAllApplications().ToList();
        var map = localApplications
            .Where(app => app.Id.HasValue)
            .ToDictionary(GetApplicationSyncId, app => app.Id!.Value, StringComparer.OrdinalIgnoreCase);

        foreach (var remoteApp in remoteApplications.Where(app => !string.IsNullOrWhiteSpace(app.Id)))
        {
            if (map.ContainsKey(remoteApp.Id))
            {
                continue;
            }

            var dto = new ApplicationDto
            {
                WindowTitle = NormalizeOptionalValue(remoteApp.WindowTitle),
                ClassName = NormalizeOptionalValue(remoteApp.ClassName),
                ProcessName = NormalizeOptionalValue(remoteApp.ProcessName),
                CategoryId = TryResolveLocalId(remoteApp.CategoryId, categoryMap),
                PositionX = remoteApp.PositionX,
                PositionY = remoteApp.PositionY,
                Width = remoteApp.Width,
                Height = remoteApp.Height,
                WindowId = remoteApp.WindowId
            };

            var existingId = _db.IsInDb(dto);
            if (existingId.HasValue)
            {
                dto.Id = existingId.Value;
                _db.UpdateApplication(dto);
                map[remoteApp.Id] = existingId.Value;
                continue;
            }

            var insertedId = _db.UpsertApplication(dto);
            map[remoteApp.Id] = insertedId;
        }

        return map;
    }

    private void MergeThresholds(
        IEnumerable<SyncThresholdRecord> remoteThresholds,
        IReadOnlyDictionary<string, int> categoryMap,
        IReadOnlyDictionary<string, int> applicationMap)
    {
        var localThresholds = _db.GetAllThresholds()
            .Where(threshold => threshold != null)
            .Select(threshold => threshold!)
            .ToList();

        foreach (var remoteThreshold in remoteThresholds.Where(threshold => !string.IsNullOrWhiteSpace(threshold.Id)))
        {
            var dto = new ThresholdDto
            {
                UserId = DefaultUserId,
                CategoryId = TryResolveLocalId(remoteThreshold.CategoryId, categoryMap),
                AppId = TryResolveLocalId(remoteThreshold.ApplicationId, applicationMap),
                Active = remoteThreshold.Active,
                TargetType = string.IsNullOrWhiteSpace(remoteThreshold.TargetType) ? "Category" : remoteThreshold.TargetType.Trim(),
                InterventionType = string.IsNullOrWhiteSpace(remoteThreshold.InterventionType) ? "Notification" : remoteThreshold.InterventionType.Trim(),
                DurationType = string.IsNullOrWhiteSpace(remoteThreshold.DurationType) ? "Daily" : remoteThreshold.DurationType.Trim(),
                SessionLimitSec = Math.Max(0, remoteThreshold.SessionLimitSec),
                DailyLimitSec = Math.Max(0, remoteThreshold.DailyLimitSec)
            };

            var existing = localThresholds.FirstOrDefault(threshold =>
                threshold.UserId == DefaultUserId &&
                string.Equals(threshold.TargetType, dto.TargetType, StringComparison.OrdinalIgnoreCase) &&
                threshold.CategoryId == dto.CategoryId &&
                threshold.AppId == dto.AppId);

            if (existing != null)
            {
                dto.Id = existing.Id;
            }

            _db.UpsertThreshold(dto);
        }
    }

    private void MergeSessions(
        IEnumerable<SyncSessionRecord> remoteSessions,
        IReadOnlyDictionary<string, int> applicationMap)
    {
        foreach (var remoteSession in remoteSessions.Where(session =>
                     !string.IsNullOrWhiteSpace(session.ApplicationId) &&
                     applicationMap.ContainsKey(session.ApplicationId) &&
                     session.StartTime != default))
        {
            var dto = new SessionDto
            {
                AppId = applicationMap[remoteSession.ApplicationId],
                UserId = DefaultUserId,
                StartTime = DateTime.SpecifyKind(remoteSession.StartTime, DateTimeKind.Utc),
                EndTime = remoteSession.EndTime == default ? null : DateTime.SpecifyKind(remoteSession.EndTime.Value, DateTimeKind.Utc)
            };

            _db.UpsertSession(dto);
        }
    }

    private void MergeActivities(
        IEnumerable<SyncActivityRecord> remoteActivities,
        IReadOnlyDictionary<string, int> categoryMap,
        IReadOnlyDictionary<string, int> applicationMap)
    {
        foreach (var remoteActivity in remoteActivities.Where(activity =>
                     !string.IsNullOrWhiteSpace(activity.ApplicationId) &&
                     applicationMap.ContainsKey(activity.ApplicationId)))
        {
            var dto = new BrowserActivityDto
            {
                UserId = DefaultUserId,
                AppId = applicationMap[remoteActivity.ApplicationId],
                CategoryId = TryResolveNullableLocalId(remoteActivity.CategoryId, categoryMap),
                Url = NormalizeOptionalValue(remoteActivity.Url)
            };

            if (!_db.IsInDb(dto).HasValue)
            {
                _db.InsertBrowserActivity(dto);
            }
        }
    }

    private void MergeSettings(IEnumerable<SyncSettingRecord> remoteSettings)
    {
        var latestSetting = remoteSettings
            .Where(setting => setting.DeltaTimeSeconds > 0)
            .OrderByDescending(setting => setting.UpdatedAt)
            .FirstOrDefault();

        if (latestSetting == null)
        {
            return;
        }

        var settings = EnsureSettingsRecord();
        settings.DeltaTimeSeconds = latestSetting.DeltaTimeSeconds;
        SaveSettings(settings);
    }

    private void SaveSettings(SettingsDto settings)
    {
        if (settings.Id > 0)
        {
            _db.UpdateSettings(settings);
        }
        else
        {
            settings.Id = _db.InsertSettings(settings);
        }
    }

    private static string BuildSyncSummary(SyncResponse response, bool registeredDevice, string message)
    {
        var importedCount = response.Categories.Count +
                            response.Applications.Count +
                            response.Sessions.Count +
                            response.Activities.Count +
                            response.Thresholds.Count +
                            response.Settings.Count;

        if (registeredDevice)
        {
            return $"{message} Dispozitivul curent a fost inregistrat pe server. Au fost procesate {importedCount} entitati din raspuns.";
        }

        return $"{message} Au fost procesate {importedCount} entitati din raspuns.";
    }

    private static int CalculateDurationSeconds(DateTime? startTime, DateTime? endTime)
    {
        if (!startTime.HasValue || !endTime.HasValue)
        {
            return 0;
        }

        return Math.Max(0, (int)(endTime.Value - startTime.Value).TotalSeconds);
    }

    private static int TryResolveLocalId(string? remoteId, IReadOnlyDictionary<string, int> map)
    {
        return string.IsNullOrWhiteSpace(remoteId) || !map.TryGetValue(remoteId, out var localId)
            ? 0
            : localId;
    }

    private static int? TryResolveNullableLocalId(string? remoteId, IReadOnlyDictionary<string, int> map)
    {
        return string.IsNullOrWhiteSpace(remoteId) || !map.TryGetValue(remoteId, out var localId)
            ? null
            : localId;
    }

    private static string GetCategorySyncId(CategoryDto category)
    {
        return SyncIdentity.Create("category", category.CategoryId, category.Name, category.Description);
    }

    private static string GetApplicationSyncId(ApplicationDto app)
    {
        return SyncIdentity.Create(
            "application",
            app.Id ?? 0,
            app.WindowTitle,
            app.ClassName,
            app.ProcessName);
    }

    private static string GetSessionSyncId(SessionDto session)
    {
        return SyncIdentity.Create(
            "session",
            session.UserId ?? DefaultUserId,
            session.AppId ?? 0,
            session.StartTime ?? DateTime.UnixEpoch);
    }

    private static string GetActivitySyncId(BrowserActivityDto activity)
    {
        return SyncIdentity.Create(
            "activity",
            activity.ActivityId,
            activity.UserId,
            activity.AppId,
            activity.Url);
    }

    private static string GetThresholdSyncId(ThresholdDto threshold)
    {
        return SyncIdentity.Create(
            "threshold",
            threshold.Id,
            threshold.UserId,
            threshold.TargetType,
            threshold.CategoryId,
            threshold.AppId,
            threshold.InterventionType,
            threshold.DurationType,
            threshold.SessionLimitSec,
            threshold.DailyLimitSec);
    }

    private static string GetSettingsSyncId(SettingsDto settings)
    {
        return SyncIdentity.Create("settings", settings.UserId, settings.Id);
    }

    private static string FormatLastSyncLabel(DateTime? timestampUtc)
    {
        return timestampUtc.HasValue
            ? $"Ultima sincronizare la {timestampUtc.Value.ToLocalTime():dd MMM yyyy, HH:mm:ss}"
            : "Nicio sincronizare remota";
    }

    private static string BuildCurrentFingerprint()
    {
        var seed = $"{Environment.MachineName}|{RuntimeInformation.OSDescription}|{RuntimeInformation.ProcessArchitecture}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(seed));
        return Convert.ToHexString(bytes);
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string FormatRelativeTime(DateTime timestampUtc)
    {
        var local = timestampUtc.Kind == DateTimeKind.Utc ? timestampUtc.ToLocalTime() : timestampUtc;
        var elapsed = DateTime.Now - local;

        if (elapsed.TotalMinutes < 1)
        {
            return "acum cateva secunde";
        }

        if (elapsed.TotalHours < 1)
        {
            return $"acum {(int)elapsed.TotalMinutes} min";
        }

        if (elapsed.TotalDays < 1)
        {
            return $"acum {(int)elapsed.TotalHours}h {(int)elapsed.Minutes}m";
        }

        return local.ToString("dd MMM yyyy, HH:mm");
    }
}

public sealed class AccountDeviceRow
{
    public int DeviceId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string DeviceType { get; init; } = string.Empty;
    public string Platform { get; init; } = string.Empty;
    public string Fingerprint { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string AppVersion { get; init; } = string.Empty;
    public bool IsTrusted { get; init; }
    public bool IsCurrentDevice { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime LastSeenAt { get; init; }
    public DateTime? RevokedAt { get; init; }

    public bool IsActive => string.Equals(Status, "Active", StringComparison.OrdinalIgnoreCase);

    public string StatusSummary => IsActive ? "Activ si conectat" : $"Revocat la {RevokedLabel}";

    public string TrustSummary => IsTrusted ? "Dispozitiv de incredere" : "Necesita verificare manuala";

    public string ActivitySummary => IsCurrentDevice
        ? $"Acest dispozitiv este activ acum ({DeviceType.ToLowerInvariant()} · {Platform})"
        : $"Ultima activitate: {LastSeenLabel}";

    public string LastSeenLabel => DevicesViewModel.FormatRelativeTime(LastSeenAt);

    public string CreatedLabel => CreatedAt.ToLocalTime().ToString("dd MMM yyyy");

    public string RevokedLabel => RevokedAt?.ToLocalTime().ToString("dd MMM yyyy, HH:mm") ?? "nedefinit";

    public string StatusChip => IsActive ? "Activ" : "Revocat";

    public string TrustChip => IsTrusted ? "De incredere" : "Review";

    public string VersionSummary => string.IsNullOrWhiteSpace(AppVersion)
        ? $"Amprenta: {Fingerprint[..Math.Min(12, Fingerprint.Length)]}"
        : $"Versiune {AppVersion} · amprenta {Fingerprint[..Math.Min(12, Fingerprint.Length)]}";

    public string TrustActionLabel => IsTrusted ? "Scoate increderea" : "Marcheaza sigur";

    public string ActionLabel => IsActive ? "Revoca" : "Reactiveaza";
}
