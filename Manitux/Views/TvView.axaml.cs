using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Manitux.Views;

public partial class TvView : UserControl
{
    public TvView()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Dispatcher.UIThread.Post(FocusFirstTvButton, DispatcherPriority.Background);
    }

    private void FocusFirstTvButton()
    {
        var firstButton = this.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => button.IsEffectivelyVisible && button.Focusable);

        firstButton?.Focus();
    }
}
