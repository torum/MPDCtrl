using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Markup;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Models;
using MPDCtrl.Services.Contracts;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.System;
using Windows.UI.ApplicationSettings;
using Windows.UI.Core;

namespace MPDCtrl.Views;

public sealed partial class ShellPage : Page
{
    private long _token;

    public MainViewModel ViewModel
    {
        get;
    }
    
    private readonly IDispatcherService _dispatcherService;

    private Type? _currentPage;

    public ShellPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        //DataContext = ViewModel;
        _dispatcherService = App.GetService<IDispatcherService>();

        InitializeComponent();

        ViewModel.AlbumSelectedNavigateToDetailsPage += this.OnAlbumSelectedNavigateToDetailsPage;
        ViewModel.GoBackButtonVisibilityChanged += this.OnGoBackButtonVisibilityChanged;
        //ViewModel.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };
        //ViewModel.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };
        ViewModel.DebugCommandClear += this.OnDebugCommandClear;
        ViewModel.DebugIdleClear += this.OnDebugIdleClear;
        ViewModel.UserCanExecuteChanged += OnUserCanExecuteChanged;
        ViewModel.UpdateProgress += (sender, arg) => { this.OnUpdateProgress(arg); };
        this.ActualThemeChanged += this.This_ActualThemeChanged;

        // Not good because this create instance in addition to navigation view.
        //NavigationFrame.Content = App.GetService<QueuePage>(); 

        /*
         * Not good when queuePage.Selected = true;. Better do it in loaded.
        if (NavigationFrame.Navigate(typeof(QueuePage), NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))
        {
            _currentPage = typeof(QueuePage);
            var queuePage = ViewModel.MainMenuItems.FirstOrDefault();
            if (queuePage != null)
            {
                queuePage.Selected = true;
            }
        }
        */

