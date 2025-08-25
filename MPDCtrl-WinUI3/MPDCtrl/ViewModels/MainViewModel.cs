using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Helpers;
using MPDCtrl.Models;

//using MPDCtrl.Models;
//using MPDCtrl.Services;
//using MPDCtrl.Services.Contracts;
using MPDCtrl.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Media.Core;
using Windows.Storage;
using WinRT.Interop;

namespace MPDCtrl.ViewModels;

public enum SystemBackdropOption
{
    Mica, MicaAlt, Acrylic, None
}

public partial class MainViewModel : ObservableObject
{
    private readonly MenuTreeBuilder _mainMenuItems = new("");
    public ObservableCollection<NodeTree> MainMenuItems
    {
        get { return _mainMenuItems.Children; }
        set
        {
            _mainMenuItems.Children = value;
            OnPropertyChanged(nameof(MainMenuItems));
        }
    }

    #region == Theme ==

    private ElementTheme _theme = ElementTheme.Default;
    public ElementTheme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    private SystemBackdropOption _material = SystemBackdropOption.Mica;
    public SystemBackdropOption Material
    {
        get => _material;
        set => SetProperty(ref _material, value);
    }

    private bool _isAcrylicSupported = false;
    public bool IsAcrylicSupported
    {
        get => _isAcrylicSupported;
        set => SetProperty(ref _isAcrylicSupported, value);
    }

    private bool _isBackdropEnabled = false;
    public bool IsBackdropEnabled
    {
        get => _isBackdropEnabled;
        set => SetProperty(ref _isBackdropEnabled, value);
    }

    private bool _isMicaSupported = false;
    public bool IsMicaSupported
    {
        get => _isMicaSupported;
        set => SetProperty(ref _isMicaSupported, value);
    }

#pragma warning disable IDE0079
#pragma warning disable CA1822
    public string VersionText
#pragma warning restore CA1822
#pragma warning restore IDE0079
    {
        get
        {
            Version version;

            if (RuntimeHelper.IsMSIX)
            {
                var packageVersion = Package.Current.Id.Version;

                version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            }
            else
            {
                version = Assembly.GetExecutingAssembly().GetName().Version!;
            }

            return $"{"Version".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }

    #endregion

    public MainViewModel()
    {


#if DEBUG
        //IsDebugWindowEnabled = true;
#else
        //IsDebugWindowEnabled = false;
#endif
    }


}
