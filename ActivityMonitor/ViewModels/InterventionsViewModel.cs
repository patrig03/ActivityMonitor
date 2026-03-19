using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using Backend.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class ViewData : ObservableObject
{
    private Category _category = new() { Name = string.Empty };
    private Threshold _threshold = new();

    public Category Category
    {
        get => _category;
        set => SetProperty(ref _category, value);
    }

    public Threshold Threshold
    {
        get => _threshold;
        set => SetProperty(ref _threshold, value);
    }
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;

    public RelayCommand(Action<object?> execute)
    {
        _execute = execute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => _execute(parameter);
}

public class InterventionsViewModel : ViewModelBase
{
    private readonly DatabaseManager _manager;
    private ViewData _editData = new();
    private Threshold? _observedEditThreshold;
    private bool _isScrollViewerVisible;

    public ObservableCollection<ViewData> Data { get; } = new();
    public ObservableCollection<Category> Categories { get; } = new();
    public ObservableCollection<ApplicationRecord> Apps { get; } = new();
    public ObservableCollection<string> InterventionTypes { get; } = new();
    public ObservableCollection<string> LimitTypes { get; } = new();
    public ObservableCollection<string> TargetTypes { get; } = new();

    public ViewData EditData
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

    public string ThresholdFormTitle => EditData.Threshold.Id > 0 ? "Edit threshold" : "Add threshold";

    public ICommand AddCommand { get; }
    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }
    public ICommand EditRowCommand { get; }
    public ICommand DeleteCommand { get; }

    public InterventionsViewModel()
    {
        _manager = new DatabaseManager(Settings.DbPath);

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

        AttachEditDataHandlers(EditData);
        LoadCategories();
        LoadApps();
        ResetEditData();
        QueryThresholds();
    }

    private void AddThreshold()
    {
        ResetEditData();
        IsScrollViewerVisible = true;
    }

    private void SaveThreshold()
    {
        SyncDraftSelection();
        EditData.Threshold.UserId = 1;

        _manager.UpsertThreshold(EditData.Threshold.ToDto());
        QueryThresholds();
        CancelEditing();
    }

    private void DeleteThreshold(object? parameter)
    {
        if (parameter is not ViewData row)
        {
            return;
        }

        _manager.DeleteThreshold(row.Threshold.ToDto());
        QueryThresholds();

        if (EditData.Threshold.Id == row.Threshold.Id)
        {
            CancelEditing();
        }
    }

    private void EditThreshold(object? parameter)
    {
        if (parameter is not ViewData row)
        {
            return;
        }

        EditData = CreateDraft(row.Threshold.Clone());
        IsScrollViewerVisible = true;
    }

    private void CancelEditing()
    {
        IsScrollViewerVisible = false;
        ResetEditData();
    }

    private void ResetEditData()
    {
        EditData = CreateDraft();
    }

    private ViewData CreateDraft(Threshold? threshold = null)
    {
        var draft = threshold ?? new Threshold();

        if (draft.CategoryId == 0 && Categories.Count > 0)
        {
            draft.CategoryId = Categories[0].Id;
        }

        if (draft.AppId == 0 && Apps.FirstOrDefault(a => a.Id.HasValue)?.Id is int firstAppId)
        {
            draft.AppId = firstAppId;
        }

        return new ViewData
        {
            Category = Categories.FirstOrDefault(c => c.Id == draft.CategoryId) ?? new Category { Name = string.Empty },
            Threshold = draft
        };
    }

    private void LoadCategories()
    {
        Categories.Clear();

        foreach (var category in _manager.GetAllCategories().Select(Category.FromDto).OrderBy(c => c.Name))
        {
            Categories.Add(category);
        }
    }

    private void LoadApps()
    {
        Apps.Clear();

        foreach (var app in _manager.GetAllApplications()
                     .Select(ApplicationRecord.FromDto)
                     .Where(a => a.Id.HasValue && !string.IsNullOrWhiteSpace(a.ProcessName))
                     .OrderBy(a => a.ProcessName))
        {
            Apps.Add(app);
        }
    }

    private void QueryThresholds()
    {
        Data.Clear();

        foreach (var thresholdDto in _manager.GetAllThresholds())
        {
            if (thresholdDto is null)
            {
                continue;
            }

            var threshold = Threshold.FromDto(thresholdDto);
            var categoryDto = _manager.GetCategory(threshold.CategoryId);
            if (categoryDto is null)
            {
                continue;
            }

            Data.Add(new ViewData
            {
                Category = Category.FromDto(categoryDto),
                Threshold = threshold
            });
        }
    }

    private void SyncDraftSelection()
    {
        if (EditData.Threshold.TargetType == Threshold.AppTargetType)
        {
            var selectedApp = Apps.FirstOrDefault(app => app.Id == EditData.Threshold.AppId);
            if (selectedApp?.CategoryId is int appCategoryId)
            {
                EditData.Threshold.CategoryId = appCategoryId;
            }

            return;
        }

        if (EditData.Threshold.CategoryId == 0 && Categories.Count > 0)
        {
            EditData.Threshold.CategoryId = Categories[0].Id;
        }
    }

    private void EnsureDraftSelection()
    {
        if (EditData.Threshold.TargetType == Threshold.AppTargetType)
        {
            if (EditData.Threshold.AppId == 0 && Apps.FirstOrDefault(a => a.Id.HasValue)?.Id is int firstAppId)
            {
                EditData.Threshold.AppId = firstAppId;
            }

            SyncDraftSelection();
            return;
        }

        if (EditData.Threshold.CategoryId == 0 && Categories.Count > 0)
        {
            EditData.Threshold.CategoryId = Categories[0].Id;
        }
    }

    private void AttachEditDataHandlers(ViewData? editData)
    {
        if (editData is null)
        {
            return;
        }

        editData.PropertyChanged += OnEditDataChanged;
        _observedEditThreshold = editData.Threshold;
        _observedEditThreshold.PropertyChanged += OnEditThresholdChanged;
        EnsureDraftSelection();
    }

    private void DetachEditDataHandlers(ViewData? editData)
    {
        if (editData is null)
        {
            return;
        }

        editData.PropertyChanged -= OnEditDataChanged;

        if (_observedEditThreshold is not null)
        {
            _observedEditThreshold.PropertyChanged -= OnEditThresholdChanged;
            _observedEditThreshold = null;
        }
    }

    private void OnEditDataChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(ViewData.Threshold))
        {
            return;
        }

        if (_observedEditThreshold is not null)
        {
            _observedEditThreshold.PropertyChanged -= OnEditThresholdChanged;
        }

        _observedEditThreshold = EditData.Threshold;
        _observedEditThreshold.PropertyChanged += OnEditThresholdChanged;
        EnsureDraftSelection();
        OnPropertyChanged(nameof(ThresholdFormTitle));
    }

    private void OnEditThresholdChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Threshold.TargetType))
        {
            EnsureDraftSelection();
        }

        if (e.PropertyName == nameof(Threshold.Id))
        {
            OnPropertyChanged(nameof(ThresholdFormTitle));
        }
    }
}
