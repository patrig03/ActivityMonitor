using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Backend.Classifier.Models;
using Backend.Interventions.Models;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class ViewData
{
    public Category Category { get; set; } = new();
    public Threshold Threshold { get; set; } = new();
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

public class InterventionsViewModel : INotifyPropertyChanged
{
    public ObservableCollection<ViewData> Data { get; set; }

    private ViewData _editData = new();
    public ViewData EditData
    {
        get => _editData;
        set
        {
            if (_editData == value) return;
            _editData = value;
            OnPropertyChanged();
        }
    }

    public ICommand AddCommand { get; }
    public ICommand EditRowCommand { get; }

    private DatabaseManager Manager { get; }

    private bool _isScrollViewerVisible;
    public bool IsScrollViewerVisible
    {
        get => _isScrollViewerVisible;
        set
        {
            if (_isScrollViewerVisible == value) return;
            _isScrollViewerVisible = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public InterventionsViewModel()
    {
        Manager = new(GetDatabasePath());
        Data = new();
        IsScrollViewerVisible = false;

        AddCommand = new RelayCommand(_ => AddThreshold());
        EditRowCommand = new RelayCommand(EditCommand);

        var thresholdDtos = Manager.GetAllThresholds();

        foreach (var thresholdDto in thresholdDtos)
        {
            if (thresholdDto == null) { continue; }

            var categoryDto = Manager.GetCategory(thresholdDto.CategoryId);
            if (categoryDto == null) { continue; }

            var data = new ViewData
            {
                Category = Category.FromDto(categoryDto),
                Threshold = Threshold.FromDto(thresholdDto),
            };

            Data.Add(data);
        }
    }

    private void AddThreshold()
    {
        EditData = new ViewData();
        IsScrollViewerVisible = true;
    }

    public void SaveCommand()
    {
        Manager.InsertThreshold(EditData.Threshold.ToDto());
    }

    public void DeleteCommand()
    {
    }

    public void EditCommand(object? parameter)
    {
        if (parameter is not ViewData row) return;

        EditData = row;
        IsScrollViewerVisible = true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private static string GetDatabasePath()
    {
        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        appDataPath = Path.Combine(appDataPath, "ActivityMonitor");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, "database.db");
    }
}