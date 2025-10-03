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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Web;

namespace MPDCtrl.Views;

public sealed partial class AlbumsPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    private readonly Compositor _compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();
    private SpringVector3NaturalMotionAnimation? _springAnimation;

    public AlbumsPage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();

        ViewModel.AlbumsCollectionHasBeenReset += this.OnAlbumsCollectionHasBeenReset;
        ViewModel.AlbumScrollIntoView += this.OnAlbumScrollIntoView;
    }

    public void OnAlbumScrollIntoView(object? sender, AlbumEx album)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            if (this.AlbumListView is not ListView lb)
            {
                return;
            }

            if (album is null)
            {
                return;
            }

            await Task.Delay(300);

            lb.ScrollIntoView(album, ScrollIntoViewAlignment.Default);//Leading

            await Task.Yield();


            // TODO: rewrite this lator.
            // below is a trick to show album cover images.
            var scrollViewer = FindScrollViewer(lb);
            if (scrollViewer is null)
            {
                return;
            }

            await Task.Delay(300); // This is needed.
            UpdateVisibleItems(lb, scrollViewer);

            /*
            // trick to simulate scrollviewer scrol event.
            scrollViewer.ChangeView(null, scrollViewer.VerticalOffset+12, null);
            await Task.Delay(300);
            scrollViewer.ChangeView(null, scrollViewer.VerticalOffset-12, null);
            */
        });
    }

    public void OnAlbumsCollectionHasBeenReset(object? sender, System.EventArgs e)
    {
        // TODO: need to rewrite this lator.
        // Need this to load image.
        // Albums sort resets ObservableCollection which is not recognized by ListViewBehavior and does not UpdateVisibleItems,
        // so forcibly fire scroll event.
        if (this.AlbumListView is ListView lb)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
            {
                var scrollViewer = FindScrollViewer(lb);
                if (scrollViewer is null)
                {
                    return;
                }

                await Task.Delay(300);
                UpdateVisibleItems(lb, scrollViewer);

                /*
                scrollViewer.ChangeView(null, 12, null);
                await Task.Delay(300);
                //scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                scrollViewer.ChangeView(null, 0, null);
                */
            });
        }
    }

    // Find the ScrollViewer in the visual tree
    private static ScrollViewer? FindScrollViewer(DependencyObject obj)
    {
        if (obj is ScrollViewer scrollViewer) return scrollViewer;

        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            var result = FindScrollViewer(child);
            if (result is not null) return result;
        }
        return null;
    }

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
        if (sender is not FrameworkElement ele)
        {
            return;
        }

        CreateOrUpdateSpringAnimation(1.05f);
        ele.CenterPoint = new Vector3((float)(ele.ActualWidth / 2.0), (float)(ele.ActualHeight / 2.0), 1f);
        ele.StartAnimation(_springAnimation);
    }
    private void Border_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (sender is not FrameworkElement ele)
        {
            return;
        }

        CreateOrUpdateSpringAnimation(1.0f);
        ele.CenterPoint = new Vector3((float)(ele.ActualWidth / 2.0), (float)(ele.ActualHeight / 2.0), 1f);
        ele.StartAnimation(_springAnimation);
    }

    private void AlbumListView_Loaded(object sender, RoutedEventArgs e)
    {
        // Already subscribed, returning.
        if (this.AlbumListView.Tag != null)
        {
            //Debug.WriteLine("(ListView.Tag != null) @ListViewBehaviors");
            return;
        }

        this.AlbumListView.Tag = "LoadedEvent_ListViewBehaviors";

        //Debug.WriteLine("listView.Loaded and subscribing. @OnPropertyChanged");

        var scrollViewer = FindScrollViewer(this.AlbumListView);
        if (scrollViewer is null)
        {
            return;
        }

        scrollViewer.ViewChanged += (sender, eventArgs) =>
        {
            UpdateVisibleItems(this.AlbumListView, scrollViewer);
        };

        scrollViewer.SizeChanged += (sender, eventArgs) =>
        {
            Debug.WriteLine("scrollViewer.SizeChanged");
            UpdateVisibleItems(this.AlbumListView, scrollViewer);
        };

        UpdateVisibleItems(this.AlbumListView, scrollViewer);
    }

    private void UpdateVisibleItems(ListView listView, ScrollViewer scrollViewer)//, ObservableCollection<object> visibleItems
    {
        //visibleItems.Clear();
        ObservableCollection<AlbumEx> visibleItems = [];

        /*
         * AOT bad 
         * https://github.com/microsoft/microsoft-ui-xaml/issues/10604
        if (listView.ItemsPanelRoot is not ItemsWrapGrid itemsPanel)
        {
            Debug.WriteLine($"ItemsPanelRoot is null or not ItemsWrapGrid");
            ViewModel.SetError($"ItemsWrapGrid is not it. {listView.ItemsPanelRoot}");
            return;
        }
        */

        // AOT
        if (listView.ItemsPanelRoot is not Microsoft.UI.Xaml.Controls.Panel itemsPanel)
        {
            Debug.WriteLine($"ItemsPanelRoot is null or not ItemsWrapGrid");
            return;
        }

        //var scrollViewer = FindScrollViewer(listView);
        if (scrollViewer is null) 
        {
            return;
        }

        var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
        foreach (var container in itemsPanel.Children)
        {
            if (container is not ListViewItem listViewItem) continue;

            var transform = listViewItem.TransformToVisual(scrollViewer);
            var itemBounds = transform.TransformBounds(new Rect(0, 0, listViewItem.ActualWidth, listViewItem.ActualHeight));

            if (viewport.IntersectsWith(itemBounds))
            {
                if (listViewItem.Content is AlbumEx dataItem)
                {
                    visibleItems.Add(dataItem);
                }
            }
        }

        //listView.SetValue(VisibleItemsProperty, visibleItems);

        ViewModel.VisibleItemsAlbumsEx = visibleItems;
    }
}
