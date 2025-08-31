using CommunityToolkit.WinUI;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace MPDCtrl.Views;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class AlbumsPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    private readonly Compositor _compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();
    private SpringVector3NaturalMotionAnimation? _springAnimation;

    private void CreateOrUpdateSpringAnimation(float finalValue)
    {
        if (_springAnimation == null)
        {
            _springAnimation = _compositor.CreateSpringVector3Animation();
            _springAnimation.Target = "Scale";
        }

        _springAnimation.FinalValue = new Vector3(finalValue);
    }

    private void Border_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.02f);

        (sender as UIElement)?.StartAnimation(_springAnimation);
    }
    private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        CreateOrUpdateSpringAnimation(1.0f);

        (sender as UIElement)?.StartAnimation(_springAnimation);
    }

    public AlbumsPage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();
    }

    /*
    private void QueueListView_Loaded(object sender, RoutedEventArgs e)
    {
        var myListViewScrollViewer = FindScrollViewer(this.QueueListView);
        if (myListViewScrollViewer is not null)
        {
            myListViewScrollViewer.ViewChanged += QueueListView_ViewChanged;
        }
    }
    private void QueueListView_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        if (sender is not ScrollViewer scrollViewer)
        {
            return;
        }

        var visibleItems = GetVisibleDataItems(this.QueueListView, scrollViewer);
        // Do something with the visible data items, e.g., logging
        System.Diagnostics.Debug.WriteLine($"--- Scroll View Changed ---");
        foreach (var item in visibleItems)
        {
            System.Diagnostics.Debug.WriteLine($"Visible item: {item.Name}");
        }
    }


    /// <summary>
    /// Finds the data items that are currently visible within the ListView's viewport.
    /// </summary>
    /// <param name="listView">The ListView control.</param>
    /// <returns>A list of data items that are visible.</returns>
    public static List<AlbumEx> GetVisibleDataItems(ListView listView, ScrollViewer scrollViewer)
    {
        var visibleDataItems = new List<AlbumEx>();

        if (listView.ItemsPanelRoot is ItemsWrapGrid itemsPanel)
        {
            //var scrollViewer = FindScrollViewer(listView);
            if (scrollViewer is not null)
            {
                var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);

                foreach (var container in itemsPanel.Children)
                {
                    if (container is ListViewItem listViewItem)
                    {
                        var transform = listViewItem.TransformToVisual(scrollViewer);
                        var itemBounds = transform.TransformBounds(new Rect(0, 0, listViewItem.ActualWidth, listViewItem.ActualHeight));

                        if (viewport.IntersectsWith(itemBounds))
                        {
                            if (listViewItem.Content is AlbumEx dataItem)
                            {
                                visibleDataItems.Add(dataItem);
                            }
                        }
                    }
                }
            }
        }

        return visibleDataItems;
    }

    /// <summary>
    /// Finds the ScrollViewer within the Visual Tree of a control.
    /// </summary>
    /// <param name="obj">The dependency object to search from.</param>
    /// <returns>The ScrollViewer, or null if not found.</returns>
    private static ScrollViewer? FindScrollViewer(DependencyObject obj)
    {
        if (obj is ScrollViewer scrollViewer)
        {
            return scrollViewer;
        }

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            var result = FindScrollViewer(child);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }
    */

}
