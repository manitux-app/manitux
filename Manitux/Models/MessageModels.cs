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

public class PageItemsChangedMessage : ValueChangedMessage<List<PageItemModel>>
{
    public PageItemsChangedMessage(List<PageItemModel> value) : base(value)
    {
    }
}
