using ActivityMonitor.ViewModels;
using Avalonia.Controls;

namespace ActivityMonitor.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContext = new SettingsViewModel();
    }
}