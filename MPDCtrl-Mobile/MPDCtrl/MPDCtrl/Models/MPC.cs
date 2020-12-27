/// 
/// 
/// MPDCtrl
/// https://github.com/torum/MPDCtrl
/// 
/// MPD Protocol
/// https://www.musicpd.org/doc/html/protocol.html
/// 

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using System.Collections.ObjectModel;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Drawing;
using MPDCtrl.ViewModels;
using MPDCtrl.Models.Classes;
using Xamarin.Forms;

namespace MPDCtrl.Models
{
    /// <summary>
    /// MPD client class. 
    /// </summary>
    public class MPC
    {

        #region == Consts, Properties, etc == 

        public enum MpdErrorTypes
        {
            CommandError, //ACK
            ConnectionError,
            StatusError, //[status] eg. "error: Failed to open audio output"
            ErrorClear, //
        }

        private string _host;
        public string MpdHost
        {
            get { return _host; }
            set
            {
                _host = value;
            }
        }

        private int _port;
        public int MpdPort
        {
            get { return _port; }
            set
            {
                _port = value;
            }
        }

        private string _password;
        public string MpdPassword
        {
            get { return _password; }
            set
            {
                _password = value;
            }
        }

        private string _mpdVer;
        public string MpdVer
        {
            get { return _mpdVer; }
            set
            {
                _mpdVer = value;
            }
        }

        private Status _status = new Status();
        public Status MpdStatus
        {
            get { return _status; }
        }

        public bool MpdStop { get; set; }

        private AlbumCover _albumCover = new AlbumCover();
        public AlbumCover AlbumArt
        {
            get { return _albumCover; }
        }

        private Song _currentSong;
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

        private ObservableCollection<SongInfo> _queue = new ObservableCollection<SongInfo>();
        public ObservableCollection<SongInfo> CurrentQueue
        {
            get { return _queue; }
        }

        private ObservableCollection<String> _playLists = new ObservableCollection<String>();
        public ObservableCollection<String> Playlists
        {
            get { return _playLists; }
        }

        private ObservableCollection<String> _localFiles = new ObservableCollection<String>();
        public ObservableCollection<String> LocalFiles
        {
            get { return _localFiles; }
        }

        private ObservableCollection<String> _localDirectories = new ObservableCollection<String>();
        public ObservableCollection<String> LocalDirectories
        {
            get { return _localDirectories; }
        }

        private ObservableCollection<Song> _searchResult = new ObservableCollection<Song>();
        public ObservableCollection<Song> SearchResult
        {
            get { return _searchResult; }
        }

        private TCPC _asyncClient = new TCPC();

        #endregion

        #region == Events == 

        //public delegate void MpdStatusChanged(MPC sender, object data);
        //public event MpdStatusChanged StatusChanged;

        public delegate void MpdError(MPC sender, MpdErrorTypes err, object data);
        public event MpdError ErrorReturned;

        public delegate void MpdConnected(MPC sender);
        public event MpdConnected Connected;

        public delegate void MpdConnectionError(MPC sender, object data);
        public event MpdConnectionError ErrorConnected;

        public delegate void MpdStatusUpdate(MPC sender, object data);
        public event MpdStatusUpdate StatusUpdate;

        public delegate void MpdDataReceived(MPC sender, object data);
        public event MpdDataReceived DataReceived;

        public delegate void MpdDataSent(MPC sender, object data);
        public event MpdDataSent DataSent;

        public delegate void MpdConnectionStatusChanged(MPC sender, TCPC.ConnectionStatus status);
        public event MpdConnectionStatusChanged ConnectionStatusChanged;

        public delegate void MpdIsBusy(MPC sender, bool on);
        public event MpdIsBusy IsBusy;

        #endregion

        public MPC(string h, int p, string a)
        {
            _host = h;
            _port = p;
            _password = a;

            _asyncClient.DataReceived += new TCPC.delDataReceived(TCPClient_DataReceived);
            _asyncClient.DataBinaryReceived += new TCPC.delDataBinaryReceived(TCPClient_DataBinaryReceived);
            _asyncClient.DataSent += new TCPC.delDataSent(TCPClient_DataSent);
            _asyncClient.ConnectionStatusChanged += new TCPC.delConnectionStatusChanged(TCPClient_ConnectionStatusChanged);
            _asyncClient.ConnectionError += new TCPC.delConnectionError(TCPClient_ConnectionError);
        }

        #region == MPC Commands ==   

        public async Task<ConnectionResult> MpdConnect()
        {
            try
            {
                MpdVer = "";
                _status.Reset();

                ConnectionResult isDone = await _asyncClient.Connect(IPAddress.Parse(_host), _port);

                if (isDone.isSuccess)
                {
                    // things will be handled at OnMpdConnected event.


                    if (_albumCover != null)
                        if (_albumCover.IsDownloading)
                            _albumCover.IsDownloading = false;
                }
                else
                {
                    await Task.Run(() => {
                        ErrorConnected?.Invoke(this, isDone.errorMessage);
                    });

                    await Task.Run(() => {
                        ErrorReturned?.Invoke(this, MpdErrorTypes.ConnectionError, isDone.errorMessage);
                    });
                }

                return isDone;
            }
            catch (Exception ex)
            {
                await Task.Run(() => {
                    ErrorConnected?.Invoke(this, ex.Message);
                });

                await Task.Run(() => {
                    ErrorReturned?.Invoke(this, MpdErrorTypes.ConnectionError, ex.Message);
                });

                ConnectionResult err = new ConnectionResult();
                err.isSuccess = false;
                err.errorMessage = ex.Message;

                return err;
            }

        }

        public void MpdDisconnect()
        {
            MpdVer = "";

            _asyncClient.DisConnect();
        }

