using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using Database.DTO;
using Database.Manager;

namespace ActivityMonitor.ViewModels;

public class WindowCategoryDto
{
    public string WmClass { get; set; } = "";
    public string Title { get; set; } = "";
    public TimeSpan VisibleFor { get; set; }
    public DateTime LastVisible { get; set; }
    public TimeSpan ActiveFor { get; set; }
    public DateTime LastActive { get; set; }

}



public class DatabaseViewModel
{
    public ObservableCollection<WindowCategoryDto> WindowCategories { get; }
    private const string DbPath = "../../../../Backend/bin/Debug/net9.0/data/database.db";
    private DatabaseManager _manager { get; }

    private Timer? _timer;

    public DatabaseViewModel()
    {
        _manager = new DatabaseManager(DbPath);
        WindowCategories = new ObservableCollection<WindowCategoryDto>();

        _timer = new Timer(_ =>
        {
            var updatedItems = _manager.GetAllApplications();

            Dispatcher.UIThread.Post(() =>
            {
                WindowCategories.Clear();

                foreach (var app in updatedItems)
                {
                
                    WindowCategories.Add(new WindowCategoryDto
                    {
                        WmClass = app.ClassName,
                        Title = app.WindowTitle,
                    });
                }
            });

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }
}