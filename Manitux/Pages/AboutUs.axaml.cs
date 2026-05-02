using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Manitux.ViewModels;

namespace Manitux.Pages;

public partial class AboutUs : UserControl
{
    public AboutUs()
    {
        InitializeComponent();
    }
    
    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (DataContext is AboutUsViewModel vm)
        {
            var launcher = TopLevel.GetTopLevel(this)?.Launcher;
            vm.Launcher = launcher;
        }
    }
}