
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public abstract class MpdResult
{
    public bool IsWaitFailed { get; set; }
    public bool IsSuccess { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class ConnectionResult : MpdResult
{

}

public class CommandResult : MpdResult
{
    public string ResultText { get; set; } = string.Empty;
}

public sealed class CommandBinaryResult : MpdResult
{
    public bool IsNoBinaryFound { get; set; }
    public bool IsTimeOut { get; set; }
    public int WholeSize { get; set; }
    public int ChunkSize { get; set; }
    public string Type { get; set; } = string.Empty;
    public byte[]? BinaryData { get; set; }
}

public sealed class CommandImageResult : MpdResult
{
    public bool IsNoBinaryFound { get; set; }
    public bool IsTimeOut { get; set; }
    public AlbumImage AlbumCover { get; set; } = new();
}

public sealed class CommandPlaylistResult : CommandResult
{
    public ObservableCollection<SongInfo>? PlaylistSongs { get; set; }
}

public sealed class CommandSearchResult : CommandResult
{
    public ObservableCollection<SongInfo>? SearchResult { get; set; }
}

