using System;
using Avalonia.Controls;
using Manitux.Core.Framework;
using Ursa.Controls;

namespace Manitux.Views;

public partial class MainWindow : UrsaWindow
{
    //private ManituxFramework framework = new ManituxFramework();

    public MainWindow()
    {
        InitializeComponent();

        //framework.Init().ConfigureAwait(false);

        // this.Loaded += async (_, _) =>
        // {
        //     await framework.Init();
        // };

        // this.Closed += async (_, _) =>
        // {
        //     await CodeLogic.CodeLogic.StopAsync();
        // };


    }

    // protected override async void OnOpened(EventArgs e)
    // {
    //     base.OnOpened(e);
    //     //await framework.Init();
    // }
}
