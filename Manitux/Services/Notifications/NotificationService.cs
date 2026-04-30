using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;


namespace Manitux.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly WindowNotificationManager _notificationManager;

    public NotificationService(WindowNotificationManager notificationManager)
    {
        _notificationManager = notificationManager;
        
        _notificationManager.MaxItems = 3;
        _notificationManager.Position = NotificationPosition.TopRight;
    }
    public void ShowNotify(string message, string title, NotificationType type, bool showIcon = false, bool showClose = false, string style = "Dark", int expiration = 5)
    {
        _notificationManager?.Show(
                new Notification(title, message),
                showIcon: showIcon,
                showClose: showClose,
                type: type,
                classes: [style], //Ligth - Dark
                expiration: TimeSpan.FromSeconds(expiration)); 
    }

    public void ShowError(string message, string title = "Error", bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title, NotificationType.Error, showIcon, showClose, style);
    }

    public void ShowInfo(string message, string title = "Information", bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title, NotificationType.Information, showIcon, showClose, style);
    }

    public void ShowSuccess(string message, string title = "Success", bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title, NotificationType.Success, showIcon, showClose, style);
    }

    public void ShowWarning(string message, string title = "Warning", bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title, NotificationType.Warning, showIcon, showClose, style);
    }
}

