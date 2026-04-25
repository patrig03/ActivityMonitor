using System;
using System.Linq;
using System.Windows.Input;
using Backend.Models;
using Backend.Sync;
using CommunityToolkit.Mvvm.ComponentModel;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private const int DefaultUserId = 1;
    private const int DefaultIntervalSeconds = 10;

    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private readonly ServerSync _serverSync = new();
    private Settings _settings = new();
    private bool _isLoading;

    private string _refreshIntervalSeconds = DefaultIntervalSeconds.ToString();
    private string _saveStatus = "Se încarcă setările...";
    private string _validationMessage = "Cadență echilibrată pentru majoritatea desktopurilor.";
    private string _intervalProfile = "Monitorizare echilibrată";
    private string _intervalImpact = "360 esantioane/ora | ~8,640 esantioane/zi";
    private string _databasePath = Settings.DatabaseEndpoint;
    private string _databaseStatus = "Endpoint MySQL configurat";
    private string _serviceMutexName = Settings.MutexName;
    private string _thresholdCoverage = "--";
    private string _activityCoverage = "--";
    private string _interventionCoverage = "--";
    private string _browserCoverage = "--";
    private string _monitoringSummary = "--";
    private string _lastSavedLabel = "Nu a fost salvat încă";
    private string _syncServerAddress = string.Empty;
    private string _syncEmail = string.Empty;
    private string _syncPassword = string.Empty;
    private string _syncEndpointPreview = "Serverul de sincronizare nu este configurat.";
    private string _syncAuthStatus = "Neautentificat";
    private string _syncDeviceStatus = "Niciun dispozitiv server configurat";
    private string _syncServerTimeStatus = "Nicio sincronizare confirmata de server";

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save());
        ResetDefaultsCommand = new RelayCommand(_ => ResetToDefaults());
        ReloadCommand = new RelayCommand(_ => Load());
        UseHighPrecisionPresetCommand = new RelayCommand(_ => ApplyPreset(5));
        UseBalancedPresetCommand = new RelayCommand(_ => ApplyPreset(DefaultIntervalSeconds));
        UseLowOverheadPresetCommand = new RelayCommand(_ => ApplyPreset(30));
        CheckSyncServerCommand = new RelayCommand(_ => CheckSyncServerAsync());
        LoginSyncCommand = new RelayCommand(_ => AuthenticateWithSyncServerAsync(register: false));
        RegisterSyncCommand = new RelayCommand(_ => AuthenticateWithSyncServerAsync(register: true));
        ClearSyncSessionCommand = new RelayCommand(_ => ClearSyncSession());

        Load();
    }

    public ICommand SaveCommand { get; }

    public ICommand ResetDefaultsCommand { get; }

    public ICommand ReloadCommand { get; }

    public ICommand UseHighPrecisionPresetCommand { get; }

    public ICommand UseBalancedPresetCommand { get; }

    public ICommand UseLowOverheadPresetCommand { get; }

    public ICommand CheckSyncServerCommand { get; }

    public ICommand LoginSyncCommand { get; }

    public ICommand RegisterSyncCommand { get; }

    public ICommand ClearSyncSessionCommand { get; }

    public string RefreshIntervalSeconds
    {
        get => _refreshIntervalSeconds;
        set
        {
            if (!SetProperty(ref _refreshIntervalSeconds, value))
            {
                return;
            }

            UpdateIntervalPreview();

            if (!_isLoading)
            {
                SaveStatus = "Modificari nesalvate";
            }
        }
    }

    public string SaveStatus
    {
        get => _saveStatus;
        set => SetProperty(ref _saveStatus, value);
    }

    public string ValidationMessage
    {
        get => _validationMessage;
        set => SetProperty(ref _validationMessage, value);
    }

    public string IntervalProfile
    {
        get => _intervalProfile;
        set => SetProperty(ref _intervalProfile, value);
    }

    public string IntervalImpact
    {
        get => _intervalImpact;
        set => SetProperty(ref _intervalImpact, value);
    }

    public string DatabasePath
    {
        get => _databasePath;
        set => SetProperty(ref _databasePath, value);
    }

    public string DatabaseStatus
    {
        get => _databaseStatus;
        set => SetProperty(ref _databaseStatus, value);
    }

    public string ServiceMutexName
    {
        get => _serviceMutexName;
        set => SetProperty(ref _serviceMutexName, value);
    }

    public string ThresholdCoverage
    {
        get => _thresholdCoverage;
        set => SetProperty(ref _thresholdCoverage, value);
    }

    public string ActivityCoverage
    {
        get => _activityCoverage;
        set => SetProperty(ref _activityCoverage, value);
    }

    public string InterventionCoverage
    {
        get => _interventionCoverage;
        set => SetProperty(ref _interventionCoverage, value);
    }

    public string BrowserCoverage
    {
        get => _browserCoverage;
        set => SetProperty(ref _browserCoverage, value);
    }

    public string MonitoringSummary
    {
        get => _monitoringSummary;
        set => SetProperty(ref _monitoringSummary, value);
    }

    public string LastSavedLabel
    {
        get => _lastSavedLabel;
        set => SetProperty(ref _lastSavedLabel, value);
    }

    public string SyncServerAddress
    {
        get => _syncServerAddress;
        set
        {
            if (!SetProperty(ref _syncServerAddress, value))
            {
                return;
            }

            UpdateSyncPreview();

            if (!_isLoading)
            {
                SaveStatus = "Modificari nesalvate";
            }
        }
    }

    public string SyncEmail
    {
        get => _syncEmail;
        set
        {
            if (!SetProperty(ref _syncEmail, value))
            {
                return;
            }

            if (!_isLoading)
            {
                SaveStatus = "Modificari nesalvate";
            }
        }
    }

    public string SyncPassword
    {
        get => _syncPassword;
        set => SetProperty(ref _syncPassword, value);
    }

    public string SyncEndpointPreview
    {
        get => _syncEndpointPreview;
        set => SetProperty(ref _syncEndpointPreview, value);
    }

    public string SyncAuthStatus
    {
        get => _syncAuthStatus;
        set => SetProperty(ref _syncAuthStatus, value);
    }

    public string SyncDeviceStatus
    {
        get => _syncDeviceStatus;
        set => SetProperty(ref _syncDeviceStatus, value);
    }

    public string SyncServerTimeStatus
    {
        get => _syncServerTimeStatus;
        set => SetProperty(ref _syncServerTimeStatus, value);
    }

    private void Load()
    {
        _isLoading = true;

        var dto = _db.GetSettings(DefaultUserId);
        _settings = dto == null ? new Settings() : Settings.FromDto(dto);

        RefreshIntervalSeconds = Math.Max(1, (int)_settings.DeltaTime.TotalSeconds).ToString();
        SyncServerAddress = _settings.SyncServerAddress ?? string.Empty;
        SyncEmail = _settings.SyncEmail ?? string.Empty;
        SyncPassword = string.Empty;
        DatabasePath = Settings.DatabaseEndpoint;
        DatabaseStatus = "Schema MySQL va fi creata automat cand conexiunea reuseste";
        ServiceMutexName = Settings.MutexName;
        LastSavedLabel = dto == null ? "Se folosesc valorile implicite" : "Încărcate din MySQL";
        SaveStatus = "Setări încărcate";

        RefreshDiagnostics();

        _isLoading = false;
    }

    private void Save()
    {
        if (!TryApplyEditorValuesToSettings(out var error))
        {
            SaveStatus = error;
            ValidationMessage = error;
            return;
        }

        PersistSettings("Persistați în tabelul MySQL de setări");
        SaveStatus = $"Salvat la {DateTime.Now:HH:mm}";
        RefreshDiagnostics();
    }

    private async void CheckSyncServerAsync()
    {
        if (!TryGetNormalizedServerAddress(out var normalizedAddress, out var error))
        {
            SaveStatus = error;
            SyncAuthStatus = error;
            ShowErrorToast(error);
            return;
        }

        SaveStatus = $"Se verifica serverul {normalizedAddress}...";
        var result = await _serverSync.CheckHealthAsync(normalizedAddress);
        SaveStatus = result.Message;
        if (result.Success)
        {
            SyncAuthStatus = $"Server disponibil ({result.Status})";
            ShowSuccessToast(SyncAuthStatus);
        }
        else
        {
            SyncAuthStatus = result.Message;
            ShowErrorToast(result.Message);
        }
        RefreshSyncState();
    }

    private async void AuthenticateWithSyncServerAsync(bool register)
    {
        if (!TryApplyEditorValuesToSettings(out var error))
        {
            SaveStatus = error;
            SyncAuthStatus = error;
            ShowErrorToast(error);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.SyncServerAddress))
        {
            SaveStatus = "Configurează mai întâi adresa serverului de sincronizare.";
            SyncAuthStatus = SaveStatus;
            ShowWarningToast(SaveStatus);
            return;
        }

        if (string.IsNullOrWhiteSpace(_settings.SyncEmail) || string.IsNullOrWhiteSpace(SyncPassword))
        {
            SaveStatus = "Emailul si parola sunt obligatorii pentru autentificarea la server.";
            SyncAuthStatus = SaveStatus;
            ShowWarningToast(SaveStatus);
            return;
        }

        SaveStatus = register
            ? $"Se creeaza contul {_settings.SyncEmail}..."
            : $"Se autentifică {_settings.SyncEmail}...";

        var authResult = register
            ? await _serverSync.RegisterAsync(_settings.SyncServerAddress, _settings.SyncEmail, SyncPassword)
            : await _serverSync.LoginAsync(_settings.SyncServerAddress, _settings.SyncEmail, SyncPassword);

        if (!authResult.Success || string.IsNullOrWhiteSpace(authResult.Token))
        {
            SaveStatus = authResult.Message;
            SyncAuthStatus = authResult.Message;
            ShowErrorToast(authResult.Message);
            return;
        }

        _settings.SyncEmail = authResult.Email ?? _settings.SyncEmail;
        _settings.SyncAuthToken = authResult.Token;
        _settings.SyncRemoteUserId = authResult.UserId;
        _settings.SyncDeviceId = null;
        _settings.SyncLastServerTimeUtc = null;
        SyncEmail = _settings.SyncEmail ?? string.Empty;
        SyncPassword = string.Empty;

        PersistSettings("Credentialele SyncServer au fost salvate");
        SaveStatus = authResult.Message;
        ShowSuccessToast(authResult.Message);
        RefreshSyncState();
    }

    private void ClearSyncSession()
    {
        if (!TryApplyEditorValuesToSettings(out var error))
        {
            SaveStatus = error;
            ShowErrorToast(error);
            return;
        }

        ResetStoredSyncSession();
        SyncPassword = string.Empty;
        PersistSettings("Sesiunea SyncServer a fost eliminata");
        SaveStatus = "Tokenul local a fost sters. Va trebui sa te autentifici din nou.";
        ShowWarningToast("Sesiunea SyncServer a fost stearsa");
        RefreshSyncState();
    }

    private void ResetToDefaults()
    {
        RefreshIntervalSeconds = DefaultIntervalSeconds.ToString();
        SyncServerAddress = string.Empty;
        SyncEmail = string.Empty;
        SyncPassword = string.Empty;
        ResetStoredSyncSession();
        ValidationMessage = "Cadenta implicita a fost restaurata. Salveaza pentru a o pastra.";
        RefreshSyncState();
    }

    private void ApplyPreset(int seconds)
    {
        RefreshIntervalSeconds = seconds.ToString();
    }

    private void UpdateIntervalPreview()
    {
        if (!TryParseInterval(out var seconds, out var error))
        {
            ValidationMessage = error;
            IntervalProfile = "Valoare invalida";
            IntervalImpact = "Introdu un număr întreg de secunde între 1 și 600.";
            MonitoringSummary = "Rezumatul eșantionării nu este disponibil";
            return;
        }

        var samplesPerHour = 3600 / (double)seconds;
        var samplesPerDay = 86400 / (double)seconds;

        if (seconds <= 5)
        {
            IntervalProfile = "Monitorizare cu precizie ridicată";
            ValidationMessage = "Captura rapida pentru schimbari scurte de activitate si limite stricte de sesiune.";
        }
        else if (seconds <= 15)
        {
            IntervalProfile = "Monitorizare echilibrată";
            ValidationMessage = "Compromis bun intre reactie rapida si volum de stocare.";
        }
        else if (seconds <= 60)
        {
            IntervalProfile = "Monitorizare cu consum redus";
            ValidationMessage = "Volum mai mic de scrieri, dar sesiunile scurte pot parea mai putin precise.";
        }
        else
        {
            IntervalProfile = "Monitorizare grosieră";
            ValidationMessage = "Potrivita doar daca vrei tendinte generale, nu interventii stricte.";
        }

        IntervalImpact = $"{samplesPerHour:0} esantioane/ora | ~{samplesPerDay:0} esantioane/zi";
        MonitoringSummary = $"Interval curent: {seconds}s | verificarile de interventie ruleaza in acelasi ritm.";
    }

    private void UpdateSyncPreview()
    {
        if (!ServerSync.TryNormalizeServerAddress(SyncServerAddress, out var normalizedSyncServerAddress, out var validationError))
        {
            SyncEndpointPreview = validationError;
            return;
        }

        SyncEndpointPreview = string.IsNullOrWhiteSpace(normalizedSyncServerAddress)
            ? "Configurează un IP sau URL. Exemplu: http://localhost:5000"
            : $"Health: {ServerSync.BuildHealthEndpointPreview(normalizedSyncServerAddress)}\nSync: {ServerSync.BuildSyncEndpointPreview(normalizedSyncServerAddress)}\nDevices: {ServerSync.BuildDevicesEndpointPreview(normalizedSyncServerAddress)}";
    }

    private void RefreshDiagnostics()
    {
        UpdateIntervalPreview();
        UpdateSyncPreview();
        RefreshSyncState();

        var categories = _db.GetAllCategories().Count();
        var applications = _db.GetAllApplications().Count();
        var thresholds = _db.GetAllThresholds().Where(threshold => threshold != null).Select(threshold => threshold!).ToList();
        var activeThresholds = thresholds.Count(threshold => threshold.Active);
        var browserEvents = _db.GetAllBrowserActivity().Count();
        var interventions = _db.GetInterventionsForUser(DefaultUserId).Count();
        var trackedSessions = _db.GetSessionsForUser(DefaultUserId).Count();

        ThresholdCoverage = $"{activeThresholds} praguri active din {thresholds.Count} limite configurate";
        ActivityCoverage = $"{applications} aplicații monitorizate, {trackedSessions} sesiuni capturate, {categories} categorii disponibile";
        InterventionCoverage = $"{interventions} intervenții înregistrate pentru utilizatorul curent";
        BrowserCoverage = $"{browserEvents} evenimente browser stocate în MySQL";
    }

    private void RefreshSyncState()
    {
        SyncAuthStatus = string.IsNullOrWhiteSpace(_settings.SyncAuthToken)
            ? "Neautentificat pe SyncServer"
            : $"Token activ pentru {_settings.SyncEmail ?? "cont necunoscut"}";

        SyncDeviceStatus = string.IsNullOrWhiteSpace(_settings.SyncDeviceId)
            ? "Dispozitivul curent nu este încă înregistrat pe server"
            : $"Device server: {ShortenGuid(_settings.SyncDeviceId)}";

        SyncServerTimeStatus = _settings.SyncLastServerTimeUtc.HasValue
            ? $"Ultimul serverTime primit: {_settings.SyncLastServerTimeUtc.Value.ToLocalTime():dd MMM yyyy, HH:mm:ss}"
            : "Nicio sincronizare confirmata de server";
    }

    private bool TryApplyEditorValuesToSettings(out string error)
    {
        error = string.Empty;

        if (!TryParseInterval(out var seconds, out error))
        {
            return false;
        }

        if (!ServerSync.TryNormalizeServerAddress(SyncServerAddress, out var normalizedSyncServerAddress, out var syncValidationError))
        {
            error = syncValidationError;
            SyncEndpointPreview = syncValidationError;
            return false;
        }

        var normalizedEmail = NormalizeOptionalValue(SyncEmail);
        var currentAddress = NormalizeOptionalValue(_settings.SyncServerAddress);
        var nextAddress = NormalizeOptionalValue(normalizedSyncServerAddress);
        var currentEmail = NormalizeOptionalValue(_settings.SyncEmail);
        var nextEmail = NormalizeOptionalValue(normalizedEmail);

        _settings.UserId = DefaultUserId;
        _settings.DeltaTime = TimeSpan.FromSeconds(seconds);
        _settings.SyncServerAddress = nextAddress;
        _settings.SyncEmail = nextEmail;

        if (!string.Equals(currentAddress, nextAddress, StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(currentEmail, nextEmail, StringComparison.OrdinalIgnoreCase))
        {
            ResetStoredSyncSession();
        }

        return true;
    }

    private bool TryGetNormalizedServerAddress(out string normalizedAddress, out string error)
    {
        normalizedAddress = string.Empty;
        error = string.Empty;

        if (!TryApplyEditorValuesToSettings(out error))
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(_settings.SyncServerAddress))
        {
            error = "Configurează mai întâi adresa serverului de sincronizare.";
            return false;
        }

        normalizedAddress = _settings.SyncServerAddress;
        return true;
    }

    private void PersistSettings(string lastSavedMessage)
    {
        if (_settings.Id > 0)
        {
            _db.UpdateSettings(_settings.ToDto());
        }
        else
        {
            _settings.Id = _db.InsertSettings(_settings.ToDto());
        }

        LastSavedLabel = lastSavedMessage;
    }

    private void ResetStoredSyncSession()
    {
        _settings.SyncAuthToken = null;
        _settings.SyncRemoteUserId = null;
        _settings.SyncDeviceId = null;
        _settings.SyncLastServerTimeUtc = null;
    }

    private bool TryParseInterval(out int seconds, out string error)
    {
        if (!int.TryParse(RefreshIntervalSeconds, out seconds))
        {
            error = "Intervalul de reîmprospătare trebuie să fie un număr întreg de secunde.";
            return false;
        }

        if (seconds < 1 || seconds > 600)
        {
            error = "Intervalul de reîmprospătare trebuie să fie între 1 și 600 de secunde.";
            return false;
        }

        error = string.Empty;
        return true;
    }

    private static string? NormalizeOptionalValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string ShortenGuid(string? value)
    {
        return string.IsNullOrWhiteSpace(value) || value.Length <= 8
            ? value ?? "--"
            : value[..8];
    }
}
