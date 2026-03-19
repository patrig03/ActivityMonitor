using Avalonia;
using Avalonia.Controls;

namespace ActivityMonitor.Views;

public partial class MainWindow : Window
{
    public enum ActiveView { Dashboard, Reports, Browser, Interventions, Settings }

    // A styled (dependency) property so bindings get notified.
    public static readonly StyledProperty<ActiveView> CurrentViewProperty =
        AvaloniaProperty.Register<MainWindow, ActiveView>(
            nameof(CurrentView),
            defaultValue: ActiveView.Dashboard);

    public ActiveView CurrentView
    {
        get => GetValue(CurrentViewProperty);
        set => SetValue(CurrentViewProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
    }
}
