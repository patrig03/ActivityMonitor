using ActivityMonitor.ViewModels;
using Avalonia.Controls;

namespace ActivityMonitor.Views;

public partial class ReportsView : UserControl
{
    public ReportsView()
    {
        InitializeComponent();
        DataContext = new ReportsViewModel();
    }
}