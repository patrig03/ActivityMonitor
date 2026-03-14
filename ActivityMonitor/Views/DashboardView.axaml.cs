using ActivityMonitor.ViewModels;
using Avalonia.Controls;

namespace ActivityMonitor.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        DataContext = new DashboardViewModel();
    }
}