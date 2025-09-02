using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using Windows.Foundation;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation.Collections;

namespace MPDCtrl.Helpers;

public static class ListViewBehavior
{
    // The Attached Dependency Property
    public static readonly DependencyProperty VisibleItemsProperty =
    DependencyProperty.RegisterAttached(
        "VisibleItems",
        typeof(ObservableCollection<object>),
        typeof(ListViewBehavior),
        new PropertyMetadata(null, OnVisibleItemsChanged));

    // Get accessor for the attached property
    public static ObservableCollection<object> GetVisibleItems(DependencyObject obj)
    {
        return (ObservableCollection<object>)obj.GetValue(VisibleItemsProperty);
    }

    // Set accessor for the attached property
    public static void SetVisibleItems(DependencyObject obj, ObservableCollection<object> value)
    {
        obj.SetValue(VisibleItemsProperty, value);
    }

    // This is called when the property is set in XAML.
    private static void OnVisibleItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not ListView listView)
        {
            return;
        }

        //if (e.NewValue is ObservableCollection<object> newCollection && e.OldValue == null)
        if (e.OldValue == null)
        {
            listView.Loaded += (s, args) =>
            {
                var scrollViewer = FindScrollViewer(listView);
                if (scrollViewer is not null)
                {
                    scrollViewer.ViewChanged += (sender, eventArgs) =>
                    {
                        //UpdateVisibleItems(listView, scrollViewer, newCollection);
                        UpdateVisibleItems(listView, scrollViewer);
                    };

                    scrollViewer.SizeChanged += (sender, eventArgs) =>
                    {
                        //UpdateVisibleItems(listView, scrollViewer, newCollection);
                        UpdateVisibleItems(listView, scrollViewer);
                    };

                    //UpdateVisibleItems(listView, scrollViewer, newCollection);
                    UpdateVisibleItems(listView, scrollViewer);
                }
            };
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

    private static void UpdateVisibleItems(ListView listView, ScrollViewer scrollViewer)//, ObservableCollection<object> visibleItems
    {
        //visibleItems.Clear();
        ObservableCollection<object> visibleItems = [];

        if (listView.ItemsPanelRoot is not ItemsWrapGrid itemsPanel) return;
        //var scrollViewer = FindScrollViewer(listView);
        if (scrollViewer is null) return;

        var viewport = new Rect(0, 0, scrollViewer.ViewportWidth, scrollViewer.ViewportHeight);
        foreach (var container in itemsPanel.Children)
        {
            if (container is not ListViewItem listViewItem) continue;

            var transform = listViewItem.TransformToVisual(scrollViewer);
            var itemBounds = transform.TransformBounds(new Rect(0, 0, listViewItem.ActualWidth, listViewItem.ActualHeight));

            if (viewport.IntersectsWith(itemBounds))
            {
                if (listViewItem.Content is object dataItem)
                {
                    visibleItems.Add(dataItem);
                }
            }
        }

        listView.SetValue(VisibleItemsProperty, visibleItems);
    }
}


