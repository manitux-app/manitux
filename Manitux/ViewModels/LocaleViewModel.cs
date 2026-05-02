using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Avalonia;
using CodeLogic.Framework.Application.Plugins;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Manitux.Core.Application;
using Manitux.Core.Models;
using Manitux.Core.Plugins;
using Manitux.Models;

namespace Manitux.ViewModels;

public partial class LocaleViewModel : ViewModelBase
{
    public ObservableCollection<LocaleItemViewModel> MenuItems { get; set; }
   
    public LocaleViewModel()
    {

        MenuItems = new ObservableCollection<LocaleItemViewModel>()
        {
            new LocaleItemViewModel
                    {
                        Header = "Türkçe",
                        Command = SelectLocaleCommand,
                        CommandParameter = new CultureInfo("tr-TR")
                    },
                    new LocaleItemViewModel
                    {
                        Header = "English",
                        Command = SelectLocaleCommand,
                        CommandParameter = new CultureInfo("en-US")
                    }
        };
        

        OnPropertyChanged(nameof(MenuItems));
    }

    [RelayCommand]
    private void SelectLocale(object? obj)
    {
        var app = Application.Current;
        if (app is null) return;
        //SemiTheme.OverrideLocaleResources(app, obj as CultureInfo);
        Debug.WriteLine("SelectLocale");
    }
}

public class LocaleItemViewModel: ViewModelBase
{
    public string? Header { get; set; }
    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }
    public IList<LocaleItemViewModel>? Items { get; set; }
}