/// 
/// 
/// MPDCtrl
/// https://github.com/torum/MPDCtrl
/// 
/// MPD Protocol
/// https://www.musicpd.org/doc/html/protocol.html
/// 
/// 
/// TODO:
///  MPD password test.
///
/// Known issue:
/// 
/// Mopidy related:
///  Mopidy does not accept command_list_begin + password
///   https://github.com/mopidy/mopidy/issues/1661
///    command_list_begin
///    password hoge
///    status
///    command_list_end
///    
///  Mopidy has some issue with M3U and UTF-8, Ext M3Us.
///    https://github.com/mopidy/mopidy/issues/1370
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
using MPDCtrl.ViewModels;

namespace MPDCtrl
{
    /// <summary>
    /// MPD client class. 
    /// </summary>
    public class MPC
    {
        public class Song : ViewModelBase
        {
            public string Id { get; set; }
            public string Pos { get; set; }
            public string file { get; set; }
            public string Title { get; set; }
            public string Track { get; set; }

            private string _timeFormatted;
            private string _time;
            public string Time {
                get
                {
                    return _timeFormatted;
                }
                set
                {
                    if (_time == value)
                        return;

                    _time = value;

                    try
                    {
                        if (!string.IsNullOrEmpty(value))
                        {

                            int sec, min, hour, s;
                            sec = Int32.Parse(value);

                            min = sec / 60;
                            s = sec % 60;
                            hour = min / 60;
                            min = min % 60;

                            if ((hour == 0) && min == 0)
                            {
                                _timeFormatted = String.Format("{0}", s);

                            }
                            else if ((hour == 0) && (min != 0))
                            {
                                _timeFormatted = String.Format("{0}:{1:00}", min, s);
                            }
                            else if ((hour != 0) && (min != 0))
                            {
                                _timeFormatted = String.Format("{0}:{1}:{2:00}", hour, min, s);
                            }
                        }
                    }
                    catch (FormatException e)
                    {
                        System.Diagnostics.Debug.WriteLine(e.Message);
                    }
                }
            }
            public string duration { get; set; }
            public string Artist { get; set; }
            public string Album { get; set; }
            public string AlbumArtist { get; set; }
            public string Composer { get; set; }
            public string Date { get; set; }
            public string Genre { get; set; }
            public string LastModified { get; set; }

