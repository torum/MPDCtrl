using CommunityToolkit.WinUI;
using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
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
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Control;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Windows.System;
using WinRT;
using WinRT.Interop;

namespace MPDCtrl.Views;

public sealed partial class MainWindow : Window
{
    private readonly MainViewModel _vm;

    // DispatcherQueue
    private Microsoft.UI.Dispatching.DispatcherQueue? _currentDispatcherQueue;// = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
    public Microsoft.UI.Dispatching.DispatcherQueue? CurrentDispatcherQueue => _currentDispatcherQueue;

    // WinUI3 workaround.
    private readonly MediaPlayer? _mediaPlayer;
    private readonly SystemMediaTransportControls? _smtc;
    private readonly bool _isMediaTransportControlEnable = false;

    private readonly WindowMessageHook? _hook;
    private readonly bool _isGlobalHotKeyEnable = false;

    // Window position and size
    // TODO: Change this lator.1920x1080
    private int winRestoreWidth = 1024;//1024;
    private int winRestoreHeight = 768;//768;
    private int winRestoreTop = 100;
    private int winRestoreleft = 100;

    //private readonly UISettings settings;
    private ElementTheme theme = ElementTheme.Default;

    public MainWindow()
    {
        // This DispatcherQueue should be alive as long as MainWindow is alive. Make sure to clear when the window is closed.
        _currentDispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

        _vm = App.GetService<MainViewModel>();

        if (this.AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.PreferredMinimumWidth = 500;
            presenter.PreferredMinimumHeight = 780;
        }

        InitializeComponent();

        this.ExtendsContentIntoTitleBar = true;

        LoadSettings();

        // It's important to set content as early as here in order to set theme.
        // But make sure to call LoadSettings() before in order to apply settings value for contents.
        this.Content = App.GetService<ShellPage>();

        // It is necessary to set theme here after the content is set.
        if (this.Content is ShellPage root)
        {
            root.RequestedTheme = theme;

            //TitleBarHelper.UpdateTitleBar(theme, this);
            SetCapitionButtonColor();

            root.CallMeWhenMainWindowIsReady(this);
        }

        _isGlobalHotKeyEnable = false;

        if (_isGlobalHotKeyEnable)
        {
            WindowHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            _hook = new WindowMessageHook(this);
            _hook.Message += OnWindowMessage;

            SetUpHotKey();

            this.Closed += (s, e) =>
            {
                CleanUpHotKey();
                _hook.Dispose();
            };
        }

        _isMediaTransportControlEnable = true;

        if (_isMediaTransportControlEnable)
        {
            // Stupid WinAppSDK and WinUI3 needs workaround for media key control. 
            //_smtc = SystemMediaTransportControls.GetForCurrentView(); //<- this is only works in UWP.
            // So, get the SystemMediaTransportControls from the MediaPlayer instance for workaround.
            // This is just so stupid.

            _mediaPlayer = new MediaPlayer();
            //_mediaPlayer.CommandManager.IsEnabled = false; <- not good if false.
            _smtc = _mediaPlayer.SystemMediaTransportControls;

            _smtc.IsPlayEnabled = true;
            _smtc.IsPauseEnabled = true;
            _smtc.IsNextEnabled = true;
            _smtc.IsPreviousEnabled = true;
            _smtc.IsStopEnabled = true;

            _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;

            //
            _smtc.ButtonPressed += Smtc_ButtonPressed;

            //
            var updater = _smtc.DisplayUpdater;
            updater.Type = MediaPlaybackType.Music;
            updater.MusicProperties.Title = "a title";
            updater.Update();
        }

    }

    private void LoadSettings()
    {
/*
        var filePath = App.AppConfigFilePath;
        if (RuntimeHelper.IsMSIX)
        {
            filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.AppName + ".config");
        }
*/
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

            theme = ElementTheme.Default;

            return;
        }

        var winState = OverlappedPresenterState.Restored;

