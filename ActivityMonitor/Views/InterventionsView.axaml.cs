using ActivityMonitor.ViewModels;
using Avalonia.Controls;

namespace ActivityMonitor.Views;

public partial class InterventionsView : UserControl
{
    public InterventionsView()
    {
        InitializeComponent();
        DataContext = new InterventionsViewModel();
        Loaded += (_, _) => (DataContext as InterventionsViewModel)?.StartTimer();
        Unloaded += (_, _) => (DataContext as InterventionsViewModel)?.StopTimer();
    }
}
