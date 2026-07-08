using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using Avalonia.Interactivity;
using Avalonia.Threading;
using LibMPVSharp;
using Manitux.Core.Models;
using Manitux.ViewModels;
using Microsoft.VisualBasic;
using Ursa.Controls;

namespace Manitux.Player;

public partial class PlayerView : UserControl, IDisposable
{
    private PlayerViewModel? _viewModel;
    private PlayerViewModel? _vm;
    private bool _isAttachedToVisualTree;
    private TopLevel? _topLevel;
    protected bool disposed = false;

    public PlayerView()
    {
        InitializeComponent();
        DataContextChanged += VM_DataContextChanged;
        AddHandler(KeyDownEvent, OnPlayerKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
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

             _viewModel.OnErrorClose -= ErrorView;
            _viewModel.OnErrorClose += ErrorView;

            _viewModel.OnAddSubtitleRequested -= AddSubtitle;
            _viewModel.OnAddSubtitleRequested += AddSubtitle;

            _vm = _viewModel;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttachedToVisualTree = true;
        base.OnAttachedToVisualTree(e);
        _topLevel = TopLevel.GetTopLevel(this);
        _topLevel?.AddHandler(KeyDownEvent, OnTopLevelKeyDown, RoutingStrategies.Tunnel, handledEventsToo: true);
        Dispatcher.UIThread.Post(() => MPView?.FocusForRemote(), DispatcherPriority.Background);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _isAttachedToVisualTree = false;
        _topLevel?.RemoveHandler(KeyDownEvent, OnTopLevelKeyDown);
        _topLevel = null;
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

    private void ErrorView(string message)
    {
        Debug.WriteLine(message);
        if (this.FindLogicalAncestorOfType<DialogControlBase>() is { } dialog)
        {
            dialog.Close();
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed && disposing)
        {
            RemoveHandler(KeyDownEvent, OnPlayerKeyDown);
            DataContextChanged -= VM_DataContextChanged;

            if (_vm is not null)
            {
                _vm.OnRequestClose -= CloseView;
                _vm.OnErrorClose -= ErrorView;
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

    private void OnPlayerKeyDown(object? sender, KeyEventArgs e)
    {
        MPView?.HandleRemoteKeyDown(e);
    }

    private void OnTopLevelKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Handled || IsEventFromPlayer(e.Source))
        {
            return;
        }

        MPView?.HandleRemoteKeyDown(e);
    }

    private bool IsEventFromPlayer(object? source)
    {
        for (var control = source as Control; control is not null; control = control.Parent as Control)
        {
            if (ReferenceEquals(control, this))
            {
                return true;
            }
        }

        return false;
    }
}
