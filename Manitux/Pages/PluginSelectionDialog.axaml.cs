using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Manitux.ViewModels;
using Ursa.Controls;

namespace Manitux.Pages;

public partial class PluginSelectionDialog : UserControl
{
    public PluginSelectionDialog()
    {
        InitializeComponent();
        KeyDown += OnKeyDown;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(FocusSelectedPlugin);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Escape or Key.Back or Key.BrowserBack))
        {
            return;
        }

        if (this.FindLogicalAncestorOfType<DialogControlBase>() is { } dialog)
        {
            dialog.Close();
            e.Handled = true;
        }
    }

    private void FocusSelectedPlugin()
    {
        var pluginButtons = this
            .GetVisualDescendants()
            .OfType<Button>()
            .Where(IsPluginButton)
            .ToList();

        var selectedButton = pluginButtons.FirstOrDefault(button =>
            button.DataContext is PluginSelectionItemViewModel { IsSelected: true });

        (selectedButton ?? pluginButtons.FirstOrDefault())?.Focus(NavigationMethod.Tab);
    }

    private static bool IsPluginButton(Button button)
    {
        return button.Classes.Contains("nav") &&
               button.IsEnabled &&
               button.IsEffectivelyVisible &&
               button.Bounds.Width > 0 &&
               button.Bounds.Height > 0;
    }
}
