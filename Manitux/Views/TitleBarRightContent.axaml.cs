using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Manitux.Views;

public partial class TitleBarRightContent : UserControl
{
    private WindowState _restoreWindowState = WindowState.Normal;

    public TitleBarRightContent()
    {
        InitializeComponent();
    }

    private async void OpenRepository(object? sender, RoutedEventArgs e)
    {
        var top = TopLevel.GetTopLevel(this);
        if (top is null) return;
        var launcher = top.Launcher;
        await launcher.LaunchUriAsync(new Uri("https://github.com/manitux-app/manitux"));
    }

    private Window? GetWindow()
    {
        return TopLevel.GetTopLevel(this) as Window;
    }

    private void MinimizeWindow(object? sender, RoutedEventArgs e)
    {
        if (GetWindow() is { } window)
        {
            //window.WindowState = WindowState.Minimized;

            window.WindowState = window.WindowState == WindowState.Maximized || window.WindowState == WindowState.FullScreen
            ? WindowState.Normal
            : WindowState.Minimized;
        }
    }

    private void ToggleMaximizeWindow(object? sender, RoutedEventArgs e)
    {
        if (GetWindow() is not { } window)
        {
            return;
        }

        window.WindowState = WindowState.Maximized;

        // window.WindowState = window.WindowState == WindowState.Maximized
        //     ? WindowState.Normal
        //     : WindowState.Maximized;
    }

    private void ToggleFullScreenWindow(object? sender, RoutedEventArgs e)
    {
        if (GetWindow() is not { } window)
        {
            return;
        }

        if (window.WindowState == WindowState.FullScreen)
        {
            window.WindowState = _restoreWindowState == WindowState.FullScreen
                ? WindowState.Normal
                : _restoreWindowState;
            return;
        }

        _restoreWindowState = window.WindowState;
        window.WindowState = WindowState.FullScreen;
    }

    private void CloseWindow(object? sender, RoutedEventArgs e)
    {
        GetWindow()?.Close();
    }
}
