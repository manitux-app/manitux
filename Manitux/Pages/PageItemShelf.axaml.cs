using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Manitux.ViewModels;

namespace Manitux.Pages;

public partial class PageItemShelf : UserControl
{
    public static readonly StyledProperty<ICommand?> ActivateCommandProperty =
        AvaloniaProperty.Register<PageItemShelf, ICommand?>(nameof(ActivateCommand));

    public PageItemShelf()
    {
        InitializeComponent();
        if (OperatingSystem.IsAndroid())
        {
            Classes.Add("android-performance");
        }
    }

    public ICommand? ActivateCommand
    {
        get => GetValue(ActivateCommandProperty);
        set => SetValue(ActivateCommandProperty, value);
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (DataContext is PageItemCategoryViewModel row)
        {
            _ = row.EnsureLoadedAsync();
        }
    }
}
