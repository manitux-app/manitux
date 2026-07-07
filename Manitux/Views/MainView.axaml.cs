using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Manitux.ViewModels;
using System.Linq;

namespace Manitux.Views;

public partial class MainView : UserControl
{
    private const double PhoneBreakpoint = 760;
    private const double AndroidPhoneBreakpoint = 540;
    private const double AndroidRailWidth = 50;

    //private MainViewModel? _viewModel;

    public MainView()
    {
        if (OperatingSystem.IsAndroid())
        {
            ApplyAndroidLayoutResources();
        }

        InitializeComponent();
        if (OperatingSystem.IsAndroid())
        {
            Classes.Add("android-performance");
            ApplyAndroidChrome();
        }

        Focusable = false;
        KeyDown += OnKeyDown;
        SizeChanged += OnSizeChanged;
        AddHandler(GotFocusEvent, OnFocusEntered, RoutingStrategies.Tunnel);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested -= OnBackRequested;
            topLevel.BackRequested += OnBackRequested;
        }

        UpdateShellMode(Bounds.Width);
        Dispatcher.UIThread.Post(FocusInitialNavigation);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            topLevel.BackRequested -= OnBackRequested;
        }

        base.OnDetachedFromVisualTree(e);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return;
        }

        if (e.Key is Key.Escape or Key.Back or Key.BrowserBack && viewModel.GoBackCommand.CanExecute(null))
        {
            viewModel.GoBackCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnFocusEntered(object? sender, GotFocusEventArgs e)
    {
        if (e.Source is not Control source || source is TextBox)
        {
            return;
        }

        if (IsInside(source, DesktopRail) && !IsSelectedNavigationButton(source, isPhone: false))
        {
            Dispatcher.UIThread.Post(() => FocusSelectedNavigationButton(isPhone: false));
            return;
        }

        if (IsInside(source, MobileNav) && !IsSelectedNavigationButton(source, isPhone: true))
        {
            Dispatcher.UIThread.Post(() => FocusSelectedNavigationButton(isPhone: true));
        }
    }

    private void OnBackRequested(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel && viewModel.GoBackCommand.CanExecute(null))
        {
            viewModel.GoBackCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        UpdateShellMode(e.NewSize.Width);
    }

    private void ToggleTheme(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            viewModel.ToggleTheme();
            return;
        }

        if (Application.Current is null)
        {
            return;
        }

        Application.Current.RequestedThemeVariant =
            Application.Current.ActualThemeVariant == ThemeVariant.Dark
                ? ThemeVariant.Light
                : ThemeVariant.Dark;
    }

    private void UpdateShellMode(double width)
    {
        var phoneBreakpoint = OperatingSystem.IsAndroid() ? AndroidPhoneBreakpoint : PhoneBreakpoint;
        var isPhone = width > 0 && width < phoneBreakpoint;

        DesktopRail.IsVisible = !isPhone;
        MobileNav.IsVisible = isPhone;
        RootShell.Margin = isPhone
            ? new Thickness(8, 8, 8, 8)
            : OperatingSystem.IsAndroid()
                ? new Thickness(8, 8, 8, 8)
                : new Thickness(22, 20, 22, 20);
        RootShell.ColumnSpacing = OperatingSystem.IsAndroid() ? 8 : 18;

        RootShell.ColumnDefinitions.Clear();
        RootShell.RowDefinitions.Clear();

        if (isPhone)
        {
            RootShell.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            RootShell.RowDefinitions.Add(new RowDefinition(GridLength.Star));
            RootShell.RowDefinitions.Add(new RowDefinition(GridLength.Auto));

            Grid.SetColumn(ContentHost, 0);
            Grid.SetRow(ContentHost, 0);
            Grid.SetColumn(MobileNav, 0);
            Grid.SetRow(MobileNav, 1);
            return;
        }

        RootShell.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(OperatingSystem.IsAndroid() ? AndroidRailWidth : 76)));
        RootShell.ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
        RootShell.RowDefinitions.Add(new RowDefinition(GridLength.Star));

        Grid.SetColumn(DesktopRail, 0);
        Grid.SetRow(DesktopRail, 0);
        Grid.SetColumn(ContentHost, 1);
        Grid.SetRow(ContentHost, 0);
    }

    private void FocusInitialNavigation()
    {
        var target = this
            .GetVisualDescendants()
            .OfType<Control>()
            .FirstOrDefault(control =>
                control.Focusable &&
                control.IsEnabled &&
                control.IsEffectivelyVisible &&
                control.Bounds.Width > 0 &&
                control.Bounds.Height > 0);

        target?.Focus(NavigationMethod.Tab);
    }

    private void FocusSelectedNavigationButton(bool isPhone)
    {
        var target = GetSelectedNavigationButton(isPhone);
        if (target is { IsEnabled: true, IsEffectivelyVisible: true })
        {
            target.Focus(NavigationMethod.Directional);
        }
    }

    private Button GetSelectedNavigationButton(bool isPhone)
    {
        if (DataContext is not MainViewModel viewModel)
        {
            return isPhone ? MobileHomeButton : DesktopHomeButton;
        }

        return viewModel.SelectedNavigationKey switch
        {
            NavigationBarKeys.Favorites => isPhone ? MobileFavoritesButton : DesktopFavoritesButton,
            NavigationBarKeys.Plugins => isPhone ? MobilePluginsButton : DesktopPluginsButton,
            NavigationBarKeys.Settings => DesktopSettingsButton,
            _ => isPhone ? MobileHomeButton : DesktopHomeButton
        };
    }

    private bool IsSelectedNavigationButton(Control source, bool isPhone)
    {
        var selected = GetSelectedNavigationButton(isPhone);
        return ReferenceEquals(source, selected) || IsInside(source, selected);
    }

    private static bool IsInside(Control source, Control container)
    {
        var current = source;
        while (current is not null)
        {
            if (ReferenceEquals(current, container))
            {
                return true;
            }

            current = current.GetVisualParent() as Control;
        }

        return false;
    }

    private static void ApplyAndroidLayoutResources()
    {
        if (Application.Current?.Resources is not { } resources)
        {
            return;
        }

        resources["ManituxPagePadding"] = new Thickness(16);
        resources["ManituxPanelPadding"] = new Thickness(14);
        resources["ManituxControlPadding"] = new Thickness(12, 7);
        resources["ManituxRemoteTargetMin"] = 38d;
        resources["ManituxPosterWidth"] = 126d;
        resources["ManituxPosterHeight"] = 189d;
        resources["ManituxFavoritePosterWidth"] = 118d;
        resources["ManituxFavoritePosterHeight"] = 174d;
        resources["ManituxDetailPosterWidth"] = 190d;
        resources["ManituxRelatedPosterWidth"] = 118d;
        resources["ManituxRelatedPosterHeight"] = 174d;
    }

    private void ApplyAndroidChrome()
    {
        DesktopRail.Padding = new Thickness(5);
        DesktopLogo.Width = 40;
        DesktopLogo.Height = 40;
        DesktopLogo.CornerRadius = new CornerRadius(12);
        DesktopPrimaryNav.Spacing = 5;
        DesktopBottomNav.Spacing = 4;
        MobileNav.Padding = new Thickness(6, 4);
    }

    // protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    // {
    //     base.OnAttachedToVisualTree(e);
    //     _viewModel = DataContext as MainViewModel;
    //     var topLevel = TopLevel.GetTopLevel(this);
    //     if (topLevel is null || _viewModel is null)
    //         return;
    //     _viewModel.NotificationManager = WindowNotificationManager.TryGetNotificationManager(topLevel, out var manager)
    //         ? manager
    //         : new WindowNotificationManager(topLevel);
    // }
}
