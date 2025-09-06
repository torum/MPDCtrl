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
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml;
using System.Xml.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;

namespace MPDCtrl.Views;

public sealed partial class MainWindow : Window
{
    // DispatcherQueue
    private Microsoft.UI.Dispatching.DispatcherQueue? _currentDispatcherQueue;// = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();
    public Microsoft.UI.Dispatching.DispatcherQueue? CurrentDispatcherQueue => _currentDispatcherQueue;

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
        _currentDispatcherQueue = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread();

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
            SetCapitionButtonColorForWin11();

            root.CallMeWhenMainWindowIsReady(this);
        }
    }

    private void LoadSettings()
    {
        var filePath = App.AppConfigFilePath;
        if (RuntimeHelper.IsMSIX)
        {
            filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.AppName + ".config");
        }

        var vm = App.GetService<MainViewModel>();

        if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
        {
            vm.IsAcrylicSupported = true;

            vm.IsBackdropEnabled = true;
        }
        if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
        {
            vm.IsMicaSupported = true;

            vm.IsBackdropEnabled = true;
        }

        if (!System.IO.File.Exists(filePath))
        {
            // Sets default.

            if (Microsoft.UI.Composition.SystemBackdrops.DesktopAcrylicController.IsSupported())
            {
                vm.IsAcrylicSupported = true;
                SystemBackdrop = new DesktopAcrylicBackdrop();
                vm.Material = SystemBackdropOption.Acrylic;

                vm.IsBackdropEnabled = true;
            }
            else if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                vm.IsMicaSupported = true;
                SystemBackdrop = new MicaBackdrop()
                {
                    Kind = MicaKind.Base
                };
                vm.Material = SystemBackdropOption.Mica;

                vm.IsBackdropEnabled = true;
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

        var xdoc = XDocument.Load(filePath);

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
                                vm.IsNavigationViewMenuOpen = true;
                            }
                            else
                            {
                                vm.IsNavigationViewMenuOpen = false;
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
            vm.Theme = eleThme;
        }
        vm.Material = bd;
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
                SetCapitionButtonColorForWin11();
            }

            this.SystemBackdrop = null;
        }
    }

    public void SetCapitionButtonColorForWin11()
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
        // For some stupid reason, we needed this, otherwise we get COM error.
        _currentDispatcherQueue = null;

        SaveSettings();
    }

    private void SaveSettings()
    {
        var vm = App.GetService<MainViewModel>();

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

        // Main window
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


        #region == MainWindow.Layout ==

        var xLay = doc.CreateElement(string.Empty, "Layout", string.Empty);

        attrs = doc.CreateAttribute("navigationViewMenuOpen");
        if (vm.IsNavigationViewMenuOpen)
        {
            attrs.Value = "True";
        }
        else
        {
            attrs.Value = "False";
        }
        xLay.SetAttributeNode(attrs);

        mainWindow.AppendChild(xLay);

        #endregion


        // set Main Window element to root.
        root.AppendChild(mainWindow);

        // Themes
        var xTheme = doc.CreateElement(string.Empty, "Theme", string.Empty);

        attrs = doc.CreateAttribute("current");
        attrs.Value = vm.Theme.ToString();
        xTheme.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("backdrop");
        attrs.Value = vm.Material.ToString();
        xTheme.SetAttributeNode(attrs);

        root.AppendChild(xTheme);

        // Options
        var xOpts = doc.CreateElement(string.Empty, "Opts", string.Empty);

        //attrs = doc.CreateAttribute("isDebugSaveLog");
        //attrs.Value = MainViewModel.IsDebugSaveLog.ToString();
        //xOpts.SetAttributeNode(attrs);

        root.AppendChild(xOpts);


        var filePath = App.AppConfigFilePath;
        if (RuntimeHelper.IsMSIX)
        {
            filePath = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, App.AppName + ".config");
        }
        else
        {
            System.IO.Directory.CreateDirectory(App.AppDataFolder);
        }

        try
        {
            doc.Save(filePath);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("MainWindow_Closed: " + ex + " while saving : " + filePath);
        }

    }
}
