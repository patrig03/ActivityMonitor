using System;
using System.Linq;
using System.Windows.Input;
using Backend.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class SettingsViewModel : ObservableObject
{
    private const int DefaultUserId = 1;
    private const int DefaultIntervalSeconds = 10;

    private readonly IDatabaseManager _db = new DatabaseManager(Settings.DatabaseConnectionString);
    private Settings _settings = new();
    private bool _isLoading;

    private string _refreshIntervalSeconds = DefaultIntervalSeconds.ToString();
    private string _saveStatus = "Se incarca setarile...";
    private string _validationMessage = "Cadenta echilibrata pentru majoritatea desktopurilor.";
    private string _intervalProfile = "Monitorizare echilibrata";
    private string _intervalImpact = "360 esantioane/ora | ~8,640 esantioane/zi";
    private string _databasePath = Settings.DatabaseEndpoint;
    private string _databaseStatus = "Endpoint MySQL configurat";
    private string _serviceMutexName = Settings.MutexName;
    private string _thresholdCoverage = "--";
    private string _activityCoverage = "--";
    private string _interventionCoverage = "--";
    private string _browserCoverage = "--";
    private string _monitoringSummary = "--";
    private string _lastSavedLabel = "Nu a fost salvat inca";

    public SettingsViewModel()
    {
        SaveCommand = new RelayCommand(_ => Save());
        ResetDefaultsCommand = new RelayCommand(_ => ResetToDefaults());
        ReloadCommand = new RelayCommand(_ => Load());
        UseHighPrecisionPresetCommand = new RelayCommand(_ => ApplyPreset(5));
        UseBalancedPresetCommand = new RelayCommand(_ => ApplyPreset(DefaultIntervalSeconds));
        UseLowOverheadPresetCommand = new RelayCommand(_ => ApplyPreset(30));

        Load();
    }

    public ICommand SaveCommand { get; }

    public ICommand ResetDefaultsCommand { get; }

    public ICommand ReloadCommand { get; }

    public ICommand UseHighPrecisionPresetCommand { get; }

    public ICommand UseBalancedPresetCommand { get; }

    public ICommand UseLowOverheadPresetCommand { get; }

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

    private void Load()
    {
        _isLoading = true;

        var dto = _db.GetSettings(DefaultUserId);
        _settings = dto == null ? new Settings() : Settings.FromDto(dto);

        RefreshIntervalSeconds = Math.Max(1, (int)_settings.DeltaTime.TotalSeconds).ToString();
        DatabasePath = Settings.DatabaseEndpoint;
        DatabaseStatus = "Schema MySQL va fi creata automat cand conexiunea reuseste";
        ServiceMutexName = Settings.MutexName;
        LastSavedLabel = dto == null ? "Se folosesc valorile implicite" : "Incarcate din MySQL";
        SaveStatus = "Setari incarcate";

        RefreshDiagnostics();

        _isLoading = false;
    }

    private void Save()
    {
        if (!TryParseInterval(out var seconds, out var error))
        {
            SaveStatus = error;
            ValidationMessage = error;
            return;
        }

        _settings.UserId = DefaultUserId;
        _settings.DeltaTime = TimeSpan.FromSeconds(seconds);

        if (_settings.Id > 0)
        {
            _db.UpdateSettings(_settings.ToDto());
        }
        else
        {
            _settings.Id = _db.InsertSettings(_settings.ToDto());
        }

        SaveStatus = $"Salvat la {DateTime.Now:HH:mm}";
        LastSavedLabel = "Persistate in tabelul MySQL de setari";
        RefreshDiagnostics();
    }

    private void ResetToDefaults()
    {
        RefreshIntervalSeconds = DefaultIntervalSeconds.ToString();
        ValidationMessage = "Cadenta implicita a fost restaurata. Salveaza pentru a o pastra.";
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
            IntervalImpact = "Introdu un numar intreg de secunde intre 1 si 600.";
            MonitoringSummary = "Rezumatul esantionarii nu este disponibil";
            return;
        }

        var samplesPerHour = 3600 / (double)seconds;
        var samplesPerDay = 86400 / (double)seconds;

        if (seconds <= 5)
        {
            IntervalProfile = "Monitorizare cu precizie ridicata";
            ValidationMessage = "Captura rapida pentru schimbari scurte de activitate si limite stricte de sesiune.";
        }
        else if (seconds <= 15)
        {
            IntervalProfile = "Monitorizare echilibrata";
            ValidationMessage = "Compromis bun intre reactie rapida si volum de stocare.";
        }
        else if (seconds <= 60)
        {
            IntervalProfile = "Monitorizare cu consum redus";
            ValidationMessage = "Volum mai mic de scrieri, dar sesiunile scurte pot parea mai putin precise.";
        }
        else
        {
            IntervalProfile = "Monitorizare grosiera";
            ValidationMessage = "Potrivita doar daca vrei tendinte generale, nu interventii stricte.";
        }

        IntervalImpact = $"{samplesPerHour:0} esantioane/ora | ~{samplesPerDay:0} esantioane/zi";
        MonitoringSummary = $"Interval curent: {seconds}s | verificarile de interventie ruleaza in acelasi ritm.";
    }

    private void RefreshDiagnostics()
    {
        UpdateIntervalPreview();

        var categories = _db.GetAllCategories().Count();
        var applications = _db.GetAllApplications().Count();
        var thresholds = _db.GetAllThresholds().Where(threshold => threshold != null).Select(threshold => threshold!).ToList();
        var activeThresholds = thresholds.Count(threshold => threshold.Active);
        var browserEvents = _db.GetAllBrowserActivity().Count();
        var interventions = _db.GetInterventionsForUser(DefaultUserId).Count();
        var trackedSessions = _db.GetSessionsForUser(DefaultUserId).Count();

        ThresholdCoverage = $"{activeThresholds} praguri active din {thresholds.Count} limite configurate";
        ActivityCoverage = $"{applications} aplicatii monitorizate, {trackedSessions} sesiuni capturate, {categories} categorii disponibile";
        InterventionCoverage = $"{interventions} interventii inregistrate pentru utilizatorul curent";
        BrowserCoverage = $"{browserEvents} evenimente browser stocate in MySQL";
    }

    private bool TryParseInterval(out int seconds, out string error)
    {
        if (!int.TryParse(RefreshIntervalSeconds, out seconds))
        {
            error = "Intervalul de reimprospatare trebuie sa fie un numar intreg de secunde.";
            return false;
        }

        if (seconds < 1 || seconds > 600)
        {
            error = "Intervalul de reimprospatare trebuie sa fie intre 1 si 600 de secunde.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
