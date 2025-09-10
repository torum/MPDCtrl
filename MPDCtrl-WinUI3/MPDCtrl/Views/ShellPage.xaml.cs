using Microsoft.UI.Input;
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
using Windows.Foundation;
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

        //AppTitleBar.SizeChanged += AppTitleBar_SizeChanged; <-This does not allways fire. Use diffrent grid and use navigated event.

        ViewModel.AlbumSelected += this.OnAlbumSelected;
        ViewModel.GoBackButtonVisibilityChanged += this.OnGoBackButtonVisibilityChanged;

        //NavigationFrame.Content = App.GetService<QueuePage>(); <- not good because this create instance in addition to navigation view.
        if (NavigationFrame.Navigate(typeof(QueuePage), NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))
        {
            _currentPage = typeof(QueuePage);
        }

        this.ActualThemeChanged += this.This_ActualThemeChanged;

        //
        ViewModel.StartMPC();
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        // Update interactive regions if the size of the window changes.
        SetRegionsForCustomTitleBar();
    }

    private void OnGoBackButtonVisibilityChanged(object? sender, System.EventArgs e)
    {
        // Update interactive regions if the size of the window changes.
        SetRegionsForCustomTitleBar();
    }

    private void SetRegionsForCustomTitleBar()
    {
        var m_AppWindow = App.MainWnd?.AppWindow;

        if (m_AppWindow is null)
        {
            return;
        }

        if (App.MainWnd?.ExtendsContentIntoTitleBar != true)
        {
            return;
        }

        double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;

        //
        /*
        GeneralTransform transform = this.SearchBox.TransformToVisual(null);
        Rect bounds = transform.TransformBounds(new Rect(0, 0,
                                                         this.SearchBox.ActualWidth,
                                                         this.SearchBox.ActualHeight));
        Windows.Graphics.RectInt32 SearchBoxRect = GetRect(bounds, scaleAdjustment);

        GeneralTransform transform = this.DummyButton.TransformToVisual(null);
        Rect bounds = transform.TransformBounds(new Rect(0, 0,
                                                         this.DummyButton.ActualWidth,
                                                         this.DummyButton.ActualHeight));
        Windows.Graphics.RectInt32 DummyButtonRect = GetRect(bounds, scaleAdjustment);
        */
        //

        double width = this.BackButton.Width;//ActualWidth won't work in certain cases.
        double height = this.BackButton.Height;//ActualHeight won't work in certain cases.

        if (this.BackButton.Visibility != Visibility.Visible)
        {
            //Debug.WriteLine("BackButton.Visibility != Visibility.Visible");
            width = 0;
            height = 0;
        }

        GeneralTransform transform = this.BackButton.TransformToVisual(null);
        Rect bounds = transform.TransformBounds(new Rect(0, 0,
                                                    width,
                                                    height));
        Windows.Graphics.RectInt32 BackButtonRect = GetRect(bounds, scaleAdjustment);

        //
        //var rectArray = new Windows.Graphics.RectInt32[] { SearchBoxRect, BackButtonRect };
        var rectArray = new Windows.Graphics.RectInt32[] { BackButtonRect };

        InputNonClientPointerSource nonClientInputSrc =
            InputNonClientPointerSource.GetForWindowId(m_AppWindow.Id);
        nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rectArray);
    }

    private static Windows.Graphics.RectInt32 GetRect(Rect bounds, double scale)
    {
        return new Windows.Graphics.RectInt32(
            _X: (int)Math.Round(bounds.X * scale),
            _Y: (int)Math.Round(bounds.Y * scale),
            _Width: (int)Math.Round(bounds.Width * scale),
            _Height: (int)Math.Round(bounds.Height * scale)
        );
    }

    public void OnAlbumSelected(object? sender, System.EventArgs e)
    {
        if (this.NavigationFrame.Navigate(typeof(AlbumDetailPage), this.NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromRight }))
        {
            _currentPage = typeof(AlbumDetailPage);
        }
    }

    public void CallMeWhenMainWindowIsReady(MainWindow wnd)
    {
        wnd.SetTitleBar(AppTitleBar);

        //SetRegionsForCustomTitleBar();
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
            if (this.NavigationFrame.Navigate(typeof(SettingsPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
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
            if (this.NavigationFrame.Navigate(typeof(QueuePage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
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
            if (this.NavigationFrame.Navigate(typeof(AlbumsPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
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
            if (this.NavigationFrame.Navigate(typeof(ArtistsPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
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
            if (this.NavigationFrame.Navigate(typeof(FilesPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
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
            if (this.NavigationFrame.Navigate(typeof(SearchPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
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
            if (this.NavigationFrame.Navigate(typeof(PlaylistItemPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
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

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        if (this.NavigationFrame is null)
        {
            return;
        }

        if (this.NavigationFrame.CanGoBack)
        {
            this.NavigationFrame.GoBack();
        }
    }

    private void NavigationFrame_Navigated(object sender, NavigationEventArgs e)
    {
        if (this.NavigationFrame is null)
        {
            return;
        }

        if (this.NavigationFrame.CanGoBack)
        {
            //this.BackButton.Visibility = Visibility.Visible;
            this.BackButton.IsEnabled = true;
        }
        else
        {
            //this.BackButton.Visibility = Visibility.Collapsed;
            this.BackButton.IsEnabled = false;
        }
    }

    private void Page_PreviewKeyDown(object sender, KeyRoutedEventArgs e)
    {
        Windows.System.VirtualKey releasedKey = e.OriginalKey;

        if (releasedKey != Windows.System.VirtualKey.Space)
        {
            return;
        }

        XamlRoot currentXamlRoot = this.Content.XamlRoot;
        var focusedElement = FocusManager.GetFocusedElement(currentXamlRoot);
        if ((focusedElement is TextBox) || (focusedElement is ListView) || (focusedElement is ListViewItem))
        {
            // Do nothing.
            return;
        }

        // Disable space key down.
        e.Handled = true;

        //
        Task.Run(ViewModel.Play);
    }

    private void Page_PreviewKeyUp(object sender, KeyRoutedEventArgs e)
    {
        Windows.System.VirtualKey releasedKey = e.OriginalKey;

        if (releasedKey != Windows.System.VirtualKey.Space)
        {
            return;
        }

        XamlRoot currentXamlRoot = this.Content.XamlRoot;
        var focusedElement = FocusManager.GetFocusedElement(currentXamlRoot);
        if (focusedElement is TextBox || (focusedElement is ListView) || (focusedElement is ListViewItem))
        {
            // Do nothing.
            return;
        }

        // Disable space key down.
        e.Handled = true;
    }
    }
