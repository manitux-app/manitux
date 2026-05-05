using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.LogicalTree;
using Avalonia.Markup.Xaml;
using LibMPVSharp;
using Manitux.Core.Models;
using Manitux.ViewModels;
using Ursa.Controls;

namespace Manitux.Player;

public partial class PlayerView : UserControl, IDisposable
{
    private PlayerViewModel? _viewModel;
    public PlayerView()
    {
        InitializeComponent();

        DataContextChanged += VM_DataContextChanged;
    }

    private void VM_DataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as PlayerViewModel;

        if (_viewModel is not null)
        {
            _viewModel.OnRequestClose -= CloseView;
            _viewModel.OnRequestClose += CloseView;
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _viewModel = DataContext as PlayerViewModel;
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }
    private void CloseView()
    {
        if (this.FindLogicalAncestorOfType<DialogControlBase>() is { } dialog) dialog.Close();
    }

    protected bool disposed = false;
    protected virtual void Dispose(bool disposing)
    {
        if (!this.disposed && disposing)
        {
            Debug.WriteLine("PlayerView Disposing");
            if (_viewModel is not null)
            {
                _viewModel.Dispose();
                Debug.WriteLine("PlayerView Dispose");
            }
        }

        this.disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
        GC.Collect();
    }
}