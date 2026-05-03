using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.VisualTree;
using Irihi.Avalonia.Shared.Contracts;
using Manitux.ViewModels;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Manitux;

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

        var dialog = this.FindLogicalAncestorOfType<DialogControlBase>();
        var topLevel = TopLevel.GetTopLevel(dialog);
        if (topLevel is null || _viewModel is null)
        {
            Debug.WriteLine("toplevel null");
            return;
        }

        //Debug.WriteLine("toplevel: " + topLevel.Width);

        _viewModel.NotificationManager = WindowNotificationManager.TryGetNotificationManager(topLevel, out var notificationManager)
            ? notificationManager
            : new WindowNotificationManager(topLevel);

        _viewModel.ToastManager = WindowToastManager.TryGetToastManager(this, out var toastManager)
           ? toastManager
           : new WindowToastManager(topLevel);
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
}