        public void MpdSendPassword()
        {
            try
            {
                if (!string.IsNullOrEmpty(_password))
                {
                    string mpdCommand = "password " + _password.Trim() + "\n";

                    _asyncClient.Send("noidle" + "\n"); // do I need this?
                    _asyncClient.Send(mpdCommand);
                    _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdSendPassword(): " + ex.Message);
            }
        }

        public void MpdSendUpdate()
        {
            try
            {
                string mpdCommand = "update" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdSendUpdate(): " + ex.Message);
            }
        }

        public void MpdQueryStatus()
        {
            try
            {
                string mpdCommand = "status" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryStatus(): " + ex.Message);
            }
        }

        public void MpdQueryCurrentQueue()
        {
            try
            {
                string mpdCommand = "playlistinfo" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryCurrentQueue(): " + ex.Message);
            }
        }

        public void MpdQueryPlaylists()
        {
            try
            {
                string mpdCommand = "listplaylists" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDQueryPlaylists(): " + ex.Message);
            }
        }

        public void MpdQueryListAll()
        {
            try
            {
                string mpdCommand = "listall" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdMpdListAll: " + ex.Message);
            }
        }

        public void MpdQueryListPlaylistinfo(string playlistName)
        {
            // Don't use this. The reply cannot be differenciated with "find/search" result.

            if (playlistName.Trim() != "")
            {
                playlistName = Regex.Escape(playlistName);

                try
                {
                    string mpdCommand = "listplaylistinfo \"" + playlistName + "\"\n";

                    _asyncClient.Send("noidle" + "\n");
                    _asyncClient.Send(mpdCommand);
                    _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error@MpdMpdlistplaylistinfo: " + ex.Message);
                }

            }
        }
        
        public void MpdClear()
        {
            try
            {
                string mpdCommand = "clear" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdClear: " + ex.Message);
            }
        }

        public void MpdSave(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                playlistName = Regex.Escape(playlistName);

                try
                {
                    string mpdCommand = "save \"" + playlistName + "\"\n";

                    _asyncClient.Send("noidle" + "\n");
                    _asyncClient.Send(mpdCommand);
                    _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error@MpdSave: " + ex.Message);
                }
            }

        }

        public void MpdAdd(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return;

            uri = Regex.Escape(uri);

            try
            {
                string mpdCommand = "add \"" + uri + "\"\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdAdd: " + ex.Message);
            }
        }

