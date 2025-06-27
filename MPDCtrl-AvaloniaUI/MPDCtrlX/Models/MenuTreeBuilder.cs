using System.Collections.ObjectModel;

namespace MPDCtrlX.Models;

public class NodeMenu : NodeTree
{
    public NodeMenu(string name) : base(name)
    {
        PathIcon = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
    }
}

public class NodeMenuQueue : NodeMenu
{
    public NodeMenuQueue(string name) : base(name)
    {
        PathIcon = "M19,9H2V11H19V9M19,5H2V7H19V5M2,15H15V13H2V15M17,13V19L22,16L17,13Z";
    }
}

public class NodeMenuSearch : NodeMenu
{
    public NodeMenuSearch(string name) : base(name)
    {
        //PathIcon = "M19.31 18.9L22.39 22L21 23.39L17.88 20.32C17.19 20.75 16.37 21 15.5 21C13 21 11 19 11 16.5C11 14 13 12 15.5 12C18 12 20 14 20 16.5C20 17.38 19.75 18.21 19.31 18.9M15.5 19C16.88 19 18 17.88 18 16.5C18 15.12 16.88 14 15.5 14C14.12 14 13 15.12 13 16.5C13 17.88 14.12 19 15.5 19M21 4V6H3V4H21M3 16V14H9V16H3M3 11V9H21V11H18.97C17.96 10.37 16.77 10 15.5 10C14.23 10 13.04 10.37 12.03 11H3Z";
        PathIcon = "M11 18.95C7.77 18.72 6 17.45 6 17V14.77C7.13 15.32 8.5 15.69 10 15.87C10 15.21 10.04 14.54 10.21 13.89C8.5 13.67 6.97 13.16 6 12.45V9.64C7.43 10.45 9.5 10.97 11.82 11C11.85 10.97 11.87 10.93 11.9 10.9C14.1 8.71 17.5 8.41 20 10.03V7C20 4.79 16.42 3 12 3S4 4.79 4 7V17C4 19.21 7.59 21 12 21C12.34 21 12.68 21 13 20.97C12.62 20.72 12.24 20.44 11.9 20.1C11.55 19.74 11.25 19.36 11 18.95M12 5C15.87 5 18 6.5 18 7S15.87 9 12 9 6 7.5 6 7 8.13 5 12 5M20.31 17.9C20.75 17.21 21 16.38 21 15.5C21 13 19 11 16.5 11S12 13 12 15.5 14 20 16.5 20C17.37 20 18.19 19.75 18.88 19.32L22 22.39L23.39 21L20.31 17.9M16.5 18C15.12 18 14 16.88 14 15.5S15.12 13 16.5 13 19 14.12 19 15.5 17.88 18 16.5 18Z";
    }
}

public class NodeMenuLibrary : NodeMenu
{
    //public bool IsAcquired { get; set; }

    public NodeMenuLibrary(string name) : base(name)
    {
        PathIcon = "M4 4C2.89 4 2 4.89 2 6V18A2 2 0 0 0 4 20H20A2 2 0 0 0 22 18V8C22 6.89 21.1 6 20 6H12L10 4H4M4 8H20V18H4V8M12 9V11H15V9H12M16 9V11H19V9H16M12 12V14H15V12H12M16 12V14H19V12H16M12 15V17H15V15H12M16 15V17H19V15H16Z";
        //PathIcon = "M12 13H7V18H12V20H5V10H7V11H12V13M8 4V6H4V4H8M10 2H2V8H10V2M20 11V13H16V11H20M22 9H14V15H22V9M20 18V20H16V18H20M22 16H14V22H22V16Z";
    }
}

public class NodeMenuFiles : NodeMenu
{
    public bool IsAcquired { get; set; }

