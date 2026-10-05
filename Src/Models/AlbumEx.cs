using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Models;

public sealed partial class AlbumEx : Album
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
