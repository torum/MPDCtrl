using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public class Result
{
    public bool IsSuccess = false;
    public string ErrorMessage = "";
}

public class ConnectionResult: Result
{

}

public class CommandResult : Result
{
    public string ResultText = "";
}

public class CommandBinaryResult : Result
{
    public int WholeSize;
    public int ChunkSize;
    public string Type = "";
    public byte[]? BinaryData;
}

public class CommandPlaylistResult : CommandResult
{
    public ObservableCollection<SongInfo>? PlaylistSongs;
}

public class CommandSearchResult : CommandResult
{
    public ObservableCollection<SongInfo>? SearchResult;
}

// TODO: Not used?
public class IdleResult : CommandResult
{

}