    public NodeMenuFiles(string name) : base(name)
    {
        //PathIcon = "M4 4C2.89 4 2 4.89 2 6V18A2 2 0 0 0 4 20H20A2 2 0 0 0 22 18V8C22 6.89 21.1 6 20 6H12L10 4H4M4 8H20V18H4V8M12 9V11H15V9H12M16 9V11H19V9H16M12 12V14H15V12H12M16 12V14H19V12H16M12 15V17H15V15H12M16 15V17H19V15H16Z";
        PathIcon = "M12 13H7V18H12V20H5V10H7V11H12V13M8 4V6H4V4H8M10 2H2V8H10V2M20 11V13H16V11H20M22 9H14V15H22V9M20 18V20H16V18H20M22 16H14V22H22V16Z";
    }
}

public class NodeMenuAlbum : NodeMenu
{
    public bool IsAcquired { get; set; }

    public NodeMenuAlbum(string name) : base(name)
    {
        //PathIcon = "M4 4C2.89 4 2 4.89 2 6V18A2 2 0 0 0 4 20H20A2 2 0 0 0 22 18V8C22 6.89 21.1 6 20 6H12L10 4H4M4 8H20V18H4V8M12 9V11H15V9H12M16 9V11H19V9H16M12 12V14H15V12H12M16 12V14H19V12H16M12 15V17H15V15H12M16 15V17H19V15H16Z";
        PathIcon = "M12,11A1,1 0 0,0 11,12A1,1 0 0,0 12,13A1,1 0 0,0 13,12A1,1 0 0,0 12,11M12,16.5C9.5,16.5 7.5,14.5 7.5,12C7.5,9.5 9.5,7.5 12,7.5C14.5,7.5 16.5,9.5 16.5,12C16.5,14.5 14.5,16.5 12,16.5M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
    }
}

public class NodeMenuArtist : NodeMenu
{
    public bool IsAcquired { get; set; }

    public NodeMenuArtist(string name) : base(name)
    {
        //PathIcon = "M11,14C12,14 13.05,14.16 14.2,14.44C13.39,15.31 13,16.33 13,17.5C13,18.39 13.25,19.23 13.78,20H3V18C3,16.81 3.91,15.85 5.74,15.12C7.57,14.38 9.33,14 11,14M11,12C9.92,12 9,11.61 8.18,10.83C7.38,10.05 7,9.11 7,8C7,6.92 7.38,6 8.18,5.18C9,4.38 9.92,4 11,4C12.11,4 13.05,4.38 13.83,5.18C14.61,6 15,6.92 15,8C15,9.11 14.61,10.05 13.83,10.83C13.05,11.61 12.11,12 11,12M18.5,10H20L22,10V12H20V17.5A2.5,2.5 0 0,1 17.5,20A2.5,2.5 0 0,1 15,17.5A2.5,2.5 0 0,1 17.5,15C17.86,15 18.19,15.07 18.5,15.21V10Z";
        PathIcon = "M11,4A4,4 0 0,1 15,8A4,4 0 0,1 11,12A4,4 0 0,1 7,8A4,4 0 0,1 11,4M11,6A2,2 0 0,0 9,8A2,2 0 0,0 11,10A2,2 0 0,0 13,8A2,2 0 0,0 11,6M11,13C12.1,13 13.66,13.23 15.11,13.69C14.5,14.07 14,14.6 13.61,15.23C12.79,15.03 11.89,14.9 11,14.9C8.03,14.9 4.9,16.36 4.9,17V18.1H13.04C13.13,18.8 13.38,19.44 13.76,20H3V17C3,14.34 8.33,13 11,13M18.5,10H20L22,10V12H20V17.5A2.5,2.5 0 0,1 17.5,20A2.5,2.5 0 0,1 15,17.5A2.5,2.5 0 0,1 17.5,15C17.86,15 18.19,15.07 18.5,15.21V10Z";
    }
}

public class NodeMenuPlaylists : NodeMenu
{
    public NodeMenuPlaylists(string name) : base(name)
    {
        PathIcon = "M20,18H4V8H20M20,6H12L10,4H4A2,2 0 0,0 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8A2,2 0 0,0 20,6M15,16H6V14H15V16M18,12H6V10H18V12Z";
    }
}

