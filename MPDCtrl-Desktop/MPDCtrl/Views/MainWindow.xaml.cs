using MPDCtrl.ViewModels;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Navigation;

namespace MPDCtrl.Views;

public partial class MainWindow : Window
{
    #region == Global Hotkey ==

    // HotKey WM ID
    private const int WM_HOTKEY = 0x0312;

    private const int HOTKEY_ID1 = 0x0001; // play/pause
    private const int HOTKEY_ID2 = 0x0002; // next
    private const int HOTKEY_ID3 = 0x0003; // prev
    private const int HOTKEY_ID4 = 0x0004; // vol up
    private const int HOTKEY_ID5 = 0x0005; // vol up
    private const int HOTKEY_ID6 = 0x0006; // vol down
    private const int HOTKEY_ID7 = 0x0007; // vol down

    private const int HOTKEY_ID8 = 0x0008; // MediaPlayPause
    private const int HOTKEY_ID9 = 0x0009; // MediaStop
    private const int HOTKEY_ID10 = 0x0010; // MediaNextTrack
    private const int HOTKEY_ID11 = 0x0011; // MediaPreviousTrack

    private readonly IntPtr WindowHandle;

    #endregion

    public MainWindow()
    {
        DataContext = App.GetService<MainViewModel>();

        InitializeComponent();

        // Set initial visibility for Window's system buttoms.
        RestoreButton.Visibility = Visibility.Collapsed;
        MaxButton.Visibility = Visibility.Visible;

        // Event subscriptions
        if (this.DataContext is MainViewModel vm)
        {
            if (vm is not null)
            {
                vm.OnWindowLoaded(this);

                // Subscribe to Window events.

                //Loaded += vm.OnWindowLoaded;
                //Closing += vm.OnWindowClosing;
                ContentRendered += vm.OnContentRendered;

                // Subscribe to ViewModel events.

                vm.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };

                vm.ScrollIntoViewAndSelect += (sender, arg) => { this.OnScrollIntoViewAndSelect(arg); };

                vm.DebugWindowShowHide += () => OnDebugWindowShowHide();

                vm.DebugWindowShowHide2 += (sender, arg) => OnDebugWindowShowHide2(arg);

                vm.DebugCommandOutput += (sender, arg) => { this.OnDebugCommandOutput(arg); };

                vm.DebugIdleOutput += (sender, arg) => { this.OnDebugIdleOutput(arg); };

                vm.DebugCommandClear += () => OnDebugCommandClear();

                vm.DebugIdleClear += () => OnDebugIdleClear();

                vm.AckWindowOutput += (sender, arg) => { this.OnAckWindowOutput(arg); };

                vm.AckWindowClear += () => OnAckWindowClear();

            }
        }

        // Need this for starting maximized.
        Window_StateChanged(this, EventArgs.Empty);

        #region == Global Hotkey ==

        var host = new WindowInteropHelper(this);
        WindowHandle = host.Handle;

        SetUpHotKey();
        ComponentDispatcher.ThreadPreprocessMessage += ComponentDispatcher_ThreadPreprocessMessage;

