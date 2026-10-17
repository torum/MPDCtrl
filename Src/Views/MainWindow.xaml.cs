using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using MPDCtrl.Helpers;
using MPDCtrl.Models;
using MPDCtrl.Services;
using MPDCtrl.Services.Contracts;
using MPDCtrl.ViewModels;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Windows.Media;
using Windows.Storage;
using WinRT;

namespace MPDCtrl.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _vm;
    private readonly SystemMediaTransportControls? _smtc;
    private readonly bool _isMediaTransportControlEnable = true;
    private readonly IDispatcherService _dispatcherService;
    private readonly IConfigurationService _configurationService;

    // Window restore position and sizes
    private int _winRestoreWidth = 1024;
    private int _winRestoreHeight = 768;
    private int _winRestoreTop = 100;
    private int _winRestoreLeft = 100;
    private ElementTheme _theme = ElementTheme.Default;

    private bool _isClosing;

    [DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
    //[DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    public MainWindow(MainViewModel vm, ShellPage shell, IDispatcherService dispatcherService, IConfigurationService configurationService)
    {
        _vm = vm;
        _dispatcherService = dispatcherService;
        _configurationService = configurationService;

        LoadSettings();

        InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;

        this.AppWindow.Title = "MPDCtrl";
        this.AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, "Assets\\MPDCtrl.ico"));

        if (this.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 600;
            presenter.PreferredMinimumHeight = 780;
        }

        // It's important to set content as early as here in order to set theme.
        // But make sure to call LoadSettings() before in order to apply settings value for contents.
        this.Content = shell;//App.GetService<ShellPage>();

        // It is necessary to set theme here after the content is set.
        shell.RequestedTheme = _theme;

        //// Let's not set caption button color here, because it will affect normal dim when activate/deactivate. Only set caption button color when theme is changed.
        SetCapitionButtonColor();

        // After the content is set
        shell.SetTitleBar(this);

        if (_isMediaTransportControlEnable)
        {
            // CLEAN THIS SHIT UP Microsoft!
            //_smtc = SystemMediaTransportControls.GetForCurrentView(); //<- this is only works in UWP. 

            //_mediaPlayer = new MediaPlayer();
            ////_mediaPlayer.CommandManager.IsEnabled = false; <- not good if false.
            //_smtc = _mediaPlayer.SystemMediaTransportControls;
            _smtc = Windows.Media.SystemMediaTransportControlsInterop.GetForWindow(WinRT.Interop.WindowNative.GetWindowHandle(this));

            OnUpdateSongInfoForSystemMediaTransportControls(this, new SongInfoForSystemMediaTransportControls());

            _smtc.ButtonPressed += Smtc_ButtonPressed;
            _vm.UpdateSongInfoForSystemMediaTransportControls += this.OnUpdateSongInfoForSystemMediaTransportControls;
            _vm.UpdateSongInfoForSystemMediaTransportControlsButtonStateOnly += this.OnUpdateSongInfoForSystemMediaTransportControlsButtonStateOnly;

            this.Closed += (s, e) =>
            {
                _smtc.ButtonPressed -= Smtc_ButtonPressed;
                _vm.UpdateSongInfoForSystemMediaTransportControls -= this.OnUpdateSongInfoForSystemMediaTransportControls;
                _vm.UpdateSongInfoForSystemMediaTransportControlsButtonStateOnly -= this.OnUpdateSongInfoForSystemMediaTransportControlsButtonStateOnly;
            };
        }

        // Unsubscribe events in Window_Closed().
        this.Closed += Window_Closed;
        this.AppWindow.Closing += AppWindow_Closing;
        this.SizeChanged += Window_SizeChanged;
        this.AppWindow.Changed += AppWindow_Changed;
        this.Activated += MainWindow_Activated;
    }

    [DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
    private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        // Ensure that the restore position and sizes are updated when the window is activated, especially after being minimized or restored.
        Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (appWindow == null)
        {
            return;
        }
        if (appWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }
        if (presenter.State == OverlappedPresenterState.Restored)
        {
            _winRestoreHeight = appWindow.Size.Height;
            _winRestoreWidth = appWindow.Size.Width;
            _winRestoreTop = appWindow.Position.Y;
            _winRestoreLeft = appWindow.Position.X;
        }
    }

    private void OnUpdateSongInfoForSystemMediaTransportControls(object? sender, SongInfoForSystemMediaTransportControls SongInfoForSMTC)
    {
        if (SongInfoForSMTC is null)
        {
            return;
        }

        if (_smtc is null)
        {
            return;
        }

        _dispatcherService.TryEnqueue(() =>
        {
            _smtc.IsPlayEnabled = SongInfoForSMTC.IsPlayEnabled;
            _smtc.IsPauseEnabled = SongInfoForSMTC.IsPauseEnabled;
            _smtc.IsNextEnabled = SongInfoForSMTC.IsNextEnabled;
            _smtc.IsPreviousEnabled = SongInfoForSMTC.IsPreviousEnabled;
            _smtc.IsStopEnabled = SongInfoForSMTC.IsStopEnabled;

            _smtc.PlaybackStatus = SongInfoForSMTC.PlaybackStatus;

            var updater = _smtc.DisplayUpdater;

            updater.Type = MediaPlaybackType.Music;

            updater.MusicProperties.AlbumArtist = SongInfoForSMTC.AlbumArtist;
            updater.MusicProperties.Artist = SongInfoForSMTC.Artist;
            updater.MusicProperties.Title = SongInfoForSMTC.Title;
            updater.MusicProperties.AlbumTitle = SongInfoForSMTC.AlbumTitle;

            updater.Thumbnail = SongInfoForSMTC.Thumbnail;

            updater.Update();
        });
    }

    private void OnUpdateSongInfoForSystemMediaTransportControlsButtonStateOnly(object? sender, Windows.Media.MediaPlaybackStatus playbackStatus)
    {
        if (_smtc is null)
        {
            return;
        }

        _dispatcherService.TryEnqueue(() =>
        {
            _smtc.PlaybackStatus = playbackStatus;
        });
    }   

    [DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
    [DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    private void LoadSettings()
    {
        if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
        {
            _vm.IsAcrylicSupported = true;

            _vm.IsBackdropEnabled = true;
        }
        if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
        {
            _vm.IsMicaSupported = true;

            _vm.IsBackdropEnabled = true;
        }

        if (!System.IO.File.Exists(App.AppConfigFilePath))
        {
            // Sets default.
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                _vm.IsAcrylicSupported = true;
                SystemBackdrop = new DesktopAcrylicBackdrop();
                _vm.Material = SystemBackdropOption.Acrylic;

                _vm.IsBackdropEnabled = true;
            }
            else if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                _vm.IsMicaSupported = true;
                SystemBackdrop = new MicaBackdrop()
                {
                    Kind = MicaKind.Base
                };
                _vm.Material = SystemBackdropOption.Mica;

                _vm.IsBackdropEnabled = true;
            }

            _theme = ElementTheme.Default;

            return;
        }

        //OverlappedPresenterState? winState = null; // For AOT workaround, don't set default, = OverlappedPresenterState.Restored;

        var config = _configurationService.CurrentConfiguration;

        //Debug.WriteLine($"MainWindow Configuration: Width={config.MainWindow.Width}, Height={config.MainWindow.Height}, Top={config.MainWindow.Top}, Left={config.MainWindow.Left}, RestoreWidth={config.MainWindow.RestoreWidth}, RestoreHeight={config.MainWindow.RestoreHeight}, RestoreTop={config.MainWindow.RestoreTop}, RestoreLeft={config.MainWindow.RestoreLeft}");

        if (config.MainWindow.Width < 500)
        {
            config.MainWindow.Width = 500;
        }
        if (config.MainWindow.Height < 500)
        {
            config.MainWindow.Height = 500;
        }
        if (config.MainWindow.Top < 0)
        {
            config.MainWindow.Top = 0;
        }
        if (config.MainWindow.Left < 0)
        {
            config.MainWindow.Left = 0;
        }

        if (config.MainWindow.RestoreWidth < 500)
        {
            config.MainWindow.RestoreWidth = 500;
        }
        if (config.MainWindow.RestoreHeight < 500)
        {
            config.MainWindow.RestoreHeight = 500;
        }
        if (config.MainWindow.RestoreTop < 0)
        {
            config.MainWindow.RestoreTop = 0;
        }
        if (config.MainWindow.RestoreLeft < 0)
        {
            config.MainWindow.RestoreLeft = 0;
        }

        // Restore window size and position
        Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (appWindow != null)
        {
            // AOT bad
            /*
            // https://github.com/microsoft/CsWinRT/issues/1930
            // Window state
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
            }
            */

            if (config.MainWindow.WindowState == OverlappedPresenterState.Maximized)
            {
                // Sets restore size and position.
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(config.MainWindow.Left, config.MainWindow.Top, config.MainWindow.Width, config.MainWindow.Height));
                // Maximize the window.
                (appWindow.Presenter as OverlappedPresenter)!.Maximize();
            }
            else if (config.MainWindow.WindowState == OverlappedPresenterState.Minimized)
            {
                // This should not happen.

                // DONT restore minimized size and pos. bad.

                (appWindow.Presenter as OverlappedPresenter)!.Restore();
               
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(config.MainWindow.Left, config.MainWindow.Top, config.MainWindow.Width, config.MainWindow.Height));
            }
            else if (config.MainWindow.WindowState == OverlappedPresenterState.Restored)
            {
                // TODO:
                //var dpi = Windows.Win32.PInvoke.GetDpiForWindow(new Windows.Win32.Foundation.HWND(WinRT.Interop.WindowNative.GetWindowHandle(this)));
                //var scalingFactor = (float)dpi / 96;
                //AppWindow.Resize(new Windows.Graphics.SizeInt32((int)(400.0f * scalingFactor), (int)(300.0f * scalingFactor)));

                // Sets restore size and position.
                appWindow.MoveAndResize(new Windows.Graphics.RectInt32(config.MainWindow.Left, config.MainWindow.Top, config.MainWindow.Width, config.MainWindow.Height));
            }
        }

        // Apply theme and backdrop
        if (config.SystemBackdrop != SystemBackdropOption.None)
        {
            _theme = config.Theme;
            _vm.Theme = config.Theme;
        }
        _vm.Material = config.SystemBackdrop;
        SwitchBackdrop(config.SystemBackdrop);
    }

    public void SwitchBackdrop(SystemBackdropOption backdrop)
    {
        //var vm = App.GetService<MainViewModel>();

        if (backdrop == SystemBackdropOption.Mica)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                this.SystemBackdrop = new MicaBackdrop()
                {
                    Kind = MicaKind.Base
                };
                _vm.Material = SystemBackdropOption.Mica;

                _vm.IsBackdropEnabled = true;
            }
        }
        else if (backdrop == SystemBackdropOption.MicaAlt)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                this.SystemBackdrop = new MicaBackdrop()
                {
                    Kind = MicaKind.BaseAlt
                };
                _vm.Material = SystemBackdropOption.MicaAlt;

                _vm.IsBackdropEnabled = true;
            }
        }
        else if (backdrop == SystemBackdropOption.Acrylic)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                this.SystemBackdrop = new DesktopAcrylicBackdrop();

                _vm.Material = SystemBackdropOption.Acrylic;

                _vm.IsBackdropEnabled = true;
            }
        }
        else if (backdrop == SystemBackdropOption.None)
        {
            this.SystemBackdrop = null;

            _vm.Material = SystemBackdropOption.None;
            _vm.IsBackdropEnabled = false;
            _vm.Theme = ElementTheme.Default;
            _theme = ElementTheme.Default;
            if (this.Content is ShellPage root)
            {
                root.RequestedTheme = _theme;

                SetCapitionButtonColor();
            }

            this.SystemBackdrop = null;
        }
    }

    [DynamicWindowsRuntimeCast(typeof(FrameworkElement))]
    public void SetCapitionButtonColor()
    {
        // This is a workaround for the WinAppSDK/WinUI3 bug that caption button color is not updated when theme is changed.
        // Use AppWindow.TitleBar.PreferredTheme instead of Color. It will affect normal dim when activate/deactivate.

        _dispatcherService.TryEnqueue(() =>
        {
            if (this.Content is null)
            {
                return;
            }

            var currentTheme = ((FrameworkElement)Content).ActualTheme;
            
            if (currentTheme == ElementTheme.Dark)
            {
                this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Dark;
            }
            else if (currentTheme == ElementTheme.Light)
            {
                this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Light;
            }
            else
            {
                if (Application.Current.RequestedTheme == ApplicationTheme.Dark)
                {
                    this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Dark;
                }
                else
                {
                    this.AppWindow.TitleBar.PreferredTheme = TitleBarTheme.Light;
                }
            }
        });
    }

    [DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (appWindow == null)
        {
            return;
        }
        if (appWindow.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }
        if (presenter.State == OverlappedPresenterState.Restored)
        {
            _winRestoreHeight = appWindow.Size.Height;
            _winRestoreWidth = appWindow.Size.Width;
            _winRestoreTop = appWindow.Position.Y;
            _winRestoreLeft = appWindow.Position.X;
        }
    }

    [DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
    private void AppWindow_Changed(AppWindow sender, AppWindowChangedEventArgs args)
    {
        if (!args.DidPositionChange)
        {
            return;
        }

        //Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (sender == null)
        {
            return;
        }
        if (sender.Presenter is not OverlappedPresenter presenter)
        {
            return;
        }
        if (presenter.State == OverlappedPresenterState.Restored)
        {
            _winRestoreHeight = sender.Size.Height;
            _winRestoreWidth = sender.Size.Width;
            _winRestoreTop = sender.Position.Y;
            _winRestoreLeft = sender.Position.X;
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        Debug.WriteLine("Window_Closed");

        // Unsubscribe events that subscribed in the ctr.
        this.Closed -= Window_Closed;
        this.AppWindow?.Closing -= AppWindow_Closing;
        this.SizeChanged -= Window_SizeChanged;
        this.AppWindow?.Changed -= AppWindow_Changed;
        this.Activated -= MainWindow_Activated;

        // * Manually dispose because now we don't use IHost to manage lifetime of classes with IDisposable interface.
        // If we use IHost, we can remove this and let IHost manage lifetime of those.
        // * Disconnect from MPD and close socket connection.
        _vm.Dispose();
    }

    [DynamicWindowsRuntimeCast(typeof(OverlappedPresenter))]
    private async void AppWindow_Closing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        Debug.WriteLine("AppWindow_Closing");

        if (_isClosing) return;
        _isClosing = true;

        args.Cancel = true;

        _vm.UpdateSettings();

        #region == Save Configuration ==

        //SaveSettings();
        var config = _configurationService.CurrentConfiguration;

        OverlappedPresenterState? winState = null; // For AOT workaround, don't set default.  = OverlappedPresenterState.Restored;

        //Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (sender != null)
        {
            // AOT bad
            // https://github.com/microsoft/CsWinRT/issues/1930
            if (sender.Presenter is OverlappedPresenter presenter)
            {
                // AOT bad
                //OverlappedPresenter? presenter = appWindow.Presenter as OverlappedPresenter;

                if (presenter?.State == OverlappedPresenterState.Maximized)
                {
                    winState = OverlappedPresenterState.Maximized;
                }
                else if (presenter?.State == OverlappedPresenterState.Minimized)
                {
                    winState = OverlappedPresenterState.Minimized;
                }
                else if (presenter?.State == OverlappedPresenterState.Restored)
                {
                    winState = OverlappedPresenterState.Restored;
                }
            }
            else
            {
                //App.AppendErrorLog("AOT Error",$"{appWindow.Presenter}"); //Microsoft.UI.Windowing.AppWindowPresenter
                //App.SaveErrorLog();

                try
                {
                    // For AOT tmp workaround
                    OverlappedPresenter presenterAOT = this.AppWindow.Presenter.As<OverlappedPresenter>();

                    if (presenterAOT.State == OverlappedPresenterState.Maximized)
                    {
                        winState = OverlappedPresenterState.Maximized;
                    }
                    else if (presenterAOT.State == OverlappedPresenterState.Minimized)
                    {
                        winState = OverlappedPresenterState.Minimized;
                    }
                    else if (presenterAOT.State == OverlappedPresenterState.Restored)
                    {
                        winState = OverlappedPresenterState.Restored;
                    }
                }
                catch (Exception ex)
                {
                    (Application.Current as App)?.AppendErrorLog("AOT Error@SaveSettings", $"{ex}");
                    (Application.Current as App)?.SaveErrorLog();
                }

            }

            // For AOT, check if winState is null or not.
            if (winState != null)
            {
                // Main Window attributes
                if (winState == OverlappedPresenterState.Restored)
                {
                    config.MainWindow.Width = sender.Size.Width;
                }
                else
                {
                    config.MainWindow.Width = _winRestoreWidth;
                }
                config.MainWindow.RestoreWidth = _winRestoreWidth;

                if (winState == OverlappedPresenterState.Restored)
                {
                    config.MainWindow.Height = sender.Size.Height;
                }
                else
                {
                    config.MainWindow.Height = _winRestoreHeight;
                }
                config.MainWindow.RestoreHeight = _winRestoreHeight;

                if (winState == OverlappedPresenterState.Restored)
                {
                    config.MainWindow.Top = sender.Position.Y;
                }
                else
                {
                    config.MainWindow.Top = _winRestoreTop;
                }
                config.MainWindow.RestoreTop = _winRestoreTop;

                if (winState == OverlappedPresenterState.Restored)
                {
                    config.MainWindow.Left = sender.Position.X;
                }
                else
                {
                    config.MainWindow.Left = _winRestoreLeft;
                }
                config.MainWindow.RestoreLeft = _winRestoreLeft;

                if (winState == OverlappedPresenterState.Maximized)
                {
                    //attrs.Value = "Maximized";
                    config.MainWindow.WindowState = OverlappedPresenterState.Maximized;
                }
                else if (winState == OverlappedPresenterState.Restored)
                {
                    //attrs.Value = "Normal";
                    config.MainWindow.WindowState = OverlappedPresenterState.Restored;
                }
                else if (winState == OverlappedPresenterState.Minimized)
                {
                    //attrs.Value = "Minimized";
                    config.MainWindow.WindowState = OverlappedPresenterState.Restored; // Ignore actual value.
                }
            }
        }

        await _configurationService.SaveConfigurationAsync();

        #endregion

        this.Close();
    }

    #region == Global Hotkey == 
    /*
    // HotKey WM ID
    private const int WM_HOTKEY = 0x0312;

    private const int HOTKEY_ID1 = 0x0001; // play/pause
    private const int HOTKEY_ID2 = 0x0002; // next
    private const int HOTKEY_ID3 = 0x0003; // prev
    private const int HOTKEY_ID4 = 0x0004; // vol up
    private const int HOTKEY_ID5 = 0x0005; // vol up
    private const int HOTKEY_ID6 = 0x0006; // vol down
    private const int HOTKEY_ID7 = 0x0007; // vol down

    // 
    private const int HOTKEY_ID8 = 0x0008; // MediaPlayPause
    private const int HOTKEY_ID9 = 0x0009; // MediaStop
    private const int HOTKEY_ID10 = 0x0010; // MediaNextTrack
    private const int HOTKEY_ID11 = 0x0011; // MediaPreviousTrack

    //private readonly IntPtr WindowHandle;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;
    */
    /*
    private void SetUpHotKey()
    {
        var result1 = RegisterHotKey(WindowHandle, HOTKEY_ID1, MOD_CONTROL, (int)Windows.System.VirtualKey.Space);
        if (result1 == 0)
        {
            Debug.WriteLine("HotKey1(Space) register failed.");
        }

        var result2 = RegisterHotKey(WindowHandle, HOTKEY_ID2, MOD_CONTROL, (int)Windows.System.VirtualKey.Right);
        if (result2 == 0)
        {
            Debug.WriteLine("HotKey2(Right) register failed.");
        }

        var result3 = RegisterHotKey(WindowHandle, HOTKEY_ID3, MOD_CONTROL, (int)Windows.System.VirtualKey.Left);
        if (result3 == 0)
        {
            Debug.WriteLine("HotKey3(Left) register failed.");
        }

        var result4 = RegisterHotKey(WindowHandle, HOTKEY_ID4, MOD_CONTROL, (int)Windows.System.VirtualKey.Up);
        if (result4 == 0)
        {
            Debug.WriteLine("HotKey4(Up) register failed.");
        }

        var result5 = RegisterHotKey(WindowHandle, HOTKEY_ID5, MOD_CONTROL, (int)Windows.System.VirtualKey.Add);
        if (result5 == 0)
        {
            Debug.WriteLine("HotKey5(Add) register failed.");
        }

        var result6 = RegisterHotKey(WindowHandle, HOTKEY_ID6, MOD_CONTROL, (int)Windows.System.VirtualKey.Down);
        if (result6 == 0)
        {
            Debug.WriteLine("HotKey6(Down) register failed.");
        }

        var result7 = RegisterHotKey(WindowHandle, HOTKEY_ID7, MOD_CONTROL, (int)Windows.System.VirtualKey.Subtract);
        if (result7 == 0)
        {
            Debug.WriteLine("HotKey7(Subtract) register failed.");
        }
    }
    */
    /*
    private void CleanUpHotKey()
    {
        Unregister(HOTKEY_ID1);
        Unregister(HOTKEY_ID2);
        Unregister(HOTKEY_ID3);
        Unregister(HOTKEY_ID4);
        Unregister(HOTKEY_ID5);
        Unregister(HOTKEY_ID6);
        Unregister(HOTKEY_ID7);
        //Unregister(HOTKEY_ID8);
        //Unregister(HOTKEY_ID9);
        //Unregister(HOTKEY_ID10);
        //Unregister(HOTKEY_ID11);
    }
    */
    /*
    private void OnWindowMessage(object? sender, WindowMessageHook.MessageEventArgs e)
    {
        if (e.Message != WM_HOTKEY) return;

        switch (e.WParam.ToInt32())
        {
            case HOTKEY_ID1:
                //Task.Run(_vm.PlayCommand.Execute());
                _vm.PlayCommand.Execute(null);
                break;
            case HOTKEY_ID2:
                _vm.PlayNextCommand.Execute(null);
                break;
            case HOTKEY_ID3:
                _vm.PlayPrevCommand.Execute(null);
                break;
            case HOTKEY_ID4:
                _vm.VolumeUpCommand.Execute(null);
                break;
            case HOTKEY_ID5:
                _vm.VolumeUpCommand.Execute(null);
                break;
            case HOTKEY_ID6:
                _vm.VolumeDownCommand.Execute(null);
                break;
            case HOTKEY_ID7:
                _vm.VolumeDownCommand.Execute(null);
                break;
            case HOTKEY_ID8:
                _vm.PlayCommand.Execute(null);
                break;
            case HOTKEY_ID9:
                _vm.PlayCommand.Execute(null);
                break;
            case HOTKEY_ID10:
                _vm.PlayNextCommand.Execute(null);
                break;
            case HOTKEY_ID11:
                _vm.PlayPrevCommand.Execute(null);
                break;
            default:
                break;
        }
    }

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int RegisterHotKey(IntPtr hWnd, int id, int modKey, int vKey);

    [LibraryImport("user32.dll", SetLastError = true)]
    private static partial int UnregisterHotKey(IntPtr hWnd, int id);

    public bool Unregister(int id)
    {
        var ret = UnregisterHotKey(WindowHandle, id);
        return ret == 0;
    }

    public partial class WindowMessageHook : IDisposable
    {
        private readonly nint _hwnd;
        private readonly SUBCLASSPROC _subclassProc;
        private bool _isHooked;

        private const uint WM_HOTKEY = 0x0312;

        public event EventHandler<MessageEventArgs>? Message;

        public WindowMessageHook(Window window)
        {
            _hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
            _subclassProc = new SUBCLASSPROC(SubclassProc);
            _isHooked = SetWindowSubclass(_hwnd, _subclassProc, 0, 0);
        }

        private nint SubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam, nint uIdSubclass, nint dwRefData)
        {
            if (uMsg == WM_HOTKEY)
            {
                Message?.Invoke(this, new MessageEventArgs(uMsg, wParam, lParam));
            }

            return DefSubclassProc(hWnd, uMsg, wParam, lParam);
        }

        public void Dispose()
        {
            if (_isHooked)
            {
                RemoveWindowSubclass(_hwnd, _subclassProc, 0);
                _isHooked = false;
            }

            GC.SuppressFinalize(this);
        }

        public class MessageEventArgs(uint message, nint wParam, nint lParam) : EventArgs
        {
            public uint Message { get; } = message;
            public nint WParam { get; } = wParam;
            public nint LParam { get; } = lParam;
        }

        [LibraryImport("Comctl32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool SetWindowSubclass(nint hWnd, SUBCLASSPROC pfnSubclass, nint uIdSubclass, nint dwRefData);

        [LibraryImport("Comctl32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static partial bool RemoveWindowSubclass(nint hWnd, SUBCLASSPROC pfnSubclass, nint uIdSubclass);

        [LibraryImport("Comctl32.dll")]
        private static partial nint DefSubclassProc(nint hWnd, uint uMsg, nint wParam, nint lParam);

        private delegate nint SUBCLASSPROC(nint hWnd, uint uMsg, nint wParam, nint lParam, nint uIdSubclass, nint dwRefData);
    }
    */
    #endregion

    #region == Windows.Media.SystemMediaTransportControls ==

    private async void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        Debug.WriteLine("ButtonPressed via SystemMediaTransportControls");

        if (_smtc is null) return;

        //if (CurrentDispatcherQueue is null) return;

        // Media key events are dispatched on a background thread.
        // Use the DispatcherQueue to execute code on the UI thread.
        await _dispatcherService.EnqueueAsync(() =>
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    //Task.Run(_vm.Play);
                    _vm.PlayCommand.Execute(null);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    //Task.Run(_vm.Pause);
                    _vm.PauseCommand.Execute(null);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case SystemMediaTransportControlsButton.Next:
                    //Task.Run(_vm.PlayNext);
                    _vm.PlayNextCommand.Execute(null);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    //Task.Run(_vm.PlayPrev);
                    _vm.PlayPrevCommand.Execute(null);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    //Task.Run(_vm.Stop);
                    _vm.StopCommand.Execute(null);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
            }
        });
    }

    #endregion
}

