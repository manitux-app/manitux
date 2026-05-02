using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.Notifications;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;

namespace Manitux.Services.Notifications;

public class ToastService : IToastService
{
    private readonly WindowToastManager _toastManager;

    public ToastService(WindowToastManager toastManager)
    {
        _toastManager = toastManager;

        _toastManager.MaxItems = 3;
    }
    public void ShowToast(string message, NotificationType type, bool showIcon = false, bool showClose = false, string style = "Light", int expiration = 5)
    {
        _toastManager?.Show(
                new Toast(message),
                showIcon: showIcon,
                showClose: showClose,
                type: type,
                classes: [style], //Ligth - Dark
                expiration: TimeSpan.FromSeconds(expiration)); 
    }

    public void ShowError(string message, bool showIcon = false, bool showClose = false, string style = "Light")
    {
        ShowToast(message, NotificationType.Error, showIcon, showClose, style);
    }

    public void ShowInfo(string message, bool showIcon = false, bool showClose = false, string style = "Light")
    {
        ShowToast(message, NotificationType.Information, showIcon, showClose, style);
    }

    public void ShowSuccess(string message, bool showIcon = false, bool showClose = false, string style = "Light")
    {
        ShowToast(message, NotificationType.Success, showIcon, showClose, style);
    }

    public void ShowWarning(string message, bool showIcon = false, bool showClose = false, string style = "Light")
    {
        ShowToast(message, NotificationType.Warning, showIcon, showClose, style);
    }
}
