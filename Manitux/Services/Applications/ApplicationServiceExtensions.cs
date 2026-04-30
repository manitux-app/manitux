using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications;
using Avalonia.Controls;
using Microsoft.Extensions.DependencyInjection;
using Manitux.Services.Notifications;
using Ursa.Controls;
using Notification = Ursa.Controls.Notification;
using WindowNotificationManager = Ursa.Controls.WindowNotificationManager;
using Manitux.Core.Application;
using CodeLogic.Framework.Application.Plugins;
using Manitux.Core.Framework;

namespace Manitux.Services.Applications;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddPluginManager(this IServiceCollection services)
    {
        var manager = new ManituxFramework().InitAsync().Result;

        services.AddSingleton<PluginManager>(manager);
        return services;
    }

    public static async Task<IServiceCollection> AddPluginManagerAsync(this IServiceCollection services)
    {
        var manager = await new ManituxFramework().InitAsync();

        services.AddSingleton<PluginManager>(manager);
        return services;
    }
}
