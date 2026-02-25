using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;

namespace ActivityMonitor.Views;

public partial class Navbar : UserControl
{

    private readonly List<Button> _navButtons = new();

    public Navbar()
    {
        InitializeComponent();
        
        var navStack = this.FindControl<StackPanel>("NavItems");
        if (navStack == null) { return; }
        
        foreach (var child in navStack.Children)
        {
            if (child is Button btn && btn.Classes.Contains("nav-button"))
            {
                _navButtons.Add(btn);
            }
        }
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button clicked) return;

        var main = this.GetVisualRoot() as MainWindow;
        if (main == null) return;
        
        foreach (var btn in _navButtons)
        {
            btn.Classes.Remove("nav-button--active");
        }
        clicked.Classes.Add("nav-button--active");

        main.CurrentView = clicked.Content?.ToString() switch
        {
            "Dashboard" => MainWindow.ActiveView.Dashboard,
            "Reports"   => MainWindow.ActiveView.Reports,
            "Settings"  => MainWindow.ActiveView.Settings,
            _           => throw new Exception("Invalid navbar button clicked!")
        };
    }

}