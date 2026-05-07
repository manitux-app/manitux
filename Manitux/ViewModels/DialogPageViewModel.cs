using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using Manitux.Pages;
using Ursa.Controls;

namespace Manitux.ViewModels;

public partial class DialogPageViewModel : ViewModelBase, IDialogContext
{
    public WindowNotificationManager? NotificationManager { get; set; }
    public WindowToastManager? ToastManager { get; set; }

    public DialogPageViewModel()
    {
        Debug.WriteLine("DialogPageViewModel loaded");

        OKCommand = new RelayCommand(OK);
        CancelCommand = new RelayCommand(Cancel);
        DialogCommand = new AsyncRelayCommand(ShowDialog);

        TestMessage();
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;

    public ICommand OKCommand { get; set; }
    public ICommand CancelCommand { get; set; }
    public ICommand DialogCommand { get; set; }

    private void OK()
    {
        RequestClose?.Invoke(this, true);
    }

    private void Cancel()
    {
        RequestClose?.Invoke(this, false);
    }

    private async Task ShowDialog()
    {
        //var options = new OverlayDialogOptions()
        //{
        //    HorizontalAnchor = HorizontalPosition.Center,
        //    VerticalAnchor = VerticalPosition.Center,
        //    FullScreen = true,
        //    Buttons = DialogButton.None,
        //    Mode = DialogMode.None,
        //    CanDragMove = false,
        //    CanResize = false,
        //};

        await OverlayDialog.ShowCustomModal<DialogPage, DialogPageViewModel, bool>(
            new DialogPageViewModel());
    }

    [RelayCommand]
    private void ShowToast(object obj)
    {
        ToastManager?.Show("This is a Toast message");
    }

    [RelayCommand]
    private void ShowNotification(object obj)
    {
        NotificationManager?.Show("This is a Notification message");
    }

    private async void TestMessage()
    {
        var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));
        while (await timer.WaitForNextTickAsync())
        {
            ShowToast(new object());
        }
    }
}