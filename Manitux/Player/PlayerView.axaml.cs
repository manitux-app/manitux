using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using LibMPVSharp;
using Manitux.Core.Models;
using Manitux.ViewModels;
using Ursa.Controls;

namespace Manitux.Player;

public partial class PlayerView : UserControl, IDisposable
{
    private PlayerViewModel? _viewModel;
    private PlayerViewModel? _vm;
    private bool _isAttachedToVisualTree;
    protected bool disposed = false;

    public PlayerView()
    {
        InitializeComponent();
        DataContextChanged += VM_DataContextChanged;
    }

    private void VM_DataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.OnRequestClose -= CloseView;
            _vm.OnAddSubtitleRequested -= AddSubtitle;
        }

        _viewModel = DataContext as PlayerViewModel;

        if (_viewModel is not null)
        {
            _viewModel.OnRequestClose -= CloseView;
            _viewModel.OnRequestClose += CloseView;

            _viewModel.OnAddSubtitleRequested -= AddSubtitle;
            _viewModel.OnAddSubtitleRequested += AddSubtitle;

            _vm = _viewModel;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttachedToVisualTree = true;
        base.OnAttachedToVisualTree(e);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttachedToVisualTree = false;
        base.OnDetachedFromVisualTree(e);

        Dispatcher.UIThread.Post(() =>
        {
            if (_isAttachedToVisualTree)
            {
                return;
            }

            Dispose();
        }, DispatcherPriority.Background);
    }

    private void AddSubtitle(List<SubtitleModel> subtitles)
    {
        var playerView = this.FindControl<MediaPlayerView>("MPView");
        playerView?.AddSubtitles(subtitles);
    }

    private void CloseView()
    {
        if (this.FindLogicalAncestorOfType<DialogControlBase>() is { } dialog)
        {
            dialog.Close();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed && disposing)
        {
            DataContextChanged -= VM_DataContextChanged;

            if (_vm is not null)
            {
                _vm.OnRequestClose -= CloseView;
                _vm.OnAddSubtitleRequested -= AddSubtitle;
                _vm.Dispose();
                _vm = null;
            }
        }

        this.disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
