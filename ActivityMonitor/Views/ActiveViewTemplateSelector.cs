using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;

namespace ActivityMonitor.Views
{
    public class ActiveViewTemplateSelector : IDataTemplate
    {
        // Tell Avalonia that we want to handle objects of type ActiveView.
        public bool Match(object? data) => data is MainWindow.ActiveView;

        // Build the appropriate view for each enum value.
        public Control Build(object? data)
        {
            return (data as MainWindow.ActiveView?) switch
            {
                MainWindow.ActiveView.Dashboard => new DashboardView(),
                MainWindow.ActiveView.Reports   => new ReportsView(),
                MainWindow.ActiveView.Browser => new BrowserView(),
                MainWindow.ActiveView.Interventions => new InterventionsView(),
                MainWindow.ActiveView.Settings  => new SettingsView(),
                _                               => throw new NotImplementedException()
            };
        }
    }
}
