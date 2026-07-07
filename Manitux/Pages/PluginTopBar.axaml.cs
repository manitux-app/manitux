using Avalonia;
using Avalonia.Controls;

namespace Manitux.Pages;

public partial class PluginTopBar : UserControl
{
    public PluginTopBar()
    {
        InitializeComponent();

        if (OperatingSystem.IsAndroid())
        {
            Classes.Add("android-performance");
            TopBarChrome.Padding = new Thickness(8, 5);
            TopBarChrome.Margin = new Thickness(0, 0, 0, 8);
            TopBarGrid.ColumnSpacing = 6;
            PluginSelectorButton.MinWidth = 132;

            foreach (var button in new[] { SearchButton, SettingsButton, RefreshButton, ExtraActionButton })
            {
                button.Width = 42;
                button.Height = 42;
                button.MinWidth = 42;
                button.MinHeight = 42;
            }
        }
    }
}
