
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

    public ObservableCollection<SongInfo> Songs { get; private set; } = [];
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
}

public class AlbumArtist
{
    public string Name { get; set; } = "";

    public ObservableCollection<AlbumEx> Albums { get; private set; } = [];
}
