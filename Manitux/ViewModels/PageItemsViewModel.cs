using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;

namespace Manitux.ViewModels;

public partial class PageItemsViewModel : ObservableObject
{
    public ObservableCollection<PageItemModel>? PageItems { get; set; }
    private PluginManager? pluginManager;

    public PageItemsViewModel()
    {
        pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        WeakReferenceMessenger.Default.Register<PageItemsViewModel, MenuItemChangedMessage>(this, OnNavigation);
    }

    private async void OnNavigation(PageItemsViewModel vm, MenuItemChangedMessage message)
    {
        string key = message.Value.Key ?? "";
        string? pluginId = message.Value.PluginId ?? null;

        if (pluginId is not null)
        {
            var plugin = pluginManager?.GetPlugin<PluginBase>(pluginId);

            if (plugin is not null && plugin.State == PluginState.Started)
            {
                var cat = message.Value.Category;
                if (cat is null) return;
                var pageItems = await plugin.GetPageItems(1, cat);
                if (pageItems is null) return;
                Debug.WriteLine($"PageItems: {JsonSerializer.Serialize(pageItems)}" + Environment.NewLine);
                PageItems = new ObservableCollection<PageItemModel>(pageItems);
                OnPropertyChanged(nameof(PageItems));
            }
        }
    }
}