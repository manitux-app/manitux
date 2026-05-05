//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;
//using System.Runtime.InteropServices;
//using Avalonia;
//using Avalonia.Controls;
//using Avalonia.LogicalTree;
//using Avalonia.Markup.Xaml;
//using Avalonia.Threading;
//using LibMPVSharp;
//using Manitux.Core.Models;
//using Manitux.ViewModels;
//using Ursa.Controls;

//namespace Manitux.Player;

//public partial class PlayerViewIssue : UserControl, IDisposable
//{
//    private PlayerViewModel? _viewModel;
//    protected bool disposed = false;

//    public PlayerViewIssue()
//    {
//        InitializeComponent();

//        DataContextChanged += VM_DataContextChanged;

//        // Visual Tree'den kaldýrýlma aný için en kararlý event
//        this.Unloaded += (s, e) =>
//        {
//            Debug.WriteLine("PlayerView Unloaded");

//            // UI Thread'i bloke etmemek için iþlemi arka plana atýyoruz
//            Dispatcher.UIThread.Post(() =>
//            {
//                CleanUpAndDispose();
//            });
//        };
//    }

//    private void VM_DataContextChanged(object? sender, EventArgs e)
//    {
//        Debug.WriteLine($"VM_DataContextChanged sender: {sender.ToString()} e: {e.ToString()}");

//        UnsubscribeFromViewModel();

//        _viewModel = DataContext as PlayerViewModel;

//        if (_viewModel is not null)
//        {
//            Debug.WriteLine("PlayerView VM_DataContextChanged");
//            _viewModel.OnAddSubtitleRequested += AddSubtitle;
//            _viewModel.OnRequestClose += CloseView;
//        }
//    }

//    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
//    {
//        base.OnDetachedFromVisualTree(e);
//        Debug.WriteLine("PlayerView OnDetachedFromVisualTree");

//        Dispatcher.UIThread.Post(() => CleanUpAndDispose());
//    }

//    private void CleanUpAndDispose()
//    {
//        if (disposed) return;

//        Debug.WriteLine("CleanUpAndDispose Started");

//        if (_viewModel is not null)
//        {
//            // Event aboneliklerini kaldýrarak Garbage Collector'ün önünü açýn
//            UnsubscribeFromViewModel();

//            try
//            {
//                _viewModel.Dispose();
//            }
//            catch (Exception ex)
//            {
//                Debug.WriteLine($"VM Dispose Error: {ex.Message}");
//            }
//            finally
//            {
//                _viewModel = null;
//            }
//        }

//        DataContextChanged -= VM_DataContextChanged;
//        disposed = true;
//    }

//    private void AddSubtitle(List<SubtitleModel> subtitles)
//    {
//        var playerView = this.FindControl<MediaPlayerView>("MPView");
//        playerView?.AddSubtitles(subtitles);
//    }

//    private void CloseView()
//    {
//        if (this.FindLogicalAncestorOfType<DialogControlBase>() is { } dialog)
//            dialog.Close();
//    }

//    private void UnsubscribeFromViewModel()
//    {
//        if (_viewModel is not null)
//        {
//            _viewModel.OnAddSubtitleRequested -= AddSubtitle;
//            _viewModel.OnRequestClose -= CloseView;
//        }
//    }

//    public void Dispose()
//    {
//        CleanUpAndDispose();
//        GC.SuppressFinalize(this);
//    }
//}

