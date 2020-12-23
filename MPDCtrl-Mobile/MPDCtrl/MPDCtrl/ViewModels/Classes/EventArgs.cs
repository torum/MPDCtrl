using System;
using System.Collections.Generic;
using System.Text;

namespace MPDCtrl.ViewModels
{

    public class AskWhichPlaylistToSaveQueueItemToEventArgs
    {
        public String File;
        public String[] Playlists;
    }

    public class AskWhichPlaylistToSaveSearchResultToEventArgs
    {
        public String[] Playlists;
    }

    public class AskWhichPlaylistToSaveSearchResultItemToEventArgs
    {
        public String File;
        public String[] Playlists;
    }

    public class AskNewNameToRenameToEventArgs
    {
        public String OriginalPlaylistName;
        public List<String> Playlists;
    }

    

}