public class NodeMenuPlaylistItem : NodeMenu
{
    public ObservableCollection<SongInfo> PlaylistSongs = [];

    public bool IsUpdateRequied { get; set; }

    public NodeMenuPlaylistItem(string name) : base(name)
    {
        PathIcon = "M15,6V8H3V6H15M15,10V12H3V10H15M3,16V14H11V16H3M17,6H22V8H19V17A3,3 0 0,1 16,20A3,3 0 0,1 13,17A3,3 0 0,1 16,14C16.35,14 16.69,14.07 17,14.18V6M16,16A1,1 0 0,0 15,17A1,1 0 0,0 16,18A1,1 0 0,0 17,17A1,1 0 0,0 16,16Z";
        //PathIcon = "M15,6H3V8H15V6M15,10H3V12H15V10M3,16H11V14H3V16M17,6V14.18C16.69,14.07 16.35,14 16,14A3,3 0 0,0 13,17A3,3 0 0,0 16,20A3,3 0 0,0 19,17V8H22V6H17Z";
    }
}

public class MenuTreeBuilder : NodeTree
{
    private readonly NodeMenuPlaylists _playlistsDirectory;
    public NodeMenuPlaylists PlaylistsDirectory
    {
        get
        {
            return _playlistsDirectory;
        }
    }

    private readonly NodeMenuSearch _searchDirectory;
    public NodeMenuSearch SearchDirectory
    {
        get
        {
            return _searchDirectory;
        }
    }

    private readonly NodeMenuAlbum _albumsDirectory;
    public NodeMenuAlbum AlbumsDirectory
    {
        get
        {
            return _albumsDirectory;
        }
    }

    private readonly NodeMenuArtist _artistsDirectory;
    public NodeMenuArtist ArtistsDirectory
    {
        get
        {
            return _artistsDirectory;
        }
    }

    private readonly NodeMenuLibrary _libraryDirectory;
    public NodeMenuLibrary LibraryDirectory
    {
        get
        {
            return _libraryDirectory;
        }
    }

    private readonly NodeMenuFiles _filesDirectory;
    public NodeMenuFiles FilesDirectory
    {
        get
        {
            return _filesDirectory;
        }
    }

    public MenuTreeBuilder(string name) : base(name)
    {
        NodeMenuQueue queue = new(Properties.Resources.MenuTreeItem_Queue)
        {
            //Selected = true,
            Selected = false,
            Expanded = false,

            Parent = this
        };
        Children.Add(queue);


        NodeMenuSearch search = new(Properties.Resources.MenuTreeItem_Search)
        {
            Selected = false,
            Expanded = false,

            Parent = this
        };
        Children.Add(search);

        _searchDirectory = search;


        NodeMenuLibrary browse = new(Properties.Resources.MenuTreeItem_Library)
        {
            Selected = false,
            Expanded = false,

            Parent = this
        };
        Children.Add(browse);

        _libraryDirectory = browse;


        NodeMenuArtist artists = new(Properties.Resources.MenuTreeItem_Artists)
        {
            Selected = false,
            Expanded = false,

            Parent = this
        };
        browse.Children.Add(artists);

        _artistsDirectory = artists;


        NodeMenuAlbum albums = new(Properties.Resources.MenuTreeItem_Albums)
        {
            Selected = false,
            Expanded = false,

            Parent = this
        };
        browse.Children.Add(albums);

        _albumsDirectory = albums;


        NodeMenuFiles files = new(Properties.Resources.MenuTreeItem_Files)
        {
            Selected = false,
            Expanded = false,

            Parent = this
        };
        browse.Children.Add(files);

        _filesDirectory = files;


        NodeMenuPlaylists playlists = new(Properties.Resources.MenuTreeItem_Playlists)
        {
            Selected = false,
            Expanded = false,

            Parent = this
        };
        Children.Add(playlists);

        _playlistsDirectory = playlists;
    }
}
