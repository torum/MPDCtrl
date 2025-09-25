using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views;

public sealed partial class SearchPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public SearchPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
    }

    private void SearchListview_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        //ListView listView = (ListView)sender;
        if (sender is not ListView listView)
        {
            return;
        }

        // When multiple items are selected, right click clears that selection. That is not good when trying to do multile items operation with popup menu. So, preserve SelectedItems value.
        FrameworkElement element = (FrameworkElement)e.OriginalSource;

        var container = FindParent<ListViewItem>(element);

        if (container == null)
        {
            return;
        }

        if (container.Content is not SongInfo song)
        {
            return;
        }
        //var song = container.Content;

        if (listView.SelectedItem == song)
        {
            return;
        }

        // For AOT compatibility, use IList<object> for SelectedItems.
        if (listView.SelectedItems is IList<object> list)
        {
            // Cast and ToList to use "Count" lator on.
            var collection = list.Cast<SongInfo>().ToList();

            if (collection.IndexOf(song) > -1)
            {
                return;
            }

            // For AOT compatibility..
            if (collection.Count > 1)
            {
                collection.Clear();
            }
        }

        listView.SelectedItem = song;

        /*
        if (e.OriginalSource is not FrameworkElement element)
        {
            return;
        }
        if (element.DataContext is SongInfoEx item)
        {
            if (listView.SelectedItem != item)
            {
                listView.SelectedItem = item;
            }
        }
        */
    }

    private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
    {
        DependencyObject parent = VisualTreeHelper.GetParent(child);
        while (parent != null && parent is not T)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }

        if (parent is not null)
        {
            return parent as T;
        }
        else
        {
            return null;
        }
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        this.SearchTextBox.Focus(FocusState.Programmatic);
    }
}
