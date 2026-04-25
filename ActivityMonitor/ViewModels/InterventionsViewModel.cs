using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Models;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public partial class InterventionsViewModel : ViewModelBase
{
    private readonly DatabaseManager _manager;
    private ThresholdEditData _editData = new();
    private Threshold? _observedEditThreshold;
    private bool _isScrollViewerVisible;
    private string _thresholdStatus = "Se încarcă pragurile";
    private string _activeThresholdCount = "0";
    private string _inactiveThresholdCount = "0";
    private string _categoryCoverage = "0";
    private string _recentAlertCount = "0";
    private string _snoozedAlertCount = "0";
    private string _mostTriggeredTarget = "Nu există încă intervenții";

    public ObservableCollection<ThresholdRow> ThresholdRows { get; } = new();
    public ObservableCollection<InterventionHistoryRow> InterventionHistory { get; } = new();
    public ObservableCollection<InterventionHistoryRow> RecentAlerts { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<ApplicationRecord> Apps { get; } = new();
    public ObservableCollection<string> InterventionTypes { get; } = new();
    public ObservableCollection<string> LimitTypes { get; } = new();
    public ObservableCollection<string> TargetTypes { get; } = new();

    public ThresholdEditData EditData
    {
        get => _editData;
        set
        {
            if (_editData == value)
            {
                return;
            }

            DetachEditDataHandlers(_editData);
            SetProperty(ref _editData, value);
            AttachEditDataHandlers(_editData);
            OnPropertyChanged(nameof(ThresholdFormTitle));
        }
    }

    public bool IsScrollViewerVisible
    {
        get => _isScrollViewerVisible;
        set => SetProperty(ref _isScrollViewerVisible, value);
    }

    public string ThresholdStatus
    {
        get => _thresholdStatus;
        set => SetProperty(ref _thresholdStatus, value);
    }

    public string ActiveThresholdCount
    {
        get => _activeThresholdCount;
        set => SetProperty(ref _activeThresholdCount, value);
    }

    public string InactiveThresholdCount
    {
        get => _inactiveThresholdCount;
        set => SetProperty(ref _inactiveThresholdCount, value);
    }

    public string CategoryCoverage
    {
        get => _categoryCoverage;
        set => SetProperty(ref _categoryCoverage, value);
    }

    public string RecentAlertCount
    {
        get => _recentAlertCount;
        set => SetProperty(ref _recentAlertCount, value);
    }

    public string SnoozedAlertCount
    {
        get => _snoozedAlertCount;
        set => SetProperty(ref _snoozedAlertCount, value);
    }

    public string MostTriggeredTarget
    {
        get => _mostTriggeredTarget;
        set => SetProperty(ref _mostTriggeredTarget, value);
    }

    public string ThresholdFormTitle => EditData.Threshold.Id > 0 ? "Editeaza pragul" : "Adauga prag";

    public ICommand AddCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand EditRowCommand { get; }
    public ICommand DeleteCommand { get; }
    public ICommand RefreshCommand { get; }

    public InterventionsViewModel()
    {
        _manager = new DatabaseManager(Settings.DatabaseConnectionString);

        InterventionTypes.Add(Threshold.NotificationInterventionType);
        InterventionTypes.Add(Threshold.TypingLockInterventionType);
        InterventionTypes.Add(Threshold.TimedLockInterventionType);

        LimitTypes.Add(Threshold.DailyLimitType);
        LimitTypes.Add(Threshold.SessionLimitType);

        TargetTypes.Add(Threshold.CategoryTargetType);
        TargetTypes.Add(Threshold.AppTargetType);

        AddCommand = new RelayCommand(_ => AddThreshold());
        SaveCommand = new RelayCommand(_ => SaveThreshold());
        CancelCommand = new RelayCommand(_ => CancelEditing());
        EditRowCommand = new RelayCommand(EditThreshold);
        DeleteCommand = new RelayCommand(DeleteThreshold);
        RefreshCommand = new RelayCommand(_ => RefreshData());

        AttachEditDataHandlers(EditData);
        RefreshData();
        ResetEditData();
    }

    private void RefreshData()
    {
        LoadCategories();
        LoadApps();
        RefreshCollections();
    }
}
