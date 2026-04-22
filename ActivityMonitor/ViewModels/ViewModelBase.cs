using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActivityMonitor.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    protected void ShowSuccessToast(string message) => ShowToastOnMainWindow(message, "success");
    protected void ShowErrorToast(string message) => ShowToastOnMainWindow(message, "error");
    protected void ShowWarningToast(string message) => ShowToastOnMainWindow(message, "warning");
    protected void ShowInfoToast(string message) => ShowToastOnMainWindow(message, "info");

    private void ShowToastOnMainWindow(string message, string type)
    {
        var app = Application.Current;
        if (app?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop && desktop.MainWindow is Views.MainWindow mainWindow)
        {
            mainWindow.ToastMessage = message;
            mainWindow.ToastType = type;
            mainWindow.IsToastVisible = true;

            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(4000) };
            timer.Tick += (_, _) =>
            {
                mainWindow.IsToastVisible = false;
                timer.Stop();
            };
            timer.Start();
        }
    }
}