        #endregion
    }

    public void OnScrollIntoView(int arg)
    {
        if ((QueueListview.Items.Count > arg) && (arg > -1))
        {
            QueueListview.ScrollIntoView(QueueListview.Items[arg]);

            ListViewItem? lvi = QueueListview.ItemContainerGenerator.ContainerFromIndex(arg) as ListViewItem;
            lvi?.Focus();
        }
    }

    public void OnScrollIntoViewAndSelect(int arg)
    {
        if ((QueueListview.Items.Count > arg) && (arg > -1))
        {
            QueueListview.SelectedItems.Clear();

            QueueListview.ScrollIntoView(QueueListview.Items[arg]);

            ListViewItem? lvi = QueueListview.ItemContainerGenerator.ContainerFromIndex(arg) as ListViewItem;
            if (lvi is not null)
            {
                //QueueListview.ScrollIntoView(lvi);
                lvi.Focus();
                lvi.IsSelected = true;
            }
        }
    }

    public void OnDebugCommandOutput(string arg)
    {
        // AppendText() is much faster than data binding.
        DebugCommandTextBox.AppendText(arg);

        DebugCommandTextBox.CaretIndex = DebugCommandTextBox.Text.Length;
        DebugCommandTextBox.ScrollToEnd();
    }

    public void OnDebugIdleOutput(string arg)
    {
        // AppendText() is much faster than data binding.
        DebugIdleTextBox.AppendText(arg);

        DebugIdleTextBox.CaretIndex = DebugIdleTextBox.Text.Length;
        DebugIdleTextBox.ScrollToEnd();
    }

    public void OnDebugCommandClear()
    {
        DebugCommandTextBox.Clear();
    }

    public void OnDebugIdleClear()
    {
        DebugIdleTextBox.Clear();
    }

    public void OnDebugWindowShowHide()
    {
        if (DebugWindowGridSplitter.Visibility == Visibility.Visible)
        {
            LayoutGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);

            LayoutGrid.RowDefinitions[3].Height = new GridLength(0);
            LayoutGrid.RowDefinitions[4].Height = new GridLength(0);

            DebugWindowGridSplitter.Visibility = Visibility.Collapsed;
            DebugWindow.Visibility = Visibility.Collapsed;
        }
        else
        {
            LayoutGrid.RowDefinitions[2].Height = new GridLength(3, GridUnitType.Star);

            LayoutGrid.RowDefinitions[3].Height = new GridLength(8);
            LayoutGrid.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);

            DebugWindowGridSplitter.Visibility = Visibility.Visible;
            DebugWindow.Visibility = Visibility.Visible;
        }
    }

    public void OnDebugWindowShowHide2(bool on)
    {
        if (on)
        {
            LayoutGrid.RowDefinitions[2].Height = new GridLength(3, GridUnitType.Star);

            LayoutGrid.RowDefinitions[3].Height = new GridLength(8);
            LayoutGrid.RowDefinitions[4].Height = new GridLength(1, GridUnitType.Star);

            DebugWindowGridSplitter.Visibility = Visibility.Visible;
            DebugWindow.Visibility = Visibility.Visible;
        }
        else
        {
            LayoutGrid.RowDefinitions[2].Height = new GridLength(1, GridUnitType.Star);

            LayoutGrid.RowDefinitions[3].Height = new GridLength(0);
            LayoutGrid.RowDefinitions[4].Height = new GridLength(0);

            DebugWindowGridSplitter.Visibility = Visibility.Collapsed;
            DebugWindow.Visibility = Visibility.Collapsed;
        }
    }

    public void OnAckWindowOutput(string arg)
    {
        // AppendText() is much faster than data binding.
        AckTextBox.AppendText(arg);

        AckTextBox.CaretIndex = AckTextBox.Text.Length;
        AckTextBox.ScrollToEnd();
    }

    public void OnAckWindowClear()
    {
        AckTextBox.Clear();
    }

    public void BringToForeground()
    {
        if (this.WindowState == WindowState.Minimized || this.Visibility == Visibility.Hidden)
        {
            this.Show();
            this.WindowState = WindowState.Normal;
        }

        this.Activate();
        this.Focus();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (this.WindowState == WindowState.Normal)
        {
            RestoreButton.Visibility = Visibility.Collapsed;
            MaxButton.Visibility = Visibility.Visible;

            LayoutGrid.Margin = new Thickness(0);
            WindowChromeBorder.Margin = new Thickness(18);
            WindowBackgroundBorder.Margin = new Thickness(18);
        }
        else if (this.WindowState == WindowState.Maximized)
        {
            RestoreButton.Visibility = Visibility.Visible;
            MaxButton.Visibility = Visibility.Collapsed;

            LayoutGrid.Margin = new Thickness(0,20,0,4);
            WindowChromeBorder.Margin = new Thickness(0,4,0,0);
            WindowBackgroundBorder.Margin = new Thickness(0);
            
        }
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }

    private void MaxButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Maximized;
    }

    private void RestoreButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Normal;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        this.WindowState = WindowState.Minimized;
    }

    private void QueueListview_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Not really good especially when multiple items are selected
        //QueueListview.ScrollIntoView(QueueListview.SelectedItem);
    }

    private void PlaylistSongsListview_TargetUpdated(object sender, DataTransferEventArgs e)
    {
        if (PlaylistSongsListview.Items.Count > 0)
        {
            PlaylistSongsListview.ScrollIntoView(PlaylistSongsListview.Items[0]);
        }
    }

    private void TextBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not null)
        {
            if (sender is TextBox tb)
            {
                if (tb.Visibility == Visibility.Visible)
                {
                    tb.Focus();
                    Keyboard.Focus((sender as TextBox));
                }
            }
        }
    }

    private void PasswordBox_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (sender is not null)
        {
            if (sender is PasswordBox pb)
            {
                //var pb = (sender as PasswordBox);
                if (pb.Visibility == Visibility.Visible)
                {
                    if (pb.Focusable)
                    {
                        Keyboard.Focus(pb);
                        pb.Focus();
                    }
                }
            }
        }

        /*
        if (DialogInputTextBox.Visibility == Visibility.Visible)
        {
            DialogInputTextBox.Focus();
            Keyboard.Focus(DialogInputTextBox);
        }
        */
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        ProcessStartInfo psi = new(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true
        };
        Process.Start(psi);
        e.Handled = true;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // It takes a few milliseconds to dispose TCPClient so, we don't want Window to hang around.
        this.Hide();

        if (this.DataContext is MainViewModel vm)
        {
            vm.OnWindowClosing(sender);
        }

        #region == Global Hotkey == 
        
        Unregister(HOTKEY_ID1);
        Unregister(HOTKEY_ID2);
        Unregister(HOTKEY_ID3);
        Unregister(HOTKEY_ID4);
        Unregister(HOTKEY_ID5);
        Unregister(HOTKEY_ID6);
        Unregister(HOTKEY_ID7);
        Unregister(HOTKEY_ID8);
        Unregister(HOTKEY_ID9);
        Unregister(HOTKEY_ID10);
        Unregister(HOTKEY_ID11);
        ComponentDispatcher.ThreadPreprocessMessage -= ComponentDispatcher_ThreadPreprocessMessage;

        #endregion
    }

    private void Window_Activated(object sender, EventArgs e)
    {
        AppTitleBar.Opacity = 1;
        AppHeader.Opacity = 1;
        StatusBar.Opacity = 1;
    }

    private void Window_Deactivated(object sender, EventArgs e)
    {
        AppTitleBar.Opacity = 0.7;
        AppHeader.Opacity = 0.7;
        StatusBar.Opacity = 0.7;
    }

    #region == Size fix on window maximize ==

    // https://engy.us/blog/2020/01/01/implementing-a-custom-window-title-bar-in-wpf/

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        ((HwndSource)PresentationSource.FromVisual(this)).AddHook(HookProc);
    }

    public static IntPtr HookProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_GETMINMAXINFO)
        {
            // We need to tell the system what our size should be when maximized. Otherwise it will cover the whole screen,
            // including the task bar.
            MINMAXINFO mmi = (MINMAXINFO)Marshal.PtrToStructure(lParam, typeof(MINMAXINFO));

            // Adjust the maximized size and position to fit the work area of the correct monitor
            IntPtr monitor = MonitorFromWindow(hwnd, MONITOR_DEFAULTTONEAREST);

            if (monitor != IntPtr.Zero)
            {
                MONITORINFO monitorInfo = new()
                {
                    cbSize = Marshal.SizeOf(typeof(MONITORINFO))
                };
                GetMonitorInfo(monitor, ref monitorInfo);
                RECT rcWorkArea = monitorInfo.rcWork;
                RECT rcMonitorArea = monitorInfo.rcMonitor;
                mmi.ptMaxPosition.X = Math.Abs(rcWorkArea.Left - rcMonitorArea.Left);
                mmi.ptMaxPosition.Y = Math.Abs(rcWorkArea.Top - rcMonitorArea.Top) - 30; // -4を付加した。てっぺんをクリックしても反応がなかったから。
                mmi.ptMaxSize.X = Math.Abs(rcWorkArea.Right - rcWorkArea.Left);
                mmi.ptMaxSize.Y = Math.Abs(rcWorkArea.Bottom - rcWorkArea.Top) + 38; // +4 付加した分の補正。
            }

            Marshal.StructureToPtr(mmi, lParam, true);
        }

        return IntPtr.Zero;
    }

    private const int WM_GETMINMAXINFO = 0x0024;

    private const uint MONITOR_DEFAULTTONEAREST = 0x00000002;

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr handle, uint flags);

    [DllImport("user32.dll")]
    private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT(int left, int top, int right, int bottom)
    {
        public int Left = left;
        public int Top = top;
        public int Right = right;
        public int Bottom = bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MONITORINFO
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct POINT(int x, int y)
    {
        public int X = x;
        public int Y = y;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MINMAXINFO
    {
        public POINT ptReserved;
        public POINT ptMaxSize;
        public POINT ptMaxPosition;
        public POINT ptMinTrackSize;
        public POINT ptMaxTrackSize;
    }

    #endregion

    #region == Global Hotkey == 

    private void SetUpHotKey()
    {
        var result1 = RegisterHotKey(WindowHandle, HOTKEY_ID1, (int)ModifierKeys.Control, KeyInterop.VirtualKeyFromKey(Key.Space));
        if (result1 == 0)
        {
            //MessageBox.Show("HotKey1 register failed.");
        }

        var result2 = RegisterHotKey(WindowHandle, HOTKEY_ID2, (int)ModifierKeys.Control, KeyInterop.VirtualKeyFromKey(Key.Right));
        if (result2 == 0)
        {
            //MessageBox.Show("HotKey2 registet failed.");
        }

        var result3 = RegisterHotKey(WindowHandle, HOTKEY_ID3, (int)ModifierKeys.Control, KeyInterop.VirtualKeyFromKey(Key.Left));
        if (result3 == 0)
        {
            //MessageBox.Show("HotKey3 registet failed.");
        }

        var result4 = RegisterHotKey(WindowHandle, HOTKEY_ID4, (int)ModifierKeys.Control, KeyInterop.VirtualKeyFromKey(Key.OemPlus));
        if (result4 == 0)
        {
            //MessageBox.Show("HotKey4 registet failed.");
        }

        var result5 = RegisterHotKey(WindowHandle, HOTKEY_ID5, (int)ModifierKeys.Control, KeyInterop.VirtualKeyFromKey(Key.Add));
        if (result5 == 0)
        {
            //MessageBox.Show("HotKey5 registet failed.");
        }

        var result6 = RegisterHotKey(WindowHandle, HOTKEY_ID6, (int)ModifierKeys.Control, KeyInterop.VirtualKeyFromKey(Key.OemMinus));
        if (result6 == 0)
        {
            //MessageBox.Show("HotKey6 registet failed.");
        }

        var result7 = RegisterHotKey(WindowHandle, HOTKEY_ID7, (int)ModifierKeys.Control, KeyInterop.VirtualKeyFromKey(Key.Subtract));
        if (result7 == 0)
        {
            //MessageBox.Show("HotKey7 registet failed.");
        }

        var result8 = RegisterHotKey(WindowHandle, HOTKEY_ID8, 0, KeyInterop.VirtualKeyFromKey(Key.MediaPlayPause));
        if (result8 == 0)
        {
            //MessageBox.Show("HotKey8 registet failed.");
        }

        var result9 = RegisterHotKey(WindowHandle, HOTKEY_ID9, 0, KeyInterop.VirtualKeyFromKey(Key.MediaStop));
        if (result9 == 0)
        {
            //MessageBox.Show("HotKey9 registet failed.");
        }

        var result10 = RegisterHotKey(WindowHandle, HOTKEY_ID10, 0, KeyInterop.VirtualKeyFromKey(Key.MediaNextTrack));
        if (result10 == 0)
        {
            //MessageBox.Show("HotKey10 registet failed.");
        }

        var result11 = RegisterHotKey(WindowHandle, HOTKEY_ID11, 0, KeyInterop.VirtualKeyFromKey(Key.MediaPreviousTrack));
        if (result11 == 0)
        {
            //MessageBox.Show("HotKey11 registet failed.");
        }
    }

    void ComponentDispatcher_ThreadPreprocessMessage(ref MSG msg, ref bool handled)
    {
        if (msg.message != WM_HOTKEY) return;

        if (this.DataContext is MainViewModel vm)
        {
            switch (msg.wParam.ToInt32())
            {
                case HOTKEY_ID1:
                    vm.PlayCommand_ExecuteAsync();
                    break;
                case HOTKEY_ID2:
                    vm.PlayNextCommand_ExecuteAsync();
                    break;
                case HOTKEY_ID3:
                    vm.PlayPrevCommand_ExecuteAsync();
                    break;
                case HOTKEY_ID4:
                    vm.VolumeUpCommand_Execute();
                    break;
                case HOTKEY_ID5:
                    vm.VolumeUpCommand_Execute();
                    break;
                case HOTKEY_ID6:
                    vm.VolumeDownCommand_Execute();
                    break;
                case HOTKEY_ID7:
                    vm.VolumeDownCommand_Execute();
                    break;
                case HOTKEY_ID8:
                    vm.PlayCommand_ExecuteAsync();
                    break;
                case HOTKEY_ID9:
                    vm.PlayCommand_ExecuteAsync();
                    break;
                case HOTKEY_ID10:
                    vm.PlayNextCommand_ExecuteAsync();
                    break;
                case HOTKEY_ID11:
                    vm.PlayPrevCommand_ExecuteAsync();
                    break;
                default:
                    break;
            }
        }
    }

    [DllImport("user32.dll")]
    private static extern int RegisterHotKey(IntPtr hWnd, int id, int modKey, int vKey);

    [DllImport("user32.dll")]
    private static extern int UnregisterHotKey(IntPtr hWnd, int id);

    public bool Unregister(int id)
    {
        var ret = UnregisterHotKey(this.WindowHandle, id);
        return ret == 0;
    }

    #endregion

    #region == Popups ==

    private void QueueListviewPopupConfirmDeleteSelected_Opened(object sender, EventArgs e)
    {
        if (QueueListviewPopupConfirmDeleteSelectedDefaultButton.Focusable)
        {
            Keyboard.Focus(QueueListviewPopupConfirmDeleteSelectedDefaultButton);
            QueueListviewPopupConfirmDeleteSelectedDefaultButton.Focus();
        }
    }

    private void QueueListviewPopupSelectedSaveAs_Opened(object sender, EventArgs e)
    {
        if (QueueListviewPopupNewPlaylistNameAtSelectedSaveAsTextbox.Focusable)
        {
            Keyboard.Focus(QueueListviewPopupNewPlaylistNameAtSelectedSaveAsTextbox);
            QueueListviewPopupNewPlaylistNameAtSelectedSaveAsTextbox.Focus();
        }
    }

    private void QueueListviewPopupPlaylistSelect_Opened(object sender, EventArgs e)
    {
        if (QueueListviewPopupPlaylistSelectListview.Focusable)
        {
            Keyboard.Focus(QueueListviewPopupPlaylistSelectListview);
            QueueListviewPopupPlaylistSelectListview.Focus();
        }
    }

    private void QueueListviewPopupConfirmClearQueue_Opened(object sender, EventArgs e)
    {
        if (QueueListviewPopupConfirmClearQueueDefaultButton.Focusable)
        {
            Keyboard.Focus(QueueListviewPopupConfirmClearQueueDefaultButton);
            QueueListviewPopupConfirmClearQueueDefaultButton.Focus();
        }
    }

    private void QueueListviewPopupSaveAs_Opened(object sender, EventArgs e)
    {
        if (QueueListviewPopupNewPlaylistNameAtSaveAsTextbox.Focusable)
        {
            Keyboard.Focus(QueueListviewPopupNewPlaylistNameAtSaveAsTextbox);
            QueueListviewPopupNewPlaylistNameAtSaveAsTextbox.Focus();
        }
    }

    private void SearchResultListviewPopupPlaylistSelect_Opened(object sender, EventArgs e)
    {
        if (PopupPlaylistSelectListviewAtSearchResult.Focusable)
        {
            Keyboard.Focus(PopupPlaylistSelectListviewAtSearchResult);
            PopupPlaylistSelectListviewAtSearchResult.Focus();
        }
    }

    private void LibraryListviewPopupPlaylistSelect_Opened(object sender, EventArgs e)
    {
        if (PopupPlaylistSelectListviewAtSongFiles.Focusable)
        {
            Keyboard.Focus(PopupPlaylistSelectListviewAtSongFiles);
            PopupPlaylistSelectListviewAtSongFiles.Focus();
        }
    }

    private void PlaylistsListviewPopupConfirmDeleteSelected_Opened(object sender, EventArgs e)
    {
        if (PlaylistsListviewPopupConfirmDeleteSelectedDefaultButton.Focusable)
        {
            Keyboard.Focus(PlaylistsListviewPopupConfirmDeleteSelectedDefaultButton);
            PlaylistsListviewPopupConfirmDeleteSelectedDefaultButton.Focus();
        }
    }

    private void PlaylistItemsListviewPopupConfirmUpdatePlaylistSongs_Opened(object sender, EventArgs e)
    {
        if (PlaylistItemsListviewPopupConfirmUpdatePlaylistSongsDefaultButton.Focusable)
        {
            Keyboard.Focus(PlaylistItemsListviewPopupConfirmUpdatePlaylistSongsDefaultButton);
            PlaylistItemsListviewPopupConfirmUpdatePlaylistSongsDefaultButton.Focus();
        }
    }

    private void PlaylistItemsListviewPopupConfirmMultipleDeletePlaylistSongsNotSupported_Opened(object sender, EventArgs e)
    {
        if (PlaylistItemsListviewPopupConfirmMultipleDeletePlaylistSongsNotSupportedDefaultButton.Focusable)
        {
            Keyboard.Focus(PlaylistItemsListviewPopupConfirmMultipleDeletePlaylistSongsNotSupportedDefaultButton);
            PlaylistItemsListviewPopupConfirmMultipleDeletePlaylistSongsNotSupportedDefaultButton.Focus();
        }
    }

    private void PlaylistItemsListviewPopupConfirmDeletePlaylistSong_Opened(object sender, EventArgs e)
    {
        if (PlaylistItemsListviewPopupConfirmDeletePlaylistSongDefaultButton.Focusable)
        {
            Keyboard.Focus(PlaylistItemsListviewPopupConfirmDeletePlaylistSongDefaultButton);
            PlaylistItemsListviewPopupConfirmDeletePlaylistSongDefaultButton.Focus();
        }
    }

    private void PlaylistItemsListviewPopupConfirmClearPlaylistSong_Opened(object sender, EventArgs e)
    {
        if (PlaylistItemsListviewPopupConfirmClearPlaylistSongDefaultButton.Focusable)
        {
            Keyboard.Focus(PlaylistItemsListviewPopupConfirmClearPlaylistSongDefaultButton);
            PlaylistItemsListviewPopupConfirmClearPlaylistSongDefaultButton.Focus();
        }
    }

    #endregion
}
