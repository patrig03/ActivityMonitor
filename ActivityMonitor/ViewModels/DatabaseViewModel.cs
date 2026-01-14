using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using Avalonia.Threading;
using BusinessLogic;
using BusinessLogic.DTO;

namespace ActivityMonitor.ViewModels;

public class DatabaseViewModel
{
    public ObservableCollection<WindowDto> Windows { get; }

    private Timer? _timer;

    public DatabaseViewModel()
    {
        var items = DatabaseManager.GetAll() ?? new List<WindowDto>();
        Windows = new ObservableCollection<WindowDto>(items);

        // Start periodic refresh every 10 seconds
        _timer = new Timer(_ =>
        {
            var updatedItems = DatabaseManager.GetAll() ?? new List<WindowDto>();

            // Update collection on UI thread
            Dispatcher.UIThread.Post(() =>
            {
                Windows.Clear();
                foreach (var win in updatedItems)
                    Windows.Add(win);
            });

        }, null, TimeSpan.Zero, TimeSpan.FromSeconds(10));
    }
}