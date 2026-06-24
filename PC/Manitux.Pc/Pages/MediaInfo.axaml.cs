using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Irihi.Avalonia.Shared.Contracts;
using Manitux.ViewModels;
using Ursa.Controls;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Manitux.Pages;

public partial class MediaInfo : UserControl
{
    private MediaInfoViewModel? _viewModel;

    public MediaInfo()
    {
        InitializeComponent();

        DataContextChanged += VM_DataContextChanged;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewModel = DataContext as MediaInfoViewModel;
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

    private void VM_DataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as MediaInfoViewModel;

        if (_viewModel is not null)
        {
            _viewModel.OnDataRefreshed -= ResetScrollPosition;
            _viewModel.OnDataRefreshed += ResetScrollPosition;

            _viewModel.OnRequestClose -= CloseView;
            _viewModel.OnRequestClose += CloseView;
        }
    }

    private void ResetScrollPosition()
    {
        //var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
        if (MainScrollViewer != null)
        {
            MainScrollViewer.Offset = new Avalonia.Vector(0, 0);
            //Debug.WriteLine("ScrollViewer Ok");
        }
    }

    private void CloseView()
    {
        if (this.FindLogicalAncestorOfType<DialogControlBase>() is { } dialog) dialog.Close();
    }

    public void Close()
    {
        throw new NotImplementedException();
    }
}
