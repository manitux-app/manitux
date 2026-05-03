using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Manitux.Core.Application;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;

namespace Manitux.ViewModels;

public class MenuViewModel : ViewModelBase
{
    public ObservableCollection<MenuItemViewModel> MenuItems { get; set; }
    public string PluginName { get; set; } = "Plugin Name";
    public string Favicon { get; set; } = "https://www.google.com/s2/favicons?domain=hdfilmcehennemi.nl&sz=64";

    // public MenuViewModel()
    // {
        
    //     // MenuItems = new ObservableCollection<MenuItemViewModel>
    //     // {
    //     //     new() { MenuHeader = "Introduction", Key = MenuKeys.MenuKeyIntroduction, IsSeparator = false },
    //     //     new() { MenuHeader = "About Us", Key = MenuKeys.MenuKeyAboutUs, IsSeparator = false },
    //     //     new() { MenuHeader = "Plugins", IsSeparator = true },
    //     //     new()
    //     //     {
    //     //         MenuHeader = "Plugin1", Children = GetMenu()
    //     //     },
    //     //     new()
    //     //     {
    //     //         MenuHeader = "Plugin2", Children = new ObservableCollection<MenuItemViewModel>
    //     //         {
    //     //             new() { MenuHeader = "Cat1", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //             new() { MenuHeader = "Cat2", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //         }
    //     //     },
    //     //     new()
    //     //     {
    //     //         MenuHeader = "Plugin3", Children = new ObservableCollection<MenuItemViewModel>
    //     //         {
    //     //             new() { MenuHeader = "Cat1", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //             new() { MenuHeader = "Cat2", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //         }
    //     //     },
    //     //     new()
    //     //     {
    //     //         MenuHeader = "Plugin4", Children = new ObservableCollection<MenuItemViewModel>
    //     //         {
    //     //             new() { MenuHeader = "Cat1", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //             new() { MenuHeader = "Cat2", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //         }
    //     //     },
    //     //     new()
    //     //     {
    //     //         MenuHeader = "Plugin5", Children = new ObservableCollection<MenuItemViewModel>
    //     //         {
    //     //             new() { MenuHeader = "Cat1", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //             new() { MenuHeader = "Cat2", Key = MenuKeys.MenuKeyCategories, Url = "", Status = "" },
    //     //         }
    //     //     },
    //     // };
    // }

    public void LoadDefaultMenu(AppStrings localize)
    {
        MenuItems = new ObservableCollection<MenuItemViewModel>
        {
            new() { MenuHeader = localize.Settings, Key = MenuKeys.MenuKeySettings, IsSeparator = false, MenuIconName = "SemiIconSetting" },
            new() { MenuHeader = localize.Plugins, IsSeparator = true, Status = "0" }
        };

        OnPropertyChanged(nameof(MenuItems));
     }

    public void LoadMenu(PluginBase plugin, List<CategoryModel>? categories)
    {
        var menus = new ObservableCollection<MenuItemViewModel>();

        if (categories is not null)
        {
            foreach (var cat in categories)
            {
                //Debug.WriteLine(JsonSerializer.Serialize(cat));
                menus.Add(new() { MenuHeader = cat.Title, Key = MenuKeys.MenuKeyCategories, Category = cat, Status = "" });
            }
        }

        MenuItems = new ObservableCollection<MenuItemViewModel>
        {
            new() { MenuHeader = "Introduction", Key = MenuKeys.MenuKeyIntroduction, IsSeparator = false },
            new() { MenuHeader = "About Us", Key = MenuKeys.MenuKeyAboutUs, IsSeparator = false },
            new() { MenuHeader = "Plugins", IsSeparator = true },
            new()
            {
                MenuHeader = plugin.Manifest.Name, Children = menus
            }
        };

        //Debug.WriteLine(JsonSerializer.Serialize(MenuItems));
        OnPropertyChanged(nameof(MenuItems));
        //return menus;
    }

    public void LoadMenus(List<PluginMenuModel> pluginMenus, AppStrings localize)
    {
        MenuItems = new ObservableCollection<MenuItemViewModel>
        {
            //new() { MenuHeader = localize.AboutUs, Key = MenuKeys.MenuKeyAboutUs, IsSeparator = false },
            new() { MenuHeader = localize.Settings, Key = MenuKeys.MenuKeySettings, IsSeparator = false, MenuIconName = "settings" },
            new() { MenuHeader = localize.Plugins, IsSeparator = true, Status = pluginMenus.Any() ? pluginMenus.Count.ToString(): "0" },
        };

        foreach (var p in pluginMenus)
        {
             var menu = new MenuItemViewModel() { MenuHeader = p.Plugin.Manifest.Name, Status = p.Plugin.Config.Language, MenuIconName = "plus"};
             
            if (p.Categories is not null)
            {
                var childrens =  new ObservableCollection<MenuItemViewModel>();
                foreach (var cat in p.Categories)
                {
                    childrens.Add(new() { MenuHeader = cat.Title, Key = MenuKeys.MenuKeyPageItems, PluginId = p.Plugin.Manifest.Id, Category = cat, MenuIconName = "play"});
                }

                menu.Children = childrens;
            }

            MenuItems.Add(menu);
        }

        //Debug.WriteLine(JsonSerializer.Serialize(MenuItems));
        OnPropertyChanged(nameof(MenuItems));
    }
}

public static class MenuKeys
{
    public const string MenuKeyIntroduction = "Introduction";
    public const string MenuKeyAboutUs = "AboutUs";
    public const string MenuKeySettings = "Settings";
    public const string MenuKeyCategories = "Categories";
    public const string MenuKeyPageItems = "PageItems";
    public const string MenuKeyMediaInfo = "MediaInfo";
    public const string MenuKeySearch = "Search";
    public const string MenuKeyPlayer = "Player";
    public const string MenuKeyEmptyPage = "EmptyPage";
    
}