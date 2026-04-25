using System;
using System.Collections.Generic;
using ActivityMonitor.Services;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace ActivityMonitor.Views;

public partial class Navbar : UserControl
{
    private readonly List<Button> _navButtons = new();
    private BackendProcessController? _backendProcessController;
    private DispatcherTimer? _statusTimer;
    private Ellipse? _statusIndicator;
    private TextBlock? _statusText;
    private TextBlock? _statusUpdateTime;
    private TextBlock? _statusActionMessage;
    private Button? _startBackendButton;
    private bool? _isBackendRunning;
    private DateTime _lastStatusChange = DateTime.Now;

    public Navbar()
    {
        InitializeComponent();
        _backendProcessController = (Application.Current as App)?.BackendProcessController;

        var navStack = this.FindControl<StackPanel>("NavItems");
        if (navStack == null) { return; }

        foreach (var child in navStack.Children)
        {
            if (child is Button btn && btn.Classes.Contains("nav-button"))
            {
                _navButtons.Add(btn);
            }
        }

        if (this.FindControl<Button>("DashboardButton") is { } dashboardButton)
        {
            dashboardButton.Classes.Add("nav-button--active");
        }

        InitializeStatusMonitoring();
    }

    private void InitializeStatusMonitoring()
    {
        _statusIndicator = this.Find<Ellipse>("StatusIndicator");
        _statusText = this.Find<TextBlock>("StatusText");
        _statusUpdateTime = this.Find<TextBlock>("StatusUpdateTime");
        _statusActionMessage = this.Find<TextBlock>("StatusActionMessage");
        _startBackendButton = this.Find<Button>("StartBackendButton");

        _statusTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(2)
        };
        _statusTimer.Tick += UpdateStatus;
        _statusTimer.Start();

        UpdateStatus(null, EventArgs.Empty);
    }

    private void UpdateStatus(object? sender, EventArgs e)
    {
        bool isRunning = _backendProcessController?.IsRunning() ?? false;

        if (_isBackendRunning != isRunning)
        {
            _lastStatusChange = DateTime.Now;
            _isBackendRunning = isRunning;
        }

        if (_statusIndicator != null)
        {
            _statusIndicator.Fill = isRunning
                ? new SolidColorBrush(Color.Parse("#6DFF9C"))
                : new SolidColorBrush(Color.Parse("#FF6D6D"));
        }

        if (_statusText != null)
        {
            _statusText.Text = isRunning ? "Monitorizare activa" : "Serviciu oprit";
        }

        if (_statusUpdateTime != null)
        {
            var elapsed = DateTime.Now - _lastStatusChange;
            string timeText;

            if (elapsed.TotalMinutes < 1)
                timeText = "acum câteva secunde";
            else if (elapsed.TotalMinutes < 60)
                timeText = $"acum {(int)elapsed.TotalMinutes} min";
            else if (elapsed.TotalHours < 24)
                timeText = $"acum {(int)elapsed.TotalHours}h {(int)elapsed.Minutes}m";
            else
                timeText = $"acum {(int)elapsed.TotalDays}d";

            _statusUpdateTime.Text = $"Ultima actualizare: {timeText}";
        }

        if (_startBackendButton != null)
        {
            _startBackendButton.Content = isRunning ? "Oprește" : "Pornește";
        }

    }

    private void BackendStatus_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_backendProcessController?.IsRunning() == true)
        {
            var error = _backendProcessController?.Stop();
            ShowActionMessage(error, isError: error != null);
            UpdateStatus(null, EventArgs.Empty);
        }
        else
        {
            var error = _backendProcessController?.Start();
            ShowActionMessage(error, isError: error != null);
            UpdateStatus(null, EventArgs.Empty);
        }
    }

    private void ShowActionMessage(string? message, bool isError)
    {
        if (_statusActionMessage == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            _statusActionMessage.IsVisible = false;
            _statusActionMessage.Text = string.Empty;
            return;
        }

        _statusActionMessage.Text = message;
        _statusActionMessage.Foreground = isError
            ? new SolidColorBrush(Color.Parse("#FFB366"))
            : new SolidColorBrush(Color.Parse("#7C879C"));
        _statusActionMessage.IsVisible = true;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _statusTimer?.Stop();
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

        main.CurrentView = clicked.Tag?.ToString() switch
        {
            "Dashboard"         => MainWindow.ActiveView.Dashboard,
            "Reports"           => MainWindow.ActiveView.Reports,
            "Browser"           => MainWindow.ActiveView.Browser,
            "Interventions"     => MainWindow.ActiveView.Interventions,
            "Devices"           => MainWindow.ActiveView.Devices,
            "Categories"        => MainWindow.ActiveView.Categories,
            "Settings"          => MainWindow.ActiveView.Settings,
            _                   => throw new Exception("Buton de navigare invalid!")
        };
    }

}
