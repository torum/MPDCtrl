/// 
/// 
/// MPD Ctrl
/// https://github.com/torumyax/MPD-Ctrl
/// 
/// TODO:
///  More detailed error message for users.
///  lock object.
///
/// Known issue:
///  Mopidy does not accept command_list_begin + password
///   https://github.com/mopidy/mopidy/issues/1661
///    command_list_begin
///    password hogehoge
///    status
///    command_list_end
///   hence > no password.
///  Mopidy issues unnecessary multiple idle subsystem events 
///   https://github.com/mopidy/mopidy/issues/1662
///  Mopidy has some issue with M3U and UTF-8, Ext M3Us.
///    https://github.com/mopidy/mopidy/issues/1370
///    


using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
//using System.Windows.Data;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using Xamarin.Forms;

namespace MPDCtrl
{
    public class MPC
    {
        /// <summary>
        /// Song Class for ObservableCollection. 
        /// </summary>
        /// 
        public class Song
        {
            public string ID { get; set; }
            public string Title { get; set; }
            public string Artist { get; set; }
        }

        /// <summary>
        /// Status Class. It holds current MPD "status" information.
        /// </summary>
        /// 
        public class Status
        {
            public enum MpdPlayState
            {
                Play, Pause, Stop
            };

            private MpdPlayState _ps;
            private int _volume;
            private bool _repeat;
            private bool _random;
            private string _songID = "";
            private double _songTime;
            private double _songElapsed;

            public MpdPlayState MpdState
            {
                get { return _ps; }
                set { _ps = value; }
            }

            public int MpdVolume
            {
                get { return _volume; }
                set
                {
                    //todo check value. "0-100 or -1 if the volume cannot be determined"
                    _volume = value;
                }
            }

            public bool MpdRepeat
            {
                get { return _repeat; }
                set
                {
                    _repeat = value;
                }
            }

            public bool MpdRandom
            {
                get { return _random; }
                set
                {
                    _random = value;
                }
            }

            public string MpdSongID
            {
                get { return _songID; }
                set
                {
                    _songID = value;
                }
            }

            public double MpdSongTime
            {
                get { return _songTime; }
                set
                {
                    _songTime = value;
                }
            }

            public double MpdSongElapsed
            {
                get { return _songElapsed; }
                set
                {
                    _songElapsed = value;
                }
            }

            public Status()
            {
                //constructor
            }
        }

        /// <summary>
        /// Main MPC (MPD Client) Class. 
        /// </summary>

        #region MPC PRIVATE FIELD declaration

        private string _h;
        private int _p;
        private string _a;
        private Status _st;
        private Song _currentSong;
        private ObservableCollection<Song> _songs = new ObservableCollection<Song>();
        private ObservableCollection<String> _playLists = new ObservableCollection<String>();
        private EventDrivenTCPClient _idleClient;
        //object _objLock = new object();
        private CommandTCPClient _commandClient;

        #endregion END of MPC PRIVATE FIELD declaration

        #region MPC PUBLIC PROPERTY and EVENT FIELD

        public string MpdHost
        {
            get { return _h; }
            set
            {
                _h = value;
            }
        }

        public int MpdPort
        {
            get { return _p; }
            set
            {
                _p = value;
            }
        }

        public Status MpdStatus
        {
            get { return _st; }
        }

        public Song MpdCurrentSong
        {
            // The Song object is currectly set only if
            // Playlist is received and song id is matched.
            get
            {
                return _currentSong;
            }
            set
            {
                _currentSong = value;
            }
        }

        public ObservableCollection<Song> CurrentQueue
        {
            get { return this._songs; }
        }

        public ObservableCollection<String> Playlists
        {
            get { return this._playLists; }
        }

        public delegate void MpdStatusChanged(MPC sender, object data);

        public event MpdStatusChanged StatusChanged;

        public delegate void MpdError(MPC sender, object data);

        public event MpdError ErrorReturned;

        public bool MpdStop { get; set; }

        #endregion END of MPC PUBLIC PROPERTY and EVENT FIELD

        // MPC Constructor
        public MPC(string h, int p, string a)
        {
            this._h = h;
            this._p = p;
            this._a = a;
            this._st = new Status();

            // Oops.
            //BindingOperations.EnableCollectionSynchronization(this._songs, new object());
            //BindingOperations.EnableCollectionSynchronization(this._playLists, new object());

            // Initialize idle tcp client.
            _idleClient = new EventDrivenTCPClient(IPAddress.Parse(this._h), this._p, true);
            _idleClient.DisableReceiveTimeout = true;
            _idleClient.DataReceived += new EventDrivenTCPClient.delDataReceived(IdleClient_DataReceived);
            _idleClient.ConnectionStatusChanged += new EventDrivenTCPClient.delConnectionStatusChanged(IdleClient_ConnectionStatusChanged);

            // Initialize cmd tcp client.
            _commandClient = new CommandTCPClient();
        }

        #region MPC METHODS

        public async Task<bool> MpdCmdConnect()
        {
            bool isDone = await _commandClient.Connect(IPAddress.Parse(this._h), this._p, this._a); 
            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("MpdCmdConnect(): _commandClient.Connect() returned false.");
                ErrorReturned?.Invoke(this, "Connection Error: (C0)");

                //TODO: better, more detailed error message handling?
            }
            return isDone;
        }

        public bool MpdCmdDisConnect()
        {
            _commandClient.DisConnect();
            return true;
        }

