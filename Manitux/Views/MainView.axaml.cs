using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Manitux.ViewModels;

namespace Manitux.Views;

public partial class MainView : UserControl
{
    //private MainViewModel? _viewModel;

    public MainView()
    {
        InitializeComponent();
        Focusable = true;
        KeyDown += OnKeyDown;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested -= OnBackRequested;
            topLevel.BackRequested += OnBackRequested;
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested -= OnBackRequested;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (e.Key is Key.Escape or Key.Back or Key.BrowserBack && viewModel.GoBackCommand.CanExecute(null))
        {
            viewModel.GoBackCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.GoBackCommand.CanExecute(null))
        {
            viewModel.GoBackCommand.Execute(null);
            e.Handled = true;
        }
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
