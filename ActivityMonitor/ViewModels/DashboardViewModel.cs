using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using BusinessLogic;

namespace ActivityMonitor.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private string _totalUsage;
    public string TotalUsage
    {
        get => _totalUsage;
        set => SetProperty(ref _totalUsage, value);
    }

    private string _focusScore;
    public string FocusScore
    {
        get => _focusScore;
        set => SetProperty(ref _focusScore, value);
    }

    private string _topApplication;
    public string TopApplication
    {
        get => _topApplication;
        set => SetProperty(ref _topApplication, value);
    }

    public ObservableCollection<string> Recommendations { get; set; } =
        new()
        {
            "Try enabling Focus Mode during work periods.",
            "You spent most time in productivity apps today.",
            "Consider scheduling short breaks to maintain focus."
        };

    public DashboardViewModel()
    {

        TotalUsage = "4h 38m";
        FocusScore = "78%";
        TopApplication = "monitor";
    }
}