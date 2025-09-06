using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.UI.ApplicationSettings;

namespace MPDCtrl.Views;

public sealed partial class ShellPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    private Type? _currentPage;

    public ShellPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        
        //DataContext = ViewModel;

        InitializeComponent();

        //NavigationFrame.Content = App.GetService<QueuePage>(); <- not good because this create instance addition to navigation view.
        if (NavigationFrame.Navigate(typeof(QueuePage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))
        {
            _currentPage = typeof(QueuePage);
        }

        this.ActualThemeChanged += this.This_ActualThemeChanged;

        //
        ViewModel.StartMPC();
    }

    public void CallMeWhenMainWindowIsReady(MainWindow wnd)
    {
        wnd.SetTitleBar(AppTitleBar);
    }

    private void This_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (App.MainWnd is null)
        {
            return;
        }

        App.MainWnd.SetCapitionButtonColorForWin11();
    }

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        var selected = ViewModel.MainMenuItems.FirstOrDefault();
        if (selected != null)
        {
            selected.Selected = true;
            //Debug.WriteLine("NavigationView_Loaded and selected");
        }
    }

    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked == true)
        {
            if (NavigationFrame.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
            {
                _currentPage = typeof(SettingsPage);
            }
            return;
        }

        // This won't work because somehow "args.InvokedItemContainer.DataContext" returns null.
        /*
        if (args.InvokedItemContainer is not NavigationViewItem item)
        {
            return;
        }

        if (item.DataContext is NodeMenuQueue)
        {
            if (NavigationFrame.Navigate(typeof(QueuePage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(QueuePage);
            }
        }
        else if (item.DataContext is NodeMenuAlbum)
        {
            if (NavigationFrame.Navigate(typeof(AlbumsPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(AlbumsPage);
            }
        }
        else if (item.DataContext is NodeMenuArtist)
        {
            if (NavigationFrame.Navigate(typeof(ArtistsPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(ArtistsPage);
            }
        }
        else if (item.DataContext is NodeMenuFiles)
        {
            if (NavigationFrame.Navigate(typeof(FilesPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(FilesPage);
            }
        }
        else if (item.DataContext is NodeMenuSearch)
        {
            if (NavigationFrame.Navigate(typeof(SearchPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(SearchPage);
            }
        }
        else if (item.DataContext is NodeMenuPlaylistItem)
        {
            if (NavigationFrame.Navigate(typeof(PlaylistItemPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(PlaylistItemPage);
            }
        }
        */

        if (_currentPage is null)
        {
            return;
        }

        if (args.InvokedItemContainer.Tag is not string tag || string.IsNullOrWhiteSpace(tag))
        {
            Debug.WriteLine("NavigationViewControl_ItemInvoked: Invalid tag or null.");
            return;
        }

        var pageTag = args.InvokedItemContainer.Tag.ToString();

        if (pageTag == "queue")
        {
            if (_currentPage == typeof(QueuePage))
            {
                return;
            }
            if (NavigationFrame.Navigate(typeof(QueuePage), null, args.RecommendedNavigationTransitionInfo))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
            {
                _currentPage = typeof(QueuePage);
            }
        }
        else if (pageTag == "albums")
        {
            if (_currentPage == typeof(AlbumsPage))
            {
                return;
            }
            if (NavigationFrame.Navigate(typeof(AlbumsPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(AlbumsPage);
            }
        }
        else if (pageTag == "artists")
        {
            if (_currentPage == typeof(ArtistsPage))
            {
                return;
            }
            if (NavigationFrame.Navigate(typeof(ArtistsPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(ArtistsPage);
            }
        }
        else if (pageTag == "files")
        {
            if (_currentPage == typeof(FilesPage))
            {
                return;
            }
            if (NavigationFrame.Navigate(typeof(FilesPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(FilesPage);
            }
        }
        else if (pageTag == "search")
        {
            if (_currentPage == typeof(SearchPage))
            {
                return;
            }
            if (NavigationFrame.Navigate(typeof(SearchPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(SearchPage);
            }
        }
        else if (pageTag == "playlistItem")
        {
            if (_currentPage == typeof(PlaylistItemPage))
            {
                return;
            }
            if (NavigationFrame.Navigate(typeof(PlaylistItemPage), null, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(PlaylistItemPage);
            }
        }

    }

    private void NaviView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (sender is null)
        {
            return;
        }

        if (ViewModel is not MainViewModel vm)
        {
            return;
        }

        if (args.SelectedItem is NodeMenuPlaylists)
        {
            // don't change page here.
        }
        else if (args.SelectedItem is NodeMenuLibrary)
        {
            // don't change page here.
        }
        else
        {
            if (args.SelectedItem is not null)
            {
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
            else
            {
                vm.SelectedNodeMenu = null;
            }
        }
    }
}
