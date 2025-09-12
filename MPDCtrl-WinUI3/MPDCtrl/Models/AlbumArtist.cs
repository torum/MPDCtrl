
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Media;
using MPDCtrl.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MPDCtrl.Models;


public partial class Album : ObservableObject
{
    public string Name { get; set; } = "";

    public bool IsSongsAcquired { get; set; } = false;

    public ObservableCollection<SongInfo> _songs = [];
    public ObservableCollection<SongInfo> Songs
    {
        get => _songs;
        set
        {
            if (_songs == value)
            {
                return;
            }
            _songs = value;
            OnPropertyChanged(nameof(Songs));
        }
    }
}

public partial class AlbumEx :Album
{
    public string AlbumArtist { get; set; } = "";

    public string? AlbumImagePath { get; set; } = null;

    private ImageSource? _albumImage = null;
    public ImageSource? AlbumImage { 
        get => _albumImage; 
        set
        {
            if (_albumImage == value)
            {
                return;
            }
            _albumImage = value;
            OnPropertyChanged(nameof(AlbumImage));
        }
    }

    public bool IsImageAcquired { get; set; } = false;
    public bool IsImageLoading { get; set; } = false;

    // Workaround for WinUI3's limitation or lack of features. 
    public MainViewModel? ParentViewModel { get; set; }
}

public class AlbumArtist
{
    public string Name { get; set; } = "";

    public ObservableCollection<AlbumEx> Albums { get; private set; } = [];
}
