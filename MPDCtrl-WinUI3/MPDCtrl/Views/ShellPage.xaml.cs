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
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.UI.ApplicationSettings;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
        ViewModel.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };
        ViewModel.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };
        ViewModel.DebugCommandClear += this.OnDebugCommandClear;
        ViewModel.DebugIdleClear += this.OnDebugIdleClear;

        /*
        //NavigationFrame.Content = App.GetService<QueuePage>(); <- not good because this create instance in addition to navigation view.
        if (NavigationFrame.Navigate(typeof(QueuePage), NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))
        {
            _currentPage = typeof(QueuePage);
            var queuepage = ViewModel.MainMenuItems.FirstOrDefault();
            if (queuepage != null)
            {
                queuepage.Selected = true;
            }
        }
        */

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

    private readonly StringBuilder _sbCommandOutput = new();
    public void OnDebugCommandOutput(string arg)
    {
        // WPF's AppendText() is much faster than data binding.
        //DebugCommandTextBox.AppendText(arg);
        //DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
        //DebugCommandTextBox.ScrollToEnd();

        _sbCommandOutput.Append(arg);

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            //DebugCommandTextBox.Text += arg;
            DebugCommandTextBox.Text = _sbCommandOutput.ToString();

            // No CaretIndex, No ScrollToEnd.
            //DebugCommandTextBox.SelectionStart = DebugCommandTextBox.Text.Length;
            // Needed to this to acutually scrol to the end.
            //DebugCommandTextBox.Focus(FocusState.Programmatic);
        });
    }

    private readonly StringBuilder _sbIdleOutput = new();
    public void OnDebugIdleOutput(string arg)
    {
        // WPF's AppendText() is much faster than data binding.
        //DebugIdleTextBox.AppendText(arg);
        //DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
        //DebugIdleTextBox.ScrollToEnd();

        _sbIdleOutput.Append(arg);

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            //DebugIdleTextBox.Text += arg;
            DebugIdleTextBox.Text = _sbIdleOutput.ToString();

            // No CaretIndex, No ScrollToEnd.
            //DebugIdleTextBox.SelectionStart = DebugIdleTextBox.Text.Length;
            // Needed to this to acutually scrol to the end.
            //DebugIdleTextBox.Focus(FocusState.Programmatic);
        });
    }

    public void OnDebugCommandClear(object? sender, System.EventArgs e)
    {
        _sbCommandOutput.Clear();
        DebugCommandTextBox.Text = string.Empty;
    }

    public void OnDebugIdleClear(object? sender, System.EventArgs e)
    {
        _sbIdleOutput.Clear();
        DebugIdleTextBox.Text = string.Empty;
    }


    private void This_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (App.MainWnd is null)
        {
            return;
        }

        App.MainWnd.SetCapitionButtonColor();
    }

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        /*
        var selected = ViewModel.MainMenuItems.FirstOrDefault();
        if (selected != null)
        {
            selected.Selected = true;
            //Debug.WriteLine("NavigationView_Loaded and selected");
        }
        */
        if (NavigationFrame.Navigate(typeof(QueuePage), NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))
        {
            _currentPage = typeof(QueuePage);
            var queuepage = ViewModel.MainMenuItems.FirstOrDefault();
            if (queuepage != null)
            {
                queuepage.Selected = true;
            }
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

        if (_currentPage is null)
        {
            return;
        }

        /*
         * Now uses Selection changed event.
         * 
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
        */
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

        if (args.SelectedItem is NodeMenuQueue)
        {
            if (_currentPage == typeof(QueuePage))
            {
                return;
            }
            if (this.NavigationFrame.Navigate(typeof(QueuePage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
            {
                _currentPage = typeof(QueuePage);
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
        }
        else if (args.SelectedItem is NodeMenuSearch)
        {
            if (_currentPage == typeof(SearchPage))
            {
                return;
            }
            if (this.NavigationFrame.Navigate(typeof(SearchPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(SearchPage);
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
        }
        else if (args.SelectedItem is NodeMenuLibrary)
        {
            // don't change page here.
        }
        else if (args.SelectedItem is NodeMenuAlbum)
        {
            if (_currentPage == typeof(AlbumsPage))
            {
                return;
            }
            if (this.NavigationFrame.Navigate(typeof(AlbumsPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(AlbumsPage);
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
        }
        else if (args.SelectedItem is NodeMenuArtist)
        {
            if (_currentPage == typeof(ArtistsPage))
            {
                return;
            }
            if (this.NavigationFrame.Navigate(typeof(ArtistsPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(ArtistsPage);
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
        }
        else if (args.SelectedItem is NodeMenuFiles)
        {
            if (_currentPage == typeof(FilesPage))
            {
                return;
            }
            if (this.NavigationFrame.Navigate(typeof(FilesPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(FilesPage);
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
        }
        else if (args.SelectedItem is NodeMenuPlaylists)
        {
            // don't change page here.
        }
        else if (args.SelectedItem is NodeMenuPlaylistItem)
        {
            if (_currentPage == typeof(PlaylistItemPage))
            {
                // Set SelectedNodeMenu here. Don't JUST return for PlaylistItemPage, otherwise it won't load playlist.
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
                return;
            }

            // TODO: I just wanna show NavigationTransition animation without navigation....
            if (this.NavigationFrame.Navigate(typeof(PlaylistItemPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))
            {
                _currentPage = typeof(PlaylistItemPage);
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
        }
        else
        {
            if (_currentPage == typeof(SettingsPage))
            {
                //
            }

            // clear vm selected just in case.
            vm.SelectedNodeMenu = null;

            /*
            if (args.SelectedItem is not null)
            {
                vm.SelectedNodeMenu = args.SelectedItem as NodeTree;
            }
            else
            {
                vm.SelectedNodeMenu = null;
            }
            */
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
