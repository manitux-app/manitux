using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Manitux.Services.Localizations;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;


namespace Manitux.Services.Notifications;

public class NotificationService : INotificationService
{
    private readonly WindowNotificationManager _notificationManager;
    private readonly ILocalizationService _localizationService;

    public NotificationService(WindowNotificationManager notificationManager, ILocalizationService localizationService)
    {
        _notificationManager = notificationManager;
        _localizationService = localizationService;
        
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

    public void ShowError(string message, string? title = null, bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title ?? _localizationService.Strings.Error, NotificationType.Error, showIcon, showClose, style);
    }

    public void ShowInfo(string message, string? title = null, bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title ?? _localizationService.Strings.Information, NotificationType.Information, showIcon, showClose, style);
    }

    public void ShowSuccess(string message, string? title = null, bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title ?? _localizationService.Strings.Success, NotificationType.Success, showIcon, showClose, style);
    }

    public void ShowWarning(string message, string? title = null, bool showIcon = false, bool showClose = false, string style = "Dark")
    {
        ShowNotify(message, title ?? _localizationService.Strings.Warning, NotificationType.Warning, showIcon, showClose, style);
    }
}

