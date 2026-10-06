using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Models;

#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed partial class AlbumEx : Album
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
{
    public string AlbumArtist { get; set; } = string.Empty;
    public string AlbumArtistSort { get; set; } = string.Empty;

    public string? AlbumImagePath { get; set; }

    [ObservableProperty]
    public partial ImageSource? AlbumImage { get; set; } = null;

    public bool IsImageAcquired { get; set; }
    public bool IsImageLoading { get; set; }

    // Workaround for WinUI3's limitation or lack of features. 
    public MainViewModel? ParentViewModel { get; set; }
}
