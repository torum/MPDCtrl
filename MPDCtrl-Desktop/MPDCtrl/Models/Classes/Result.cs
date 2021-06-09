using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Models
{
    public class Result
    {
        public bool IsSuccess;
        public string ErrorMessage;
    }

    public class ConnectionResult: Result
    {

    }

    // generic
    public class CommandResult : Result
    {
        public string ResultText;
    }

    public class CommandBinaryResult : Result
    {
        public int WholeSize;
        public int ChunkSize;
        public string Type;
        public byte[] BinaryData;
    }

    // for commands that return playlist songs.
    public class CommandPlaylistResult : CommandResult
    {
        public ObservableCollection<SongInfo> PlaylistSongs;
    }

    // for commands that return search result.
    public class CommandSearchResult : CommandResult
    {
        public ObservableCollection<SongInfo> SearchResult;
    }

    // TODO: Not used?
    public class IdleResult : CommandResult
    {

    }
}