        ElementTheme eleThme = ElementTheme.Default;
        SystemBackdropOption bd = SystemBackdropOption.None;
        bool isFoundNewThemeSetting = false;

        double top = 100;
        double left = 100;
        double height = 768;
        double width = 1024;

        var xdoc = XDocument.Load(App.AppConfigFilePath);

        // Main window
        if (xdoc.Root != null)
        {
            // Main Window element
            var mainWindow = xdoc.Root.Element("MainWindow");
            if (mainWindow != null)
            {
                var hoge = mainWindow.Attribute("top");
                if (hoge != null)
                {
                    top = double.Parse(hoge.Value);
                }

                hoge = mainWindow.Attribute("left");
                if (hoge != null)
                {
                    left = double.Parse(hoge.Value);
                }

                hoge = mainWindow.Attribute("height");
                if (hoge != null)
                {
                    height = double.Parse(hoge.Value);
                }

                hoge = mainWindow.Attribute("width");
                if (hoge != null)
                {
                    width = double.Parse(hoge.Value);
                }

                hoge = mainWindow.Attribute("state");
                if (hoge != null)
                {
                    if (hoge.Value == "Maximized")
                    {
                        winState = OverlappedPresenterState.Maximized;
                    }
                    else if (hoge.Value == "Normal")
                    {
                        winState = OverlappedPresenterState.Restored;
                    }
                    else if (hoge.Value == "Minimized")
                    {
                        // Ignore minimized.
                        winState = OverlappedPresenterState.Restored;
                    }
                }

                var xLay = mainWindow.Element("Layout");
                if (xLay != null)
                {
                    if (xLay.Attribute("navigationViewMenuOpen") != null)
                    {
                        var xbool = xLay.Attribute("navigationViewMenuOpen")?.Value;
                        if (!string.IsNullOrEmpty(xbool))
                        {
                            if (xbool.Equals("True"))
                            {
                                _vm.IsNavigationViewMenuOpen = true;
                            }
                            else
                            {
                                _vm.IsNavigationViewMenuOpen = false;
                            }
                        }
                    }
                }
            }

            // Themes
            var opts = xdoc.Root.Element("Theme");
            if (opts != null)
            {
                var xvalue = opts.Attribute("current");
                if (xvalue != null)
                {
                    if (!string.IsNullOrEmpty(xvalue.Value))
                    {
                        if (Enum.TryParse(xvalue.Value, out ElementTheme cacheTheme))
                        {
                            eleThme = cacheTheme;
                            isFoundNewThemeSetting = true;
                        }
                    }
                }
                xvalue = opts.Attribute("backdrop");
                if (xvalue != null)
                {
                    if (!string.IsNullOrEmpty(xvalue.Value))
                    {
                        if (Enum.TryParse(xvalue.Value, out SystemBackdropOption cacheBackdrop))
                        {
                            bd = cacheBackdrop;
                            isFoundNewThemeSetting = true;
                        }
                    }
                }
            }

            // Options
            opts = xdoc.Root.Element("Opts");
            if (opts != null)
            {
                /*
                xvalue = opts.Attribute("IsDebugSaveLog");
                if (xvalue != null)
                {
                    if (!string.IsNullOrEmpty(xvalue.Value))
                    {
                        //MainViewModel.IsDebugSaveLog = xvalue.Value == "True";
                    }
                }
                */
            }

            #region == Profiles  ==

            var xProfiles = xdoc.Root.Element("Profiles");
            if (xProfiles is not null)
            {
                var profileList = xProfiles.Elements("Profile");

                foreach (var p in profileList)
                {
                    Profile pro = new();

                    if (p.Attribute("Name") is not null)
                    {
                        var s = p.Attribute("Name")?.Value;
                        if (!string.IsNullOrEmpty(s))
                            pro.Name = s;
                    }
                    if (p.Attribute("Host") is not null)
                    {
                        var s = p.Attribute("Host")?.Value;
                        if (!string.IsNullOrEmpty(s))
                            pro.Host = s;
                    }
                    if (p.Attribute("Port") is not null)
                    {
                        var s = p.Attribute("Port")?.Value;
                        if (!string.IsNullOrEmpty(s))
                        {
                            try
                            {
                                pro.Port = Int32.Parse(s);
                            }
                            catch
                            {
                                pro.Port = 6600;
                            }
                        }
                    }
                    if (p.Attribute("Password") is not null)
                    {
                        var s = p.Attribute("Password")?.Value;
                        if (!string.IsNullOrEmpty(s))
                            pro.Password = MainViewModel.Decrypt(s);
                    }
                    if (p.Attribute("IsDefault") is not null)
                    {
                        var s = p.Attribute("IsDefault")?.Value;
                        if (!string.IsNullOrEmpty(s))
                        {
                            if (s == "True")
                            {
                                pro.IsDefault = true;

                            }
                        }
                    }
                    if (p.Attribute("Volume") is not null)
                    {
                        var s = p.Attribute("Volume")?.Value;
                        if (!string.IsNullOrEmpty(s))
                        {
                            try
                            {
                                pro.Volume = double.Parse(s);
                            }
                            catch
                            {
                                pro.Volume = 50;
                            }
                        }
                    }

                    if (!string.IsNullOrEmpty(pro.Host.Trim()))
                        {
                            if (pro.IsDefault)
                            {
                                _vm.CurrentProfile = pro;

                                // Only add if Hot is present?
                                _vm.Profiles.Add(pro);
                            }
                        }
                }
            }

            #endregion
        }

