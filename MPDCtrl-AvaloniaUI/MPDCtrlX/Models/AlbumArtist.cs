using System;
using System.Collections.ObjectModel;

namespace MPDCtrlX.Models;


public class Album
{
    public string Name { get; set; } = "";

}

public class AlbumArtist
{
    public string Name { get; set; } = "";

    public ObservableCollection<Album> Albums { get; private set; } = [];
}