        // Do this at shell page loaded event after everything is initilized even App.MainWnd in app.xaml.cs.
        // It is too early here to show dialogs.
        //ViewModel.StartMPC();
    }

    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Set focus so that space shortcut works.
            this.PlaybackPlay.Focus(FocusState.Programmatic);

            if (App.MainWnd is not null)
            {
                App.MainWnd.Activated += MainWindow_Activated;

                // Everything (MainWindow including the DispatcherQueue, MainViewModel including settings and ShellPage)
                // is loaded, initialized, set, drawn, navigated. So start the connection.

                //await ViewModel.StartMpcAsync();
                // Let's not await, for faster startup. Fire and forget.
                ViewModel.Start();
            }
            else
            {
                Debug.WriteLine("App.MainWnd is null. Init order is wrong.");
            }

            // For animation fade
            _token = AlbumCoverImage.RegisterPropertyChangedCallback(Microsoft.UI.Xaml.Controls.Image.SourceProperty, OnSourceChanged);

        }
        catch (Exception ex)
        {
            _ = ex;
            Debug.WriteLine($"Exception @Page_Loaded::ShellPage: {ex}");
        }

    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        ViewModel.AlbumSelectedNavigateToDetailsPage -= this.OnAlbumSelectedNavigateToDetailsPage;
        ViewModel.GoBackButtonVisibilityChanged -= this.OnGoBackButtonVisibilityChanged;
        //ViewModel.DebugCommandOutput -= (sender, arg) => { this.OnDebugCommandOutput(arg); };
        //ViewModel.DebugIdleOutput -= (sender, arg) => { this.OnDebugIdleOutput(arg); };
        ViewModel.DebugCommandClear -= this.OnDebugCommandClear;
        ViewModel.DebugIdleClear -= this.OnDebugIdleClear;
        ViewModel.UserCanExecuteChanged -= OnUserCanExecuteChanged;
        ViewModel.UpdateProgress -= (sender, arg) => { this.OnUpdateProgress(arg); };
        this.ActualThemeChanged -= this.This_ActualThemeChanged;

        App.MainWnd?.Activated -= MainWindow_Activated;

        AlbumCoverImage.UnregisterPropertyChangedCallback(Microsoft.UI.Xaml.Controls.Image.SourceProperty, _token);
    }

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        /*
        if (this.NavigationFrame.Navigate(typeof(SettingsPage), this.NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
        {
            _currentPage = typeof(SettingsPage);
        }
        return;
        */
        
        // This right here is better. the initial Selected = .. messed up in the constructor.
        if (this.NavigationFrame.Navigate(typeof(QueuePage), this.NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))
        {
            _currentPage = typeof(QueuePage);
            var queuePage = ViewModel.MainMenuItems.FirstOrDefault();
            queuePage?.Selected = true;
        }

        SetRegionsForCustomTitleBar();
    }

    private void AppTitleBar_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        SetRegionsForCustomTitleBar();
    }

    private void OnGoBackButtonVisibilityChanged(object? sender, System.EventArgs e)
    {
        // Update interactive regions if the size of the window changes.
        SetRegionsForCustomTitleBar();
    }
    private void OnUserCanExecuteChanged(object? sender, EventArgs e)
    {
        VolumeSlider.IsEnabled = ViewModel.SetVolumeCanExecute();
        SeekSlider.IsEnabled = ViewModel.SetSeekCanExecute();

        RepeatButton.IsEnabled = ViewModel.SetRpeatCanExecute();
        SingleButton.IsEnabled = ViewModel.SetSingleCanExecute();
        RandomButton.IsEnabled = ViewModel.SetRandomCanExecute();
        ConsumeButton.IsEnabled = ViewModel.SetConsumeCanExecute();
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
        
        // Settings button
        GeneralTransform transform1 = this.SettingsButton.TransformToVisual(null);
        Rect bounds1 = transform1.TransformBounds(new Rect(0, 0,
                                                         this.SettingsButton.ActualWidth,
                                                         this.SettingsButton.ActualHeight));
        Windows.Graphics.RectInt32 SettingsButton = GetRect(bounds1, scaleAdjustment);

        // Back button
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

        var rectArray = new Windows.Graphics.RectInt32[] { BackButtonRect, SettingsButton };

        InputNonClientPointerSource nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(m_AppWindow.Id);
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

    public void OnAlbumSelectedNavigateToDetailsPage(object? sender, System.EventArgs e)
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

        // Do this after everything is initilized even App.MainWnd in app.xaml.cs.
        //ViewModel.StartMPC(this.XamlRoot);
    }

    public void OnUpdateProgress(string arg)
    {
        _dispatcherService.TryEnqueue(() =>
        {
            this.StatusBarText.Text = arg;
        });
    }

    public void OnDebugCommandOutput(string arg)
    {
        // WPF's AppendText() is virtualized and much faster.
        //DebugCommandTextBox.AppendText(arg);
        //DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
        //DebugCommandTextBox.ScrollToEnd();

        _dispatcherService.TryEnqueue(() =>
        {
            //TextBox,RichEditBox,ichTextBlock in WinUI 3 are not virtualized.
            //var range = DebugCommandTextBox.Document.GetRange(int.MaxValue, int.MaxValue);
            //range.Text = arg;
        });
    }

    public void OnDebugIdleOutput(string arg)
    {
        // WPF's AppendText() is virtualized and much faster.
        //DebugIdleTextBox.AppendText(arg);
        //DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
        //DebugIdleTextBox.ScrollToEnd();

        _dispatcherService.TryEnqueue(() =>
        {
            //TextBox,RichEditBox,ichTextBlock in WinUI 3 are not virtualized.
            //var range = DebugIdleTextBox.Document.GetRange(int.MaxValue, int.MaxValue);
            //range.Text = arg;
        });
    }

    public void OnDebugCommandClear(object? sender, System.EventArgs e)
    {
        _dispatcherService.TryEnqueue(() =>
        {
            
        });
    }

    public void OnDebugIdleClear(object? sender, System.EventArgs e)
    {
        _dispatcherService.TryEnqueue(() =>
        {
            
        });
    }

    private void This_ActualThemeChanged(FrameworkElement sender, object args)
    {
        if (App.MainWnd is null)
        {
            return;
        }

        App.MainWnd.SetCapitionButtonColor();
    }

    private void MainWindow_Activated(object sender, Microsoft.UI.Xaml.WindowActivatedEventArgs args)
    {
        //var resource = args.WindowActivationState == WindowActivationState.Deactivated ? "WindowCaptionForegroundDisabled" : "WindowCaptionForeground";
        // Stupid workaround for stupid WinUI3 bug. This changes color regardless of theme dark/light setting. so use dummy. Applying this changes button's color too. But cause error in AOT...
        //this.AppTitleDummyTextBlock.Foreground = (SolidColorBrush)App.Current.Resources[resource];

        //this.AppTitleTextBlock.Opacity = args.WindowActivationState == WindowActivationState.Deactivated ? 0.3 : 1;
        //this.AppTitleIcon.Opacity = args.WindowActivationState == WindowActivationState.Deactivated ? 0.3 : 1;

        //this.Opacity = args.WindowActivationState == WindowActivationState.Deactivated ? 0.7 : 1;

        //
        var compositor = Microsoft.UI.Xaml.Media.CompositionTarget.GetCompositorForCurrentThread();
        var visual = Microsoft.UI.Xaml.Hosting.ElementCompositionPreview.GetElementVisual(RootGrid);

        var animation = compositor.CreateScalarKeyFrameAnimation();
        if (args.WindowActivationState != WindowActivationState.CodeActivated)
        {
            animation.InsertKeyFrame(0f, 1f); // Start opacity
            animation.InsertKeyFrame(1f, 0.9f); // End opacity
        }
        else
        {
            animation.InsertKeyFrame(0f, 0.9f); // Start opacity
            animation.InsertKeyFrame(1f, 1f); // End opacity
        }
        animation.Duration = TimeSpan.FromMilliseconds(100);
        visual.StartAnimation("Opacity", animation);
    }

    private async void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.IsSettingsInvoked == true)
        {
            if (this.NavigationFrame.Navigate(typeof(SettingsPage), this.NavigationFrame, args.RecommendedNavigationTransitionInfo))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
            {
                _currentPage = typeof(SettingsPage);

                await ViewModel.GetCacheFolderSizeAsync();
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

        /*
         *  Handled in KeyboardAccelerator_Invoked
        if (releasedKey == Windows.System.VirtualKey.F1)
        {
            // show help dialog.

            e.Handled = true;
            _ = App.GetService<IDialogService>().ShowKeybindingsDialog();

            return;
        }
        */

        if (releasedKey != Windows.System.VirtualKey.Space)
        {
            return;
        }

        XamlRoot currentXamlRoot = this.Content.XamlRoot;
        var focusedElement = FocusManager.GetFocusedElement(currentXamlRoot);
        if ((focusedElement is TextBox) || (focusedElement is ListView) || (focusedElement is ListViewItem))//
        {
            // Do nothing.
            return;
        }

        // Get the state of the Alt key for the current thread
        var altKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        // Check if the Alt key is in the "Down" state
        bool isAltPressed = altKeyState.HasFlag(CoreVirtualKeyStates.Down);
        if (isAltPressed)
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
        if ((focusedElement is TextBox) || (focusedElement is ListView) || (focusedElement is ListViewItem))//
        {
            // Do nothing.
            return;
        }

        // Get the state of the Alt key for the current thread
        var altKeyState = InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Menu);
        // Check if the Alt key is in the "Down" state
        bool isAltPressed = altKeyState.HasFlag(CoreVirtualKeyStates.Down);
        if (isAltPressed)
        {
            // Do nothing.
            return;
        }

        // Disable space key down.
        e.Handled = true;
    }

    private void VolumeSlider_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(sender as UIElement);
        var delta = pointerPoint.Properties.MouseWheelDelta; // +120 for scroll up, -120 for scroll down

        // Adjust the Slider's Value based on the mouse wheel delta
        // You might want to add a step value or sensitivity factor here
        VolumeSlider.Value += (delta > 0) ? VolumeSlider.SmallChange : -VolumeSlider.SmallChange;

        // Optional: Prevent the event from bubbling up to parent controls
        e.Handled = true;
    }

    private void SeekSlider_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var pointerPoint = e.GetCurrentPoint(sender as UIElement);
        var delta = pointerPoint.Properties.MouseWheelDelta; // +120 for scroll up, -120 for scroll down

        double step = 10.0; // Adjust this value as needed for the desired scroll sensitivity

        // Adjust the Slider's Value based on the mouse wheel delta
        // You might want to add a step value or sensitivity factor here
        SeekSlider.Value += (delta > 0) ? SeekSlider.SmallChange + step : -SeekSlider.SmallChange + step;

        // Optional: Prevent the event from bubbling up to parent controls
        e.Handled = true;
    }

    private async void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        //
        if (this.NavigationFrame.Navigate(typeof(SettingsPage), this.NavigationFrame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromBottom }))//, args.RecommendedNavigationTransitionInfo //new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }
        {
            _currentPage = typeof(SettingsPage);

            ViewModel.SelectedNodeMenu = null;

            NaviView.SelectedItem = null;

            await ViewModel.GetCacheFolderSizeAsync();
        }
    }

    private void OnSourceChanged(DependencyObject sender, DependencyProperty dp)
    {
        FadeInStoryboard.Begin();
    }

    private void NaviView_PaneClosed(NavigationView sender, object args)
    {
        SetRegionsForCustomTitleBar();
    }

    private void NaviView_PaneOpened(NavigationView sender, object args)
    {
        SetRegionsForCustomTitleBar();
    }

    private void AppTitleBarGrid_Loaded(object sender, RoutedEventArgs e)
    {
        SetRegionsForCustomTitleBar();
    }

    private void NavigationViewItem_DragOver(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement) return;

        if (sender is NavigationViewItem nvi)
        {
            if (nvi.DataContext is NodeMenuPlaylistItem targetPlaylist)
            {
                //Debug.WriteLine($"tag: {nvi.Tag}, datacontext: {nvi.DataContext}");
                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Move;
                e.DragUIOverride.Caption = $"{targetPlaylist.Name}";
                e.DragUIOverride.IsCaptionVisible = true;
            }
            else
            {
                if (nvi.DataContext is null)
                {
                    Debug.WriteLine("NavigationViewItem_DragOver: DataContext is null.");
                }

                e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
            }
        }
        else
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.None;
        }

        e.Handled = true;
    }

    private async void NavigationViewItem_Drop(object sender, DragEventArgs e)
    {
        if (sender is not FrameworkElement) return;

        if (sender is not NavigationViewItem nvi)
        {
            e.Handled = true;
            return;
        }

        if (nvi.DataContext is null)
        {
            Debug.WriteLine("NavigationViewItem_DragOver: DataContext is null.");
            e.Handled = true;
            return;
        }

        if (nvi.DataContext is NodeMenuPlaylistItem targetPlaylist)
        {
            if (e.DataView.Properties.ContainsKey("QueueListViewDragItems"))
            {
                // Retrieve and cast the custom object
                var items = e.DataView.Properties["QueueListViewDragItems"] as List<SongInfoEx>;

                if (items is null)
                {
                    return;
                }

                var uris = items.Select(i => i.File).ToList();

                await ViewModel.AddToPlaylist(targetPlaylist.Name, uris);
            }
        }

        e.Handled = true;
    }

    private async void KeyboardAccelerator_Invoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.F1)
        {
            // set this first.
            args.Handled = true;

            // show help dialog.
            await App.GetService<IDialogService>().ShowKeybindingsDialog();

            return;
        }


        if (args.KeyboardAccelerator.Modifiers == Windows.System.VirtualKeyModifiers.Control)
        {
            if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.S)
            {
                // set this first.
                args.Handled = true;

                // TODO: Go to settings page.
                Debug.WriteLine("Show settings page.");
                ViewModel.GoToSearchPage();

                return;
            }

            if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Subtract)
            {
                // set this first.
                args.Handled = true;

                // Handle Ctrl + Subtract
                if (ViewModel.SetVolumeCanExecute())
                {
                    ViewModel.VolumeDown();
                }
            }
            else if (args.KeyboardAccelerator.Key == Windows.System.VirtualKey.Add)
            {
                // set this first.
                args.Handled = true;

                // Handle Ctrl + Add
                if (ViewModel.SetVolumeCanExecute())
                {
                    ViewModel.VolumeUp();
                }
            }
        }


    }
}
