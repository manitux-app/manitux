using Avalonia;
using Avalonia.Controls;
using Manitux.ViewModels;

namespace Manitux.Views;

public partial class MainView : UserControl
{
    //private MainViewModel? _viewModel;

    public MainView()
    {
        InitializeComponent();
    }

    // protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    // {
    //     base.OnAttachedToVisualTree(e);
    //     _viewModel = DataContext as MainViewModel;
    //     var topLevel = TopLevel.GetTopLevel(this);
    //     if (topLevel is null || _viewModel is null)
    //         return;
    //     _viewModel.NotificationManager = WindowNotificationManager.TryGetNotificationManager(topLevel, out var manager)
    //         ? manager
    //         : new WindowNotificationManager(topLevel);
    // }
}