using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace ActivityMonitor.ViewModels;

public partial class MainViewModel : ObservableObject
{
    // Total usage time today (formatted string, e.g., "5h 24m")
    [ObservableProperty]
    private string _totalUsage = "0h 00m";

    // Focus score (0–100 or % string)
    [ObservableProperty]
    private string _focusScore = "0%";

    // Dominant app used today
    [ObservableProperty]
    private string _topApplication = "—";

    // Adaptive Insights / Recommendations
    public ObservableCollection<string> Recommendations { get; set; } =
        new()
        {
            "Try enabling Focus Mode during work periods.",
            "You spent most time in productivity apps today.",
            "Consider scheduling short breaks to maintain focus."
        };

    public MainViewModel()
    {
        // Example simulated initial data
        TotalUsage = "4h 38m";
        FocusScore = "78%";
        TopApplication = "Visual Studio Code";
    }
}