using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Irihi.Avalonia.Shared.Contracts;

namespace Manitux.ViewModels;

public partial class EmptyPageViewModel : ViewModelBase
{
    public EmptyPageViewModel()
    {
        Debug.WriteLine("EmptyPageViewModel loaded");
    }
}