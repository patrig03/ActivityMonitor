using ActivityMonitor.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ActivityMonitor.Views;

public partial class DashboardContent : UserControl
{
    public DashboardContent()
    {
        InitializeComponent();
        DataContext = new DatabaseViewModel();
    }
}
