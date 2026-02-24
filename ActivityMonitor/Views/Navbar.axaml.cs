using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ActivityMonitor.Views;

public partial class Navbar : UserControl
{
    public Navbar()
    {
        InitializeComponent();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn) return;

        var main = this.GetVisualRoot() as MainWindow;
        if (main == null) return;
        
        switch (btn.Content?.ToString())
        {
            case "Dashboard":
                main.CurrentView = MainWindow.ActiveView.Dashboard;
                break;
            case "Reports":
                main.CurrentView = MainWindow.ActiveView.Reports;
                break;
            case "Settings":
                main.CurrentView = MainWindow.ActiveView.Settings;
                break;
        }
    }

}