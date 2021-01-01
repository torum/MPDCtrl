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

    /*
    // for commands that return nothing but "OK".
    public class CommandBoolResult : CommandResult
    {
    }

    // for commands that return something but ignore it and wait for idle event.
    public class CommandEventWaitResult : CommandResult
    {
        
    }

    */


    // for commands that return playlist songs.
    public class CommandPlaylistResult : CommandResult
    {
        public ObservableCollection<Song> PlaylistSongs;
    }

    // for commands that return search result.
    public class CommandSearchResult : CommandResult
    {
        public ObservableCollection<Song> SearchResult;
    }


    public class IdleResult : CommandResult
    {

    }

}
