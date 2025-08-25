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
    private readonly MainViewModel _viewModel;
    private Type? _currentPage;

    public MainViewModel ViewModel => _viewModel;

    public ShellPage()
    {
        _viewModel = App.GetService<MainViewModel>();
        
        InitializeComponent();

        NavigationFrame.Content = App.GetService<QueuePage>();
        //NavigationFrame.Navigate(typeof(QueuePage), null, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom });
        _currentPage = typeof(QueuePage);

        this.ActualThemeChanged += this.This_ActualThemeChanged;
    }

    public void InitWhenMainWindowIsReady(MainWindow wnd)
    {
        wnd.SetTitleBar(AppTitleBar);
    }

    private void This_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (App.MainWnd is null)
        {
            return;
        }

        //App.MainWnd.SetCapitionButtonColorForWin11();
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
            NavigationFrame.Navigate(typeof(SettingsPage), null, args.RecommendedNavigationTransitionInfo);//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
            _currentPage = typeof(SettingsPage);
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
            NavigationFrame.Navigate(typeof(QueuePage), null, args.RecommendedNavigationTransitionInfo);//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
            _currentPage = typeof(QueuePage);
        }
        else if (pageTag == "albums")
        {
            if (_currentPage == typeof(AlbumsPage))
            {
                return;
            }
            NavigationFrame.Navigate(typeof(AlbumsPage), null, args.RecommendedNavigationTransitionInfo);
            _currentPage = typeof(AlbumsPage);
        }
        else if (pageTag == "artists")
        {
            if (_currentPage == typeof(ArtistsPage))
            {
                return;
            }
            NavigationFrame.Navigate(typeof(ArtistsPage), null, args.RecommendedNavigationTransitionInfo);
            _currentPage = typeof(ArtistsPage);
        }
        else if (pageTag == "files")
        {
            if (_currentPage == typeof(FilesPage))
            {
                return;
            }
            NavigationFrame.Navigate(typeof(FilesPage), null, args.RecommendedNavigationTransitionInfo);
            _currentPage = typeof(FilesPage);
        }
        else if (pageTag == "search")
        {
            if (_currentPage == typeof(SearchPage))
            {
                return;
            }
            NavigationFrame.Navigate(typeof(SearchPage), null, args.RecommendedNavigationTransitionInfo);
            _currentPage = typeof(SearchPage);
        }

    }
}
