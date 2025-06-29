using Avalonia.Controls.Chrome;
using Avalonia.Media.Imaging;
using MPDCtrlX.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MPDCtrlX.Models;


public class Album : ViewModelBase
{
    public string Name { get; set; } = "";

    public bool IsSongsAcquired { get; set; } = false;

    public ObservableCollection<SongInfo> Songs { get; set; } = [];
}

public class AlbumArtist
{
    public string Name { get; set; } = "";

    public ObservableCollection<Album> Albums { get; private set; } = [];
}


public class AlbumEx :Album
{
    public string AlbumArtist { get; set; } = "";

    public string? AlbumImagePath { get; set; } = null;

    private Bitmap? _albumImage = null;
    public Bitmap? AlbumImage { 
        get => _albumImage; 
        set
        {
            if (_albumImage != value)
            {
                _albumImage = value;
                NotifyPropertyChanged(nameof(AlbumImage));
            }
        }
    }

    public bool IsImageAcquired { get; set; } = false;

}