            private bool _isPlaying;
            public bool IsPlaying
            {
                get
                {
                    return _isPlaying;
                }
                set
                {
                    if (_isPlaying == value)
                        return;

                    _isPlaying = value;
                    this.NotifyPropertyChanged("IsPlaying");
                }
            }
        }

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
            private bool _consume;
            private string _songID = "";
            private double _songTime = 0;
            private double _songElapsed = 0;

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
            public bool MpdConsume
            {
                get { return _consume; }
                set
                {
                    _consume = value;
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

        public enum MpdErrorTypes
        {
            ProtocolError, //Ark...
            StatusError, //[status] error: Failed to open audio output
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

        private Status _status;
        public Status MpdStatus
        {
            get { return _status; }
        }

        public bool MpdStop { get; set; }

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

        private ObservableCollection<Song> _queue = new ObservableCollection<Song>();
        public ObservableCollection<Song> CurrentQueue
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

        private AsynchronousTCPClient _asyncClient;

        #region == Events == 

        public delegate void MpdStatusChanged(MPC sender, object data);
        public event MpdStatusChanged StatusChanged;

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

        public delegate void MpdConnectionStatusChanged(MPC sender, AsynchronousTCPClient.ConnectionStatus status);
        public event MpdConnectionStatusChanged ConnectionStatusChanged;

        #endregion

        public MPC(string h, int p, string a)
        {
            _host = h;
            _port = p;
            _password = a;

            _status = new Status();

            _asyncClient = new AsynchronousTCPClient();
            _asyncClient.DataReceived += new AsynchronousTCPClient.delDataReceived(TCPClient_DataReceived);
            _asyncClient.DataSent += new AsynchronousTCPClient.delDataSent(TCPClient_DataSent);
            _asyncClient.ConnectionStatusChanged += new AsynchronousTCPClient.delConnectionStatusChanged(TCPClient_ConnectionStatusChanged);
        }

        #region == MPC Method ==   

        public async Task<ConnectionResult> MpdConnect()
        {
            try
            {
                ConnectionResult isDone = await _asyncClient.Connect(IPAddress.Parse(_host), _port, _password);

                if (!isDone.isSuccess)
                {
                    ErrorConnected?.Invoke(this, isDone.errorMessage);
                }

                return isDone;
            }
            catch (Exception ex)
            {
                ErrorConnected?.Invoke(this, ex.Message);

                ConnectionResult err = new ConnectionResult();
                err.isSuccess = false;
                err.errorMessage = ex.Message;

                return err;
            }

        }

        public void MpdDisconnect()
        {
            _asyncClient.DisConnect();
        }

        public void MpdSendPassword()
        {
            try
            {
                if (!string.IsNullOrEmpty(_password))
                {
                    string mpdCommand = "password " + _password.Trim() + "\n";

                    _asyncClient.Send("noidle" + "\n");
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

                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "update" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

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

                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "status" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

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
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";

                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";

                    mpdCommand = mpdCommand + "playlistinfo" + "\n";

                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

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
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";

                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";

                    mpdCommand = mpdCommand + "listplaylists" + "\n";

                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

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
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "listall" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "listall" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdMpdListAll: " + ex.Message);
            }
        }

        public void MpdClear()
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "clear" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "clear" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdAdd: " + ex.Message);
            }
        }

        public void MpdAdd(string uri)
        {
            if (string.IsNullOrEmpty(uri))
                return;

            uri = Regex.Escape(uri);

            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "add \"" + uri + "\"\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "add \"" + uri + "\"\n";
                }

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
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    foreach (var uri in uris)
                    {
                        var urie = Regex.Escape(uri);
                        mpdCommand = mpdCommand + "add \"" + urie + "\"\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "command_list_begin" + "\n";
                    foreach (var uri in uris)
                    {
                        var urie = Regex.Escape(uri);
                        mpdCommand = mpdCommand + "add \"" + urie + "\"\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdAdd: " + ex.Message);
            }
        }

        public void MpdDeleteId(List<string> ids)
        {
            if (ids.Count < 1)
                return;

            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    foreach (var id in ids)
                    {
                        mpdCommand = mpdCommand + "deleteid " + id + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "command_list_begin" + "\n";
                    foreach (var id in ids)
                    {
                        mpdCommand = mpdCommand + "deleteid " + id + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdDeleteId: " + ex.Message);
            }
        }

        public void MpdPlaybackPlay(string songID = "")
        {
            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = mpdCommand + "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    if (songID != "")
                    {
                        mpdCommand = mpdCommand + "playid " + songID + "\n";
                    }
                    else
                    {
                        mpdCommand = mpdCommand + "play" + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    if (songID != "")
                    {
                        mpdCommand = "playid " + songID + "\n";
                    }
                    else
                    {
                        mpdCommand = "play" + "\n";
                    }
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

        public void MpdPlaybackSeek(string songID, int seekTime)
        {
            if ((songID == "") || (seekTime == 0)) { return; }

            try
            {
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "seekid " + songID + " " + seekTime.ToString() + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "seekid " + songID + " " + seekTime.ToString() + "\n";
                }

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
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "pause 1" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "pause 1" + "\n";
                }

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
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "pause 0" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "pause 0" + "\n";
                }

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
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "stop" + "\n";
                    mpdCommand = mpdCommand + "command_list_end";
                }
                else
                {
                    mpdCommand = "stop" + "\n";
                }

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
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "next" + "\n";
                    mpdCommand = mpdCommand + "command_list_end\n";
                }
                else
                {
                    mpdCommand = "next\n";
                }

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
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "previous" + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "previous" + "\n";
                }

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
                string mpdCommand = "";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "setvol " + v.ToString() + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "setvol " + v.ToString() + "\n";
                }

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
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    if (on)
                    {
                        mpdCommand = mpdCommand + "repeat 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = mpdCommand + "repeat 0" + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    if (on)
                    {
                        mpdCommand = "repeat 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = "repeat 0" + "\n";
                    }
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
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    if (on)
                    {
                        mpdCommand = mpdCommand + "random 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = mpdCommand + "random 0" + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    if (on)
                    {
                        mpdCommand = "random 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = "random 0" + "\n";
                    }
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
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = "command_list_begin" + "\n";
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    if (on)
                    {
                        mpdCommand = mpdCommand + "consume 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = mpdCommand + "consume 0" + "\n";
                    }
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    if (on)
                    {
                        mpdCommand = "consume 1" + "\n";
                    }
                    else
                    {
                        mpdCommand = "consume 0" + "\n";
                    }
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

        public void MpdChangePlaylist(string playlistName)
        {
            if (playlistName.Trim() != "")
            {
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                }
                mpdCommand = mpdCommand + "clear" + "\n";

                mpdCommand = mpdCommand + "load " + playlistName.Trim() + "\n";

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
                string mpdCommand = "command_list_begin" + "\n";
                if (!string.IsNullOrEmpty(_password))
                {
                    mpdCommand = mpdCommand + "password " + _password.Trim() + "\n";
                    mpdCommand = mpdCommand + "load " + playlistName.Trim() + "\n";
                    mpdCommand = mpdCommand + "command_list_end" + "\n";
                }
                else
                {
                    mpdCommand = "load " + playlistName.Trim() + "\n";
                }

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist\n");

            }
        }

        private async void TCPClient_ConnectionStatusChanged(AsynchronousTCPClient sender, AsynchronousTCPClient.ConnectionStatus status)
        {
            await Task.Run(() => { ConnectionStatusChanged?.Invoke(this, status); });
        }

        private async void TCPClient_DataSent(AsynchronousTCPClient sender, object data)
        {
            await Task.Run(() => { DataSent?.Invoke(this, (data as string)); });
        }

        private async void TCPClient_DataReceived(AsynchronousTCPClient sender, object data)
        {
            await Task.Run(() => { DataReceived?.Invoke(this, (data as string)); });

            if ((data as string).StartsWith("OK MPD"))
            {
                // Connected.

                //TODO: Needs to be tested.
                MpdSendPassword();

                //
                await Task.Run(() => { Connected?.Invoke(this); });

            }
            else
            {
                DataReceived_Dispatch((data as string));
            }

        }

        private void DataReceived_Dispatch(string str)
        {
            List<string> reLines = str.Split('\n').ToList();

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
                else
                {
                    d = d + value + "\n";
                }
            }
        }

