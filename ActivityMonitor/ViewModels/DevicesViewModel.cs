using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
    private readonly ServerSync _serverSync = new();

    private string? _selectedDeviceId;
    private string _pageSubtitle = "Se încarcă inventarul de dispozitive din server...";
    private string _deviceStatus = "Citire in curs";
    private string _lastRefreshLabel = "Actualizare in curs";
    private string _accountLabel = "Cont sincronizat";
    private string _currentDeviceLabel = "--";
    private string _currentDeviceDetail = "Detectam dispozitivul curent.";
    private string _totalDevices = "0";
    private string _activeDevices = "0";
    private string _revokedDevices = "0";
    private string _unknownDevices = "0";
    private string _selectedDeviceName = string.Empty;
    private string _selectedDeviceType = "Necunoscut";
    private string _selectedDevicePlatform = "Necunoscut";
    private string _selectedDeviceVersion = "Necunoscut";
    private string _selectedDeviceIdentifier = string.Empty;
    private string _selectedDeviceState = "Selecteaza un dispozitiv";
    private string _selectedDeviceTimeline = "Detaliile de activitate vor aparea aici.";
    private string _selectedDeviceTrust = "Alege un dispozitiv pentru a vedea increderea si statusul serverului.";
    private string _selectedDeviceServerStatus = "Statusul complet va aparea aici.";
    private bool _hasSelectedDevice;
    private AccountDeviceRow? _selectedDevice;
    private string _syncServerLabel = "Server sincronizare neconfigurat";
    private string _lastServerSyncLabel = "Nicio sincronizare remota";

    public DevicesViewModel()
    {
        RefreshCommand = new RelayCommand(_ => LoadServerDevicesAsync());
        SyncWithServerCommand = new RelayCommand(_ => SyncWithServerAsync());
        ClearSelectionCommand = new RelayCommand(_ => ClearSelection());

        var user = _db.GetUser(DefaultUserId);
        AccountLabel = !string.IsNullOrWhiteSpace(user?.DisplayName)
            ? user!.DisplayName!
            : "Cont sincronizat";

        CurrentDeviceLabel = DetectCurrentDeviceName();
        CurrentDeviceDetail = $"{DetectCurrentDeviceType()} · {DetectPlatformLabel()}";

        LoadServerDevicesAsync();
    }

    public ObservableCollection<AccountDeviceRow> Devices { get; } = [];

    public ICommand RefreshCommand { get; }

    public ICommand SyncWithServerCommand { get; }

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

    public string RevokedDevices
    {
        get => _revokedDevices;
        set => SetProperty(ref _revokedDevices, value);
    }

    public string UnknownDevices
    {
        get => _unknownDevices;
        set => SetProperty(ref _unknownDevices, value);
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

    public string SelectedDeviceIdentifier
    {
        get => _selectedDeviceIdentifier;
        set => SetProperty(ref _selectedDeviceIdentifier, value);
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

    public string SelectedDeviceServerStatus
    {
        get => _selectedDeviceServerStatus;
        set => SetProperty(ref _selectedDeviceServerStatus, value);
    }

    public AccountDeviceRow? SelectedDevice
    {
        get => _selectedDevice;
        set
        {
            if (!SetProperty(ref _selectedDevice, value))
            {
                return;
            }

            if (value == null)
            {
                ClearSelection();
                return;
            }

            PopulateSelection(value);
        }
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
        }
    }

    public bool NoSelectedDevice => !HasSelectedDevice;

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

    private async void LoadServerDevicesAsync(string? preferredDeviceId = null)
    {
        await LoadServerDevicesInternalAsync(preferredDeviceId);
    }

    private async Task LoadServerDevicesInternalAsync(string? preferredDeviceId = null, string? completionStatus = null)
    {
        RefreshSyncServerStatus();

        var settings = EnsureSettingsRecord();
        AccountLabel = !string.IsNullOrWhiteSpace(settings.SyncEmail)
            ? settings.SyncEmail!
            : AccountLabel;
        CurrentDeviceLabel = DetectCurrentDeviceName();
        CurrentDeviceDetail = $"{DetectCurrentDeviceType()} · {DetectPlatformLabel()}";
        DeviceStatus = "Se citesc dispozitivele asociate contului de pe server...";

        if (!TryValidateSyncConfiguration(settings, out var normalizedAddress, out var bearerToken, out var error))
        {
            Devices.Clear();
            TotalDevices = "0";
            ActiveDevices = "0";
            RevokedDevices = "0";
            UnknownDevices = "0";
            PageSubtitle = "Pagina afișează dispozitivele de pe server pentru contul sincronizat. Configurează și autentifică mai întâi sesiunea de sync.";
            LastRefreshLabel = "Fara date server";
            DeviceStatus = error;
            ClearSelection();
            return;
        }

        var result = await _serverSync.GetDevicesAsync(normalizedAddress, bearerToken);
        if (!result.Success)
        {
            Devices.Clear();
            TotalDevices = "0";
            ActiveDevices = "0";
            RevokedDevices = "0";
            UnknownDevices = "0";
            PageSubtitle = "Nu am putut încărca dispozitivele de pe server pentru acest cont.";
            LastRefreshLabel = "Citire server eșuată";
            DeviceStatus = result.Message;
            ClearSelection();
            return;
        }

        var currentServerDeviceId = settings.SyncDeviceId?.Trim();
        var rows = result.Devices
            .Select(device => ToRow(device, currentServerDeviceId))
            .OrderByDescending(device => device.IsCurrentDevice)
            .ThenByDescending(device => device.LastSeenAt ?? DateTime.MinValue)
            .ThenBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase)
            .ToList();

        Devices.Clear();
        foreach (var row in rows)
        {
            Devices.Add(row);
        }

        var activeCount = Devices.Count(device => device.IsActive);
        var revokedCount = Devices.Count(device => device.IsRevoked);
        var unknownCount = Devices.Count - activeCount - revokedCount;
        var currentDevice = Devices.FirstOrDefault(device => device.IsCurrentDevice);
        var mostRecent = Devices
            .Where(device => device.LastSeenAt.HasValue)
            .OrderByDescending(device => device.LastSeenAt)
            .FirstOrDefault();

        TotalDevices = Devices.Count.ToString();
        ActiveDevices = activeCount.ToString();
        RevokedDevices = revokedCount.ToString();
        UnknownDevices = unknownCount.ToString();
        PageSubtitle = Devices.Count == 0
            ? "Contul autentificat nu are încă dispozitive înregistrate pe server. Rulează o sincronizare pentru a adăuga dispozitivul curent."
            : $"Serverul raportează {Devices.Count} dispozitive pentru acest cont, dintre care {activeCount} active și {revokedCount} revocate.";
        LastRefreshLabel = mostRecent == null
            ? "Actualizat acum"
            : $"Activitate recenta {FormatRelativeTime(mostRecent.LastSeenAt!.Value)}";
        CurrentDeviceLabel = currentDevice?.Name ?? DetectCurrentDeviceName();
        CurrentDeviceDetail = currentDevice == null
            ? "Dispozitivul curent nu este încă asociat pe server. Rulează o sincronizare pentru a-l înregistra."
            : currentDevice.ActivitySummary;
        DeviceStatus = completionStatus ?? (Devices.Count == 0
            ? "Nu există dispozitive pe server pentru acest cont."
            : "Inventarul de dispozitive de pe server a fost actualizat.");

        var selectedId = preferredDeviceId ?? _selectedDeviceId ?? currentServerDeviceId;
        var selectedRow = !string.IsNullOrWhiteSpace(selectedId)
            ? Devices.FirstOrDefault(device => string.Equals(device.DeviceId, selectedId, StringComparison.OrdinalIgnoreCase))
            : null;

        selectedRow ??= currentDevice ?? Devices.FirstOrDefault();
        if (selectedRow == null)
        {
            ClearSelection();
            return;
        }

        SelectedDevice = selectedRow;
    }

    private async void SyncWithServerAsync()
    {
        var settings = EnsureSettingsRecord();

        if (!TryValidateSyncConfiguration(settings, out var normalizedAddress, out var bearerToken, out var error))
        {
            DeviceStatus = error;
            RefreshSyncServerStatus();
            return;
        }

        DeviceStatus = $"Se inregistreaza dispozitivul curent pe {normalizedAddress}...";
        var deviceRegistration = await _serverSync.EnsureDeviceAsync(
            normalizedAddress,
            bearerToken,
            settings.SyncDeviceId,
            DetectCurrentDeviceName());

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
        RefreshSyncServerStatus();
        LastServerSyncLabel = FormatLastSyncLabel(settings.SyncLastServerTimeUtc);

        var summary = BuildSyncSummary(result.Data, deviceRegistration.Created, result.Message);
        await LoadServerDevicesInternalAsync(settings.SyncDeviceId, summary);
    }

    private void PopulateSelection(AccountDeviceRow row)
    {
        _selectedDeviceId = row.DeviceId;
        SelectedDeviceName = row.Name;
        SelectedDeviceType = row.DeviceType;
        SelectedDevicePlatform = row.Platform;
        SelectedDeviceVersion = row.AppVersion;
        SelectedDeviceIdentifier = row.DeviceId;
        SelectedDeviceState = row.StatusSummary;
        SelectedDeviceTimeline = row.ActivityTimeline;
        SelectedDeviceTrust = row.TrustSummary;
        SelectedDeviceServerStatus = row.ServerStatusSummary;
        HasSelectedDevice = true;
    }

    private void ClearSelection()
    {
        _selectedDeviceId = null;
        if (_selectedDevice != null)
        {
            SetProperty(ref _selectedDevice, null, nameof(SelectedDevice));
        }
        SelectedDeviceName = string.Empty;
        SelectedDeviceType = "Necunoscut";
        SelectedDevicePlatform = "Necunoscut";
        SelectedDeviceVersion = "Necunoscut";
        SelectedDeviceIdentifier = string.Empty;
        SelectedDeviceState = "Selecteaza un dispozitiv";
        SelectedDeviceTimeline = "Alege un dispozitiv din lista pentru a vedea istoricul serverului.";
        SelectedDeviceTrust = "Nu este selectat niciun dispozitiv.";
        SelectedDeviceServerStatus = "Statusul complet va aparea aici.";
        HasSelectedDevice = false;
    }

    private static AccountDeviceRow ToRow(ServerDeviceDescriptor device, string? currentServerDeviceId)
    {
        return new AccountDeviceRow
        {
            DeviceId = device.DeviceId,
            Name = string.IsNullOrWhiteSpace(device.Name) ? $"Device {device.DeviceId[..Math.Min(8, device.DeviceId.Length)]}" : device.Name.Trim(),
            DeviceType = string.IsNullOrWhiteSpace(device.DeviceType) ? "Necunoscut" : device.DeviceType.Trim(),
            Platform = string.IsNullOrWhiteSpace(device.Platform) ? "Platforma necunoscuta" : device.Platform.Trim(),
            Status = string.IsNullOrWhiteSpace(device.Status) ? "Necunoscut" : device.Status.Trim(),
            AppVersion = string.IsNullOrWhiteSpace(device.AppVersion) ? "Necunoscut" : device.AppVersion.Trim(),
            IsTrusted = device.IsTrusted,
            IsCurrentDevice = device.IsCurrentDevice == true ||
                              (!string.IsNullOrWhiteSpace(currentServerDeviceId) &&
                               string.Equals(device.DeviceId, currentServerDeviceId, StringComparison.OrdinalIgnoreCase)),
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

        SyncServerLabel = $"{ServerSync.BuildDevicesEndpointPreview(normalizedAddress)} · {authState} · {deviceState}";
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
            error = "Configurează mai întâi serverul de sincronizare din pagina Setări.";
            return false;
        }

        if (!ServerSync.TryNormalizeServerAddress(settings.SyncServerAddress, out normalizedAddress, out error))
        {
            return false;
        }

        bearerToken = settings.SyncAuthToken?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            error = "Autentifică-te în pagina Setări înainte să încarci dispozitivele contului.";
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
                    WindowId = (int?)app.WindowId
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
                CategoryId = TryResolveNullableLocalId(remoteApp.CategoryId, categoryMap),
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
    public string DeviceId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string DeviceType { get; init; } = "Necunoscut";
    public string Platform { get; init; } = "Platforma necunoscuta";
    public string Status { get; init; } = "Necunoscut";
    public string AppVersion { get; init; } = "Necunoscut";
    public bool? IsTrusted { get; init; }
    public bool IsCurrentDevice { get; init; }
    public DateTime? CreatedAt { get; init; }
    public DateTime? LastSeenAt { get; init; }
    public DateTime? RevokedAt { get; init; }

    public bool IsActive => MatchesAny(Status, "active", "online", "connected", "enabled");

    public bool IsRevoked => MatchesAny(Status, "revoked", "inactive", "disabled", "blocked");

    public string StatusSummary => IsActive
        ? "Dispozitiv activ pe server"
        : IsRevoked
            ? $"Acces revocat {RevokedSuffix}"
            : $"Status raportat de server: {Status}";

    public string TrustSummary => IsTrusted switch
    {
        true => "Marcat de server ca dispozitiv de incredere",
        false => "Dispozitivul nu este marcat ca sigur pe server",
        _ => "Serverul nu a furnizat un indicator explicit de incredere"
    };

    public string ActivitySummary => IsCurrentDevice
        ? $"Acest dispozitiv este asociat sesiunii locale de sync ({DeviceType.ToLowerInvariant()} · {Platform})"
        : LastSeenAt.HasValue
            ? $"Ultima activitate pe server: {LastSeenLabel}"
            : $"Fara timestamp de activitate raportat ({DeviceType.ToLowerInvariant()} · {Platform})";

    public string ActivityTimeline => CreatedAt.HasValue
        ? $"Creat {CreatedLabel} · {ActivitySummary}"
        : ActivitySummary;

    public string ServerStatusSummary => $"Status server: {StatusChip} · ID {ShortId}";

    public string LastSeenLabel => LastSeenAt.HasValue
        ? DevicesViewModel.FormatRelativeTime(LastSeenAt.Value)
        : "necunoscut";

    public string CreatedLabel => CreatedAt?.ToLocalTime().ToString("dd MMM yyyy, HH:mm") ?? "necunoscut";

    public string RevokedLabel => RevokedAt?.ToLocalTime().ToString("dd MMM yyyy, HH:mm") ?? "necunoscut";

    public string StatusChip => IsActive
        ? "Activ"
        : IsRevoked
            ? "Revocat"
            : Status;

    public string TrustChip => IsTrusted switch
    {
        true => "De incredere",
        false => "Verificare",
        _ => "Necunoscut"
    };

    public string VersionSummary => string.Equals(AppVersion, "Necunoscut", StringComparison.OrdinalIgnoreCase)
        ? $"ID server {ShortId}"
        : $"Versiune {AppVersion} · ID server {ShortId}";

    private string ShortId => DeviceId[..Math.Min(12, DeviceId.Length)];

    private string RevokedSuffix => RevokedAt.HasValue ? $"la {RevokedLabel}" : "de server";

    private static bool MatchesAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => value.Contains(candidate, StringComparison.OrdinalIgnoreCase));
    }
}
