using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Helpers;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views;

public sealed partial class QueuePage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public QueuePage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();

        ViewModel.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };
        ViewModel.ScrollIntoViewAndSelect += (sender, arg) => { this.OnScrollIntoViewAndSelect(arg); };
    }

    private void OnScrollIntoView(object obj)
    {
        if (obj == null) { return; }

        if (this.QueueListview is ListView lb)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                lb.ScrollIntoView(obj, ScrollIntoViewAlignment.Default);
            });
        }
    }

    private void OnScrollIntoViewAndSelect(object obj)
    {
        if (obj == null) { return; }

        if (this.QueueListview is not ListView lb)
        {
            return;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            lb.ScrollIntoView(obj, ScrollIntoViewAlignment.Leading);

            if (obj is not SongInfoEx song)
            {
                return;
            }

            //song.IsSelected = true; //This won't work in Winui3?
            lb.SelectedItem = song;
            //lb.Focus(FocusState.Programmatic);

            ListViewItem? selectedItemContainer = lb.ContainerFromIndex(lb.SelectedIndex) as ListViewItem;
            selectedItemContainer?.Focus(FocusState.Programmatic);
        });
    }

    private async void QueueListview_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        //ListView listView = (ListView)sender;
        if (sender is not ListView listView)
        {
            return;
        }

        // UI element that was right-clicked
        FrameworkElement element = (FrameworkElement)e.OriginalSource;

        var container = FindParent<ListViewItem>(element);

        if (container is null)
        {
            return;
        }

        if (listView.SelectedItem != container.Content)
        {
            return;
        }

        if (ViewModel is null)
        {
            return;
        }

        if (listView.SelectedItem is not SongInfoEx song)
        {
            return;
        }

        await ViewModel.QueueSelectedPlay(song);
    }

    private async void QueueListview_KeyUp(object sender, KeyRoutedEventArgs e)
    {
        if (sender is not ListView listView)
        {
            return;
        }

        if (e.OriginalSource is not Microsoft.UI.Xaml.Controls.ListViewItem)
        {
            return;
        }

        if (ViewModel is null)
        {
            return;
        }

        if (listView.SelectedItem is not SongInfoEx song)
        {
            return;
        }

        Windows.System.VirtualKey releasedKey = e.OriginalKey;

        if (releasedKey != Windows.System.VirtualKey.Enter)
        {
            return;
        }

        await ViewModel.QueueSelectedPlay(song);
    }

    private void QueueListview_RightTapped(object sender, RightTappedRoutedEventArgs e)
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

        if (container.Content is not SongInfoEx song)
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
            var collection = list.Cast<SongInfoEx>().ToList();

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

    private void FilterQueueListBox_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        if (sender is not ListView)
        {
            return;
        }

        FrameworkElement element = (FrameworkElement)e.OriginalSource;

        var container = FindParent<ListViewItem>(element);

        if (container is null)
        {
            return;
        }

        if (container.Content is not SongInfoEx song)
        {
            return;
        }

        if (this.QueueListview is ListView lb)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                lb.ScrollIntoView(song, ScrollIntoViewAlignment.Default);

                //song.IsSelected = true;//This won't work in Winui3?
                lb.SelectedItem = song;
            });
        }
    }

    private void TglButtonQueueFilter_Click(object sender, RoutedEventArgs e)
    {
        if (this.TglButtonQueueFilter is ToggleButton tb)
        {
            if (tb.IsChecked == true)
            {
                this.FilterQueueQueryTextBox.Focus(FocusState.Programmatic);
            }
            else
            {
                this.TglButtonQueueFilter.Focus(FocusState.Programmatic);
            }
        }
    }
}