        private async void DataReceived_ParseData(string str)
        {
            if (str.StartsWith("changed:"))
            {
                await Task.Run(() => { StatusChanged?.Invoke(this, str); });

                List<string> SubSystems = str.Split('\n').ToList();

                try
                {
                    bool isPlayer = false;
                    bool isCurrentQueue = false;
                    bool isStoredPlaylist = false;
                    foreach (string line in SubSystems)
                    {
                        if (line == "changed: playlist")
                        {
                            // playlist: the queue (i.e.the current playlist) has been modified
                            isCurrentQueue = true;
                        }
                        if (line == "changed: player")
                        {
                            // player: the player has been started, stopped or seeked
                            isPlayer = true;
                        }
                        if (line == "changed: options")
                        {
                            // options: options like repeat, random, crossfade, replay gain
                            isPlayer = true;
                        }
                        if (line == "changed: mixer")
                        {
                            // mixer: the volume has been changed
                            isPlayer = true;
                        }
                        if (line == "changed: stored_playlist")
                        {
                            // stored_playlist: a stored playlist has been modified, renamed, created or deleted
                            isStoredPlaylist = true;
                        }
                        if (line == "changed: update")
                        {
                            // update: a database update has started or finished.If the database was modified during the update, the database event is also emitted.
                            // TODO:
                        }

                        /*
                        output: an audio output has been added, removed or modified(e.g.renamed, enabled or disabled)
                        partition: a partition was added, removed or changed
                        sticker: the sticker database has been modified.
                        subscription: a client has subscribed or unsubscribed to a channel
                        message: a message was received on a channel this client is subscribed to; this event is only emitted when the queue is empty
                        neighbor: a neighbor was found or lost
                        mount: the mount list has changed
                         */
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
            else if (str.StartsWith("volume:") ||
                   str.StartsWith("repeat:") ||
                   str.StartsWith("random:") ||
                   str.StartsWith("state:") ||
                   str.StartsWith("song:") ||
                   str.StartsWith("songid:") ||
                   str.StartsWith("time:") ||
                   str.StartsWith("elapsed:") ||
                   str.StartsWith("duration:"))
            {
                // "status"

                List<string> reLines = str.Split('\n').ToList();

                ParseStatus(reLines);

                await Task.Run(() => { StatusUpdate?.Invoke(this, "isPlayer"); });

            }
            else if (str.StartsWith("file:") || str.StartsWith("directory:") ||
                str.StartsWith("Modified:") ||
                str.StartsWith("Artist:") ||
                str.StartsWith("AlbumArtist:") ||
                str.StartsWith("Title:") ||
                str.StartsWith("Album:"))
            {
                // "playlistinfo"

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

                // listall

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

                bool isPlaylistinfo = false;

                foreach (string line in reLines)
                {
                    if (line.StartsWith("Title:") || line.StartsWith("Artist:") || line.StartsWith("Id:") )
                    {
                        isPlaylistinfo = true;
                        break;
                    }
                }

                if (isPlaylistinfo)
                {
                    ParsePlaylistInfo(reLines);

                    await Task.Run(() => { StatusUpdate?.Invoke(this, "isCurrentQueue"); });
                }
                else
                {
                    ParseListAll(reLines);

                    await Task.Run(() => { StatusUpdate?.Invoke(this, "isLocalFiles"); });
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
            else
            {
                System.Diagnostics.Debug.WriteLine("IdleConnection DataReceived Dispa ParseData NON: " + str);

            }
        }

        private bool ParseStatus(List<string> sl)
        {
            if (MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("ParseStatusResponse sl==null");
                return false;
            }
            if (sl.Count == 0)
            {
                System.Diagnostics.Debug.WriteLine("ParseStatusResponse slCount == 0");
                return false;
            }

            Dictionary<string, string> MpdStatusValues = new Dictionary<string, string>();

            try
            {
                foreach (string value in sl)
                {
                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParseStatusResponse: " + value);
                        ErrorReturned?.Invoke(this, MpdErrorTypes.ProtocolError, value);
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

                // Song time.
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
            }

            return true;
        }

        private bool ParsePlaylistInfo(List<string> sl) 
        {
            if (MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("ConError@ParsePlaylistInfoResponse: null");
                //ErrorReturned?.Invoke(this, "Connection Error: (@C2)");
                return false;
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

            //_isBusy = true;
            try
            {
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentQueue.Clear();
                });

                Dictionary<string, string> SongValues = new Dictionary<string, string>();

                foreach (string value in sl)
                {
                    if (value.StartsWith("ACK"))
                    {
                        System.Diagnostics.Debug.WriteLine("ACK@ParsePlaylistInfoResponse: " + value);
                        ErrorReturned?.Invoke(this, MpdErrorTypes.ProtocolError, value);
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

                        if (SongValues.ContainsKey("Id"))
                        {
                            Song sng = new Song();
                            sng.Id = SongValues["Id"];

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
                                sng.duration = SongValues["duration"];
                            }
                            if (SongValues.ContainsKey("Pos"))
                            {
                                sng.Pos = SongValues["Pos"];
                            }


                            if (sng.Id == _status.MpdSongID)
                            {
                                _currentSong = sng;
                                //System.Diagnostics.Debug.WriteLine(sng.ID + ":" + sng.Title + " - is current.");
                            }

                            SongValues.Clear();

                            try
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    CurrentQueue.Add(sng);
                                });

                                // This will significantly slows down the load but gives more ui responsiveness.
                                //await Task.Delay(10);
                            }
                            catch (Exception ex)
                            {
                                //_isBusy = false;
                                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse _songs.Add: " + ex.Message);
                                return false;
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        //_isBusy = false;
                        System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);
                        return false;
                    }
                }
                
                //_isBusy = false;
                
                return true;
            }
            catch (Exception ex)
            {
                //_isBusy = false;
                System.Diagnostics.Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);
                return false;
            }
        }

        private bool ParsePlaylists(List<string> sl)
        {
            if (MpdStop) { return false; }
            if (sl == null)
            {
                System.Diagnostics.Debug.WriteLine("Connected response@ParsePlaylistsResponse: null");
                //ErrorReturned?.Invoke(this, "Connection Error: (C3)");
                return false;
            }

            if (Application.Current == null) { return false; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Playlists.Clear();
            });

            // Tmp list for sorting.
            List<string> slTmp = new List<string>();

            foreach (string value in sl)
            {
                //System.Diagnostics.Debug.WriteLine("@ParsePlaylistsResponse() loop: " + value + "");

                if (value.StartsWith("ACK"))
                {
                    System.Diagnostics.Debug.WriteLine("ACK@ParsePlaylistsResponse: " + value);
                    ErrorReturned?.Invoke(this, MpdErrorTypes.ProtocolError, value);
                    return false;
                }

                if (value.StartsWith("playlist:"))
                {
                    if (value.Split(':').Length > 1)
                    {
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
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Playlists.Add(v);
                });
            }

            return true;
        }

