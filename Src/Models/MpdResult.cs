
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public abstract class MpdResult
{
    public bool IsWaitFailed = false;
    public bool IsSuccess = false;
    public string ErrorMessage = string.Empty;
}

public class ConnectionResult : MpdResult
{

}

public class CommandResult : MpdResult
{
    public string ResultText = string.Empty;
}

public sealed class CommandBinaryResult : MpdResult
{
    public bool IsNoBinaryFound = false;
    public bool IsTimeOut = false;
    public int WholeSize;
    public int ChunkSize;
    public string Type = string.Empty;
    public byte[]? BinaryData;
}

public sealed class CommandImageResult : MpdResult
{
    public bool IsNoBinaryFound = false;
    public bool IsTimeOut = false;
    public AlbumImage AlbumCover = new();
}

public sealed class CommandPlaylistResult : CommandResult
{
    public ObservableCollection<SongInfo>? PlaylistSongs;
}

public sealed class CommandSearchResult : CommandResult
{
    public ObservableCollection<SongInfo>? SearchResult;
}

