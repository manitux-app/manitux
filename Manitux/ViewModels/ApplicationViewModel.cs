using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace Manitux.ViewModels;

public partial class ApplicationViewModel : ObservableObject
{
    [RelayCommand]
    private void JumpTo(string header)
    {
        WeakReferenceMessenger.Default.Send(header, "JumpTo");
    }

    // [RelayCommand]
    // private void JumpTo(MenuItemViewModel menuItem)
    // {
    //     WeakReferenceMessenger.Default.Send(menuItem, "JumpTo");
    // }
}