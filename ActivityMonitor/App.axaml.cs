using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using ActivityMonitor.ViewModels;
using ActivityMonitor.Views;
using ActivityMonitor.Services;
using Avalonia.Markup.Xaml.Templates;

namespace ActivityMonitor;

public partial class App : Application
{
    public BackendProcessController BackendProcessController { get; } = new();

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        DataTemplates.Add(new DataTemplate
        {
            DataType = typeof(DashboardViewModel),
            Content = typeof(DashboardView),
        });
        DataTemplates.Add(new DataTemplate
        {
            DataType = typeof(ReportsViewModel),
            Content = typeof(ReportsView)
        });
        DataTemplates.Add(new DataTemplate
        {
            DataType = typeof(BrowserViewModel),
            Content = typeof(BrowserView)
        });
        DataTemplates.Add(new DataTemplate
        {
            DataType = typeof(InterventionsViewModel),
            Content = typeof(InterventionsView)
        });
        DataTemplates.Add(new DataTemplate{
            DataType = typeof(SettingsViewModel), 
            Content = typeof(SettingsView)
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            desktop.ShutdownRequested += OnShutdownRequested;

            desktop.MainWindow = new MainWindow
            {
                DataContext = new DashboardViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            singleViewPlatform.MainView = new DashboardView
            {
                DataContext = new DashboardViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (BackendProcessController.IsRunning())
        {
            System.Diagnostics.Debug.WriteLine(
                "[App] Shutting down UI — backend service continues running in background.");
        }
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
