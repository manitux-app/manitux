using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Manitux.ViewModels;

namespace Manitux.Pages;

public partial class PageItems : UserControl
{
    private PageItemsViewModel? _viewModel;
    public PageItems()
    {
        InitializeComponent();

        DataContextChanged += VM_DataContextChanged;
    }

     private void VM_DataContextChanged(object? sender, EventArgs e)
    {
        _viewModel = DataContext as PageItemsViewModel;

        if (_viewModel is not null)
        {
            _viewModel.OnDataRefreshed -= ResetScrollPosition;
            _viewModel.OnDataRefreshed += ResetScrollPosition;
        }
    }

    private void ResetScrollPosition()
    {
        //var scrollViewer = this.FindDescendantOfType<ScrollViewer>();
        if (MainScrollViewer != null)
        {
            MainScrollViewer.Offset = new Avalonia.Vector(0, 0);
            //Debug.WriteLine("ScrollViewer Ok");
        }
    }
}