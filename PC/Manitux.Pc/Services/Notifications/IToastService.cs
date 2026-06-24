using System;
using Avalonia.Controls.Notifications;

namespace Manitux.Services.Notifications;

public interface IToastService
    {
        void ShowSuccess(string message, bool showIcon = false, bool showClose = false, string style = "Light");
        void ShowError(string message, bool showIcon = false, bool showClose = false, string style = "Light");
        void ShowWarning(string message, bool showIcon = false, bool showClose = false, string style = "Light");
        void ShowInfo(string message, bool showIcon = false, bool showClose = false, string style = "Light");
        void ShowToast(string message, NotificationType type, bool showIcon = false, bool showClose = false, string style = "Light", int expiration = 5);
    }  