        if (_vm.Profiles.Count > 0)
        {
            if (_vm.CurrentProfile is null)
            {
                var prof = _vm.Profiles.FirstOrDefault(x => x.IsDefault);
                _vm.CurrentProfile = prof ?? _vm.Profiles[0];
            }
        } 

        winRestoreWidth = (int)width;
        winRestoreHeight = (int)height;
        winRestoreTop = (int)top;
        winRestoreleft = (int)left;

        // Restore window size and position
        Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (appWindow != null)
        {
            // Window state
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                if (winState == OverlappedPresenterState.Maximized)
                {
                    // Sets restore size and position.
                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(winRestoreleft, winRestoreTop, winRestoreWidth, winRestoreHeight));
                    // Maximize the window.
                    presenter.Maximize();
                }
                else if (winState == OverlappedPresenterState.Minimized)
                {
                    // This should not happen, but just in case.
                    presenter.Restore();
                    // Sets restore size and position.
                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(winRestoreleft, winRestoreTop, winRestoreWidth, winRestoreHeight));
                }
                else
                {
                    // Sets restore size and position.
                    appWindow.MoveAndResize(new Windows.Graphics.RectInt32(winRestoreleft, winRestoreTop, winRestoreWidth, winRestoreHeight));
                }
            }

            //
            appWindow.Closing += (s, a) =>
            {
                //
            };
        }

        // For the strictly backward compatibility reason, load preference from localsetting.
        if (RuntimeHelper.IsMSIX && (!isFoundNewThemeSetting))
        {
            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("AppSystemBackdropOption", out var obj))
            {
                if (obj is not null)
                {
                    if (obj is string s)
                    {
                        if (s == SystemBackdropOption.Acrylic.ToString())
                        {
                            bd = SystemBackdropOption.Acrylic;
                        }
                        else if (s == SystemBackdropOption.Mica.ToString())
                        {
                            bd = SystemBackdropOption.Mica;
                        }
                    }
                }
            }

            if (ApplicationData.Current.LocalSettings.Values.TryGetValue("AppBackgroundRequestedTheme", out var obj2))
            {
                if (obj2 is not null)
                {
                    if (obj2 is string themeName)
                    {
                        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
                        {
                            eleThme = cacheTheme;
                        }
                    }
                }
            }
        }

        // Apply theme and backdrop
        if (bd != SystemBackdropOption.None)
        {
            theme = eleThme;
            _vm.Theme = eleThme;
        }
        _vm.Material = bd;
        SwitchBackdrop(bd);
    }

    public void SwitchBackdrop(SystemBackdropOption backdrop)
    {
        var vm = App.GetService<MainViewModel>();

        if (backdrop == SystemBackdropOption.Mica)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                this.SystemBackdrop = new MicaBackdrop()
                {
                    Kind = MicaKind.Base
                };
                vm.Material = SystemBackdropOption.Mica;
                //TitleBarHelper.UpdateTitleBar(Theme, App.MainWnd);

                vm.IsBackdropEnabled = true;
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
                vm.Material = SystemBackdropOption.MicaAlt;
                //TitleBarHelper.UpdateTitleBar(Theme, App.MainWnd);

                vm.IsBackdropEnabled = true;
            }
        }
        else if (backdrop == SystemBackdropOption.Acrylic)
        {
            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                this.SystemBackdrop = new DesktopAcrylicBackdrop();

                vm.Material = SystemBackdropOption.Acrylic;
                //TitleBarHelper.UpdateTitleBar(Theme, App.MainWnd);

                vm.IsBackdropEnabled = true;
            }
        }
        else if (backdrop == SystemBackdropOption.None)
        {
            this.SystemBackdrop = null;

            vm.Material = SystemBackdropOption.None;
            vm.IsBackdropEnabled = false;
            vm.Theme = ElementTheme.Default;
            theme = ElementTheme.Default;
            if (this.Content is ShellPage root)
            {
                root.RequestedTheme = theme;

                //TitleBarHelper.UpdateTitleBar(theme, this);
                SetCapitionButtonColor();
            }

            this.SystemBackdrop = null;
        }
    }

    public void SetCapitionButtonColor()
    {
        var currentTheme = ((FrameworkElement)Content).ActualTheme;
        if (currentTheme == ElementTheme.Dark)
        {
            this.AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
            this.AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.White;
        }
        else if (currentTheme == ElementTheme.Light)
        {
            this.AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
            this.AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Black;
        }
        else
        {
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                this.AppWindow.TitleBar.ButtonForegroundColor = Colors.White;
                this.AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.White;
            }
            else
            {
                this.AppWindow.TitleBar.ButtonForegroundColor = Colors.Black;
                this.AppWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Black;
            }
        }
    }

    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (appWindow != null)
        {
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State == OverlappedPresenterState.Maximized)
                {
                }
                else if (presenter.State == OverlappedPresenterState.Minimized)
                {
                }
                else
                {
                    winRestoreHeight = (int)appWindow.Size.Height;
                    winRestoreWidth = (int)appWindow.Size.Width;
                    winRestoreTop = (int)appWindow.Position.Y;
                    winRestoreleft = (int)appWindow.Position.X;
                }
            }
        }
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        // Disconnect from MPD and close socket connection.
        _vm.CleanUp();

        // For some stupid reason, we needed this, otherwise we get COM error.
        _currentDispatcherQueue = null;

        SaveSettings();
    }

    private void SaveSettings()
    {
        XmlDocument doc = new();
        var xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

        // Root Document Element
        var root = doc.CreateElement(string.Empty, "App", string.Empty);
        doc.AppendChild(root);

        //XmlAttribute attrs = doc.CreateAttribute("Version");
        //attrs.Value = _appVer;
        //root.SetAttributeNode(attrs);
        XmlAttribute attrs;

        #region == Main window ==

        // Main Window element
        var mainWindow = doc.CreateElement(string.Empty, "MainWindow", string.Empty);

        var winState = OverlappedPresenterState.Restored;

        Microsoft.UI.Windowing.AppWindow? appWindow = this.AppWindow;
        if (appWindow != null)
        {
            if (appWindow.Presenter is OverlappedPresenter presenter)
            {
                if (presenter.State == OverlappedPresenterState.Maximized)
                {
                    winState = OverlappedPresenterState.Maximized;
                }
                else if (presenter.State == OverlappedPresenterState.Minimized)
                {
                    winState = OverlappedPresenterState.Restored;
                }
                else
                {
                    winState = OverlappedPresenterState.Restored;
                }
            }
        }

        // Main Window attributes
        attrs = doc.CreateAttribute("width");
        if (winState == OverlappedPresenterState.Maximized)
        {
            attrs.Value = winRestoreWidth.ToString();
        }
        else
        {
            attrs.Value = this.AppWindow.Size.Width.ToString();
        }
        //attrs.Value = this.AppWindow.Size.Width.ToString();
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("height");
        if (winState == OverlappedPresenterState.Maximized)
        {
            attrs.Value = winRestoreHeight.ToString();
        }
        else
        {
            attrs.Value = this.AppWindow.Size.Height.ToString();
        }
        //attrs.Value = App.MainWindow.AppWindow.Size.Height.ToString();//App.MainWindow.GetAppWindow().Size.Height.ToString();
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("top");
        if (winState == OverlappedPresenterState.Maximized)
        {
            attrs.Value = winRestoreTop.ToString();
        }
        else
        {
            attrs.Value = this.AppWindow.Position.Y.ToString();
        }
        //attrs.Value = App.MainWindow.AppWindow.Position.Y.ToString();
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("left");
        if (winState == OverlappedPresenterState.Maximized)
        {
            attrs.Value = winRestoreleft.ToString();
        }
        else
        {
            attrs.Value = this.AppWindow.Position.X.ToString();
        }
        //attrs.Value = App.MainWindow.AppWindow.Position.X.ToString();
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("state");
        if (winState == OverlappedPresenterState.Maximized)
        {
            attrs.Value = "Maximized";
        }
        else if (winState == OverlappedPresenterState.Restored)
        {
            attrs.Value = "Normal";

        }
        else if (winState == OverlappedPresenterState.Minimized)
        {
            attrs.Value = "Minimized";
        }
        mainWindow.SetAttributeNode(attrs);

        #endregion

        #region == MainWindow.Layout ==

        var xLay = doc.CreateElement(string.Empty, "Layout", string.Empty);

        attrs = doc.CreateAttribute("navigationViewMenuOpen");
        if (_vm.IsNavigationViewMenuOpen)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        xLay.SetAttributeNode(attrs);

        mainWindow.AppendChild(xLay);

        // set Main Window element to root.
        root.AppendChild(mainWindow);

        #endregion

        #region == Theme ==

        // Themes
        var xTheme = doc.CreateElement(string.Empty, "Theme", string.Empty);

        attrs = doc.CreateAttribute("current");
        attrs.Value = _vm.Theme.ToString();
        xTheme.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("backdrop");
        attrs.Value = _vm.Material.ToString();
        xTheme.SetAttributeNode(attrs);

        root.AppendChild(xTheme);

        #endregion

        #region == Options ==

        // Options
        var xOpts = doc.CreateElement(string.Empty, "Opts", string.Empty);

        //attrs = doc.CreateAttribute("isDebugSaveLog");
        //attrs.Value = MainViewModel.IsDebugSaveLog.ToString();
        //xOpts.SetAttributeNode(attrs);

        root.AppendChild(xOpts);

        #endregion

        #region == Profiles  ==

        XmlElement xProfiles = doc.CreateElement(string.Empty, "Profiles", string.Empty);

        XmlElement xProfile;
        XmlAttribute xAttrs;

        if (_vm.Profiles.Count == 1)
            _vm.Profiles[0].IsDefault = true;

        foreach (var p in _vm.Profiles)
        {
            xProfile = doc.CreateElement(string.Empty, "Profile", string.Empty);

            xAttrs = doc.CreateAttribute("Name");
            xAttrs.Value = p.Name;
            xProfile.SetAttributeNode(xAttrs);

            xAttrs = doc.CreateAttribute("Host");
            xAttrs.Value = p.Host;
            xProfile.SetAttributeNode(xAttrs);

            xAttrs = doc.CreateAttribute("Port");
            xAttrs.Value = p.Port.ToString();
            xProfile.SetAttributeNode(xAttrs);

            xAttrs = doc.CreateAttribute("Password");
            xAttrs.Value = MainViewModel.Encrypt(p.Password);

            xProfile.SetAttributeNode(xAttrs);

            if (p.IsDefault)
            {
                xAttrs = doc.CreateAttribute("IsDefault");
                xAttrs.Value = "True";
                xProfile.SetAttributeNode(xAttrs);
            }

            xAttrs = doc.CreateAttribute("Volume");
            if (p == _vm.CurrentProfile)
            {
                xAttrs.Value = _vm.Volume.ToString();
            }
            else
            {
                xAttrs.Value = p.Volume.ToString();
            }
            xProfile.SetAttributeNode(xAttrs);

            xProfiles.AppendChild(xProfile);
        }

        root.AppendChild(xProfiles);
        /*
        if (_vm.IsRememberAsProfile)
        {
            root.AppendChild(xProfiles);
        }
        */

        #endregion

        /*
        var filePath = App.AppConfigFilePath;
        if (RuntimeHelper.IsMSIX)
        {
            filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.AppName + ".config");
        }
        else
        {
            System.IO.Directory.CreateDirectory(App.AppDataFolder);
        }
        */

        if (!Directory.Exists(App.AppDataFolder))
        {
            System.IO.Directory.CreateDirectory(App.AppDataFolder);
        }

        try
        {
            doc.Save(App.AppConfigFilePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("MainWindow_Closed: " + ex + " while saving : " + App.AppConfigFilePath);
        }
    }

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

    // 
    private const int HOTKEY_ID8 = 0x0008; // MediaPlayPause
    private const int HOTKEY_ID9 = 0x0009; // MediaStop
    private const int HOTKEY_ID10 = 0x0010; // MediaNextTrack
    private const int HOTKEY_ID11 = 0x0011; // MediaPreviousTrack

    private readonly IntPtr WindowHandle;
    private const int MOD_CONTROL = 0x0002;
    private const int MOD_SHIFT = 0x0004;

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

    private void OnWindowMessage(object? sender, WindowMessageHook.MessageEventArgs e)
    {
        if (e.Message != WM_HOTKEY) return;

        switch (e.WParam.ToInt32())
        {
            case HOTKEY_ID1:
                Task.Run(_vm.Play);
                break;
            case HOTKEY_ID2:
                Task.Run(_vm.PlayNext);
                break;
            case HOTKEY_ID3:
                Task.Run(_vm.PlayPrev);
                break;
            case HOTKEY_ID4:
                _vm.VolumeUp();
                break;
            case HOTKEY_ID5:
                _vm.VolumeUp();
                break;
            case HOTKEY_ID6:
                _vm.VolumeDown();
                break;
            case HOTKEY_ID7:
                _vm.VolumeDown();
                break;
            case HOTKEY_ID8:
                Task.Run(_vm.Play);
                break;
            case HOTKEY_ID9:
                Task.Run(_vm.Play);
                break;
            case HOTKEY_ID10:
                Task.Run(_vm.PlayNext);
                break;
            case HOTKEY_ID11:
                Task.Run(_vm.PlayPrev);
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

    #endregion

    #region == Windows.Media.SystemMediaTransportControls ==

    private async void Smtc_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
    {
        Debug.WriteLine("Smtc_ButtonPressed");

        if (_smtc is null) return;

        if (CurrentDispatcherQueue is null)
        {
            return;
        }

        // Media key events are dispatched on a background thread.
        // Use the DispatcherQueue to execute code on the UI thread.
        await CurrentDispatcherQueue.EnqueueAsync(() =>
        {
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Play:
                    Task.Run(_vm.Play);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case SystemMediaTransportControlsButton.Pause:
                    Task.Run(_vm.Pause);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Task.Run(_vm.PlayNext);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Task.Run(_vm.PlayPrev);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case SystemMediaTransportControlsButton.Stop:
                    Task.Run(_vm.Stop);
                    _smtc.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
            }
        });
    }

    #endregion
}

