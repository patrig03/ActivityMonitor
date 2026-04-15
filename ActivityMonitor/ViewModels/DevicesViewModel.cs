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
        var settings = _db.GetSettings(DefaultUserId);
        var configuredAddress = settings?.SyncServerAddress;

        if (string.IsNullOrWhiteSpace(configuredAddress))
        {
            DeviceStatus = "Configureaza mai intai serverul de sincronizare din pagina Setari.";
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

        DeviceStatus = $"Se sincronizeaza cu {configuredAddress}...";
        var result = await _serverSync.SyncDevicesAsync(configuredAddress, DefaultUserId, devices, currentDevice);
        if (!result.Success)
        {
            DeviceStatus = result.Message;
            LastServerSyncLabel = "Ultima sincronizare a esuat";
            RefreshSyncServerStatus();
            return;
        }

        MergeRemoteDevices(result.Devices);
        LastServerSyncLabel = $"Ultima sincronizare la {DateTime.Now:HH:mm}";
        var syncStatus = result.Devices.Count == 0
            ? result.Message
            : $"{result.Message} Au fost sincronizate {result.Devices.Count} dispozitive.";
        Load(currentDevice.DeviceId);
        DeviceStatus = syncStatus;
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
            return;
        }

        SyncServerLabel = ServerSync.TryNormalizeServerAddress(configuredAddress, out var normalizedAddress, out _)
            ? ServerSync.BuildDevicesEndpointPreview(normalizedAddress)
            : $"Server sincronizare invalid: {configuredAddress}";
    }

    private void MergeRemoteDevices(IEnumerable<DeviceDto> remoteDevices)
    {
        var now = DateTime.UtcNow;

        foreach (var device in remoteDevices
                     .Where(device => !string.IsNullOrWhiteSpace(device.Fingerprint))
                     .GroupBy(device => device.Fingerprint, StringComparer.OrdinalIgnoreCase)
                     .Select(group => group.Last()))
        {
            device.DeviceId = 0;
            device.UserId = DefaultUserId;
            device.Name = string.IsNullOrWhiteSpace(device.Name) ? "Dispozitiv sincronizat" : device.Name.Trim();
            device.DeviceType = string.IsNullOrWhiteSpace(device.DeviceType) ? "Desktop" : device.DeviceType.Trim();
            device.Platform = string.IsNullOrWhiteSpace(device.Platform) ? "Necunoscut" : device.Platform.Trim();
            device.Status = string.IsNullOrWhiteSpace(device.Status) ? "Active" : device.Status.Trim();
            device.AppVersion = NormalizeOptionalValue(device.AppVersion);
            device.Fingerprint = device.Fingerprint.Trim();
            device.CreatedAt = device.CreatedAt == default ? now : device.CreatedAt;
            device.LastSeenAt = device.LastSeenAt == default ? now : device.LastSeenAt;
            device.IsCurrentDevice = string.Equals(device.Fingerprint, _currentFingerprint, StringComparison.OrdinalIgnoreCase);
            if (device.IsCurrentDevice)
            {
                device.Status = "Active";
                device.RevokedAt = null;
            }

            _db.UpsertDevice(device);
        }
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

    public string TrustChip => IsTrusted ? "Sigur" : "Review";

    public string VersionSummary => string.IsNullOrWhiteSpace(AppVersion) ? "versiune necunoscuta" : AppVersion;

    public string TrustActionLabel => IsTrusted ? "Scoate trust" : "Marcheaza sigur";

    public string ActionLabel => IsCurrentDevice && IsActive ? "Protejat" : IsActive ? "Revoca" : "Reactiveaza";
}