        public void MpdAdd(List<string> uris)
        {
            if (uris.Count < 1)
                return;

            try
            {
                string mpdCommand = "";

                mpdCommand = "command_list_begin" + "\n";
                foreach (var uri in uris)
                {
                    var urie = Regex.Escape(uri);
                    mpdCommand = mpdCommand + "add \"" + urie + "\"\n";
                }
                mpdCommand = mpdCommand + "command_list_end" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdAdd: " + ex.Message);
            }

        }
        public void MpdDeleteId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            try
            {
                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send("deleteid " + id + "\n");
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdDeleteId: " + ex.Message);
            }
        }

        public void MpdDeleteId(List<string> ids)
        {
            if (ids.Count < 1)
                return;

            try
            {
                string mpdCommand = "";

                mpdCommand = "command_list_begin" + "\n";
                foreach (var id in ids)
                {
                    mpdCommand = mpdCommand + "deleteid " + id + "\n";
                }
                mpdCommand = mpdCommand + "command_list_end" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdDeleteId: " + ex.Message);
            }
        }

        public void MpdMoveId(Dictionary<string, string> IdToNewPosPair)
        {
            if (IdToNewPosPair == null) return;
            if (IdToNewPosPair.Count < 1) return;

            try
            {
                string mpdCommand = "";

                mpdCommand = "command_list_begin" + "\n";
                foreach (KeyValuePair<string, string> pair in IdToNewPosPair)
                {
                    mpdCommand = mpdCommand + "moveid " + pair.Key + " " + pair.Value + "\n";
                }
                mpdCommand = mpdCommand + "command_list_end" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdMoveId: " + ex.Message);
            }
        }

        public void MpdPlaybackPlay(string songId = "")
        {
            try
            {
                string mpdCommand = "";

                if (songId != "")
                {
                    mpdCommand = "playid " + songId + "\n";
                }
                else
                {
                    mpdCommand = "play" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPlay: " + ex.Message);
            }
        }

        public void MpdPlaybackSeek(string songId, int seekTime)
        {
            if ((songId == "") || (seekTime == 0)) { return; }

            try
            {
                string mpdCommand = "seekid " + songId + " " + seekTime.ToString() + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdPlaybackSeek: " + ex.Message);
            }
        }

        public void MpdPlaybackPause()
        {
            try
            {
                string mpdCommand = "pause 1" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPause: " + ex.Message);
            }
        }

        public void MpdPlaybackResume()
        {
            try
            {
                string mpdCommand = "pause 0" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackResume: " + ex.Message);
            }
        }

        public void MpdPlaybackStop()
        {
            try
            {
                string mpdCommand = "stop" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackStop: " + ex.Message);
            }
        }

        public void MpdPlaybackNext()
        {
            try
            {
                string mpdCommand = "next\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackNext: " + ex.Message);
            }
        }

        public void MpdPlaybackPrev()
        {
            try
            {
                string mpdCommand = "previous" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackPrev: " + ex.Message);
            }
        }

        public void MpdSetVolume(int v)
        {
            if (v == _status.MpdVolume) { return; }

            try
            {
                string mpdCommand = "setvol " + v.ToString() + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetVol: " + ex.Message);
            }
        }

        public void MpdSetRepeat(bool on)
        {
            if (_status.MpdRepeat == on) { return; }

            try
            {
                string mpdCommand = "";

                if (on)
                {
                    mpdCommand = "repeat 1" + "\n";
                }
                else
                {
                    mpdCommand = "repeat 0" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRepeat: " + ex.Message);
            }
        }

        public void MpdSetRandom(bool on)
        {
            if (_status.MpdRandom == on) { return; }

            try
            {
                string mpdCommand = "";

                if (on)
                {
                    mpdCommand = "random 1" + "\n";
                }
                else
                {
                    mpdCommand = "random 0" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetRandom: " + ex.Message);
            }
        }
        
        public void MpdSetConsume(bool on)
        {
            if (_status.MpdConsume == on) { return; }

            try
            {
                string mpdCommand = "";

                if (on)
                {
                    mpdCommand = "consume 1" + "\n";
                }
                else
                {
                    mpdCommand = "consume 0" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetConsume: " + ex.Message);
            }
        }

        public void MpdSetSingle(bool on)
        {
            if (_status.MpdSingle == on) { return; }

            try
            {
                string mpdCommand = "";

                if (on)
                {
                    mpdCommand = "single 1" + "\n";
                }
                else
                {
                    mpdCommand = "single 0" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MPDPlaybackSetSingle: " + ex.Message);
            }
        }
        
        public void MpdChangePlaylist(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                playlistName = Regex.Escape(playlistName.Trim());

                string mpdCommand = "command_list_begin" + "\n";

                mpdCommand = mpdCommand + "clear" + "\n";
                mpdCommand = mpdCommand + "load \"" + playlistName + "\"\n";
                mpdCommand = mpdCommand + "command_list_end" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
        }

        public void MpdLoadPlaylist(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                playlistName = Regex.Escape(playlistName.Trim());

                string mpdCommand = "load \"" + playlistName + "\"\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
        }

        public void MpdRenamePlaylist(string playlistName, string newPlaylistName)
        {
            if ((playlistName.Trim() != "") && (newPlaylistName.Trim() != ""))
            {
                playlistName = Regex.Escape(playlistName.Trim());
                newPlaylistName = Regex.Escape(newPlaylistName.Trim());

                string mpdCommand = "rename \"" + playlistName + "\" \"" + newPlaylistName + "\"\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
        }

        public void MpdRemovePlaylist(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                playlistName = Regex.Escape(playlistName.Trim());

                string mpdCommand = "rm \"" + playlistName + "\"\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
        }

        public void MpdPlaylistAdd(string playlistName, string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return;

            if (playlistName.Trim() != "")
            {
                playlistName = Regex.Escape(playlistName);
                try
                {
                    uri = Regex.Escape(uri);

                    string mpdCommand = "playlistadd " + "\"" + playlistName + "\"" + " " + "\"" + uri + "\"\n";

                    _asyncClient.Send("noidle" + "\n");
                    _asyncClient.Send(mpdCommand);
                    _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error@MpdPlaylistAdd: " + ex.Message);
                }
            }
        }

        public void MpdPlaylistAdd(string playlistName, List<string> uris)
        {
            if (uris.Count < 1)
                return;

            if (playlistName.Trim() != "")
            {
                playlistName = Regex.Escape(playlistName);
                try
                {
                    string mpdCommand = "command_list_begin" + "\n";
                    foreach (var uri in uris)
                    {
                        var urie = Regex.Escape(uri);
                        mpdCommand = mpdCommand + "playlistadd " + "\"" + playlistName + "\"" + " " + "\"" + urie + "\"\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";

                    _asyncClient.Send("noidle" + "\n");
                    _asyncClient.Send(mpdCommand);
                    _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("Error@MpdPlaylistAdd: " + ex.Message);
                }
            }
        }

        public void MpdSearch(string queryTag, string queryShiki, string queryValue)
        {
            /*
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                SearchResult.Clear();
            });
            */
            Device.BeginInvokeOnMainThread(
                    () =>
                    {
                        SearchResult.Clear();
                    });

            if (string.IsNullOrEmpty(queryTag)) return;
            if (string.IsNullOrEmpty(queryValue)) return;
            if (string.IsNullOrEmpty(queryShiki)) return;

            //find "(Artist == \"foo\\'bar\\\"\")"

            var expression = queryTag + " " + queryShiki + " \'" + Regex.Escape(queryValue) + "\'";

            try
            {
                string mpdCommand = "search \"(" + expression + ")\"\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdFind: " + ex.Message);
            }
        }

        public void MpdQueryAlbumArt(string uri, string songId)
        {
            if (string.IsNullOrEmpty(uri))
                return;


            if (_albumCover.IsDownloading)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.IsDownloading. Ignoring.");
                return;
            }

            if (_albumCover.SongFilePath == uri)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.SongFilePath == uri. Ignoring.");
                return;
            }

            if (songId != MpdStatus.MpdSongID)
            {
                // probably you double clicked on "Next song".
                System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: songId != MpdStatus.MpdSongID. Ignoring.");
                return;
            }

            _albumCover = new AlbumCover();
            _albumCover.IsDownloading = true;
            _albumCover.SongFilePath = uri;

            uri = Regex.Escape(uri);

            try
            {
                string mpdCommand = "albumart \"" + uri + "\" 0" + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: " + ex.Message);
            }
        }

        private async void MpdReQueryAlbumArt(string uri, int offset)
        {
            if (string.IsNullOrEmpty(uri))
                return;

            if (!_albumCover.IsDownloading)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.IsDownloading == false. Ignoring.");
                return;
            }

            if (_albumCover.SongFilePath != uri)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.SongFilePath != uri. Ignoring.");
                _albumCover.IsDownloading = false;

                return;
            }

            // wait for a bit. 
            await Task.Delay(300);

            uri = Regex.Escape(uri);

            try
            {
                string mpdCommand = "albumart \"" + uri + "\" " + offset.ToString() + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdReQueryAlbumArt: " + ex.Message);
            }
        }

        #endregion

        #region == Other Method == 

        private async void TCPClient_ConnectionStatusChanged(TCPC sender, TCPC.ConnectionStatus status)
        {
            await Task.Run(() => { ConnectionStatusChanged?.Invoke(this, status); });
        }

        private async void TCPClient_ConnectionError(TCPC sender, string data)
        {
            await Task.Run(() => { ErrorReturned?.Invoke(this, MpdErrorTypes.ConnectionError, data); });
        }

        private void TCPClient_DataSent(TCPC sender, object data)
        {
            //await Task.Run(() => { DataSent?.Invoke(this, (data as string)); });
        }

        private async void TCPClient_DataReceived(TCPC sender, object data)
        {
            //await Task.Run(() => { DataReceived?.Invoke(this, (data as string)); });

            if ((data as string).StartsWith("OK MPD"))
            {
                string ver = (data as string).Replace("OK MPD ", "");
                MpdVer = ver.Trim();

                MpdSendPassword();

                // MpdConnected.
                await Task.Run(() => { Connected?.Invoke(this); });
            }
            else
            {
                await Task.Run(() => { IsBusy?.Invoke(this, true); });

                DataReceived_Dispatch((data as string));

                await Task.Run(() => { IsBusy?.Invoke(this, false); });
            }
        }

        private void TCPClient_DataBinaryReceived(TCPC sender, byte[] data)
        {
            //await Task.Run(() => { DataReceived?.Invoke(this, "[binary data respose should follow]"); });

            string res = Encoding.Default.GetString(data, 0, data.Length);

            int gabPre = 0;
            int gabAfter = 0;
            bool found = false;

            List<string> values = res.Split(new string[] { "OK\n" }, StringSplitOptions.None).ToList();
            //List<string> values = res.Split("OK\n").ToList();

            try
            {
                if (values.Count > 0)
                {
                    foreach (var val in values)
                    {
                        if (!val.StartsWith("size: "))
                        {
                            if (val.Length > 0)
                            {
                                if (found)
                                {
                                    gabAfter = gabAfter + val.Length;
                                }
                                else
                                {
                                    gabPre = gabPre + val.Length;
                                }

                                TCPClient_DataReceived(sender, val + "OK");
                            }
                        }
                        else if (val.StartsWith("size: "))
                        {
                            found = true;
                        }
                    }

                    //await Task.Delay(100);

                    //await Task.Run(() => { IsBusy?.Invoke(this, true); });

                    BinaryDataReceived_ParseData(data, gabPre, gabAfter);

                    //await Task.Run(() => { IsBusy?.Invoke(this, false); });
                }

            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error@TCPClient_DataBinaryReceived: " + e.ToString());
                return;
            }
        }

        private async void BinaryDataReceived_ParseData(byte[] data, int gabPre, int gabAfter)
        {
            if (MpdStop) return;

            if (data.Length > 1000000) //2000000000
            {
                System.Diagnostics.Debug.WriteLine("**TCPClient_DataBinaryReceived: binary file too big: " + data.Length.ToString());

                _albumCover.IsDownloading = false;
                return;
            }

            if (string.IsNullOrEmpty(_albumCover.SongFilePath))
            {
                System.Diagnostics.Debug.WriteLine("**TCPClient_DataBinaryReceived: File path is not set.");

                _albumCover.IsDownloading = false;
                return;
            }

            if (!_albumCover.IsDownloading)
            {
                System.Diagnostics.Debug.WriteLine("**TCPClient_DataBinaryReceived: IsDownloading = false. Downloading canceld? .");

                _albumCover.IsDownloading = false;
                return;
            }

            try
            {
                string debug = "";
                int gabStart = gabPre;
                int gabEnd = gabAfter;

                int binSize = 0;
                int binResSize = 0;

                string res = Encoding.Default.GetString(data, 0, data.Length);

                List<string> values = res.Split('\n').ToList();

                bool found = false;
                foreach (var val in values)
                {
                    if (val.StartsWith("size: "))
                    {
                        found = true;

                        gabStart = gabStart + val.Length + 1;

                        List<string> s = val.Split(':').ToList();
                        if (s.Count > 1)
                        {
                            if (Int32.TryParse(s[1], out int i))
                            {
                                binSize = i;
                            }
                        }

                        debug = debug + Environment.NewLine + val;
                    }
                    else if (val.StartsWith("type: "))
                    {
                        gabStart = gabStart + val.Length + 1;

                        //
                    }
                    else if (val.StartsWith("binary: "))
                    {
                        gabStart = gabStart + val.Length + 1;

                        List<string> s = val.Split(':').ToList();
                        if (s.Count > 1)
                        {
                            if (Int32.TryParse(s[1], out int i))
                            {
                                binResSize = i;
                            }
                        }

                        debug = debug + Environment.NewLine + val + Environment.NewLine + "[binary data]";
                    }
                    else if (val.StartsWith("OK"))
                    {
                        //gabEnd = gabEnd + val.Length + 1;
                        if (found)
                        {
                            gabEnd = gabEnd + val.Length + 1;
                            //System.Diagnostics.Debug.WriteLine("OK:after " + val);
                        }
                        else
                        {
                            gabStart = gabStart + val.Length + 1;
                            //System.Diagnostics.Debug.WriteLine("OK:before " + val);
                        }

                        debug = debug + Environment.NewLine + val;
                    }
                    else if (val.StartsWith("changed:"))
                    {
                        // Song is changed... so should skip??
                        DataReceived_ParseData(val);

                        if (found)
                        {
                            gabEnd = gabEnd + val.Length + 1;
                            System.Diagnostics.Debug.WriteLine("changed:after " + val);
                        }
                        else
                        {
                            gabStart = gabStart + val.Length + 1;
                            System.Diagnostics.Debug.WriteLine("changed:before " + val);
                        }

                        debug = debug + Environment.NewLine + val;
                        
                    }
                    else
                    {
                        // should be binary...
                    }
                }

                if (binSize > 1000000)
                {
                    System.Diagnostics.Debug.WriteLine("**TCPClient_DataBinaryReceived: binary file too big: " + binSize.ToString());

                    _albumCover.IsDownloading = false;

                    //await Task.Run(() => { DataReceived?.Invoke(this, "[binary file too big. (Size > 1000000) Max 1MB]: " + binSize.ToString()); });

                    return;
                }

                gabEnd = gabEnd + 1; // I'm not really sure why I need the extra +1. 

                //await Task.Run(() => { DataReceived?.Invoke(this, debug); });

                if ((binSize == 0))
                {
                    System.Diagnostics.Debug.WriteLine("binary file size is Zero: " + binSize.ToString() + ", " + binResSize.ToString() + ", " + data.Length.ToString());

                    _albumCover.IsDownloading = false;
                    return;
                }

                if (binResSize != ((data.Length - gabStart) - gabEnd))
                {
                    System.Diagnostics.Debug.WriteLine("binary file size mismatch: " + binSize.ToString() + ", [" + binResSize.ToString() + ", " + (data.Length - gabStart - gabEnd) + "], " + data.Length.ToString());

                    _albumCover.IsDownloading = false;
                    return;
                }

                if ((binSize != 0) && (binSize == binResSize))
                {
                    // 小さいサイズの画像で一発で来た。

                    // 今回受け取ったバイナリ用にバイトアレイをイニシャライズ
                    byte[] resBinary = new byte[data.Length - gabStart - gabEnd];
                    try
                    {
                        // 今回受け取ったバイナリをresBinaryへコピー
                        Array.Copy(data, gabStart, resBinary, 0, resBinary.Length);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error@TCPClient_DataBinaryReceived: " + ex.Message);

                        _albumCover.IsDownloading = false;
                        return;
                    }

                    _albumCover.BinaryData = resBinary;

                    _albumCover.IsDownloading = false;
                    /*
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _albumCover.AlbumImageSource = BitmapImageFromBytes(_albumCover.BinaryData);
                    });
                    */
                    Device.BeginInvokeOnMainThread(
                    () =>
                    {
                        //_albumCover.AlbumImageSource = BitmapImageFromBytes(_albumCover.BinaryData);
                        _albumCover.AlbumImageSource = ImageSource.FromStream(() => new MemoryStream(_albumCover.BinaryData));
                    });

                    _albumCover.IsSuccess = true;

                    await Task.Run(() => { StatusUpdate?.Invoke(this, "isAlbumart"); });

                    return;

                }
                else if (binSize != _albumCover.BinaryData.Length)
                {

                    if (_albumCover.BinarySize == 0)
                    {
                        _albumCover.BinarySize = binSize;
                    }
                    else
                    {
                        if (_albumCover.BinarySize != binSize)
                        {
                            System.Diagnostics.Debug.WriteLine("binary file size mismatch: Maybe different download? This should not be happening.");

                            _albumCover.IsDownloading = false;
                            return;
                        }
                    }

                    try
                    {
                        try
                        {
                            // 今回受け取ったバイナリ用にバイトアレイをイニシャライズ
                            byte[] resBinary = new byte[data.Length - gabStart - gabEnd];
                            // 今回受け取ったバイナリをresBinaryへコピー
                            Array.Copy(data, gabStart, resBinary, 0, resBinary.Length);

                            // 既存のチャンクに追加する為の新たなバイトアレイをイニシャライズ
                            byte[] appended = new byte[_albumCover.BinaryData.Length + resBinary.Length];
                            // 既存のチャンクバイナリをappendedへコピー
                            Array.Copy(_albumCover.BinaryData, 0, appended, 0, _albumCover.BinaryData.Length);
                            // 今回受け取ったバイナリをappendedへコピー
                            Array.Copy(resBinary, 0, appended, _albumCover.BinaryData.Length, resBinary.Length);

                            _albumCover.BinaryData = appended;

                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("Error@TCPClient_DataBinaryReceived (a): " + ex.Message);

                            _albumCover.IsDownloading = false;
                            return;
                        }

                        if (binSize != _albumCover.BinaryData.Length)
                        {
                            //System.Diagnostics.Debug.WriteLine("Trying again for the rest of binary data.");

                            MpdReQueryAlbumArt(_albumCover.SongFilePath, _albumCover.BinaryData.Length);

                        }
                        else
                        {
                            try
                            {
                                // wait little bit.
                                //await Task.Delay(100);
                                /*
                                if (Application.Current == null) { return; }
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    _albumCover.AlbumImageSource = BitmapImageFromBytes(_albumCover.BinaryData);
                                });
                                */
                                Device.BeginInvokeOnMainThread(
                                () =>
                                {
                                    //_albumCover.AlbumImageSource = BitmapImageFromBytes(_albumCover.BinaryData);
                                    _albumCover.AlbumImageSource = ImageSource.FromStream(() => new MemoryStream(_albumCover.BinaryData));
                                });
                                
                                _albumCover.IsSuccess = true;
                                _albumCover.IsDownloading = false;

                                await Task.Run(() => { StatusUpdate?.Invoke(this, "isAlbumart"); });

                                return;
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine("Error@TCPClient_DataBinaryReceived (b): " + ex.Message);

                                _albumCover.IsDownloading = false;
                                return;
                            }
                        }

                        return;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine("Error@TCPClient_DataBinaryReceived (e): " + ex.Message);

                        _albumCover.IsDownloading = false;
                        return;
                    }

                }
                else if ((binResSize == 0) && (binSize == _albumCover.BinaryData.Length))
                {
                    _albumCover.IsDownloading = false;
                    /*
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _albumCover.AlbumImageSource = BitmapImageFromBytes(_albumCover.BinaryData);
                    });
                    */
                    Device.BeginInvokeOnMainThread(
                    () =>
                    {
                        //_albumCover.AlbumImageSource = BitmapImageFromBytes(_albumCover.BinaryData);
                        _albumCover.AlbumImageSource = ImageSource.FromStream(() => new MemoryStream(_albumCover.BinaryData));
                    });
                    _albumCover.IsSuccess = true;

                    await Task.Run(() => { StatusUpdate?.Invoke(this, "isAlbumart"); });

                    return;
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("binary file download : Somehow, things went bad.");

                    _albumCover.IsDownloading = false;
                    return;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@TCPClient_DataBinaryReceived (l): " + ex.Message);

                _albumCover.IsDownloading = false;
                return;
            }


        }

        /*
        public BitmapImage BitmapImageFromBytes(Byte[] bytes)
        {
            try
            {
                // バイト配列をBitmapImageオブジェクトに変換（Imageに表示するSource）
                using (var stream = new MemoryStream(bytes))
                {
                    BitmapImage bmimage = new BitmapImage();

                    bmimage.BeginInit();
                    bmimage.CacheOption = BitmapCacheOption.OnLoad;
                    bmimage.StreamSource = stream;
                    bmimage.EndInit();

                    return bmimage;
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseListAll: " + e.ToString());
                return null;
            }

        }
        */

        private async void DataReceived_Dispatch(string str)
        {
            List<string> reLines = str.Split('\n').ToList();

            try
            {
                string d = "";
                foreach (string value in reLines)
                {
                    if (value == "OK")
                    {
                        if (d != "")
                        {
                            DataReceived_ParseData(d);
                            d = "";
                        }
                    }
                    else if (value.StartsWith("ACK"))
                    {
                        // Ignore "ACK [50@1] {albumart} No file exists"
                        if (value.Contains("{albumart}"))
                        {
                            _albumCover = new AlbumCover();
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("ACK@DataReceived_Dispatch: " + value);

                            await Task.Run(() => { ErrorReturned?.Invoke(this, MpdErrorTypes.CommandError, value); });
                        }
                    }
                    else
                    {
                        d = d + value + "\n";
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error@DataReceived_Dispatch: " + e.ToString());
                return;
            }

        }

        private async void DataReceived_ParseData(string str)
        {
            if (MpdStop) return;
            if (str == "") return;

            try
            {
                if (str.StartsWith("changed:"))
                {
                    //await Task.Run(() => { StatusChanged?.Invoke(this, str); });

                    List<string> SubSystems = str.Split('\n').ToList();

                    try
                    {
                        bool isPlayer = false;
                        bool isCurrentQueue = false;
                        bool isStoredPlaylist = false;

                        foreach (string line in SubSystems)
                        {
                            if (line.ToLower() == "changed: playlist")
                            {
                                // playlist: the queue (i.e.the current playlist) has been modified
                                isCurrentQueue = true;
                            }
                            if (line.ToLower() == "changed: player")
                            {
                                // player: the player has been started, stopped or seeked
                                isPlayer = true;
                            }
                            if (line.ToLower() == "changed: options")
                            {
                                // options: options like repeat, random, crossfade, replay gain
                                isPlayer = true;
                            }
                            if (line.ToLower() == "changed: mixer")
                            {
                                // mixer: the volume has been changed
                                isPlayer = true;
                            }
                            if (line.ToLower() == "changed: stored_playlist")
                            {
                                // stored_playlist: a stored playlist has been modified, renamed, created or deleted
                                isStoredPlaylist = true;
                            }
                            if (line.ToLower() == "changed: update")
                            {
                                // update: a database update has started or finished.If the database was modified during the update, the database event is also emitted.
                                // TODO:
                            }

                            //output: an audio output has been added, removed or modified(e.g.renamed, enabled or disabled)
                            //partition: a partition was added, removed or changed
                            //sticker: the sticker database has been modified.
                            //subscription: a client has subscribed or unsubscribed to a channel
                            //message: a message was received on a channel this client is subscribed to; this event is only emitted when the queue is empty
                            //neighbor: a neighbor was found or lost
                            //mount: the mount list has changed
                        }

                        if (isCurrentQueue)
                        {
                            MpdQueryCurrentQueue();
                        }

                        if (isStoredPlaylist)
                        {
                            MpdQueryPlaylists();
                        }

                        if (isPlayer)
                        {
                            MpdQueryStatus();
                        }

                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("--Error@IdleConnection DataReceived ParseData: " + str);
                    }
                }
                else if (str.StartsWith("volume:") || str.StartsWith("repeat:") || str.StartsWith("random:") || str.StartsWith("state:") || str.StartsWith("song:") || str.StartsWith("songid:") || str.StartsWith("time:") || str.StartsWith("elapsed:") || str.StartsWith("duration:"))
                {
                    // "status"

                    List<string> reLines = str.Split('\n').ToList();

                    ParseStatus(reLines);

                    await Task.Run(() => { StatusUpdate?.Invoke(this, "isPlayer"); });

                }
                else if (str.StartsWith("file:") || str.StartsWith("directory:") || str.StartsWith("Modified:") || str.StartsWith("Artist:") || str.StartsWith("AlbumArtist:") || str.StartsWith("Title:") || str.StartsWith("Album:"))
                {
                    // "playlistinfo" aka Queue,
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

                    // "listplaylist" .. songs containd in a playlist.
                    /*
                     file: Nina Simone/Cellular Soundtrack/Sinnerman (remix).mp3
                     Last-Modified: 2020-09-07T22:56:19Z
                     Artist: Nina Simone 
                     Album: Cellular Soundtrack
                     Title: Sinnerman (remix)
                     Genre: Soundtrack
                     Time: 274
                     duration: 274.364
                    */

                    // "find" search result
                    /*
                     file: 2Pac/Better Dayz/17 Better Dayz (Feat. Mr. Biggs).mp3
                     Last-Modified: 2011-02-27T14:20:18Z
                     Format: 44100:f:2
                     Time: 258
                     duration: 257.677
                     Artist: 2Pac
                     Album: Better Dayz
                     Title: Better Dayz (Feat. Mr. Biggs)
                     Track: 17
                     Genre: Rap
                     Date: 2002
                     */

                    // "listall" aka LocalFiles
                    /*
                     directory: 2Pac
                     directory: 2Pac/Better Dayz
                     file: 2Pac/Better Dayz/17 Better Dayz (Feat. Mr. Biggs).mp3
                     file: 2Pac/Better Dayz/1-02 Thugz Mansion.mp3
                     directory: 2Pac/Greatest Hits
                     file: 2Pac/Greatest Hits/California Love.mp3
                     file: 2Pac/Greatest Hits/Changes.mp3
                     file: 2Pac/Greatest Hits/god bless the dead.mp3
                    */

                    List<string> reLines = str.Split('\n').ToList();

                    var comparer = StringComparer.OrdinalIgnoreCase;
                    Dictionary<string, string> Values = new Dictionary<string, string>(comparer);

                    int i = 0;
                    foreach (string line in reLines)
                    {
                        if (i > 15) break;

                        string[] ValuePair = line.Split(':');
                        if (ValuePair.Length > 1)
                        {
                            if (!Values.ContainsKey(ValuePair[0].Trim()))
                                Values.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }

                        i++;
                    }

                    if ((Values.ContainsKey("Id")) && Values.ContainsKey("Pos") && Values.ContainsKey("Title") && Values.ContainsKey("Artist"))
                    {
                        // Queue = true;
                        if (ParsePlaylistInfo(reLines))
                        {
                            await Task.Run(() => { StatusUpdate?.Invoke(this, "isCurrentQueue"); });
                        }

                    }
                    else if (Values.ContainsKey("Title") && Values.ContainsKey("Artist"))
                    {
                        // listplaylistinfo or find result.... can't differenciate.
                        //System.Diagnostics.Debug.WriteLine("Opps. DataReceived_ParseData : Not implemented. " + str);

                        // TODO:
                        ParseSongResult(reLines);
                        await Task.Run(() => { StatusUpdate?.Invoke(this, "isSongs"); });

                    }
                    else if (Values.ContainsKey("file") || Values.ContainsKey("directory"))
                    {
                        // LocalFies = true;
                        ParseListAll(reLines);

                        await Task.Run(() => { StatusUpdate?.Invoke(this, "isLocalFiles"); });
                    }
                    else
                    {
                        // TODO:
                        System.Diagnostics.Debug.WriteLine("Opps. DataReceived_ParseData : " + str);
                    }
                }
                else if (str.StartsWith("playlist:"))
                {
                    // listplaylists

                    List<string> reLines = str.Split('\n').ToList();

                    ParsePlaylists(reLines);

                    await Task.Run(() => { StatusUpdate?.Invoke(this, "isStoredPlaylist"); });
                }
                else if (str.StartsWith("updating_db:"))
                {
                    // update


                    await Task.Run(() => { StatusUpdate?.Invoke(this, "isUpdating_db"); });
                }
                else if (str.StartsWith("size: ") || str.StartsWith("binary: "))
                {
                    //  albumart
                    // this should not be happening since binary response is handled at TCPClient_DataBinaryReceived

                    //await Task.Run(() => { StatusUpdate?.Invoke(this, "isAlbumart"); });
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Opps. DataReceived_ParseData NON: " + str);
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error@DataReceived_ParseData: " + e.ToString());
                
            }
        }

        private bool ParseStatus(List<string> sl)
        {
            if (MpdStop) { return false; }
            if (sl == null) return false;
            if (sl.Count == 0) return false;

            var comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> MpdStatusValues = new Dictionary<string, string>(comparer);

            try
            {
                foreach (string line in sl)
                {
                    string[] StatusValuePair = line.Split(':');
                    if (StatusValuePair.Length > 1)
                    {
                        if (MpdStatusValues.ContainsKey(StatusValuePair[0].Trim()))
                        {
                            // new

                            MpdStatusValues[StatusValuePair[0].Trim()] = line.Replace(StatusValuePair[0].Trim() + ": ", "");
                        }
                        else
                        {
                            MpdStatusValues.Add(StatusValuePair[0].Trim(), line.Replace(StatusValuePair[0].Trim() + ": ", ""));
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
                                _status.MpdState = Status.MpdPlayState.Play;
                                break;
                            }
                        case "pause":
                            {
                                _status.MpdState = Status.MpdPlayState.Pause;
                                break;
                            }
                        case "stop":
                            {
                                _status.MpdState = Status.MpdPlayState.Stop;
                                break;
                            }
                    }
                }

                // Volume
                if (MpdStatusValues.ContainsKey("volume"))
                {
                    try
                    {
                        _status.MpdVolume = Int32.Parse(MpdStatusValues["volume"]);
                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // songID
                _status.MpdSongID = "";
                if (MpdStatusValues.ContainsKey("songid"))
                {
                    _status.MpdSongID = MpdStatusValues["songid"];
                }

                // Repeat opt bool.
                if (MpdStatusValues.ContainsKey("repeat"))
                {
                    try
                    {
                        if (MpdStatusValues["repeat"] == "1")
                        {
                            _status.MpdRepeat = true;
                        }
                        else
                        {
                            _status.MpdRepeat = false;
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
                            _status.MpdRandom = true;
                        }
                        else
                        {
                            _status.MpdRandom = false;
                        }

                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Consume opt bool.
                if (MpdStatusValues.ContainsKey("consume"))
                {
                    try
                    {
                        if (Int32.Parse(MpdStatusValues["consume"]) > 0)
                        {
                            _status.MpdConsume = true;
                        }
                        else
                        {
                            _status.MpdConsume = false;
                        }

                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Single opt bool.
                if (MpdStatusValues.ContainsKey("single"))
                {
                    try
                    {
                        if (Int32.Parse(MpdStatusValues["single"]) > 0)
                        {
                            _status.MpdSingle = true;
                        }
                        else
                        {
                            _status.MpdSingle = false;
                        }

                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }

                // Song time. deprecated. 
                if (MpdStatusValues.ContainsKey("time"))
                {
                    try
                    {
                        if (MpdStatusValues["time"].Split(':').Length > 1)
                        {
                            _status.MpdSongTime = Double.Parse(MpdStatusValues["time"].Split(':')[1].Trim());
                            _status.MpdSongElapsed = Double.Parse(MpdStatusValues["time"].Split(':')[0].Trim());
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
                        _status.MpdSongElapsed = Double.Parse(MpdStatusValues["elapsed"]);
                    }
                    catch { }
                }

                // Song duration.
                if (MpdStatusValues.ContainsKey("duration"))
                {
                    try
                    {
                        _status.MpdSongTime = Double.Parse(MpdStatusValues["duration"]);
                    }
                    catch { }
                }

                // Error
                if (MpdStatusValues.ContainsKey("error"))
                {
                    ErrorReturned?.Invoke(this, MpdErrorTypes.StatusError, MpdStatusValues["error"]);
                }
                else
                {
                    ErrorReturned?.Invoke(this, MpdErrorTypes.ErrorClear, "");
                }


                // TODO: more?
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseStatusResponse:" + ex.Message);

                IsBusy?.Invoke(this, false);
            }

            return true;
        }

        private bool ParsePlaylistInfo(List<string> sl) 
        {
            if (MpdStop) return false;
            if (sl == null) return false;

            // aka Queue

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

            try
            {
                /*
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentQueue.Clear();
                });
                */
                Device.BeginInvokeOnMainThread( () =>
                    {
                        CurrentQueue.Clear();
                    });

                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new Dictionary<string, string>(comparer);

                int i = 0;

                foreach (string value in sl)
                {
                    string[] StatusValuePair = value.Split(':');
                    if (StatusValuePair.Length > 1)
                    {
                        if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                        {
                            if (SongValues.ContainsKey("Id"))
                            {
                                SongInfo sng = FillSongInfo(SongValues, i);

                                if (sng != null)
                                {
                                    /*
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        CurrentQueue.Add(sng);
                                    });
                                    */
                                    Device.BeginInvokeOnMainThread(() =>
                                    {
                                        CurrentQueue.Add(sng);
                                    });

                                    i++;

                                    SongValues.Clear();
                                }

                            }

                            SongValues.Add(StatusValuePair[0].Trim(), value.Replace(StatusValuePair[0].Trim() + ": ", ""));
                        }
                        else
                        {
                            SongValues.Add(StatusValuePair[0].Trim(), value.Replace(StatusValuePair[0].Trim() + ": ", ""));
                        }
                    }
                }

                if ((SongValues.Count > 0) && SongValues.ContainsKey("Id"))
                {
                    SongInfo sng = FillSongInfo(SongValues, i);

                    if (sng != null)
                    {
                        SongValues.Clear();
                        /*
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentQueue.Add(sng);
                        });
                        */
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            CurrentQueue.Add(sng);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);
                
                return false;
            }
            
            return true;
        }

        private SongInfo FillSongInfo(Dictionary<string, string> SongValues, int i)
        {
            try
            {
                SongInfo sng = new SongInfo();

                if (SongValues.ContainsKey("Id"))
                {
                    sng.Id = SongValues["Id"];
                }

                if (SongValues.ContainsKey("Title"))
                {
                    sng.Title = SongValues["Title"];
                }
                else
                {
                    sng.Title = "";
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
                        // TODO: Should I?
                        sng.Artist = SongValues["AlbumArtist"];
                    }
                    else
                    {
                        sng.Artist = "";
                    }
                }

                if (SongValues.ContainsKey("Last-Modified"))
                {
                    sng.LastModified = SongValues["Last-Modified"];
                }

                if (SongValues.ContainsKey("AlbumArtist"))
                {
                    sng.AlbumArtist = SongValues["AlbumArtist"];
                }

                if (SongValues.ContainsKey("Album"))
                {
                    sng.Album = SongValues["Album"];
                }

                if (SongValues.ContainsKey("Track"))
                {
                    sng.Track = SongValues["Track"];
                }

                if (SongValues.ContainsKey("Disc"))
                {
                    sng.Disc = SongValues["Disc"];
                }

                if (SongValues.ContainsKey("Date"))
                {
                    sng.Date = SongValues["Date"];
                }

                if (SongValues.ContainsKey("Genre"))
                {
                    sng.Genre = SongValues["Genre"];
                }

                if (SongValues.ContainsKey("Composer"))
                {
                    sng.Composer = SongValues["Composer"];
                }

                if (SongValues.ContainsKey("Time"))
                {
                    sng.Time = SongValues["Time"];
                }

                if (SongValues.ContainsKey("duration"))
                {
                    sng.Time = SongValues["duration"];
                    sng.duration = SongValues["duration"];
                }

                if (SongValues.ContainsKey("Pos"))
                {
                    sng.Pos = SongValues["Pos"];
                }

                if (SongValues.ContainsKey("file"))
                {
                    sng.file = SongValues["file"];
                }

                //
                if (sng.Id == _status.MpdSongID)
                {
                    _currentSong = sng;
                }

                // for sorting.
                sng.Index = i;

                return sng;
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error@FillSongInfo: " + e.ToString());
                return null;
            }
        }

        private bool ParsePlaylists(List<string> sl)
        {
            if (MpdStop) { return false; }
            if (sl == null)
            {
                return false;
            }

            try
            {
                /*
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Playlists.Clear();
                });
                */
                Device.BeginInvokeOnMainThread(() =>
                {
                    Playlists.Clear();
                });

                // Tmp list for sorting.
                List<string> slTmp = new List<string>();

                foreach (string value in sl)
                {
                    if (value.StartsWith("playlist:"))
                    {
                        if (value.Split(':').Length > 1)
                        {
                            //slTmp.Add(value.Split(':')[1].Trim());
                            slTmp.Add(value.Replace(value.Split(':')[0] + ": ", ""));
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
                    /*
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Playlists.Add(v);
                    });
                    */
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Playlists.Add(v);
                    });

                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylists: " + e.ToString());
                return false;
            }


            return true;
        }

        private bool ParseListAll(List<string> sl)
        {
            try
            {
                /*
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalFiles.Clear();
                    LocalDirectories.Clear();
                });
                */
                Device.BeginInvokeOnMainThread(() =>
                {
                    LocalFiles.Clear();
                    LocalDirectories.Clear();
                });

                foreach (string value in sl)
                {
                    if (value.StartsWith("directory:"))
                    {
                        /*
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LocalDirectories.Add(value.Replace("directory: ", ""));
                        });
                        */
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            LocalDirectories.Add(value.Replace("directory: ", ""));
                        });
                    }
                    else if (value.StartsWith("file:"))
                    {
                        /*
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LocalFiles.Add(value.Replace("file: ", ""));
                        });
                        */
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            LocalFiles.Add(value.Replace("file: ", ""));
                        });
                    }
                    else if ((value.StartsWith("OK")))
                    {
                        // Ignoring.
                    }
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseListAll: " + e.ToString());
                return false;
            }
            return true;
        }

        private bool ParseSongResult(List<string> sl)
        {
            if (MpdStop) return false;
            if (sl == null) return false;


            // "listplaylist" .. songs containd in a playlist.
            /*
             file: Nina Simone/Cellular Soundtrack/Sinnerman (remix).mp3
             Last-Modified: 2020-09-07T22:56:19Z
             Artist: Nina Simone 
             Album: Cellular Soundtrack
             Title: Sinnerman (remix)
             Genre: Soundtrack
             Time: 274
             duration: 274.364
            */

            // "find" search result
            /*
             file: 2Pac/Better Dayz/17 Better Dayz (Feat. Mr. Biggs).mp3
             Last-Modified: 2011-02-27T14:20:18Z
             Format: 44100:f:2
             Time: 258
             duration: 257.677
             Artist: 2Pac
             Album: Better Dayz
             Title: Better Dayz (Feat. Mr. Biggs)
             Track: 17
             Genre: Rap
             Date: 2002
             */

            try
            {
                /*
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    SearchResult.Clear();
                });
                */
                Device.BeginInvokeOnMainThread(() =>
                {
                    SearchResult.Clear();
                });
                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new Dictionary<string, string>(comparer);

                int i = 0;

                foreach (string line in sl)
                {
                    string[] ValuePair = line.Split(':');
                    if (ValuePair.Length > 1)
                    {
                        if (SongValues.ContainsKey(ValuePair[0].Trim()))
                        {
                            // Contains means new one.

                            // save old one and clear songvalues.
                            if (SongValues.ContainsKey("file"))
                            {
                                Song sng = FillSong(SongValues, i);

                                i++;

                                SongValues.Clear();
                                /*
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    SearchResult.Add(sng);
                                });
                                */
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    SearchResult.Add(sng);
                                });
                            }

                            // start over
                            SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }
                        else
                        {
                            SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }

                    }
                }

                // last one
                if ((SongValues.Count > 0) && SongValues.ContainsKey("file"))
                {
                    Song sng = FillSong(SongValues,i);
                    
                    SongValues.Clear();
                    /*
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SearchResult.Add(sng);
                    });
                    */
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        SearchResult.Add(sng);
                    });
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseSongResult: " + ex.Message);

                return false;
            }

            return true;
        }

        private Song FillSong(Dictionary<string, string> SongValues, int i)
        {

            Song sng = new Song();

            if (SongValues.ContainsKey("Title"))
            {
                sng.Title = SongValues["Title"];
            }
            else
            {
                sng.Title = "";
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
                    // TODO: Should I?
                    sng.Artist = SongValues["AlbumArtist"];
                }
                else
                {
                    sng.Artist = "";
                }
            }

            if (SongValues.ContainsKey("Last-Modified"))
            {
                sng.LastModified = SongValues["Last-Modified"];
            }

            if (SongValues.ContainsKey("AlbumArtist"))
            {
                sng.AlbumArtist = SongValues["AlbumArtist"];
            }

            if (SongValues.ContainsKey("Album"))
            {
                sng.Album = SongValues["Album"];
            }

            if (SongValues.ContainsKey("Track"))
            {
                sng.Track = SongValues["Track"];
            }

            if (SongValues.ContainsKey("Disc"))
            {
                sng.Disc = SongValues["Disc"];
            }

            if (SongValues.ContainsKey("Date"))
            {
                sng.Date = SongValues["Date"];
            }

            if (SongValues.ContainsKey("Genre"))
            {
                sng.Genre = SongValues["Genre"];
            }

            if (SongValues.ContainsKey("Composer"))
            {
                sng.Composer = SongValues["Composer"];
            }

            if (SongValues.ContainsKey("Time"))
            {
                sng.Time = SongValues["Time"];
            }

            if (SongValues.ContainsKey("duration"))
            {
                sng.Time = SongValues["duration"];
                sng.duration = SongValues["duration"];
            }

            if (SongValues.ContainsKey("file"))
            {
                sng.file = SongValues["file"];
            }

            // for sorting.
            sng.Index = i;

            return sng;
        }


        #endregion

    }
}

