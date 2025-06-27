using Avalonia.Controls.Chrome;
using System;
using System.Collections.ObjectModel;

namespace MPDCtrlX.Models;


public class Album
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

}