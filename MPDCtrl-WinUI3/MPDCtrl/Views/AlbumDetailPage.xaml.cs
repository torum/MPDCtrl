using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views;

public class Breadcrumb
{
    public string? Name
    {
        get; set;
    }
    /*
    public string? Page
    {
        get; set;
    }
    */
}

public sealed partial class AlbumDetailPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> BreadcrumbItems = [];

    private Frame? _frame;

    private readonly ResourceLoader _resourceLoader = new();

    public AlbumDetailPage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();

        var selectedAlbumName = ViewModel.SelectedAlbum?.Name ?? string.Empty;
        var basePageTitle = _resourceLoader.GetString("MenuTreeItem_Albums");

        BreadcrumbItems = [
            new() { Name = basePageTitle},
            new() { Name = selectedAlbumName },
        ];
        /*
        BreadcrumbBar1.ItemsSource = new ObservableCollection<Breadcrumb>{
        new() { Name = basePageTitle},
        new() { Name = selectedAlbumName },
        };
        */
        //BreadcrumbBar1.ItemClicked += BreadcrumbBar_ItemClicked;
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (_frame is null)
        {
            return;
        }

        if (args.Index == 0)
        {
            if (_frame.Navigate(typeof(Views.AlbumsPage), _frame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }))
            {
                // Needed to invoke the same album in AlbumListView
                //ViewModel.SelectedAlbum = null; <- do it in OnNavigatedFrom.
            }
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is not Frame frame)
        {
            return;
        }

        _frame = frame;

        ViewModel.IsGoBackButtonVisible = true;

        BreadcrumbItems[1].Name = ViewModel.SelectedAlbum?.Name ?? string.Empty;

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Needed to invoke the same album in AlbumListView
        ViewModel.SelectedAlbum = null;

        ViewModel.IsGoBackButtonVisible = false;

        base.OnNavigatedFrom(e); // Always call the base implementation
    }

    private void AlbumSongsListView_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        //ListView listView = (ListView)sender;
        if (sender is not ListView listView)
        {
            return;
        }

        // When multiple items are selected, right click select clears that selection. That is not good when trying to do multile items operation with popup menu. So, preserve SelectedItems value.
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

}
