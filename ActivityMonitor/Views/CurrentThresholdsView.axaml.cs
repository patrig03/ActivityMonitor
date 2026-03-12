using ActivityMonitor.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ActivityMonitor.Views;

public partial class CurrentThresholdsView : UserControl
{
    public CurrentThresholdsView()
    {
        InitializeComponent();
        DataContext = new InterventionsViewModel();
    }
}