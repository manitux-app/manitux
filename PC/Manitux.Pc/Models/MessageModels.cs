using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.ViewModels;

namespace Manitux.Models;

public class MenuItemChangedMessage : ValueChangedMessage<MenuItemViewModel>
{
    public MenuItemChangedMessage(MenuItemViewModel value) : base(value)
    {
    }
}

public class PageItemChangedMessage : ValueChangedMessage<PageItemModel>
{
    public PageItemChangedMessage(PageItemModel value) : base(value)
    {
    }
}

public class PageChangedMessage : ValueChangedMessage<int>
{
    public PageChangedMessage(int value) : base(value)
    {
    }
}

public class PluginCatalogChangedMessage : ValueChangedMessage<bool>
{
    public PluginCatalogChangedMessage(bool value) : base(value)
    {
    }
}

public class PluginCatalogReloadingMessage : ValueChangedMessage<bool>
{
    public PluginCatalogReloadingMessage(bool value) : base(value)
    {
    }
}
