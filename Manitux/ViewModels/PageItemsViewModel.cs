using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;

namespace Manitux.ViewModels;

public partial class PageItemsViewModel :  ViewModelBase
{
    public ObservableCollection<PageItemModel>? PageItems { get; set; }
    //private PluginManager? pluginManager;

    
    public PageItemsViewModel(List<PageItemModel>? pageItems)
    {
        //pluginManager = CodeLogic.CodeLogic.GetPluginManager();
        //WeakReferenceMessenger.Default.Register<PageItemsViewModel, MenuItemChangedMessage>(this, OnNavigation);

        if (pageItems is null) return;
        PageItems = new ObservableCollection<PageItemModel>(pageItems);
        OnPropertyChanged(nameof(PageItems));
    }

    public void OnActivate(PageItemModel pageItem)
    {
        if (pageItem is null) return;
        WeakReferenceMessenger.Default.Send(new PageItemChangedMessage(pageItem));
    }


    //[RelayCommand] {Binding AvtivateCommand}
    //public void Activate(PageItemModel pageItem)
    //{
    //    if (pageItem == null) return;
    //    WeakReferenceMessenger.Default.Send(new PageItemChangedMessage(pageItem));
    //}

    //private async void OnNavigation(PageItemsViewModel vm, MenuItemChangedMessage message)
    //{
    //    string key = message.Value.Key ?? "";
    //    string? pluginId = message.Value.PluginId ?? null;

    //    if (pluginId is not null)
    //    {
    //        var plugin = pluginManager?.GetPlugin<PluginBase>(pluginId);

    //        if (plugin is not null && plugin.State == PluginState.Started)
    //        {
    //            var cat = message.Value.Category;
    //            if (cat is null) return;
    //            var pageItems = await plugin.GetPageItems(1, cat);
    //            if (pageItems is null) return;
    //            Debug.WriteLine($"PageItems: {JsonSerializer.Serialize(pageItems)}" + Environment.NewLine);
    //            PageItems = new ObservableCollection<PageItemModel>(pageItems);

    //            //foreach(var item in pageItems)
    //            //{
    //            //    PageItems.Add(item);
    //            //}

    //            OnPropertyChanged(nameof(PageItems));
    //        }
    //    }
    //}
}