using System;
using System.IO;
using System.IO.Pipes;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Backend;

namespace ActivityMonitor.Views;

public partial class Navbar : UserControl
{
    public Navbar()
    {
        InitializeComponent();
    }

    private async void Button_OnClick(object? sender, RoutedEventArgs e)
    {

    }

}