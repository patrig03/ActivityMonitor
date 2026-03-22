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
    private string _saveStatus = "Loading settings...";
    private string _validationMessage = "Balanced cadence for most desktops.";
    private string _intervalProfile = "Balanced monitoring";
    private string _intervalImpact = "360 samples/hour | ~8,640 samples/day";
    private string _databasePath = Settings.DatabaseEndpoint;
    private string _databaseStatus = "MySQL endpoint configured";
    private string _serviceMutexName = Settings.MutexName;
    private string _thresholdCoverage = "--";
    private string _activityCoverage = "--";
    private string _interventionCoverage = "--";
    private string _browserCoverage = "--";
    private string _monitoringSummary = "--";
    private string _lastSavedLabel = "Not saved yet";

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
                SaveStatus = "Unsaved changes";
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
        DatabaseStatus = "MySQL schema will be created automatically when the connection succeeds";
        ServiceMutexName = Settings.MutexName;
        LastSavedLabel = dto == null ? "Using defaults" : "Loaded from MySQL";
        SaveStatus = "Settings loaded";

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

        SaveStatus = $"Saved at {DateTime.Now:HH:mm}";
        LastSavedLabel = "Persisted in MySQL settings table";
        RefreshDiagnostics();
    }

    private void ResetToDefaults()
    {
        RefreshIntervalSeconds = DefaultIntervalSeconds.ToString();
        ValidationMessage = "Default cadence restored. Save to persist it.";
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
            IntervalProfile = "Invalid value";
            IntervalImpact = "Enter a whole number of seconds between 1 and 600.";
            MonitoringSummary = "Sampling summary unavailable";
            return;
        }

        var samplesPerHour = 3600 / (double)seconds;
        var samplesPerDay = 86400 / (double)seconds;

        if (seconds <= 5)
        {
            IntervalProfile = "High precision monitoring";
            ValidationMessage = "Fast capture for short task switches and strict session limits.";
        }
        else if (seconds <= 15)
        {
            IntervalProfile = "Balanced monitoring";
            ValidationMessage = "Good tradeoff between responsiveness and storage churn.";
        }
        else if (seconds <= 60)
        {
            IntervalProfile = "Low overhead monitoring";
            ValidationMessage = "Lower write volume, but short sessions may look less precise.";
        }
        else
        {
            IntervalProfile = "Coarse monitoring";
            ValidationMessage = "Suitable only if you want broad trends rather than tight interventions.";
        }

        IntervalImpact = $"{samplesPerHour:0} samples/hour | ~{samplesPerDay:0} samples/day";
        MonitoringSummary = $"Current interval: {seconds}s | intervention checks evaluate on the same cadence.";
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

        ThresholdCoverage = $"{activeThresholds} active thresholds across {thresholds.Count} configured guardrails";
        ActivityCoverage = $"{applications} tracked applications, {trackedSessions} captured sessions, {categories} categories available";
        InterventionCoverage = $"{interventions} interventions recorded for the current user";
        BrowserCoverage = $"{browserEvents} browser events stored in MySQL";
    }

    private bool TryParseInterval(out int seconds, out string error)
    {
        if (!int.TryParse(RefreshIntervalSeconds, out seconds))
        {
            error = "Refresh interval must be a whole number of seconds.";
            return false;
        }

        if (seconds < 1 || seconds > 600)
        {
            error = "Refresh interval must stay between 1 and 600 seconds.";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
