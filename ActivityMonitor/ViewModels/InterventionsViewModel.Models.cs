using System;
using System.Windows.Input;
using Backend.Classifier.Models;
using Backend.DataCollector.Models;
using Backend.Interventions.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActivityMonitor.ViewModels;

public class ThresholdEditData : ObservableObject
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

public class ThresholdRow
{
    public Category Category { get; init; } = new() { Name = string.Empty };
    public ApplicationRecord? App { get; init; }
    public Threshold Threshold { get; init; } = new();

    public string TargetName =>
        Threshold.TargetType == Threshold.AppTargetType
            ? App?.ProcessName ?? $"App {Threshold.AppId}"
            : Category.Name;

    public string LimitSummary => $"{Threshold.LimitType}: {Threshold.Limit:hh\\:mm\\:ss}";
}

public class InterventionHistoryRow
{
    public int Id { get; init; }
    public int ThresholdId { get; init; }
    public string TargetName { get; init; } = string.Empty;
    public string TargetType { get; init; } = string.Empty;
    public string CategoryName { get; init; } = string.Empty;
    public string InterventionType { get; init; } = string.Empty;
    public string LimitSummary { get; init; } = string.Empty;
    public bool Snoozed { get; init; }
    public DateTime TriggeredAt { get; init; }
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
