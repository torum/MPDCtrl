
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public abstract class Result
{
    public bool IsWaitFailed = false;
    public bool IsSuccess = false;
    public string ErrorMessage = string.Empty;
}

public class ConnectionResult : Result
{

}

public class CommandResult : Result
{
    public string ResultText = string.Empty;
}

public sealed class CommandBinaryResult : Result
{
    public bool IsNoBinaryFound = false;
    public bool IsTimeOut = false;
    public int WholeSize;
    public int ChunkSize;
    public string Type = string.Empty;
    public byte[]? BinaryData;
}

public sealed class CommandImageResult : Result
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

// TODO: Not used?
public sealed class IdleResult : CommandResult
{

}
