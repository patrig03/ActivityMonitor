using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using Backend;
using Backend.DTO;

namespace ActivityMonitor.ViewModels;

public class WindowCategoryDto
{
    public string WmClass { get; set; } = "";
    public string Title { get; set; } = "";
    public TimeSpan VisibleFor { get; set; }
    public DateTime LastVisible { get; set; }
    public TimeSpan ActiveFor { get; set; }
    public DateTime LastActive { get; set; }
    
    public string CategoryName { get; set; } = "";
    public double Confidence { get; set; }
}



public class DatabaseViewModel
{
    public ObservableCollection<WindowCategoryDto> WindowCategories { get; }

    private Timer? _timer;

    public DatabaseViewModel()
    {
        WindowCategories = new ObservableCollection<WindowCategoryDto>();

        _timer = new Timer(_ =>
        {
            var updatedItems = DatabaseManager.GetAll() ?? new List<WindowDto>();

            Dispatcher.UIThread.Post(() =>
            {
                WindowCategories.Clear();

                foreach (var win in updatedItems)
                {
                    var (category, confidence) = ActivityClassifier.Classify(win);
                
                    WindowCategories.Add(new WindowCategoryDto
                    {
                        WmClass = win.WmClass,
                        Title = win.Title,
                        VisibleFor = win.VisibleFor,
                        LastVisible = win.LastVisible,
                        ActiveFor = win.ActiveFor,
                        LastActive = win.LastActive,
                        CategoryName = category.ToString(),
                        Confidence = confidence
                    });
                }
            });

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }

}