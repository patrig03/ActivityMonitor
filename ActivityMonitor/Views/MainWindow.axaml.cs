using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ActivityMonitor.Views;

public partial class MainWindow : Window
{
    public enum ActiveView { Dashboard, Reports, Browser, Interventions, Devices, Categories, Settings }

    public static readonly StyledProperty<ActiveView> CurrentViewProperty =
        AvaloniaProperty.Register<MainWindow, ActiveView>(
            nameof(CurrentView),
            defaultValue: ActiveView.Dashboard);

    public static readonly StyledProperty<string> ToastMessageProperty =
        AvaloniaProperty.Register<MainWindow, string>(
            nameof(ToastMessage),
            defaultValue: string.Empty);

    public static readonly StyledProperty<bool> IsToastVisibleProperty =
        AvaloniaProperty.Register<MainWindow, bool>(
            nameof(IsToastVisible),
            defaultValue: false);

    public static readonly StyledProperty<string> ToastTypeProperty =
        AvaloniaProperty.Register<MainWindow, string>(
            nameof(ToastType),
            defaultValue: "info");

    public static readonly IValueConverter ToastBackgroundConverter = new FuncValueConverter<string, IBrush>(type => type switch
    {
        "success" => new SolidColorBrush(Color.Parse("#2ECC71")),
        "error" => new SolidColorBrush(Color.Parse("#E74C3C")),
        "warning" => new SolidColorBrush(Color.Parse("#F39C12")),
        _ => new SolidColorBrush(Color.Parse("#3498DB"))
    });

    public ActiveView CurrentView
    {
        get => GetValue(CurrentViewProperty);
        set => SetValue(CurrentViewProperty, value);
    }

    public string ToastMessage
    {
        get => GetValue(ToastMessageProperty);
        set => SetValue(ToastMessageProperty, value);
    }

    public bool IsToastVisible
    {
        get => GetValue(IsToastVisibleProperty);
        set => SetValue(IsToastVisibleProperty, value);
    }

    public string ToastType
    {
        get => GetValue(ToastTypeProperty);
        set => SetValue(ToastTypeProperty, value);
    }

    public MainWindow()
    {
        InitializeComponent();
    }
}
