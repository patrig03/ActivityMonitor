using ActivityMonitor.ViewModels;
using Avalonia.Controls;

namespace ActivityMonitor.Views;

public partial class AddThresholdView : UserControl
{
    public AddThresholdView()
    {
        InitializeComponent();
        DataContext = new InterventionsViewModel();
    }
}