using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using MPDCtrl.Models;
using MPDCtrl.Services.Contracts;
using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

#pragma warning disable IDE0079 // Remove unnecessary suppression
#pragma warning disable IDE0290 // Use primary constructor

namespace MPDCtrl.Services;

/*
 * WinUI3/AvaloniaUI does not provide a way to get the restore bounds of a window like WPF does when it is maximized or minimized.
 * So, we need to manually track the restore bounds of the window when it is in the restored state.
 * AppWindow_Changed(WinUI3)/Window_PositionChanged(AvaloniaUI) and Window_SizeChanged event handlers are called when the window state changes, such as when the window is maximized, minimized, or restored.
 * Make sure to update the restore bounds when the window is in the restored state.
 * Also, use Window_Activated event handler to update the restore bounds when the window is activated, because sometimes the window state change events are not fired when the window is activated.
 * This way, even if the window is closed while maximized or minimized, we can still save the restore bounds to the configuration file.
    _winRestoreHeight = sender.Size.Height;
    _winRestoreWidth = sender.Size.Width;
    _winRestoreTop = sender.Position.Y;
    _winRestoreLeft = sender.Position.X;
 */

public sealed class ConfigurationService : IConfigurationService
{
    private bool _isInitialized;
    private bool _isConfigLoaded;
    private string? _configFilePath = string.Empty;
    private XDocument? _xdoc;
    private readonly IDispatcherService _dispatcherService;

    public ConfigurationService(IDispatcherService dispatcherService)
    {
        _dispatcherService = dispatcherService;

        _isConfigLoaded = false;
        CurrentConfiguration = new Models.Configuration();
    }

    public Models.Configuration CurrentConfiguration { get; private set; }

    public void Initialize(string configFilePath)
    {
        _configFilePath = configFilePath;
        _isConfigLoaded = false;
        CurrentConfiguration = new Models.Configuration();
        _xdoc = null;
        _isInitialized = true;
    }