        public async Task<bool> MpdQueryStatus()
        {
            try
            {
                string mpdCommand = "status";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";

                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";

                    mpdCommand = mpdCommand + "status" + "\n";

                    mpdCommand = mpdCommand + "command_list_end";
                }

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                return ParseStatusResponse(tsResponse.Result);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryStatus(): " + ex.Message);
            }
            return false;
        }

        private bool ParseStatusResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null) {
                System.Diagnostics.Debug.WriteLine("ParseStatusResponse sl==null");
                // Fire up error event.
                ErrorReturned?.Invoke(this, "Connection Error: (@C1)");
                return false; }
            if (sl.Count == 0) {
                System.Diagnostics.Debug.WriteLine("ParseStatusResponse slCount == 0");
                return false; } 

            Dictionary<string, string> MpdStatusValues = new Dictionary<string, string>();

            try {

                foreach (string value in sl)
                {
                    //System.Diagnostics.Debug.WriteLine("ParseStatusResponse loop: " + value);

                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParseStatusResponse: " + value);
                        ErrorReturned?.Invoke(this, "MPD Error (@C1): " + value);
                        return false;
                    }

                    string[] StatusValuePair = value.Split(':');
                    if (StatusValuePair.Length > 1)
                    {
                        if (MpdStatusValues.ContainsKey(StatusValuePair[0].Trim()))
                        {
                            if (StatusValuePair.Length == 2)
                            {
                                MpdStatusValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                            }
                            else if (StatusValuePair.Length == 3)
                            {
                                MpdStatusValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim() + ':' + StatusValuePair[2].Trim();
                            }

                        }
                        else
                        {
                            if (StatusValuePair.Length == 2)
                            {
                                MpdStatusValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                            }
                            else if (StatusValuePair.Length == 3)
                            {
                                MpdStatusValues.Add(StatusValuePair[0].Trim(), (StatusValuePair[1].Trim() + ":" + StatusValuePair[2].Trim()));
                            }
                        }
                    }
                }

                // Play state
                if (MpdStatusValues.ContainsKey("state"))
                {
                    switch (MpdStatusValues["state"])
                    {
                        case "play":
                            {
                                _st.MpdState = Status.MpdPlayState.Play;
                                break;
                            }
                        case "pause":
                            {
                                _st.MpdState = Status.MpdPlayState.Pause;
                                break;
                            }
                        case "stop":
                            {
                                _st.MpdState = Status.MpdPlayState.Stop;
                                break;
                            }
                    }
                }

                // Volume
                if (MpdStatusValues.ContainsKey("volume"))
                {
                    try
                    {
                        _st.MpdVolume = Int32.Parse(MpdStatusValues["volume"]);
                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // songID
                _st.MpdSongID = "";
                if (MpdStatusValues.ContainsKey("songid"))
                {
                    _st.MpdSongID = MpdStatusValues["songid"];
                    //System.Diagnostics.Debug.WriteLine("StatusResponse songid:"+ _st.MpdSongID);
                    /*
                    if (_st.MpdSongID != "") {
                        // Not good when multithreading.

                        // Set currentSong.
                        try
                        {
                            lock (_objLock)
                            {
                                var item = _songs.FirstOrDefault(i => i.ID == _st.MpdSongID);
                                if (item != null)
                                {
                                    this._currentSong = (item as Song);
                                }
                            }

                            //var listItem = _songs.Where(i => i.ID == _st.MpdSongID);
                            //if (listItem != null)
                            //{
                            //    foreach (var item in listItem)
                            //    {
                            //        this._currentSong = item as Song;
                            //        break;
                            //        //System.Diagnostics.Debug.WriteLine("StatusResponse linq: _songs.Where?="+ _currentSong.Title);
                            //    }
                            //}

                        }
                        catch (Exception ex)
                        {
                            // System.NullReferenceException
                            System.Diagnostics.Debug.WriteLine("Error@StatusResponse linq: _songs.Where?: " + ex.Message);
                        }
                    }
                    */
                }

                // Repeat opt bool.
                if (MpdStatusValues.ContainsKey("repeat"))
                {
                    try
                    {
                        if (MpdStatusValues["repeat"] == "1")
                        {
                            _st.MpdRepeat = true;
                        }
                        else
                        {
                            _st.MpdRepeat = false;
                        }

                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Random opt bool.
                if (MpdStatusValues.ContainsKey("random"))
                {
                    try
                    {
                        if (Int32.Parse(MpdStatusValues["random"]) > 0)
                        {
                            _st.MpdRandom = true;
                        }
                        else
                        {
                            _st.MpdRandom = false;
                        }

                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Song time.
                if (MpdStatusValues.ContainsKey("time"))
                {
                    //System.Diagnostics.Debug.WriteLine(MpdStatusValues["time"]);
                    try
                    {
                        if (MpdStatusValues["time"].Split(':').Length > 1)
                        {
                            _st.MpdSongTime = Double.Parse(MpdStatusValues["time"].Split(':')[1].Trim());
                            _st.MpdSongElapsed = Double.Parse(MpdStatusValues["time"].Split(':')[0].Trim());
                        }
                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Song time elapsed.
                if (MpdStatusValues.ContainsKey("elapsed"))
                {
                    try
                    {
                        _st.MpdSongElapsed = Double.Parse(MpdStatusValues["elapsed"]);
                    }
                    catch { }
                }

                // Song duration.
                if (MpdStatusValues.ContainsKey("duration"))
                {
                    try
                    {
                        _st.MpdSongTime = Double.Parse(MpdStatusValues["duration"]);
                    }
                    catch { }
                }

                //TODO: more?
            }
            catch(Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseStatusResponse:" + ex.Message);
            }

            return true;
        }

        public async Task<bool> MpdQueryCurrentPlaylist()
        {
            try
            {
                string mpdCommand = "playlistinfo";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";

                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";

                    mpdCommand = mpdCommand + "playlistinfo" + "\n";

                    mpdCommand = mpdCommand + "command_list_end";
                }

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                return ParsePlaylistInfoResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryCurrentPlaylist(): " + ex.Message);
            }
            return false;
        }

        private bool ParsePlaylistInfoResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null) {
                System.Diagnostics.Debug.WriteLine("ConError@ParsePlaylistInfoResponse: null");
                ErrorReturned?.Invoke(this, "Connection Error: (@C2)");
                return false; }

            try {
                // Don't. Not good when multithreading. Make sure you clear the collection before calling MpdQueryCurrentPlaylist().
                //_songs.Clear();

                Dictionary<string, string> SongValues = new Dictionary<string, string>();
                foreach (string value in sl)
                {
                    //System.Diagnostics.Debug.WriteLine("@ParsePlaylistInfoResponse() loop: " + value);

                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParsePlaylistInfoResponse: " + value);
                        ErrorReturned?.Invoke(this, "MPD Error (@C2): " + value);
                        return false;
                    }

                    try
                    {
                        string[] StatusValuePair = value.Split(':');
                        if (StatusValuePair.Length > 1)
                        {
                            if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                            {
                                SongValues[StatusValuePair[0].Trim()] = StatusValuePair[1].Trim();
                            }
                            else
                            {
                                SongValues.Add(StatusValuePair[0].Trim(), StatusValuePair[1].Trim());
                            }
                        }
                        /*
file: Creedence Clearwater Revival/Chronicle, Vol. 1/11 Who'll Stop the Rain.mp3
Last-Modified: 2011-08-03T16:08:06Z
Artist: Creedence Clearwater Revival
AlbumArtist: Creedence Clearwater Revival
Title: Who'll Stop the Rain
Album: Chronicle, Vol. 1
Track: 11
Date: 1976
Genre: Rock
Composer: John Fogerty
Time: 149
duration: 149.149
Pos: 5
Id: 22637
                         */

                        if (SongValues.ContainsKey("Id"))
                        {

                            Song sng = new Song();
                            sng.ID = SongValues["Id"];

                            if (SongValues.ContainsKey("Title"))
                            {
                                sng.Title = SongValues["Title"];
                            }
                            else
                            {
                                sng.Title = "- no title";
                                if (SongValues.ContainsKey("file"))
                                {
                                    sng.Title = Path.GetFileName(SongValues["file"]);
                                }
                            }

                            if (SongValues.ContainsKey("Artist"))
                            {
                                sng.Artist = SongValues["Artist"];
                            }
                            else
                            {
                                if (SongValues.ContainsKey("AlbumArtist"))
                                {
                                    sng.Artist = SongValues["AlbumArtist"];
                                }
                                else
                                {
                                    sng.Artist = "...";
                                }
                            }
                            //


                            if (sng.ID == _st.MpdSongID)
                            {
                                this._currentSong = sng;
                                //System.Diagnostics.Debug.WriteLine(sng.ID + ":" + sng.Title + " - is current.");
                            }


                            SongValues.Clear();

                            try
                            {
                                //_songs.Add(sng);
                                Device.BeginInvokeOnMainThread(
                                    () =>
                                    {
                                        _songs.Add(sng);
                                    });

                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse _songs.Add: " + ex.Message);
                                return false;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);
                return false;
            }
        }

        public async Task<bool> MpdQueryPlaylists()
        {
            try
            {
                string mpdCommand = "listplaylists";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = "command_list_begin" + "\n";

                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";

                    mpdCommand = mpdCommand + "listplaylists" + "\n";

                    mpdCommand = mpdCommand + "command_list_end";
                }

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                /*
                playlist: Blues
                Last-Modified: 2018-01-26T12:12:10Z
                playlist: Jazz
                Last-Modified: 2018-01-26T12:12:37Z
                OK
                 */

                return ParsePlaylistsResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryPlaylists(): " + ex.Message);
            }
            return false;
        }

        private bool ParsePlaylistsResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null) {
                System.Diagnostics.Debug.WriteLine("Connected response@ParsePlaylistsResponse: null");
                ErrorReturned?.Invoke(this, "Connection Error: (C3)");
                return false; }

            // Don't. Not good when multithreading. Make sure you clear the collection before calling MpdQueryCurrentPlaylist().
            //_playLists.Clear();

            // Tmp list for sorting.
            List<string> slTmp = new List<string>();

            foreach (string value in sl)
            {
                //System.Diagnostics.Debug.WriteLine("@ParsePlaylistsResponse() loop: " + value + "");

                if (value.StartsWith("ACK"))
                {
                    System.Diagnostics.Debug.WriteLine("ACK@ParsePlaylistsResponse: " + value);
                    ErrorReturned?.Invoke(this, "MPD Error (@C3): " + value);
                    return false;
                }

                if (value.StartsWith("playlist:")) {
                    if (value.Split(':').Length > 1) {
                        //_playLists.Add(value.Split(':')[1].Trim()); // need sort.
                        slTmp.Add(value.Split(':')[1].Trim());
                    }
                }
                else if (value.StartsWith("Last-Modified: ") || (value.StartsWith("OK"))) 
                {
                    // Ignoring for now.
                }
            }

            // Sort.
            slTmp.Sort();
            foreach (string v in slTmp)
            {
                _playLists.Add(v);
            }

            return true;
        }

        public async Task<bool> MpdPlaybackPlay(string songID = "")
        {
            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                if (songID != "")
                {
                    mpdCommand = mpdCommand + "playid " + songID + "\n";
                }
                else
                {
                    mpdCommand = mpdCommand + "play" + "\n";
                }

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);

                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPlay: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdPlaybackSeek(string songID, int seekTime)
        {
            if ((songID == "") || (seekTime == 0)) { return false; }

            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "seekid " + songID + " " + seekTime.ToString() + "\n";

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdPlaybackSeek: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdPlaybackPause()
        {
            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "pause 1" + "\n";

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPause: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MpdPlaybackResume()
        {
            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "pause 0" + "\n";

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackResume: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MpdPlaybackStop()
        {
            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "stop" + "\n";

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackStop: " + ex.Message);
            }

            return false;
        }

        public async Task<bool> MpdPlaybackNext()
        {
            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "next" + "\n";

                if (_st.MpdState != Status.MpdPlayState.Play)
                {
                    mpdCommand = mpdCommand + "play" + "\n";
                }

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackNext: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdPlaybackPrev()
        {
            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "previous" + "\n";

                if (_st.MpdState != Status.MpdPlayState.Play)
                {
                    mpdCommand = mpdCommand + "play" + "\n";
                }

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPrev: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdSetVolume(int v)
        {
            if (v == _st.MpdVolume){return true;}

            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "setvol " + v.ToString() + "\n";

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetVol: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdSetRepeat(bool on)
        {
            if (_st.MpdRepeat == on){ return true;}

            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                if (on) {
                    mpdCommand = mpdCommand + "repeat 1" + "\n";
                }
                else
                {
                    mpdCommand = mpdCommand + "repeat 0" + "\n";
                }

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRepeat: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdSetRandom(bool on)
        {
            if (_st.MpdRandom == on){return true;}

            try
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                if (on)
                {
                    mpdCommand = mpdCommand + "random 1" + "\n";
                }
                else
                {
                    mpdCommand = mpdCommand + "random 0" + "\n";
                }

                //mpdCommand = mpdCommand + "status" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParseStatusResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRandom: " + ex.Message);
            }
            return false;
        }

        public async Task<bool> MpdChangePlaylist(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                string mpdCommand = "command_list_begin" + "\n";

                if (!string.IsNullOrEmpty(this._a))
                {
                    mpdCommand = mpdCommand + "password " + this._a.Trim() + "\n";
                }

                mpdCommand = mpdCommand + "clear" + "\n";

                mpdCommand = mpdCommand + "load " + playlistName.Trim() + "\n";
                
                mpdCommand = mpdCommand + "play" + "\n";

                //Testing. Trust idle connection will notify us or not.  
                //mpdCommand = mpdCommand + "playlistinfo" + "\n";

                mpdCommand = mpdCommand + "command_list_end";

                Task<List<string>> tsResponse = _commandClient.SendCommand(mpdCommand);
                await tsResponse;

                //return ParsePlaylistInfoResponse(tsResponse.Result);
                return ParseVoidResponse(tsResponse.Result);
            }
            else
            {
                return false;
            }
        }

        private bool ParseVoidResponse(List<string> sl)
        {
            if (this.MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("ConError@ParseVoidResponse: null");
                ErrorReturned?.Invoke(this, "Connection Error: (@C)");
                return false;
            }

            try
            {
                foreach (string value in sl)
                {
                    //System.Diagnostics.Debug.WriteLine("@ParseVoidResponse() loop: " + value);

                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParseVoidResponse: " + value);
                        ErrorReturned?.Invoke(this, "MPD Error (@C): " + value);
                        return false;
                    }

                }

                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseVoidResponse: " + ex.Message);
                return false;
            }
        }

        public bool MpdIdleConnect()
        {
            //Idle client connect
            try
            {
                if (_idleClient.ConnectionState != EventDrivenTCPClient.ConnectionStatus.Connected)
                {
                    _idleClient.Connect(); 
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdIdleConnect: " + ex.Message);
                return false;
            }

        }

        public bool MpdIdleDisConnect()
        {
            // Close Idle client connection.
            try
            {
                if (_idleClient.ConnectionState == EventDrivenTCPClient.ConnectionStatus.Connected)
                {
                    _idleClient.Disconnect();
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdIdleDisConnect(): " + ex.Message);
                return false;
            }
        }

        private void IdleClient_ConnectionStatusChanged(EventDrivenTCPClient sender, EventDrivenTCPClient.ConnectionStatus status)
        {
            System.Diagnostics.Debug.WriteLine("--IdleConnection: " + status.ToString());

            if (status == EventDrivenTCPClient.ConnectionStatus.Connected)
            {
                /*
                // Connected to MPD and received OK. Now we are idling and wait.
                if (!string.IsNullOrEmpty(this._a))
                {
                    sender.Send("command_list_begin" + "\n" + "password " + this._a.Trim() + "\n" + "idle player mixer options playlist stored_playlist\n" + "command_list_end\n");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("--IdleConnection established, Start idle.");
                    sender.Send("idle player mixer options playlist stored_playlist\n");
                }
                */
            }
        }

        private void IdleClient_DataReceived(EventDrivenTCPClient sender, object data)
        {
            System.Diagnostics.Debug.WriteLine("\n--IdleConnection DataReceived: \n" + (data as string).TrimEnd());

            if (data == null) { return; }

            //Testing. vv Now we handle initial "idle" command at IdleClient_ConnectionStatusChanged. 
            if ((data as string).StartsWith("OK MPD"))
            {
                
                // Connected to MPD and received OK. Now we are idling and wait.
                if (!string.IsNullOrEmpty(this._a))
                {
                    _idleClient.Send("command_list_begin" + "\n" + "password " + this._a.Trim() + "\n" + "idle player mixer options playlist stored_playlist\n" + "command_list_end\n");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("IdleConnection OK MPD, Start idle.\n");
                    _idleClient.Send("idle player mixer options playlist stored_playlist\n");
                }
                
            }
            else
            {

                // Go idle again and wait.
                if (!string.IsNullOrEmpty(this._a))
                {
                    sender.Send("command_list_begin" + "\n" + "password " + this._a.Trim() + "\n" + "idle player mixer options playlist stored_playlist\n" + "command_list_end\n");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("--IdleConnection Going idle again.");
                    sender.Send("idle player mixer options playlist stored_playlist\n");
                }

                /*
                 changed: playlist
                 changed: player
                 changed: options
                 OK
                */

                // Init List which is later used in StatusChangeEvent.
                List<string> SubSystems = new List<string>();

                try
                {
                    string[] Lines = Regex.Split((data as string), "\n");

                    foreach (string line in Lines)
                    {
                        string[] Values;
                        if (line.Trim() != "" && line != "OK")
                        {
                            Values = line.Split(':');
                            if (Values.Length > 1)
                            {
                                //System.Diagnostics.Debug.WriteLine("IdleConnection DataReceived lines loop: " + Values[1]);
                                SubSystems.Add(Values[1].Trim());
                            }
                        }
                    }

                    if (SubSystems.Count > 0)
                    {
                        // Fire up changed event.
                        StatusChanged?.Invoke(this, SubSystems);
                    }
                }
                catch
                {
                    System.Diagnostics.Debug.WriteLine("--Error@IdleConnection DataReceived: " + (data as string));
                }


                /*
                // Go idle again and wait.
                if (!string.IsNullOrEmpty(this._a))
                {
                    sender.Send("command_list_begin" + "\n" + "password " + this._a.Trim() + "\n" + "idle player mixer options playlist stored_playlist\n" + "command_list_end\n");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("IdleConnection Going idle again.");
                    sender.Send("idle player mixer options playlist stored_playlist\n");
                }
                */

            }
        }

        public async Task<bool> MpdIdleStart()
        {
            // send "idle" command
            try
            {
                if (_idleClient.ConnectionState == EventDrivenTCPClient.ConnectionStatus.Connected)
                {
                    await Task.Run(() => { _idleClient.Send("idle player mixer options playlist stored_playlist\n"); });
                    return true;
                }
                else { return false; }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDIdleStart(): " + ex.Message);
                return false;
            }
        }

        public async Task<bool> MpdIdleStop()
        {
            //Idle client send "noidle" command
            try
            {
                if (_idleClient.ConnectionState == EventDrivenTCPClient.ConnectionStatus.Connected)
                {
                    await Task.Run(() => { _idleClient.Send("noidle\n"); });
                    return true;
                }
                else { return false; }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDIdleStop(): " + ex.Message);
                return false;
            }
        }

        #endregion END of MPD METHODS

        /// END OF MPC Client Class 
    }


    public class CommandTCPClient
    {
        private TcpClient _TCP;
        private IPAddress _ip = IPAddress.None;
        private int _p = 0;
        private string _a = "";
        private int _retryAttempt = 0;

        public CommandTCPClient()
        {
            this._TCP = new TcpClient();
            this._TCP.NoDelay = true;
            this._TCP.ReceiveTimeout = 1000;

            // KeepAlive testing.
            this._TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);
        }

        public async Task<bool> Connect(IPAddress ip, int port, string auth)
        {
            _ip = ip;
            _p = port;
            _a = auth;
            _retryAttempt = 0;
            return await DoConnect(ip, port, auth);
        }

        public async Task<bool> ReConnect()
        {
            if (_retryAttempt > 0)
            {
                System.Diagnostics.Debug.WriteLine("**SendCommand@ReConnect() _retryAttempt > 0");
                return false;
            }

            _retryAttempt++;

            try {
                this._TCP.Close();
                //this._TCP.Client.Dispose();
                //this._TCP.Client.Disconnect(true);
            }
            catch { }

            await Task.Delay(500);

            this._TCP = new TcpClient();
            this._TCP.NoDelay = true;
            this._TCP.ReceiveTimeout = 1000;

            // KeepAlive testing.
            this._TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            return await DoConnect( _ip, _p, _a);
        }

        public async Task<bool> DoConnect(IPAddress ip, int port, string auth)
        {
            try
            {
                await _TCP.ConnectAsync(ip, port);
                NetworkStream networkStream = _TCP.GetStream();
                StreamReader reader = new StreamReader(networkStream);

                // First check MPD's initial response on connect.
                string responseLine = await reader.ReadLineAsync();

                if (responseLine == null)
                {
                    System.Diagnostics.Debug.WriteLine("**DoConnect response: null");
                    return false;
                }

                System.Diagnostics.Debug.WriteLine("**DoConnect response: " + responseLine);

                if (responseLine.StartsWith("OK MPD"))
                {
                    _retryAttempt = 0;
                    return true;
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("**DoConnected MPD Non-OK response: " + responseLine);
                    return false;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@DoConnect: " + ex.Message);
                return false;
            }

        }

        public async Task<List<string>> SendCommand(string cmd)
        {
            Task<List<string>> tsResponse =  DoSendCommand(this._TCP, cmd);

            try { 
                await tsResponse;
            }
            catch (IOException)
            {
                //System.IO.IOException
                //Unable to transfer data on the transport connection: An established connection was aborted by the software in your host machine.

                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: IOException - TRYING TO RECONNECT.");

                // Reconnect.
                if (await ReConnect())
                {
                    return await DoSendCommand(_TCP, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: IOException - GIVING UP reconnect.");
                    return null;
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: " + ex.Message);
                return null;
            }

            if ((tsResponse.Result.Count == 0)  && (!IsConnected))
            {
                System.Diagnostics.Debug.WriteLine("**@SendCommand@responseMultiLines.Count == 0 & !IsConnected - TRYING TO RECONNECT");

                // Reconnect.
                if (await ReConnect())
                {
                    return await DoSendCommand(_TCP, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@SendCommand@responseMultiLines.Count == 0: GIVING UP RECONNECT.");
                    return null;
                }
            }

            return tsResponse.Result;
        }

        public static async Task<List<string>> DoSendCommand(TcpClient tcp, string cmd)
        {
            // It's a magic. Everything works..iPhone, and NotifyPropertyChanged("SelectedSong") in UWP works with this...
            await Task.Delay(100);

            List<string> responseMultiLines = new List<string>();

            NetworkStream networkStream = tcp.GetStream();
            StreamWriter writer = new StreamWriter(networkStream);
            StreamReader reader = new StreamReader(networkStream);
            //writer.AutoFlush = true;

            System.Diagnostics.Debug.WriteLine("**Request: " + cmd);

            await writer.WriteLineAsync(cmd);
            await writer.FlushAsync();

            System.Diagnostics.Debug.WriteLine("**Waiting Response...");

            string responseLine;

            while (!reader.EndOfStream)
            {
                if (!String.IsNullOrEmpty(responseLine = await reader.ReadLineAsync()))
                {
                    //System.Diagnostics.Debug.WriteLine("**ResponseLine:" + responseLine);

                    // Read multiple lines untill "OK".
                    if (!responseLine.StartsWith("OK"))
                    {
                        responseMultiLines.Add(responseLine);

                        if (responseLine.StartsWith("ACK"))
                        {
                            System.Diagnostics.Debug.WriteLine("**Response ACK: " + responseLine + "\n");
                            break;
                        }
                    }
                    else
                    {
                        responseMultiLines.Add(responseLine);

                        System.Diagnostics.Debug.WriteLine("**Response line(s) received 'tll OK.");

                        break;
                    }
                }
                else
                {
                    break;
                }
            }

            await Task.Delay(100);
            return responseMultiLines;
        }

        public bool IsConnected
        {
            get
            {
                try
                {
                    //return _tcpClient != null && _tcpClient.Client != null && _tcpClient.Client.Connected;

                    if (_TCP != null && _TCP.Client != null && _TCP.Client.Connected)
                    {

                        /* As the documentation:
                            * When passing SelectMode.SelectRead as a parameter to the Poll method it will return 
                            * -either- true if Socket.Listen(Int32) has been called and a connection is pending;
                            * -or- true if data is available for reading; 
                            * -or- true if the connection has been closed, reset, or terminated; 
                            * otherwise, returns false
                            */

                        // Detect if client disconnected
                        if (_TCP.Client.Poll(0, SelectMode.SelectRead))
                        {
                            byte[] buff = new byte[1];
                            if (_TCP.Client.Receive(buff, SocketFlags.Peek) == 0)
                            {
                                // Client disconnected
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        public bool DisConnect()
        {
            if (this._TCP != null) {
                try
                {
                    this._TCP.Close();
                    this._TCP.Client.Dispose();
                    System.Diagnostics.Debug.WriteLine("**Connection closed.");
                }
                catch { }
            }
            return true;
        }

    }


    /// <summary>
    /// Event driven TCP client wrapper
    /// https://www.daniweb.com/programming/software-development/code/422291/user-friendly-asynchronous-event-driven-tcp-client
    /// 
    /// Modified to use Task. Added disable TimeoutReceive.
    /// 
    /// </summary>
    public class EventDrivenTCPClient : IDisposable
    {
        #region Consts/Default values
        const int DEFAULTTIMEOUT = 5000; //Default to 5 seconds on all timeouts
        const int RECONNECTINTERVAL = 2000; //Default to 2 seconds reconnect attempt rate
        #endregion

        #region Components, Events, Delegates, and CTOR
        //Timer used to detect receive timeouts
        private System.Timers.Timer tmrReceiveTimeout = new System.Timers.Timer();
        private System.Timers.Timer tmrSendTimeout = new System.Timers.Timer();
        private System.Timers.Timer tmrConnectTimeout = new System.Timers.Timer();
        public delegate void delDataReceived(EventDrivenTCPClient sender, object data);
        public event delDataReceived DataReceived;
        public delegate void delConnectionStatusChanged(EventDrivenTCPClient sender, ConnectionStatus status);
        public event delConnectionStatusChanged ConnectionStatusChanged;
        public enum ConnectionStatus
        {
            NeverConnected,
            Connecting,
            Connected,
            AutoReconnecting,
            DisconnectedByUser,
            DisconnectedByHost,
            ConnectFail_Timeout,
            ReceiveFail_Timeout,
            SendFail_Timeout,
            SendFail_NotConnected,
            Error
        }
        public EventDrivenTCPClient(IPAddress ip, int port, bool autoreconnect = true)
        {
            this._IP = ip;
            this._Port = port;
            this._AutoReconnect = autoreconnect;
            this._client = new TcpClient(AddressFamily.InterNetwork);
            this._client.NoDelay = true; //Disable the nagel algorithm for simplicity
            ReceiveTimeout = DEFAULTTIMEOUT;
            SendTimeout = DEFAULTTIMEOUT;
            ConnectTimeout = DEFAULTTIMEOUT;
            ReconnectInterval = RECONNECTINTERVAL;
            tmrReceiveTimeout.AutoReset = false;
            tmrReceiveTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrReceiveTimeout_Elapsed);
            tmrConnectTimeout.AutoReset = false;
            tmrConnectTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrConnectTimeout_Elapsed);
            tmrSendTimeout.AutoReset = false;
            tmrSendTimeout.Elapsed += new System.Timers.ElapsedEventHandler(tmrSendTimeout_Elapsed);

            ConnectionState = ConnectionStatus.NeverConnected;
        }
        #endregion

        #region Private methods/Event Handlers
        void tmrSendTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.ConnectionState = ConnectionStatus.SendFail_Timeout;
            DisconnectByHost();
        }
        void tmrReceiveTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.ConnectionState = ConnectionStatus.ReceiveFail_Timeout;
            DisconnectByHost();
        }
        void tmrConnectTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            ConnectionState = ConnectionStatus.ConnectFail_Timeout;
            DisconnectByHost();
        }
        private void DisconnectByHost()
        {
            this.ConnectionState = ConnectionStatus.DisconnectedByHost;
            tmrReceiveTimeout.Stop();
            if (AutoReconnect)
                Reconnect();
        }
        private void Reconnect()
        {
            if (this.ConnectionState == ConnectionStatus.Connected)
                return;
            this.ConnectionState = ConnectionStatus.AutoReconnecting;
            try
            {
                this._client.Client.BeginDisconnect(true, new AsyncCallback(cbDisconnectByHostComplete), this._client.Client);
            }
            catch { }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Try connecting to the remote host
        /// </summary>
        public void Connect()
        {
            if (this.ConnectionState == ConnectionStatus.Connected)
                return;
            this.ConnectionState = ConnectionStatus.Connecting;
            tmrConnectTimeout.Start();
            this._client.BeginConnect(this._IP, this._Port, new AsyncCallback(cbConnect), this._client.Client);
        }
        /// <summary>
        /// Try disconnecting from the remote host
        /// </summary>
        public void Disconnect()
        {
            if (this.ConnectionState != ConnectionStatus.Connected)
                return;
            this._client.Client.BeginDisconnect(true, new AsyncCallback(cbDisconnectComplete), this._client.Client);
        }
        /// <summary>
        /// Try sending a string to the remote host
        /// </summary>
        /// <param name="data">The data to send</param>
        public void Send(string data)
        {
            if (this.ConnectionState != ConnectionStatus.Connected)
            {
                this.ConnectionState = ConnectionStatus.SendFail_NotConnected;
                return;
            }
            var bytes = _encode.GetBytes(data);
            SocketError err = new SocketError();
            tmrSendTimeout.Start();
            this._client.Client.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, out err, new AsyncCallback(cbSendComplete), this._client.Client);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
        }
        /// <summary>
        /// Try sending byte data to the remote host
        /// </summary>
        /// <param name="data">The data to send</param>
        public void Send(byte[] data)
        {
            if (this.ConnectionState != ConnectionStatus.Connected)
                throw new InvalidOperationException("Cannot send data, socket is not connected");
            SocketError err = new SocketError();
            this._client.Client.BeginSend(data, 0, data.Length, SocketFlags.None, out err, new AsyncCallback(cbSendComplete), this._client.Client);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
        }
        public void Dispose()
        {
            this._client.Close();
            this._client.Client.Dispose();
        }
        #endregion

        #region Callbacks
        private void cbConnectComplete()
        {
            if (_client.Connected == true)
            {
                tmrConnectTimeout.Stop();
                ConnectionState = ConnectionStatus.Connected;

                System.Diagnostics.Debug.WriteLine("^^cbConnectComplete, BeginReceive");
                this._client.Client.BeginReceive(this.dataBuffer, 0, this.dataBuffer.Length, SocketFlags.None, new AsyncCallback(cbDataReceived), this._client.Client);

            }
            else
            {
                ConnectionState = ConnectionStatus.Error;
            }
        }
        private void cbDisconnectByHostComplete(IAsyncResult result)
        {
            var r = result.AsyncState as Socket;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            r.EndDisconnect(result);
            if (this.AutoReconnect)
            {
                Action doConnect = new Action(Connect);
                doConnect.Invoke();
                return;
            }
        }
        private void cbDisconnectComplete(IAsyncResult result)
        {
            var r = result.AsyncState as Socket;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            r.EndDisconnect(result);
            this.ConnectionState = ConnectionStatus.DisconnectedByUser;

        }
        private void cbConnect(IAsyncResult result)
        {
            var sock = result.AsyncState as Socket;
            if (result == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            if (!sock.Connected)
            {
                if (AutoReconnect)
                {
                    System.Threading.Thread.Sleep(ReconnectInterval);
                    Action reconnect = new Action(Connect);
                    reconnect.Invoke();
                    return;
                }
                else
                    return;
            }
            sock.EndConnect(result);
            var callBack = new Action(cbConnectComplete);
            callBack.Invoke();
        }
        private void cbSendComplete(IAsyncResult result)
        {
            var r = result.AsyncState as Socket;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a socket object");
            SocketError err = new SocketError();
            r.EndSend(result, out err);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
            else
            {
                lock (SyncLock)
                {
                    tmrSendTimeout.Stop();
                }
            }
        }
        /* No longer used.
        private void cbChangeConnectionStateComplete(IAsyncResult result)
        {
            var r = result.AsyncState as EventDrivenTCPClient;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as a EDTC object");
            r.ConnectionStatusChanged.EndInvoke(result);
        }
        */
        private void cbDataReceived(IAsyncResult result)
        {
            var sock = result.AsyncState as Socket;
            if (sock == null)
                throw new InvalidOperationException("Invalid IASyncResult - Could not interpret as a socket");
            SocketError err = new SocketError();
            int bytes = sock.EndReceive(result, out err);  
            if (bytes == 0 || err != SocketError.Success)
            {
                lock (SyncLock)
                {
                    if (!_DisableReceiveTimeout)
                    {
                        tmrReceiveTimeout.Start();
                    }
                    return;
                }
            }
            else
            {
                lock (SyncLock)
                {
                    tmrReceiveTimeout.Stop();
                }
            }
            if (DataReceived != null)
            {
                // Old code.
                //DataReceived.BeginInvoke(this, _encode.GetString(dataBuffer, 0, bytes), new AsyncCallback(cbDataRecievedCallbackComplete), this);

                // Modified. Substitute for DataReceived.BeginInvoke cbDataRecievedCallbackComplete
                Task.Run(() => { DataReceived.Invoke(this, _encode.GetString(dataBuffer, 0, bytes)); });
                System.Diagnostics.Debug.WriteLine("^^cbDataRecieved&CallbackComplete, BeginReceive...");
                this._client.Client.BeginReceive(this.dataBuffer, 0, this.dataBuffer.Length, SocketFlags.None, new AsyncCallback(cbDataReceived), this._client.Client);
            }
        }
        /* No longer used.
        private void cbDataRecievedCallbackComplete(IAsyncResult result)
        {
            var r = result.AsyncState as EventDrivenTCPClient;
            if (r == null)
                throw new InvalidOperationException("Invalid IAsyncResult - Could not interpret as EDTC object");
            r.DataReceived.EndInvoke(result);
            SocketError err = new SocketError();
            //this._client.Client.BeginReceive(this.dataBuffer, 0, this.dataBuffer.Length, SocketFlags.None, out err, new AsyncCallback(cbDataReceived), this._client.Client);
            if (err != SocketError.Success)
            {
                Action doDCHost = new Action(DisconnectByHost);
                doDCHost.Invoke();
            }
        }
        */
        #endregion

        #region Properties and members
        private IPAddress _IP = IPAddress.None;
        private ConnectionStatus _ConStat;
        private TcpClient _client;
        private byte[] dataBuffer = new byte[5000];
        private bool _AutoReconnect = false;
        private bool _DisableReceiveTimeout = true;
        private int _Port = 0;
        private Encoding _encode = Encoding.Default;
        object _SyncLock = new object();
        /// <summary>
        /// Syncronizing object for asyncronous operations
        /// </summary>
        public object SyncLock
        {
            get
            {
                return _SyncLock;
            }
        }
        /// <summary>
        /// Encoding to use for sending and receiving
        /// </summary>
        public Encoding DataEncoding
        {
            get
            {
                return _encode;
            }
            set
            {
                _encode = value;
            }
        }
        /// <summary>
        /// Current state that the connection is in
        /// </summary>
        public ConnectionStatus ConnectionState
        {
            get
            {
                return _ConStat;
            }
            private set
            {
                bool raiseEvent = value != _ConStat;
                _ConStat = value;
                if (ConnectionStatusChanged != null && raiseEvent)
                    //ConnectionStatusChanged.BeginInvoke(this, _ConStat, new AsyncCallback(cbChangeConnectionStateComplete), this);
                    // Modified.
                    Task.Run(() => { ConnectionStatusChanged(this, _ConStat); });
            }
        }
        /// <summary>
        /// True to autoreconnect at the given reconnection interval after a remote host closes the connection
        /// </summary>
        public bool AutoReconnect
        {
            get
            {
                return _AutoReconnect;
            }
            set
            {
                _AutoReconnect = value;
            }
        }
        public int ReconnectInterval { get; set; }
        /// <summary>
        /// IP of the remote host
        /// </summary>
        public IPAddress IP
        {
            get
            {
                return _IP;
            }
        }
        /// <summary>
        /// Port to connect to on the remote host
        /// </summary>
        public int Port
        {
            get
            {
                return _Port;
            }
        }
        /// <summary>
        /// Time to wait after a receive operation is attempted before a timeout event occurs
        /// </summary>
        public int ReceiveTimeout
        {
            get
            {
                return (int)tmrReceiveTimeout.Interval;
            }
            set
            {
                tmrReceiveTimeout.Interval = (double)value;
            }
        }
        /// <summary>
        /// Time to wait after a send operation is attempted before a timeout event occurs
        /// </summary>
        public int SendTimeout
        {
            get
            {
                return (int)tmrSendTimeout.Interval;
            }
            set
            {
                tmrSendTimeout.Interval = (double)value;
            }
        }
        /// <summary>
        /// Time to wait after a connection is attempted before a timeout event occurs
        /// </summary>
        public int ConnectTimeout
        {
            get
            {
                return (int)tmrConnectTimeout.Interval;
            }
            set
            {
                tmrConnectTimeout.Interval = (double)value;
            }
        }

        /// Added.
        public bool DisableReceiveTimeout
        {
            get
            {
                return _DisableReceiveTimeout;
            }
            set
            {
                _DisableReceiveTimeout = value;
            }
        }

        #endregion
    }

}
