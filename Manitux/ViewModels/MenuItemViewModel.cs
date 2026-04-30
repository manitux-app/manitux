using System.Collections.ObjectModel;
using System.Windows.Input;
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;

namespace Manitux.ViewModels;

public enum ControlStatus
{
    New,
    Beta,
    Stable,
}

public class MenuItemViewModel: ViewModelBase
{
    public string? MenuHeader { get; set; }
    public string? MenuIconName { get; set; }
    public string? Key { get; set; }
    public string? PluginId { get; set; }
    public CategoryModel? Category { get; set; }
    public string? Status { get; set; }
    
    public bool IsSeparator { get; set; }
    public ObservableCollection<MenuItemViewModel> Children { get; set; } = new();
    
    public ICommand ActivateCommand { get; set; }

    public MenuItemViewModel()
    {
        ActivateCommand = new RelayCommand(OnActivate);
    }

    private void OnActivate()
    {
        if (IsSeparator || Key is null) return;
        //WeakReferenceMessenger.Default.Send(Key, "JumpTo");
        WeakReferenceMessenger.Default.Send(new MenuItemChangedMessage(this));
    }
}