using CommunityToolkit.WinUI;
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public partial class NodeMenu : NodeTree
{
    public NodeMenu(string name) : base(name)
    {
        PathIcon = "M20,18H4V8H20M20,6H12L10,4H4C2.89,4 2,4.89 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8C22,6.89 21.1,6 20,6Z";
    }
}

public partial class NodeMenuQueue : NodeMenu
{
    public NodeMenuQueue(string name) : base(name)
    {
        PathIcon = "M17 5.5A2.5 2.5 0 0 0 14.5 3h-9A2.5 2.5 0 0 0 3 5.5v9A2.5 2.5 0 0 0 5.5 17h4.1a5.5 5.5 0 0 1-.393-1H5.5A1.5 1.5 0 0 1 4 14.5V7h12v2.207q.524.149 1 .393zM5.5 4h9A1.5 1.5 0 0 1 16 5.5V6H4v-.5A1.5 1.5 0 0 1 5.5 4M19 14.5a4.5 4.5 0 1 1-9 0a4.5 4.5 0 0 1 9 0m-2.287-.437l-2.97-1.65a.5.5 0 0 0-.743.437v3.3a.5.5 0 0 0 .743.437l2.97-1.65a.5.5 0 0 0 0-.874";
    }
}

public partial class NodeMenuSearch : NodeMenu
{
    public NodeMenuSearch(string name) : base(name)
    {
        PathIcon = "M11 18.95C7.77 18.72 6 17.45 6 17V14.77C7.13 15.32 8.5 15.69 10 15.87C10 15.21 10.04 14.54 10.21 13.89C8.5 13.67 6.97 13.16 6 12.45V9.64C7.43 10.45 9.5 10.97 11.82 11C11.85 10.97 11.87 10.93 11.9 10.9C14.1 8.71 17.5 8.41 20 10.03V7C20 4.79 16.42 3 12 3S4 4.79 4 7V17C4 19.21 7.59 21 12 21C12.34 21 12.68 21 13 20.97C12.62 20.72 12.24 20.44 11.9 20.1C11.55 19.74 11.25 19.36 11 18.95M12 5C15.87 5 18 6.5 18 7S15.87 9 12 9 6 7.5 6 7 8.13 5 12 5M20.31 17.9C20.75 17.21 21 16.38 21 15.5C21 13 19 11 16.5 11S12 13 12 15.5 14 20 16.5 20C17.37 20 18.19 19.75 18.88 19.32L22 22.39L23.39 21L20.31 17.9M16.5 18C15.12 18 14 16.88 14 15.5S15.12 13 16.5 13 19 14.12 19 15.5 17.88 18 16.5 18Z";
    }
}

public partial class NodeMenuLibrary : NodeMenu
{
    //public bool IsAcquired { get; set; }

    public NodeMenuLibrary(string name) : base(name)
    {
        PathIcon = "M4 4C2.89 4 2 4.89 2 6V18A2 2 0 0 0 4 20H20A2 2 0 0 0 22 18V8C22 6.89 21.1 6 20 6H12L10 4H4M4 8H20V18H4V8M12 9V11H15V9H12M16 9V11H19V9H16M12 12V14H15V12H12M16 12V14H19V12H16M12 15V17H15V15H12M16 15V17H19V15H16Z";
    }
}

public partial class NodeMenuFiles : NodeMenu
{
    public bool IsAcquired { get; set; }

    public NodeMenuFiles(string name) : base(name)
    {
        PathIcon = "M12 13H7V18H12V20H5V10H7V11H12V13M8 4V6H4V4H8M10 2H2V8H10V2M20 11V13H16V11H20M22 9H14V15H22V9M20 18V20H16V18H20M22 16H14V22H22V16Z";
    }
}

public partial class NodeMenuAlbum : NodeMenu
{
    public NodeMenuAlbum(string name) : base(name)
    {
        PathIcon = "M12,11A1,1 0 0,0 11,12A1,1 0 0,0 12,13A1,1 0 0,0 13,12A1,1 0 0,0 12,11M12,16.5C9.5,16.5 7.5,14.5 7.5,12C7.5,9.5 9.5,7.5 12,7.5C14.5,7.5 16.5,9.5 16.5,12C16.5,14.5 14.5,16.5 12,16.5M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
    }
}

