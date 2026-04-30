using System;
using Avalonia.Controls.Notifications;

namespace Manitux.Services.Notifications;

public interface INotificationService
{
    void ShowSuccess(string message, string title = "Success", bool showIcon = false, bool showClose = false, string style = "Dark");
    void ShowError(string message, string title = "Error", bool showIcon = false, bool showClose = false, string style = "Dark");
    void ShowWarning(string message, string title = "Warning", bool showIcon = false, bool showClose = false, string style = "Dark");
    void ShowInfo(string message, string title = "Information", bool showIcon = false, bool showClose = false, string style = "Dark");
    void ShowNotify(string message, string title, NotificationType type, bool showIcon = false, bool showClose = false, string style = "Dark", int expiration = 5);
}

