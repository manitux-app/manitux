using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Manitux.Core.Models;
using Manitux.Ui.Input;
using Manitux.ViewModels;

namespace Manitux.Pages;

public partial class PageItems : UserControl, IRemoteDirectionalNavigation
{
    private PageItemsViewModel? _viewModel;
    private int _focusRequestVersion;
    public PageItems()
    {
        InitializeComponent();
        if (OperatingSystem.IsAndroid())
        {
            Classes.Add("android-performance");
        }

        DataContextChanged += VM_DataContextChanged;
    }

    private void VM_DataContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.OnDataRefreshed -= ResetScrollPosition;
        }

        _viewModel = DataContext as PageItemsViewModel;

        if (_viewModel is not null)
        {
            _viewModel.OnDataRefreshed += ResetScrollPosition;
        }
    }

    private void ResetScrollPosition()
    {
        //var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
        var scrollViewer = CategoryList.FindDescendantOfType<ScrollViewer>();
        if (scrollViewer is not null)
        {
            scrollViewer.Offset = new Avalonia.Vector(0, 0);
            //Debug.WriteLine("ScrollViewer Ok");
        }

        Dispatcher.UIThread.Post(FocusFirstPoster);
    }

    private void FocusFirstPoster()
    {
        var firstPoster = this
            .GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button =>
                button.Classes.Contains("poster") &&
                button.IsEnabled &&
                button.IsEffectivelyVisible &&
                button.Bounds.Width > 0 &&
                button.Bounds.Height > 0);

        firstPoster?.Focus(Avalonia.Input.NavigationMethod.Tab);
    }

    public bool TryMoveFocus(Control current, NavigationDirection direction)
    {
        var focusRequestVersion = Interlocked.Increment(ref _focusRequestVersion);
        if (current is not Button poster || !poster.Classes.Contains("poster") ||
            poster.DataContext is not PageItemModel pageItem ||
            poster.FindAncestorOfType<PageItemShelf>()?.DataContext is not PageItemCategoryViewModel row)
        {
            return false;
        }

        var itemIndex = row.PageItems.IndexOf(pageItem);
        var rowIndex = _viewModel?.CategoryRows.IndexOf(row) ?? -1;
        if (itemIndex < 0 || rowIndex < 0 || _viewModel is null)
        {
            return false;
        }

        var realizedPosters = poster.FindAncestorOfType<PageItemShelf>()!
            .GetVisualDescendants()
            .OfType<Button>()
            .Where(candidate =>
                candidate.Classes.Contains("poster") &&
                candidate.IsEffectivelyVisible)
            .OrderBy(candidate => candidate.TranslatePoint(new Point(0, 0), this)?.X ?? double.MaxValue)
            .ToList();
        var realizedIndex = realizedPosters.IndexOf(poster);

        switch (direction)
        {
            case NavigationDirection.Left:
                if (realizedIndex > 0)
                {
                    FocusRealizedPoster(realizedPosters[realizedIndex - 1]);
                    return true;
                }

                if (itemIndex <= 0)
                {
                    FocusHomeButton();
                }
                else
                {
                    FocusPoster(row, itemIndex - 1, focusRequestVersion);
                }
                return true;

            case NavigationDirection.Right:
                if (itemIndex >= row.PageItems.Count - 1)
                {
                    return true;
                }

                if (realizedIndex >= 0 && realizedIndex < realizedPosters.Count - 1)
                {
                    FocusRealizedPoster(realizedPosters[realizedIndex + 1]);
                    return true;
                }

                FocusPoster(row, itemIndex + 1, focusRequestVersion);
                return true;

            case NavigationDirection.Up:
                if (rowIndex == 0)
                {
                    FocusFirstTopBarButton();
                    return true;
                }

                FocusFirstPosterInRow(_viewModel.CategoryRows[rowIndex - 1], focusRequestVersion);
                return true;

            case NavigationDirection.Down:
                if (rowIndex == _viewModel.CategoryRows.Count - 1)
                {
                    return true;
                }

                FocusFirstPosterInRow(_viewModel.CategoryRows[rowIndex + 1], focusRequestVersion);
                return true;

            default:
                return false;
        }
    }

    private void FocusHomeButton()
    {
        var root = TopLevel.GetTopLevel(this) as Visual;
        var homeButton = root?.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button =>
                button.IsEffectivelyVisible &&
                button.Name is "DesktopHomeButton" or "MobileHomeButton");
        homeButton?.Focus(NavigationMethod.Directional);
    }

    private void FocusFirstTopBarButton()
    {
        var button = this.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(candidate =>
                candidate.FindAncestorOfType<PluginTopBar>() is not null &&
                candidate.IsEnabled &&
                candidate.IsEffectivelyVisible);
        button?.Focus(NavigationMethod.Directional);
    }

    private static void FocusRealizedPoster(Button poster)
    {
        if (poster.Focus(NavigationMethod.Directional))
        {
            poster.BringIntoView();
        }
    }

    private async void FocusFirstPosterInRow(PageItemCategoryViewModel row, int focusRequestVersion)
    {
        var realized = FindRealizedPoster(row, 0);
        if (realized is not null)
        {
            FocusRealizedPoster(realized);
            return;
        }

        await row.EnsureLoadedAsync();
        if (focusRequestVersion == _focusRequestVersion && row.PageItems.Count > 0)
        {
            FocusPoster(row, 0, focusRequestVersion);
        }
    }

    private Button? FindRealizedPoster(PageItemCategoryViewModel row, int itemIndex)
    {
        if (itemIndex < 0 || itemIndex >= row.PageItems.Count)
        {
            return null;
        }

        var pageItem = row.PageItems[itemIndex];
        return this.GetVisualDescendants()
            .OfType<PageItemShelf>()
            .FirstOrDefault(candidate => ReferenceEquals(candidate.DataContext, row))?
            .GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(candidate =>
                candidate.Classes.Contains("poster") &&
                ReferenceEquals(candidate.DataContext, pageItem));
    }

    private async void FocusPoster(
        PageItemCategoryViewModel row,
        int itemIndex,
        int focusRequestVersion)
    {
        if (itemIndex < 0 || itemIndex >= row.PageItems.Count)
        {
            return;
        }

        var realized = FindRealizedPoster(row, itemIndex);
        if (realized is not null)
        {
            FocusRealizedPoster(realized);
            return;
        }

        var pageItem = row.PageItems[itemIndex];
        for (var attempt = 0; attempt < 8; attempt++)
        {
            if (focusRequestVersion != _focusRequestVersion || TopLevel.GetTopLevel(this) is null)
            {
                return;
            }

            CategoryList.ScrollIntoView(row);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);

            var shelf = this.GetVisualDescendants()
                .OfType<PageItemShelf>()
                .FirstOrDefault(candidate => ReferenceEquals(candidate.DataContext, row));
            var list = shelf?.GetVisualDescendants().OfType<ListBox>().FirstOrDefault();
            list?.ScrollIntoView(pageItem);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);

            var target = FindRealizedPoster(row, itemIndex);
            if (target?.Focus(NavigationMethod.Directional) == true)
            {
                target.BringIntoView();
                return;
            }

            await Task.Delay(16);
        }
    }
}