public partial class NodeMenuArtist : NodeMenu
{
    public NodeMenuArtist(string name) : base(name)
    {
        PathIcon = "M11,4A4,4 0 0,1 15,8A4,4 0 0,1 11,12A4,4 0 0,1 7,8A4,4 0 0,1 11,4M11,6A2,2 0 0,0 9,8A2,2 0 0,0 11,10A2,2 0 0,0 13,8A2,2 0 0,0 11,6M11,13C12.1,13 13.66,13.23 15.11,13.69C14.5,14.07 14,14.6 13.61,15.23C12.79,15.03 11.89,14.9 11,14.9C8.03,14.9 4.9,16.36 4.9,17V18.1H13.04C13.13,18.8 13.38,19.44 13.76,20H3V17C3,14.34 8.33,13 11,13M18.5,10H20L22,10V12H20V17.5A2.5,2.5 0 0,1 17.5,20A2.5,2.5 0 0,1 15,17.5A2.5,2.5 0 0,1 17.5,15C17.86,15 18.19,15.07 18.5,15.21V10Z";
    }
}

public partial class NodeMenuPlaylists : NodeMenu
{
    public NodeMenuPlaylists(string name) : base(name)
    {
        PathIcon = "M20,18H4V8H20M20,6H12L10,4H4A2,2 0 0,0 2,6V18A2,2 0 0,0 4,20H20A2,2 0 0,0 22,18V8A2,2 0 0,0 20,6M15,16H6V14H15V16M18,12H6V10H18V12Z";
    }
}

public partial class NodeMenuPlaylistItem : NodeMenu
{
    public ObservableCollection<SongInfo> PlaylistSongs = [];

    public bool IsUpdateRequied { get; set; }

    public NodeMenuPlaylistItem(string name) : base(name)
    {
        Tag = "playlistItem";
        PathIcon = "M15,6V8H3V6H15M15,10V12H3V10H15M3,16V14H11V16H3M17,6H22V8H19V17A3,3 0 0,1 16,20A3,3 0 0,1 13,17A3,3 0 0,1 16,14C16.35,14 16.69,14.07 17,14.18V6M16,16A1,1 0 0,0 15,17A1,1 0 0,0 16,18A1,1 0 0,0 17,17A1,1 0 0,0 16,16Z";
    }
}

public partial class MenuTreeBuilder : NodeTree
{
    public NodeMenuPlaylists PlaylistsDirectory { get; }

    public NodeMenuSearch SearchDirectory { get; }

    public NodeMenuAlbum AlbumsDirectory { get; }

    public NodeMenuArtist ArtistsDirectory { get; }

    public NodeMenuLibrary LibraryDirectory { get; }

    public NodeMenuFiles FilesDirectory { get; }

    public NodeMenuQueue QueueDirectory { get; }

    public MenuTreeBuilder(string name) : base(name)
    {
        //Microsoft.Windows.ApplicationModel.Resources.ResourceLoader resourceLoader = new();

        NodeMenuQueue queue = new($"{"MenuTreeItem_Queue".GetLocalized()}")
        {
            Tag = "queue",
            //Selected = true,
            Selected = false,
            Expanded = false,

            Parent = this
        };
        Children.Add(queue);
        QueueDirectory = queue;


        NodeMenuLibrary library = new($"{"MenuTreeItem_Library".GetLocalized()}")
        {
            Tag = "library",
            Selected = false,
            Expanded = true,

            Parent = this
        };
        Children.Add(library);
        LibraryDirectory = library;


        NodeMenuAlbum albums = new($"{"MenuTreeItem_Albums".GetLocalized()}")
        {
            Tag = "albums",
            Selected = false,
            Expanded = false,

            Parent = this
        };
        library.Children.Add(albums);
        AlbumsDirectory = albums;


        NodeMenuArtist artists = new($"{"MenuTreeItem_Artists".GetLocalized()}")
        {
            Tag = "artists",
            Selected = false,
            Expanded = false,

            Parent = this
        };
        library.Children.Add(artists);
        ArtistsDirectory = artists;


        NodeMenuFiles files = new($"{"MenuTreeItem_Files".GetLocalized()}")
        {
            Tag = "files",
            Selected = false,
            Expanded = false,

            Parent = this
        };
        library.Children.Add(files);
        FilesDirectory = files;


        NodeMenuSearch search = new($"{"MenuTreeItem_Search".GetLocalized()}")
        {
            Tag = "search",
            Selected = false,
            Expanded = false,

            Parent = this
        };
        library.Children.Add(search);
        SearchDirectory = search;


        NodeMenuPlaylists playlists = new($"{"MenuTreeItem_Playlists".GetLocalized()}")
        {
            Tag = "playlists",
            Selected = false,
            Expanded = false,

            Parent = this
        };
        Children.Add(playlists);
        PlaylistsDirectory = playlists;
    }
}
