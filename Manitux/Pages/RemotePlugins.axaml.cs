using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Manitux.Core.Services.Plugins;

namespace Manitux.Pages;

public partial class RemotePlugins : UserControl
{
    public RemotePlugins()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(FocusInitialTarget);
    }

    private void FocusInitialTarget()
    {
        var target = this
            .GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control =>
                control.Focusable &&
                control.IsEnabled &&
                control.IsEffectivelyVisible &&
                control.Bounds.Width > 0 &&
                control.Bounds.Height > 0);

        target?.Focus(NavigationMethod.Tab);
    }

    private void PluginToggle_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is not ToggleButton toggle ||
            toggle.DataContext is not RemotePluginManifest plugin)
        {
            return;
        }

        if (!plugin.IsInstalled)
        {
            plugin.IsEnabled = false;
            toggle.IsChecked = false;
            e.Handled = true;
            return;
        }

        plugin.IsEnabled = toggle.IsChecked == true;
    }
}
