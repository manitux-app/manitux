using System;
using Avalonia.Controls;
using Manitux.Core.Framework;
using TlsClient.Api;
using TlsClient.Native;
using Ursa.Controls;

namespace Manitux.Views;

public partial class MainWindow : UrsaWindow
{
    private const double PhoneBreakpoint = 760;

    //private ManituxFramework framework = new ManituxFramework();

    public MainWindow()
    {
        InitializeComponent();
        //SizeChanged += OnSizeChanged;
        //Opened += OnOpened;

        //framework.Init().ConfigureAwait(false);

        //this.Loaded += (_, _) =>
        //{
        //    if (!OperatingSystem.IsLinux())
        //    {
        //        // use native on non linux platforms
        //        NativeTlsClient.Initialize(null);
        //    }
        //    else
        //    {
        //        // use api on linux
        //        ApiTlsClient.Initialize(null);
        //    }

        //    // test on win dev env
        //    //ApiTlsClient.Initialize(null);
        //};

        this.Closed += (_, _) =>
        {
            if (!OperatingSystem.IsLinux())
            {
                // destroy native on non linux platforms
                //NativeTlsClient.Dsipose();
            }
            else
            {
                // destroy api on linux
                ApiTlsClient.Dispose();
            }
        };
    }

    // private void OnOpened(object? sender, EventArgs e)
    // {
    //     UpdateDesktopChrome(Bounds.Width);
    // }

    // private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    // {
    //     UpdateDesktopChrome(e.NewSize.Width);
    // }

    // private void UpdateDesktopChrome(double width)
    // {
    //     var isDesktopLayout = width <= 0 || width >= PhoneBreakpoint;

    //     CanMinimize = isDesktopLayout;
    //     CanMaximize = isDesktopLayout;
    //     IsCloseButtonVisible = isDesktopLayout;
    //     IsManagedResizerVisible = isDesktopLayout && OperatingSystem.IsLinux();
    //     DesktopTitleActions.IsVisible = isDesktopLayout;
    // }

    // protected override async void OnOpened(EventArgs e)
    // {
    //     base.OnOpened(e);
    //     //await framework.Init();
    // }
}
