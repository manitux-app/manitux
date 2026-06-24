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

    //private MainViewModel? _viewModel;

    public MainView()
    {
        InitializeComponent();
        Focusable = true;
        KeyDown += OnKeyDown;
        SizeChanged += OnSizeChanged;
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
        var isPhone = width > 0 && width < PhoneBreakpoint;

        DesktopRail.IsVisible = !isPhone;
        MobileNav.IsVisible = isPhone;
        RootShell.Margin = isPhone ? new Thickness(14, 14, 14, 12) : new Thickness(22, 20, 22, 20);

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

        RootShell.ColumnDefinitions.Add(new ColumnDefinition(new GridLength(76)));
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
