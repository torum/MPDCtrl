using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using WinRT;

namespace MPDCtrl.Views;

public sealed class Breadcrumb
{
    public string? Name
    {
        get; set;
    }
}

public sealed partial class AlbumDetailPage : Page
{

    private Frame? _frame;

    public AlbumDetailPage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();
    }

    public MainViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> BreadcrumbItems { get; set; } = [];

    // TEMP: Require CsWinRT 2.3.0-prerelease.251115.2
    // https://github.com/dotnet/runtime/issues/121590
    [DynamicWindowsRuntimeCast(typeof(Frame))]
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is not Frame frame)
        {
            base.OnNavigatedTo(e);
            return;
        }

        _frame = frame;

        ViewModel.IsGoBackButtonVisible = true;

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        if (ViewModel.SelectedAlbum is not null)
        {
            // When go back to AlbumsPage, scroll into view.
            ViewModel.GoBackFromAlbumDetailsPageCommand.Execute(ViewModel.SelectedAlbum);
        }

        // Needed this to invoke the same album in AlbumListView
        ViewModel.SelectedAlbum = null;

        ViewModel.IsGoBackButtonVisible = false;

        base.OnNavigatedFrom(e); // Always call the base implementation
    }

    // TEMP: Require CsWinRT 2.3.0-prerelease.251115.2
    // https://github.com/dotnet/runtime/issues/121590
    [DynamicWindowsRuntimeCast(typeof(ListView))]
    [DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
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
