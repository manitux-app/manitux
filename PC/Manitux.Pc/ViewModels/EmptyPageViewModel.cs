using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;
using Manitux.Core.Application;
using Manitux.Services.Localizations;

namespace Manitux.ViewModels;

public partial class EmptyPageViewModel : ViewModelBase
{
    public AppStrings L { get; }

    public EmptyPageViewModel(ILocalizationService localizationService)
    {
        L = localizationService.Strings;
        Debug.WriteLine("EmptyPageViewModel loaded");
    }
}
