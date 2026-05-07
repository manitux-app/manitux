using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Manitux.ViewModels;
using Ursa.Controls;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Manitux.Pages;

public partial class DialogPage : UserControl
{
    private DialogPageViewModel? _viewModel;

    public DialogPage()
    {
        InitializeComponent();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewModel = this.DataContext as DialogPageViewModel;
        var visualLayerManager = this.FindAncestorOfType<VisualLayerManager>();
        if (_viewModel == null) return;
        _viewModel.NotificationManager =
            WindowNotificationManager.TryGetNotificationManager(visualLayerManager, out var notificationManager)
                ? notificationManager
                : new WindowNotificationManager(visualLayerManager) { MaxItems = 3 };
        _viewModel.ToastManager = WindowToastManager.TryGetToastManager(visualLayerManager, out var toastManager)
            ? toastManager
            : new WindowToastManager(visualLayerManager) { MaxItems = 3 };
        Debug.Assert(WindowNotificationManager.TryGetNotificationManager(visualLayerManager, out _));
    }
}