        private bool ParseListAll(List<string> sl)
        {

            if (Application.Current == null) { return false; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                LocalFiles.Clear();
            });

            // Tmp list for sorting.
            List<string> slTmp = new List<string>();

            foreach (string value in sl)
            {
                //System.Diagnostics.Debug.WriteLine("@ParseListAllloop: " + value + "");

                if (value.StartsWith("directory:"))
                {
                    // TODO: Ignoring for now.



                }
                else if (value.StartsWith("file:"))
                {
                    if (value.Split(':').Length > 1)
                    {
                        //_playLists.Add(value.Split(':')[1].Trim()); // need sort.
                        slTmp.Add(value.Split(':')[1].Trim());
                    }
                }
                else if ((value.StartsWith("OK")))
                {
                    // Ignoring.
                }
            }

            // Sort.
            slTmp.Sort();
            foreach (string v in slTmp)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalFiles.Add(v);
                });
            }

            return true;
        }

        #endregion END of MPD Method

    }

    public class ConnectionResult
    {
        public bool isSuccess;
        public string errorMessage;
    }

    public class StateObject
    {
        // Client socket.  
        public Socket workSocket = null;
        // Size of receive buffer.  
        public const int BufferSize = 5000;
        // Receive buffer.  
        public byte[] buffer = new byte[BufferSize];
        // Received data string.  
        public StringBuilder sb = new StringBuilder();

    }

    public class AsynchronousTCPClient
    {
        public enum ConnectionStatus
        {
            NeverConnected,
            Connecting,
            Connected,
            MpdOK,
            MpdAck,
            AutoReconnecting,
            DisconnectedByUser,
            DisconnectedByHost,
            ConnectFail_Timeout,
            ReceiveFail_Timeout,
            SendFail_Timeout,
            SendFail_NotConnected,
            Error
        }

        private TcpClient _TCP;
        private IPAddress _ip = IPAddress.None;
        private int _p = 0;
        private string _a = "";
        private ConnectionStatus _ConStat;
        private int _retryAttempt = 0;
        private static ManualResetEvent sendDone = new ManualResetEvent(false);
        public delegate void delDataReceived(AsynchronousTCPClient sender, object data);
        public event delDataReceived DataReceived;
        public delegate void delConnectionStatusChanged(AsynchronousTCPClient sender, ConnectionStatus status);
        public event delConnectionStatusChanged ConnectionStatusChanged;
        public delegate void delDataSent(AsynchronousTCPClient sender, object data);
        public event delDataSent DataSent;

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

                if (raiseEvent)
                    Task.Run(() => { ConnectionStatusChanged?.Invoke(this, _ConStat); });
            }
        }

        static AsynchronousTCPClient()
        {

        }

        public async Task<ConnectionResult> Connect(IPAddress ip, int port, string auth)
        {
            ConnectionState = ConnectionStatus.Connecting;

            _ip = ip;
            _p = port;
            _a = auth;
            _retryAttempt = 0;

            _TCP = new TcpClient();
            // This will crash on iPhone.
            //_TCP.ReceiveTimeout = System.Threading.Timeout.Infinite;
            _TCP.ReceiveTimeout = 0;
            _TCP.SendTimeout = 5000;
            _TCP.Client.ReceiveTimeout = 0;
            //_TCP.Client.ReceiveTimeout = System.Threading.Timeout.Infinite;
            //_TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            return await DoConnect(ip, port);
        }

        public async Task<bool> ReConnect()
        {
            System.Diagnostics.Debug.WriteLine("**ReConnecting...");

            ConnectionState = ConnectionStatus.AutoReconnecting;

            if (_retryAttempt > 1)
            {
                System.Diagnostics.Debug.WriteLine("**SendCommand@ReConnect() _retryAttempt > 1");
                
                ConnectionState = ConnectionStatus.DisconnectedByHost;

                return false;
            }

            _retryAttempt++;

            try
            {
                _TCP.Close();
            }
            catch { }

            await Task.Delay(500);

            _TCP = new TcpClient();
            _TCP.ReceiveTimeout = 0;//System.Threading.Timeout.Infinite;
            _TCP.SendTimeout = 5000;
            _TCP.Client.ReceiveTimeout = 0;//System.Threading.Timeout.Infinite;
            //_TCP.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.KeepAlive, true);

            ConnectionResult r = await DoConnect(_ip, _p);

            return r.isSuccess;
        }

        public async Task<ConnectionResult> DoConnect(IPAddress ip, int port)
        {
            ConnectionResult r = new ConnectionResult();

            try
            {
                await _TCP.ConnectAsync(ip, port);
            }
            catch (SocketException ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@DoConnect: SocketException " + ex.Message);
                ConnectionState = ConnectionStatus.Error;

                r.isSuccess = false;
                r.errorMessage = ex.Message + " (SocketException)";
                return r;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@DoConnect: Exception " + ex.Message);
                ConnectionState = ConnectionStatus.Error;

                r.isSuccess = false;
                r.errorMessage = ex.Message + " (Exception)";
                return r;
            }

            Receive(_TCP.Client);

            ConnectionState = ConnectionStatus.Connected;
            _retryAttempt = 0;

            r.isSuccess = true;
            r.errorMessage = "";
            return r;
        }

        private void Receive(Socket client)
        {
            try
            {
                // Create the state object.  
                StateObject state = new StateObject();
                state.workSocket = client;

                // Begin receiving the data.  
                client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@Receive" + ex.ToString());
                ConnectionState = ConnectionStatus.Error;
            }
        }

        private async void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket from the asynchronous state object.  
            StateObject state = (StateObject)ar.AsyncState;
            Socket client = state.workSocket;
            SocketError err = new SocketError();

            try
            {
                int bytesRead = client.EndReceive(ar, out err);

                if (bytesRead > 0)
                {
                    string res = Encoding.Default.GetString(state.buffer, 0, bytesRead);
                    state.sb.Append(res);

                    if (res.EndsWith("OK\n") || res.StartsWith("OK MPD") || res.StartsWith("ACK"))
                    //if (client.Available == 0)
                    {
                        if (!string.IsNullOrEmpty(state.sb.ToString().Trim()))
                        {
                            DataReceived?.Invoke(this, state.sb.ToString().Trim());
                        }
                        state = new StateObject();
                        state.workSocket = client;
                    }

                    client.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReceiveCallback), state);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("ReceiveCallback bytesRead 0. Disconnected By Host.");

                    //https://msdn.microsoft.com/en-us/library/ms145145(v=vs.110).aspx
                    ConnectionState = ConnectionStatus.DisconnectedByHost;

                    if (!await ReConnect())
                    {
                        System.Diagnostics.Debug.WriteLine("**ReceiveCallback: bytesRead 0 - GIVING UP reconnect.");
                    }
                }

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ReceiveCallback: " + err.ToString() + ". " + ex.ToString() + ". ");
                ConnectionState = ConnectionStatus.Error;
            }
        }

        public async void Send(string cmd)
        {
            if (ConnectionState != ConnectionStatus.Connected) { return; }
            
            try
            {
                DoSend(_TCP.Client, cmd);
            }
            catch (IOException)
            {
                //System.IO.IOException
                //Unable to transfer data on the transport connection: An established connection was aborted by the software in your host machine.

                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: IOException - TRYING TO RECONNECT.");

                // Reconnect.
                if (await ReConnect())
                {
                    DoSend(_TCP.Client, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: IOException - GIVING UP reconnect.");

                }

            }
            catch (SocketException)
            {
                //System.Net.Sockets.SocketException
                //An established connection was aborted by the software in your host machine
                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: SocketException - TRYING TO RECONNECT.");

                // Reconnect.
                if (await ReConnect())
                {
                    DoSend(_TCP.Client, cmd);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: SocketException - GIVING UP reconnect.");

                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("**Error@SendCommand@Read/WriteLineAsync: " + ex.Message);

            }


            sendDone.WaitOne();
        }

        private void DoSend(Socket client, String data)
        {
            DataSent?.Invoke(this, ">>" + data);

            // Convert the string data to byte data using ASCII encoding.  
            byte[] byteData = Encoding.Default.GetBytes(data);
            try
            {
                // Begin sending the data to the remote device.  
                client.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), client);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@DoSend" + ex.ToString());
                ConnectionState = ConnectionStatus.Error;
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.  
                Socket client = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.  
                int bytesSent = client.EndSend(ar);
                //System.Diagnostics.Debug.WriteLine("Sent {0} bytes to server.", bytesSent);

                // Signal that all bytes have been sent.  
                sendDone.Set();

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@SendCallback" + ex.ToString());
                ConnectionState = ConnectionStatus.Error;
            }
        }

        public void DisConnect()
        {
            // Release the socket.  
            try
            {
                _TCP.Client.Shutdown(SocketShutdown.Both);
                _TCP.Client.Close();
            }
            catch { }
        }
    }

}

