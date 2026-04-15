using ActivityMonitor.ViewModels;
using Avalonia.Controls;

namespace ActivityMonitor.Views;

public partial class CategoriesView : UserControl
{
    public CategoriesView()
    {
        InitializeComponent();
        DataContext = new CategoriesViewModel();
    }
}
