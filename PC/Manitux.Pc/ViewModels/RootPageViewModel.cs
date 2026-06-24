using System;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using Irihi.Avalonia.Shared.Contracts;
using Ursa.Controls;

namespace Manitux.ViewModels;

public partial class RootPageViewModel : ViewModelBase, IDialogContext
{
    public WindowNotificationManager? NotificationManager { get; set; }
    public WindowToastManager? ToastManager { get; set; }

    [ObservableProperty]
    private UserControl? _content;

    public RootPageViewModel(UserControl? content)
    {
        Content = content;
    }

    public void Close()
    {
        RequestClose?.Invoke(this, null);
    }

    public event EventHandler<object?>? RequestClose;
}