    public async Task<Models.Configuration> LoadConfigurationAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ConfigurationService is not initialized.");
        }

        var config = new Models.Configuration();

        if (_isConfigLoaded)
        {
            Debug.WriteLine("LoadConfigurationAsync() - Already loaded, returning current configuration.");
            return CurrentConfiguration ?? config;
        }

        if (string.IsNullOrEmpty(_configFilePath))
        {
            throw new InvalidOperationException("Configuration file path is not set.");
        }
        if (!System.IO.File.Exists(_configFilePath))
        {
            Debug.WriteLine($"Configuration file not found: {_configFilePath}");
            // Set _isConfigLoaded to true so that SaveConfigurationAsync() can create the file with default settings.
            _isConfigLoaded = true;
            return config;
        }

        Debug.WriteLine("LoadConfigurationAsync()");

        try
        {
            _xdoc = XDocument.Load(App.AppConfigFilePath);
            if (_xdoc.Root is null)
            {
                return config;
            }

            #region == Window setting ==
            
            // Main Window element
            var mainWindow = _xdoc.Root.Element("MainWindow");
            if (mainWindow is not null)
            {
                int wY = 24;
                int wX = 24;

                // Window sizes
                var hoge = mainWindow.Attribute("top");
                if (hoge is not null)
                {
                    if (Int32.TryParse(hoge.Value, out wY))
                    {
                        config.MainWindow.Top = wY;
                    }
                }

                hoge = mainWindow.Attribute("left");
                if (hoge is not null)
                {
                    if (Int32.TryParse(hoge.Value, out wX))
                    {
                        config.MainWindow.Left = wX;
                    }
                }

                hoge = mainWindow.Attribute("height");
                if (hoge is not null)
                {
                    if (!string.IsNullOrEmpty(hoge.Value))
                    {
                        config.MainWindow.Height = Int32.Parse(hoge.Value, CultureInfo.InvariantCulture);
                    }
                }

                hoge = mainWindow.Attribute("width");
                if (hoge is not null)
                {
                    if (!string.IsNullOrEmpty(hoge.Value))
                    {
                        config.MainWindow.Width = Int32.Parse(hoge.Value, CultureInfo.InvariantCulture);
                    }
                }

                // Restore window sizes
                hoge = mainWindow.Attribute("restoreTop");
                if (hoge is not null)
                {
                    if (Int32.TryParse(hoge.Value, out wY))
                    {
                        config.MainWindow.RestoreTop = wY;
                    }
                }

                hoge = mainWindow.Attribute("restoreLeft");
                if (hoge is not null)
                {
                    if (Int32.TryParse(hoge.Value, out wX))
                    {
                        config.MainWindow.RestoreLeft = wX;
                    }
                }

                hoge = mainWindow.Attribute("restoreHeight");
                if (hoge is not null)
                {
                    if (!string.IsNullOrEmpty(hoge.Value))
                    {
                        config.MainWindow.RestoreHeight = Int32.Parse(hoge.Value, CultureInfo.InvariantCulture);
                    }
                }

                hoge = mainWindow.Attribute("restoreWidth");
                if (hoge is not null)
                {
                    if (!string.IsNullOrEmpty(hoge.Value))
                    {
                        config.MainWindow.RestoreWidth = Int32.Parse(hoge.Value, CultureInfo.InvariantCulture);
                    }
                }

                // Window state
                hoge = mainWindow.Attribute("state");
                if (hoge is not null)
                {
                    if (hoge.Value == "Maximized")
                    {
                        // TODO: Since there is no restorebounds in AvaloniaUI or WinUI3.....
                        config.MainWindow.WindowState = OverlappedPresenterState.Maximized;
                    }
                    else if (hoge.Value == "Normal")
                    {
                        config.MainWindow.WindowState = OverlappedPresenterState.Restored;
                    }
                    else if (hoge.Value == "Minimized")
                    {
                        // Ignore minimized state, and restore to normal state.
                        config.MainWindow.WindowState = OverlappedPresenterState.Restored;
                    }
                }

                #region == Layout ==

                var lay = mainWindow.Element("Layout");
                if (lay is not null)
                {
                    // old name. Leave it for backward compatibility.
                    var xbool = lay.Attribute("navigationViewMenuOpen")?.Value;
                    if (!string.IsNullOrEmpty(xbool))
                    {
                        if (xbool.Equals("True", StringComparison.OrdinalIgnoreCase))
                        {
                            config.IsNavigationViewMenuOpen = true;
                        }
                        else
                        {
                            config.IsNavigationViewMenuOpen = false;
                        }
                    }

                    xbool = lay.Attribute("isNavigationViewMenuOpen")?.Value;
                    if (!string.IsNullOrEmpty(xbool))
                    {
                        if (xbool.Equals("True", StringComparison.OrdinalIgnoreCase))
                        {
                            config.IsNavigationViewMenuOpen = true;
                        }
                        else
                        {
                            config.IsNavigationViewMenuOpen = false;
                        }
                    }

                }

                #endregion

            }

            #endregion

            #region == Theme ==

            var opts = _xdoc.Root.Element("Theme");
            if (opts != null)
            {
                // Old name. Leave it for backward compatibility.
                var xvalue = opts.Attribute("current");
                if (xvalue != null)
                {
                    if (!string.IsNullOrEmpty(xvalue.Value))
                    {
                        if (Enum.TryParse(xvalue.Value, out ElementTheme cacheTheme))
                        {
                            config.Theme = cacheTheme;
                            //isFoundNewThemeSetting = true;
                        }
                    }
                }
                // Old name. Leave it for backward compatibility.
                xvalue = opts.Attribute("backdrop");
                if (xvalue != null)
                {
                    if (!string.IsNullOrEmpty(xvalue.Value))
                    {
                        if (Enum.TryParse(xvalue.Value, out SystemBackdropOption cacheBackdrop))
                        {
                            config.SystemBackdrop = cacheBackdrop;
                            //isFoundNewThemeSetting = true;
                        }
                    }
                }
                // New name. 
                xvalue = opts.Attribute("ThemeName");
                if (xvalue != null)
                {
                    if (!string.IsNullOrEmpty(xvalue.Value))
                    {
                        if (Enum.TryParse(xvalue.Value, out ElementTheme cacheTheme))
                        {
                            config.Theme = cacheTheme;
                            //isFoundNewThemeSetting = true;
                        }
                    }
                }
                // New name. 
                xvalue = opts.Attribute("SystemBackdrop");
                if (xvalue != null)
                {
                    if (!string.IsNullOrEmpty(xvalue.Value))
                    {
                        if (Enum.TryParse(xvalue.Value, out SystemBackdropOption cacheBackdrop))
                        {
                            config.SystemBackdrop = cacheBackdrop;
                            //isFoundNewThemeSetting = true;
                        }
                    }
                }
            }

            #endregion

            #region == Options ==

            // TODO: Change to "Options" in the future, but keep "Opts" for backward compatibility.
            opts = _xdoc.Root.Element("Opts");
            if (opts is not null)
            {
                var hoge = opts.Attribute("isAutoScrollToNowPlaying");
                if (hoge is not null)
                {
                    if (hoge.Value == "True")
                    {
                        config.IsAutoScrollToNowPlaying = true;
                    }
                    else
                    {
                        config.IsAutoScrollToNowPlaying = false;
                    }
                }

                hoge = opts.Attribute("isArtistSortWithoutThePrefix");
                if (hoge is not null)
                {
                    if (hoge.Value == "True")
                    {
                        config.IsArtistSortWithoutThePrefix = true;
                    }
                    else
                    {
                        config.IsArtistSortWithoutThePrefix = false;
                    }
                }

                hoge = opts.Attribute("isAlbumSortWithoutThePrefix");
                if (hoge is not null)
                {
                    if (hoge.Value == "True")
                    {
                        config.IsAlbumSortWithoutThePrefix = true;
                    }
                    else
                    {
                        config.IsAlbumSortWithoutThePrefix = false;
                    }
                }

                hoge = opts.Attribute("isUpdateOnStartup");
                if (hoge is not null)
                {
                    if (hoge.Value == "True")
                    {
                        config.IsUpdateOnStartup = true;
                    }
                    else
                    {
                        config.IsUpdateOnStartup = false;
                    }
                }

                hoge = opts.Attribute("isDownloadAlbumArtEmbeddedUsingReadPicture");
                if (hoge is not null)
                {
                    if (hoge.Value == "True")
                    {
                        config.IsDownloadAlbumArtEmbeddedUsingReadPicture = true;

                    }
                    else
                    {
                        config.IsDownloadAlbumArtEmbeddedUsingReadPicture = false;
                    }
                }

                hoge = opts.Attribute("isForceSetVolumeExplicitly");
                if (hoge is not null)
                {
                    if (hoge.Value == "True")
                    {
                        config.IsForceSetVolumeExplicitly = true;

                    }
                    else
                    {
                        config.IsForceSetVolumeExplicitly = false;
                    }
                }
            }
            
            #endregion

            #region == Profiles  ==

            var xProfiles = _xdoc.Root.Element("Profiles");
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
                                pro.Port = Int32.Parse(s, CultureInfo.InvariantCulture);
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
                            pro.Password = Helpers.EncryptDecrypt.Decrypt(s);
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
                                pro.Volume = double.Parse(s, CultureInfo.InvariantCulture);
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
                            config.CurrentProfile = pro;

                        }
                        config.Profiles.Add(pro);
                    }
                }
            }

            #endregion

            _isConfigLoaded = true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading configuration: {ex}");
            _dispatcherService.TryEnqueue(() => { (App.Current as App)?.AppendErrorLog("Error loading configuration", ex.Message); });
        }

        CurrentConfiguration = config;

        return CurrentConfiguration;
    }

    public async Task SaveConfigurationAsync()
    {
        if (!_isInitialized)
        {
            throw new InvalidOperationException("ConfigurationService is not initialized.");
        }

        // TODO: Make sure Window and settings have been fully loaded and not overriding with empty data.
        if (!_isConfigLoaded)
        {
            // Don't return, because we want to save the configuration even if it hasn't been loaded yet, like the first time.
            //return;
        }
        if (string.IsNullOrEmpty(_configFilePath))
        {
            throw new InvalidOperationException("Configuration file path is not set.");
        }

        Debug.WriteLine("SaveConfigurationAsync()");

        // TODO: Load xdoc from global variable if exists, and apply changes to it, then save it back to file.
        //_xdoc

        var config = CurrentConfiguration;

        XmlDocument doc = new();
        XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
        doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

        // Root Document Element
        XmlElement root = doc.CreateElement(string.Empty, "App", string.Empty);
        doc.AppendChild(root);

        XmlAttribute attrs;

        #region == Window settings ==

        var w = config.MainWindow;
        //double windowWidth = 780;

        // Main Window element
        XmlElement mainWindow = doc.CreateElement(string.Empty, "MainWindow", string.Empty);

        // Main Window attributes

        // Window sizes
        attrs = doc.CreateAttribute("height");
        attrs.Value = w.WindowState == OverlappedPresenterState.Restored ? w.Height.ToString(CultureInfo.InvariantCulture) : w.RestoreHeight.ToString(CultureInfo.InvariantCulture);
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("width");
        attrs.Value = w.WindowState == OverlappedPresenterState.Restored ? w.Width.ToString(CultureInfo.InvariantCulture) : w.RestoreWidth.ToString(CultureInfo.InvariantCulture);

        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("top");
        attrs.Value = w.WindowState == OverlappedPresenterState.Restored ? w.Top.ToString(CultureInfo.InvariantCulture) : w.RestoreTop.ToString(CultureInfo.InvariantCulture);
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("left");
        attrs.Value = w.WindowState == OverlappedPresenterState.Restored ? w.Left.ToString(CultureInfo.InvariantCulture) : w.RestoreLeft.ToString(CultureInfo.InvariantCulture);
        mainWindow.SetAttributeNode(attrs);

        // Restore window sizes
        attrs = doc.CreateAttribute("restoreHeight");
        attrs.Value = w.RestoreHeight.ToString(CultureInfo.InvariantCulture);
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("restoreWidth");
        attrs.Value = w.RestoreWidth.ToString(CultureInfo.InvariantCulture);
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("restoreTop");
        attrs.Value = w.RestoreTop.ToString(CultureInfo.InvariantCulture);
        mainWindow.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("restoreLeft");
        attrs.Value = w.RestoreLeft.ToString(CultureInfo.InvariantCulture);
        mainWindow.SetAttributeNode(attrs);


        attrs = doc.CreateAttribute("state");
        attrs.Value = w.WindowState switch
        {
            OverlappedPresenterState.Maximized => "Maximized",
            OverlappedPresenterState.Restored => "Normal",
            OverlappedPresenterState.Minimized => "Minimized",
            _ => attrs.Value
        };
        mainWindow.SetAttributeNode(attrs);

        #region == Layout ==

        var xLay = doc.CreateElement(string.Empty, "Layout", string.Empty);

        attrs = doc.CreateAttribute("isNavigationViewMenuOpen");
        attrs.Value = config.IsNavigationViewMenuOpen ? "True" : "False";
        xLay.SetAttributeNode(attrs);

        mainWindow.AppendChild(xLay);
        
        #endregion

        // set MainWindow element to root.
        root.AppendChild(mainWindow);

        #endregion

        #region == Theme ==

        XmlElement thm = doc.CreateElement(string.Empty, "Theme", string.Empty);

        attrs = doc.CreateAttribute("ThemeName");
        attrs.Value = config.Theme.ToString();
        thm.SetAttributeNode(attrs);

        attrs = doc.CreateAttribute("SystemBackdrop");
        attrs.Value = config.SystemBackdrop.ToString();
        thm.SetAttributeNode(attrs);

        /// 
        root.AppendChild(thm);

        #endregion

        #region == Options ==

        // TODO: Change to "Options" in the future, but keep "Opts" for backward compatibility.
        var opts = doc.CreateElement(string.Empty, "Opts", string.Empty); 

        //
        attrs = doc.CreateAttribute("isAutoScrollToNowPlaying");
        attrs.Value = config.IsAutoScrollToNowPlaying ? "True" : "False";
        opts.SetAttributeNode(attrs);

        //
        attrs = doc.CreateAttribute("isArtistSortWithoutThePrefix");
        attrs.Value = config.IsArtistSortWithoutThePrefix ? "True" : "False";
        opts.SetAttributeNode(attrs);

        //
        attrs = doc.CreateAttribute("isAlbumSortWithoutThePrefix");
        attrs.Value = config.IsAlbumSortWithoutThePrefix ? "True" : "False";
        opts.SetAttributeNode(attrs);

        // 
        attrs = doc.CreateAttribute("isUpdateOnStartup");
        attrs.Value = config.IsUpdateOnStartup ? "True" : "False";
        opts.SetAttributeNode(attrs);

        //
        attrs = doc.CreateAttribute("isDownloadAlbumArtEmbeddedUsingReadPicture");
        attrs.Value = config.IsDownloadAlbumArtEmbeddedUsingReadPicture ? "True" : "False";
        opts.SetAttributeNode(attrs);

        //
        attrs = doc.CreateAttribute("isForceSetVolumeExplicitly");
        attrs.Value = config.IsForceSetVolumeExplicitly ? "True" : "False";
        opts.SetAttributeNode(attrs);

        /// 
        root.AppendChild(opts);

        #endregion

        #region == Profiles  ==

        var xProfiles = doc.CreateElement(string.Empty, "Profiles", string.Empty);

        XmlElement xProfile;
        XmlAttribute xAttrs;

        if (config.Profiles.Count == 1)
            config.Profiles[0].IsDefault = true;

        foreach (var p in config.Profiles)
        {
            xProfile = doc.CreateElement(string.Empty, "Profile", string.Empty);

            xAttrs = doc.CreateAttribute("Name");
            xAttrs.Value = p.Name;
            xProfile.SetAttributeNode(xAttrs);

            xAttrs = doc.CreateAttribute("Host");
            xAttrs.Value = p.Host;
            xProfile.SetAttributeNode(xAttrs);

            xAttrs = doc.CreateAttribute("Port");
            xAttrs.Value = p.Port.ToString(CultureInfo.InvariantCulture);
            xProfile.SetAttributeNode(xAttrs);

            xAttrs = doc.CreateAttribute("Password");
            xAttrs.Value = Helpers.EncryptDecrypt.Encrypt(p.Password);
            xProfile.SetAttributeNode(xAttrs);

            if (p.IsDefault)
            {
                xAttrs = doc.CreateAttribute("IsDefault");
                xAttrs.Value = "True";
                xProfile.SetAttributeNode(xAttrs);
            }

            xAttrs = doc.CreateAttribute("Volume");
            xAttrs.Value = p == config.CurrentProfile ? 20.ToString(CultureInfo.InvariantCulture) : p.Volume.ToString(CultureInfo.InvariantCulture);
            xProfile.SetAttributeNode(xAttrs);

            xProfiles.AppendChild(xProfile);
        }

        root.AppendChild(xProfiles);

        #endregion

        try
        {
            if (!Directory.Exists(App.AppDataFolder))
            {
                Directory.CreateDirectory(App.AppDataFolder);
            }

            //doc.Save(App.AppConfigFilePath);
            var temporaryPath = App.AppConfigFilePath + ".tmp";
            doc.Save(temporaryPath);
            File.Move(temporaryPath, App.AppConfigFilePath, overwrite: true);
        }
        //catch (System.IO.FileNotFoundException) { }
        catch (Exception ex)
        {
            _dispatcherService.TryEnqueue(() =>
            {
                (App.Current as App)?.AppendErrorLog("Exception@OnWindowClosing", ex.Message);
            });
        }

        // Save error logs.
        _dispatcherService.TryEnqueue(() => { (App.Current as App)?.SaveErrorLog(); });

    }
}
