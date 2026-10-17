
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace MPDCtrl.Models;

public class Configuration
{
    public class WindowSettings
    {
        public int Width { get; set; } = 780;
        public int Height { get; set; } = 600;
        public int Left { get; set; } = 100;
        public int Top { get; set; } = 100;
        public OverlappedPresenterState WindowState { get; set; } = OverlappedPresenterState.Restored;

        public int RestoreWidth { get; set; } = 780;
        public int RestoreHeight { get; set; } = 600;
        public int RestoreLeft { get; set; } = 100;
        public int RestoreTop { get; set; } = 100;
    }

    public WindowSettings MainWindow { get; set; } = new WindowSettings();

    public bool IsNavigationViewMenuOpen { get; set; } = true;

    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    public SystemBackdropOption SystemBackdrop { get; set; } = SystemBackdropOption.Mica;

    public List<Profile> Profiles { get; set; } = [];

    public Profile? CurrentProfile { get; set; }

    public bool IsAutoScrollToNowPlaying { get; set; } = true;

    public bool IsArtistSortWithoutThePrefix { get; set; } = true;

    public bool IsAlbumSortWithoutThePrefix { get; set; } = true;

    public bool IsUpdateOnStartup { get; set; } = true;

    public bool IsDownloadAlbumArtEmbeddedUsingReadPicture { get; set; } = true;

    public bool IsForceSetVolumeExplicitly { get; set; }

}
