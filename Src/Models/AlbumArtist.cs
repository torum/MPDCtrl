
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using MPDCtrl.ViewModels;
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public partial class Album : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    public string NameSort { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReleaseYear { get; set; } = string.Empty;

    public bool IsSongsAcquired { get; set; } = false;

    [ObservableProperty]
    public partial ObservableCollection<SongInfo> Songs { get; set; } = [];
}

public partial class AlbumEx :Album
{
    public string AlbumArtist { get; set; } = string.Empty;
    public string AlbumArtistSort { get; set; } = string.Empty;

    public string? AlbumImagePath { get; set; } = null;

    [ObservableProperty]
    public partial ImageSource? AlbumImage { get; set; } = null;

    public bool IsImageAcquired { get; set; } = false;
    public bool IsImageLoading { get; set; } = false;

    // Workaround for WinUI3's limitation or lack of features. 
    public MainViewModel? ParentViewModel { get; set; }
}

public partial class AlbumArtist : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string NameSort { get; set; } = string.Empty;

    public ObservableCollection<AlbumEx> Albums { get; private set; } = [];
}
