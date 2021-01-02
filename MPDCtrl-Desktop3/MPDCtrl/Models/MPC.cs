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
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace MPDCtrl.Models
{
    /// <summary>
    /// MPD client class. 
    /// </summary>
    public class MPC
    {
        #region == Consts, Properties, etc == 

        private string _host;
        public string MpdHost
        {
            get { return _host; }
        }

        private int _port;
        public int MpdPort
        {
            get { return _port; }
        }

        private string _password;
        public string MpdPassword
        {
            get { return _password; }
        }

        private string _mpdVer;
        public string MpdVer
        {
            get { return _mpdVer; }
        }

        private Status _status = new Status();
        public Status MpdStatus
        {
            get { return _status; }
        }

        public bool MpdStop { get; set; }

        // TODO:
        private Song _currentSong;
        public Song MpdCurrentSong
        {
            // The Song object is currectly set only if
            // Playlist is received and song id is matched.
            get
            {
                return _currentSong;
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

        // TODO:
        private AlbumCover _albumCover = new AlbumCover();
        public AlbumCover AlbumArt
        {
            get { return _albumCover; }
        }

        #endregion

        #region == Events == 

        public delegate void IsBusyEvent(MPC sender, bool on);
        public event IsBusyEvent IsBusy;

        public delegate void DebugCommandOutputEvent(MPC sender, string data);
        public event DebugCommandOutputEvent DebugCommandOutput;

        public delegate void DebugIdleOutputEvent(MPC sender, string data);
        public event DebugIdleOutputEvent DebugIdleOutput;

        public delegate void ConnectionStatusChangedEvent(MPC sender, ConnectionStatus status);
        public event ConnectionStatusChangedEvent ConnectionStatusChanged;

        public delegate void ConnectionErrorEvent(MPC sender, string data);
        public event ConnectionErrorEvent ConnectionError;

        public delegate void IsMpdIdleConnectedEvent(MPC sender);
        public event IsMpdIdleConnectedEvent MpdIdleConnected;

        public delegate void MpdAckErrorEvent(MPC sender, string data);// << TODO:
        public event MpdAckErrorEvent MpdAckError;

        public delegate void MpdStatusErrorEvent(MPC sender, string data);
        public event MpdStatusErrorEvent MpdStatusError;


        public delegate void MpdPlayerStatusChangedEvent (MPC sender);
        public event MpdPlayerStatusChangedEvent MpdPlayerStatusChanged;

        public delegate void MpdCurrentQueueChangedEvent(MPC sender);
        public event MpdCurrentQueueChangedEvent MpdCurrentQueueChanged;

        public delegate void MpdPlaylistsChangedEvent(MPC sender);
        public event MpdPlaylistsChangedEvent MpdPlaylistsChanged;


        /*
        public delegate void MpdStatusChanged(MPC sender, object data);
        public event MpdStatusChanged StatusChanged;

        public delegate void MpdError(MPC sender, MpdErrorTypes err, object data);
        public event MpdError ErrorReturned;


        public delegate void MpdStatusUpdate(MPC sender, object data);
        public event MpdStatusUpdate StatusUpdate;


        public delegate void MpdAlbumArtStatusChanged(MPC sender);
        public event MpdAlbumArtStatusChanged OnAlbumArtStatusChanged;
        */
        #endregion

        #region == Connections ==

        private static TcpClient _commandConnection = new TcpClient();
        private StreamReader _commandReader;
        private StreamWriter _commandWriter;

        private static TcpClient _idleConnection = new TcpClient();
        private static StreamReader _idleReader;
        private static StreamWriter _idleWriter;

        public enum ConnectionStatus
        {
            NeverConnected,
            Connecting,
            Connected,
            DisconnectedByUser,
            DisconnectedByHost,
            ConnectFail_Timeout,
            ReceiveFail_Timeout,
            SendFail_Timeout,
            SendFail_NotConnected,
            Disconnecting,
            Disconnected,
            SeeConnectionErrorEvent
        }

        private ConnectionStatus _connectionState;
        public ConnectionStatus ConnectionState
        {
            get
            {
                return _connectionState;
            }
            private set
            {
                if (value == _connectionState)
                    return;

                _connectionState = value;

                ConnectionStatusChanged?.Invoke(this, _connectionState);
            }
        }

        // TODO:ちゃんと使っていないので利用する。
        public bool IsMpdCommandConnected { get; set; }
        public bool IsMpdIdleConnected { get; set; }

        #endregion

        public MPC()
        {
            ConnectionState = ConnectionStatus.NeverConnected;

        }

        #region == Idle Connection ==

        public async Task<bool> MpdIdleConnect(string host, int port)
        {
            IsMpdIdleConnected = false;

            _idleConnection = new TcpClient();

            _host = host;
            _port = port;


            DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Trying to connect." + "\n" + "\n");

            ConnectionState = ConnectionStatus.Connecting;

            try
            {
                await _idleConnection.ConnectAsync(_host, _port);

                if (_idleConnection.Client.Connected)
                {
                    DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Connection established." + "\n" + "\n");

                    ConnectionState = ConnectionStatus.Connected;

                    var tcpStream = _idleConnection.GetStream();
                    tcpStream.ReadTimeout = System.Threading.Timeout.Infinite;

                    _idleReader = new StreamReader(tcpStream);
                    _idleWriter = new StreamWriter(tcpStream);
                    _idleWriter.AutoFlush = true;

                    string response = await _idleReader.ReadLineAsync();

                    if (response.StartsWith("OK MPD "))
                    {
                        _mpdVer = response.Replace("OK MPD ", string.Empty).Trim();

                        DebugIdleOutput?.Invoke(this, "<<<<" + response.Trim() + "\n" + "\n");

                        IsMpdIdleConnected = true;

                        MpdIdleConnected?.Invoke(this);

                        // Done for now.
                    }
                    else
                    {
                        DebugIdleOutput?.Invoke(this, "TCP Idle Connection: MPD did not respond with proper respose." + "\n" + "\n");

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "TCP connection error: MPD did not respond with proper respose.");
                    }
                }
                else
                {
                    //?

                    Debug.WriteLine("**** !client.Client.Connected");

                    DebugIdleOutput?.Invoke(this, "TCP Idle Connection: FAIL to established... Client not connected." + "\n" + "\n");

                    ConnectionState = ConnectionStatus.NeverConnected;

                    ConnectionError?.Invoke(this, "TCP Idle Connection: FAIL to established... Client not connected.");
                }
            }
            catch (Exception e)
            {
                // TODO: Test.

                DebugCommandOutput?.Invoke(this, "TCP Idle Connection: Error while connecting. Fail to connect: " + e.Message + "\n" + "\n");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }

            return IsMpdIdleConnected;
        }

        private async Task<CommandResult> MpdIdleSendCommand(string cmd)
        {
            CommandResult ret = new CommandResult();

            if (_idleConnection.Client == null)
            {
                Debug.WriteLine("@MpdIdleSendCommand: " + "TcpClient.Client == null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "TcpClient.Client == null";

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdIdleSendCommand", "TcpClient.Client == null", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            if ((_idleWriter == null) || (_idleReader == null))
            {
                Debug.WriteLine("@MpdIdleSendCommand: " + "_idleWriter or _idleReader is null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "_idleWriter or _idleReader is null";

                DebugIdleOutput?.Invoke(this, string.Format("################ Error :@{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdIdleSendCommand", "_idleWriter or _idleReader is null", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            if (!_idleConnection.Client.Connected)
            {
                Debug.WriteLine("Exception@MpdIdleSendCommand: " + "NOT IsMpdIdleConnected");

                ret.IsSuccess = false;
                ret.ErrorMessage = "NOT IsMpdIdleConnected";

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdIdleSendCommand", "!CommandConnection.Client.Connected", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            string cmdDummy = cmd;
            if (cmd.StartsWith("password "))
                cmdDummy = "password ****";

            DebugIdleOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");

            try
            {
                if (cmd.Trim().StartsWith("idle"))
                {
                    await _idleWriter.WriteAsync(cmd.Trim() + "\n");

                    return ret;
                }
                else
                {
                    //await _idleWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n");
                    await _idleWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (System.IO.IOException e)
            {
                Debug.WriteLine("IOException@MpdIdleSendCommand: " + cmd.Trim() + " WriteAsync :" + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                // Could be application shutdopwn.
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugIdleOutput?.Invoke(this, string.Format("################ Error@{0}, Reason:{1}, Data:{2}, {3} Exception: {4} {5}", "WriteAsync@MpdIdleSendCommand", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "The connection (command) has been terminated (IOException): " + e.Message);
                }

                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "WriteAsync@MpdIdleSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "The connection (command) has been terminated (Exception): " + e.Message);
                }

                return ret;
            }

            try
            {
                StringBuilder stringBuilder = new StringBuilder();

                while (true)
                {
                    string line = await _idleReader.ReadLineAsync();

                    if (line != null)
                    {
                        if (line.StartsWith("ACK"))
                        {
                            Debug.WriteLine("ACK line @MpdIdleSendCommand: " + cmd.Trim() + " and " + line);

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            break;
                            //isTalking = false;
                        }
                        else if (line.StartsWith("OK"))
                        {
                            ret.IsSuccess = true;

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            break;
                            //isTalking = false;
                        }
                        else if (line.StartsWith("changed: "))
                        {
                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                stringBuilder.Append(line + "\n"); // << has to be \n
                            }
                            else
                            {
                                Debug.WriteLine("line == IsNullOrEmpty");

                                break;
                                //isTalking = false;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("@MpdIdleSendCommand ReadLineAsync line != null");

                        DebugIdleOutput?.Invoke(this, string.Format("################ Error @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdleSendCommand", "ReadLineAsync received null data", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "The connection (command) has been terminated. ");

                        ret.ResultText = stringBuilder.ToString();
                        ret.ErrorMessage = "ReadLineAsync@MpdIdleSendCommand received null data";

                        break;
                        //isTalking = false;
                    }
                }

                DebugIdleOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");

                ret.ResultText = stringBuilder.ToString();
                ret.ErrorMessage = "";

                return ret;
            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdIdleSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdleSendCommand", "InvalidOperationException (Most likely the connection is overloaded)", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                //ConnectionError?.Invoke(this, "The connection (command) has been terminated. Most likely the connection has been overloaded.");


                //await _idleWriter.WriteAsync("noidle\n");
                //await _idleWriter.WriteAsync("idle player\n");
                await Task.Delay(1000);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdIdleSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdleSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                return ret;
            }

            return ret;
        }

        public async Task<CommandResult> MpdIdleSendPassword(string password = "")
        {
            _password = password;

            CommandResult ret = new CommandResult();

            if (string.IsNullOrEmpty(password))
            {
                ret.IsSuccess = true;
                ret.ResultText = "OK";
                ret.ErrorMessage = "";

                return ret;
            }

            string cmd = "password " + password + "\n";

            return await MpdIdleSendCommand(cmd);

        }

        public async Task<CommandResult> MpdIdleQueryStatus()
        {
            CommandResult result = await MpdIdleSendCommand("status");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParseStatus(result.ResultText);
                if (result.IsSuccess)
                {
                    //Debug.WriteLine("@MpdQueryStatus: IsSuccess.");
                }
                else
                {
                    //Debug.WriteLine("@MpdQueryStatus: NOT IsSuccess.");
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdIdleQueryCurrentQueue()
        {
            CommandResult result = await MpdIdleSendCommand("playlistinfo");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParsePlaylistInfo(result.ResultText);
                if (result.IsSuccess)
                {
                    //Debug.WriteLine("@MpdQueryStatus: IsSuccess.");
                }
                else
                {
                    //Debug.WriteLine("@MpdQueryStatus: NOT IsSuccess.");
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdIdleQueryPlaylists()
        {
            CommandResult result = await MpdIdleSendCommand("listplaylists");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParsePlaylists(result.ResultText);
            }

            return result;
        }

        public async Task<CommandResult> MpdIdleQueryListAll()
        {
            CommandResult result = await MpdIdleSendCommand("listall");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParseListAll(result.ResultText);
            }

            return result;
        }

        public void MpdIdleStart()
        {
            MpdIdle();
        }

        private async void MpdIdle()
        {
            if (MpdStop)
                return;

            if (_idleConnection.Client == null)
            {
                Debug.WriteLine("@MpdIdle: " + "TcpClient.Client == null");

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdIdle", "TcpClient.Client == null", "", Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return;
            }

            if ((_commandWriter == null) || (_commandReader == null))
            {
                Debug.WriteLine("@MpdIdle: " + "_idleWriter or _idleReader is null");

                DebugIdleOutput?.Invoke(this, string.Format("################ Error :@{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdIdle", "_idleWriter or _idleReader is null", "", Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return;
            }

            if (!_idleConnection.Client.Connected)
            {
                Debug.WriteLine("@MpdIdle: " + "!_idleConnection.Client.Connected");

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdIdle", "!_idleConnection.Client.Connected", "", Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return;
            }

            string cmd = "idle player mixer options playlist stored_playlist\n";

            DebugIdleOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");

            try
            {
                await _idleWriter.WriteAsync(cmd);
            }
            catch (IOException e)
            {
                // connection terminated by ... something.

                Debug.WriteLine("[IOException@MpdIdle] ({0} ):\n{1}", "WriteAsync", e.Message);

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "WriteAsync@MpdIdle", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "The connection (idle) has been terminated. IOException: " + e.Message);

                return;
            }
            catch (Exception e)
            {
                Debug.WriteLine("[Exception@MpdIdle] ({0} ):\n{1}", "WriteAsync", e.Message);

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "WriteAsync@MpdIdle", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "The connection (idle) has been terminated. Exception: " + e.Message);

                return;
            }

            try
            {
                StringBuilder stringBuilder = new StringBuilder();

                while (true)
                {
                    if (MpdStop)
                        break;

                    string line = await _idleReader.ReadLineAsync();

                    if (line != null)
                    {
                        if (line.StartsWith("ACK"))
                        {
                            Debug.WriteLine("ACK: " + line);

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            break;
                        }
                        else if (line.StartsWith("OK"))
                        {
                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            break;
                        }
                        else if (line.StartsWith("changed: "))
                        {
                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                stringBuilder.Append(line + "\n");
                            }
                        }
                    }
                    else
                    {
                        if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser) || (ConnectionState == ConnectionStatus.Connecting))
                        {
                            // nothing wrong.
                        }
                        else
                        {
                            Debug.WriteLine("TCP Idle Connection: ReadLineAsync @MpdIdle received null data.");

                            DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdle", "ReadLineAsync@MpdIdle received null data", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));
                            
                            ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                            ConnectionError?.Invoke(this, "The connection has been terminated.");
                            
                            break;
                        }

                        return;
                    }
                }

                string result = stringBuilder.ToString();

                DebugIdleOutput?.Invoke(this, "<<<<" + result.Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");

                // Parse & Raise event and MpdIdle();
                await ParseSubSystemsAndRaseChangedEvent(result);

            }
            catch (System.IO.IOException e)
            {
                // Could be application shutdopwn.

                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser) || (ConnectionState == ConnectionStatus.Connecting))
                {
                    // no problem
                }
                else
                {
                    Debug.WriteLine("[IOException@MpdIdle] ({0}):\n{1}", "ReadLineAsync: " + ConnectionState.ToString(), e.Message);

                    DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdle", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "The connection (idle) has been terminated. Exception: " + e.Message);
                }
            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdle", "InvalidOperationException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "The connection (idle) has been terminated. Most likely the connection is overloaded.");

            }
            catch (Exception e)
            {
                // Could be application shutdopwn.

                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {
                    // no problem...probably.
                }
                else
                {
                    Debug.WriteLine("[Exception@MpdIdle] ({0} ):\n{1}", "ReadLineAsync", e.Message);

                    DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdle", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "The connection (idle) has been terminated. Exception: " + e.Message);
                }
            }
        }

        private async Task<bool> ParseSubSystemsAndRaseChangedEvent(string result)
        {
            /*
            public enum MpdSubSystems
            {
                //player mixer options playlist stored_playlist
                //database: the song database has been modified after update.
                //update: a database update has started or finished. If the database was modified during the update, the database event is also emitted.
                //stored_playlist: a stored playlist has been modified, renamed, created or deleted
                //playlist: the queue (i.e.the current playlist) has been modified
                //player: the player has been started, stopped or seeked
                //mixer: the volume has been changed
                //output: an audio output has been added, removed or modified(e.g.renamed, enabled or disabled)
                //options: options like repeat, random, crossfade, replay gain
                //partition: a partition was added, removed or changed
                //sticker: the sticker database has been modified.
                //subscription: a client has subscribed or unsubscribed to a channel
                //message: a message was received on a channel this client is subscribed to; this event is only emitted when the queue is empty
                //neighbor: a neighbor was found or lost
                //mount: the mount list has changed

                player,
                mixer,
                options,
                playlist,
                stored_playlist
            }
            */

            List<string> SubSystems = result.Split('\n').ToList();

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
                }

                if (isPlayer)
                {
                    CommandResult idleResult = await MpdIdleQueryStatus();
                    if (idleResult.IsSuccess)
                    {
                        MpdPlayerStatusChanged?.Invoke(this);
                    }
                }

                if (isCurrentQueue)
                {
                    CommandResult idleResult = await MpdIdleQueryCurrentQueue();
                    if (idleResult.IsSuccess)
                    {
                        MpdCurrentQueueChanged?.Invoke(this);
                    }
                }

                if (isStoredPlaylist)
                {
                    CommandResult idleResult = await MpdIdleQueryPlaylists();
                    if (idleResult.IsSuccess)
                    {
                        MpdPlaylistsChanged?.Invoke(this);
                    }
                }

                // start over.
                MpdIdle();
            }
            catch
            {
                Debug.WriteLine("**Error@ParseSubSystemsAndRaseChangedEvent: " + result);
            }

            return true;
        }

        #endregion

        #region == Command Connection ==

        public async void MpdCommandConnectionStart(string host, int port, string password)
        {
            ConnectionResult r = await MpdCommandConnect(host, port);

            if (r.IsSuccess)
            {
                CommandResult d = await MpdCommandSendPassword(password);

                if (d.IsSuccess)
                {
                    // ここでIdleにして、以降はnoidle + cmd + idleの組み合わせでやる。
                    // ただし、実際にはidleのあとReadしていないからタイムアウトで切断されてしまう模様。

                    // await しなくても良いのだけれど。
                    await MpdSendIdle();
                }
            }
        }

        public async Task<ConnectionResult> MpdCommandConnect(string host, int port)
        {
            ConnectionResult result = new ConnectionResult();

            IsMpdCommandConnected = false;

            _commandConnection = new TcpClient();

            _host = host;
            _port = port;


            DebugCommandOutput?.Invoke(this, "TCP Command Connection: Trying to connect." + "\n" + "\n");

            ConnectionState = ConnectionStatus.Connecting;

            try
            {
                await _commandConnection.ConnectAsync(_host, _port);

                if (_commandConnection.Client.Connected)
                {
                    DebugCommandOutput?.Invoke(this, "TCP Command Connection: Connection established." + "\n" + "\n");

                    ConnectionState = ConnectionStatus.Connected;

                    var tcpStream = _commandConnection.GetStream();
                    tcpStream.ReadTimeout = System.Threading.Timeout.Infinite;

                    _commandReader = new StreamReader(tcpStream);
                    _commandWriter = new StreamWriter(tcpStream);
                    _commandWriter.AutoFlush = true;

                    string response = await _commandReader.ReadLineAsync();

                    if (response.StartsWith("OK MPD "))
                    {
                        _mpdVer = response.Replace("OK MPD ", string.Empty).Trim();

                        DebugCommandOutput?.Invoke(this, "<<<<" + response.Trim() + "\n" + "\n");

                        IsMpdCommandConnected = true;

                        result.IsSuccess = true;

                        //MpdConnected?.Invoke(this);

                        // Done for now.
                    }
                    else
                    {
                        DebugCommandOutput?.Invoke(this, "TCP Command Connection: MPD did not respond with proper respose." + "\n" + "\n");

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "TCP connection error: MPD did not respond with proper respose.");
                    }
                }
                else
                {
                    //?

                    Debug.WriteLine("**** !client.Client.Connected");

                    DebugCommandOutput?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected." + "\n" + "\n");

                    ConnectionState = ConnectionStatus.NeverConnected;

                    ConnectionError?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected.");
                }
            }
            catch (Exception e)
            {
                // TODO: Test.

                DebugCommandOutput?.Invoke(this, "TCP Command Connection: Error while connecting. Fail to connect: " + e.Message + "\n" + "\n");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }

            return result;
        }

        public async Task<CommandResult> MpdCommandSendPassword(string password = "")
        {
            _password = password;

            CommandResult ret = new CommandResult();

            if (string.IsNullOrEmpty(password))
            {
                ret.IsSuccess = true;
                ret.ResultText = "OK";//Or OK
                ret.ErrorMessage = "";

                return ret;
            }

            string cmd = "password " + password + "\n";

            return await MpdSendCommand(cmd);

        }

        private async Task<CommandResult> MpdSendCommand(string cmd, bool isAutoIdling = false)
        {
            CommandResult ret = new CommandResult();

            if (_commandConnection.Client == null)
            {
                Debug.WriteLine("@MpdSendCommand: " + "TcpClient.Client == null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "TcpClient.Client == null";

                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdSendCommand", "TcpClient.Client == null", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            if ((_commandWriter == null) || (_commandReader == null))
            {
                Debug.WriteLine("@MpdSendCommand: " + "_commandWriter or _commandReader is null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "_commandWriter or _commandReader is null";

                DebugCommandOutput?.Invoke(this, string.Format("################ Error :@{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdSendCommand", "_commandWriter or _commandReader is null", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            if (!_commandConnection.Client.Connected)
            {
                Debug.WriteLine("@MpdSendCommand: " + "NOT IsMpdCommandConnected");

                ret.IsSuccess = false;
                ret.ErrorMessage = "NOT IsMpdCommandConnected";

                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdSendCommand", "!CommandConnection.Client.Connected", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            try
            {
                if (cmd.Trim().StartsWith("idle"))
                {
                    DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");

                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");

                    if (!isAutoIdling)
                        return ret;
                }
                else
                {
                    string cmdDummy = cmd;
                    if (cmd.StartsWith("password "))
                        cmdDummy = "password ****";

                    cmdDummy = cmdDummy.Trim().Replace("\n", "\n" + ">>>>");

                    if (isAutoIdling)
                        DebugCommandOutput?.Invoke(this, ">>>>" + "noidle\n>>>>" + cmdDummy.Trim() + "\n>>>>idle player" + "\n" + "\n");
                    else
                        DebugCommandOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");

                    if (isAutoIdling)
                        await _commandWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n" + "idle player\n");
                    else
                        await _commandWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (System.IO.IOException e)
            {
                Debug.WriteLine("IOException@MpdSendCommand: " + cmd.Trim() + " WriteAsync :" + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                // Could be application shutdopwn.
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error@{0}, Reason:{1}, Data:{2}, {3} Exception: {4} {5}", "WriteAsync@MpdSendCommand", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));



                    // タイムアウトしていたらここでエラーになる模様。

                    // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。













                    //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    //ConnectionError?.Invoke(this, "The connection (command) has been terminated (IOException): " + e.Message);
                }

                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "WriteAsync@MpdSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    //ConnectionError?.Invoke(this, "The connection (command) has been terminated (Exception): " + e.Message);
                }

                return ret;
            }


            try
            {
                StringBuilder stringBuilder = new StringBuilder();

                bool isDoubleOk = false;

                while (true)
                {
                    string line = await _commandReader.ReadLineAsync();

                    if (line != null)
                    {
                        if (line.StartsWith("ACK"))
                        {
                            Debug.WriteLine("ACK line @MpdSendCommand: " + cmd.Trim() + " and " + line);

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            break;
                        }
                        else if (line.StartsWith("OK"))
                        {
                            if (isAutoIdling)
                            {
                                if (isDoubleOk)
                                {
                                    if (!string.IsNullOrEmpty(line))
                                        stringBuilder.Append(line + "\n");

                                    ret.IsSuccess = true;

                                    break;
                                }

                                if (!string.IsNullOrEmpty(line))
                                    stringBuilder.Append(line + "\n");

                                isDoubleOk = true;

                            }
                            else
                            {
                                ret.IsSuccess = true;

                                if (!string.IsNullOrEmpty(line))
                                    stringBuilder.Append(line + "\n");

                                break;
                            }
                        }
                        else if (line.StartsWith("changed: "))
                        {
                            // noidleでついてくるかもしれないchanged. idleConnectionで見ているからここでは無視したいが・・・。

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                stringBuilder.Append(line + "\n"); // << has to be \n
                            }
                            else
                            {
                                Debug.WriteLine("line == IsNullOrEmpty");

                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("@MpdSendCommand ReadLineAsync line != null");

                        DebugCommandOutput?.Invoke(this, string.Format("################ Error @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "ReadLineAsync received null data", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                        //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        //ConnectionError?.Invoke(this, "The connection (command) has been terminated. ");

                        ret.ResultText = stringBuilder.ToString();
                        ret.ErrorMessage = "ReadLineAsync@MpdSendCommand received null data";

                        break;
                    }
                }

                DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");

                ret.ResultText = stringBuilder.ToString();
                ret.ErrorMessage = "";

                return ret;
            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "InvalidOperationException (Most likely the connection is overloaded)", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                //ConnectionError?.Invoke(this, "The connection (command) has been terminated. Most likely the connection has been overloaded.");

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                return ret;
            }

        }

        #endregion

        #region == MPD Commands == 

        // TODO: 
        private async Task<bool> CheckCommandQueue()
        {
            /*
            int c = 100;

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (commandQueueCount <= 0)
                    commandQueueCumulativeCount = 0;

                commandQueueCumulativeCount++;
                commandQueueCount++;

            });

            await Task.Delay(c);

            Application.Current.Dispatcher.Invoke(() =>
            {
                if (commandQueueCount > 1)
                    c = (commandQueueCumulativeCount * 100);

                if (commandQueueCount > 1)
                    MpcInfo?.Invoke(this, string.Format("Too many command requests({0}) for a short period of time. Throttling.... ({1})", commandQueueCumulativeCount.ToString(), commandQueueCount.ToString()));

            });

            await Task.Delay(c);

            //Debug.WriteLine("Waiting@checkCommandQueue: " + commandQueueCount.ToString() + " : " + c.ToString());

            Application.Current.Dispatcher.Invoke(() =>
            {
                commandQueueCount--;

                if (commandQueueCount <= 0)
                    MpcInfo?.Invoke(this, "");

                if (commandQueueCount > 0)
                    MpcInfo?.Invoke(this, string.Format("Too many command requests({0}) for a short period of time. Throttling.... ({1})", commandQueueCumulativeCount.ToString(), commandQueueCount.ToString()));

            });
            */
            return true;
        }

        // TODO:
        private async Task<CommandResult> MpdSendCommandSet(string cmd)
        {
            CommandResult ret = new CommandResult();

            if (_commandConnection.Client == null)
            {
                Debug.WriteLine("Exception@MpdSendCommandSet: " + "NOT _tcpCommandClient.Client = null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "NOT _tcpCommandClient.Client = null";
                DebugCommandOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                return ret;
            }

            if (!_commandConnection.Client.Connected)
            {
                Debug.WriteLine("Exception@MpdSendCommandSet: " + "NOT IsMpdCommandConnected");

                ret.IsSuccess = false;
                ret.ErrorMessage = "NOT IsMpdCommandConnected";
                DebugCommandOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                return ret;
            }

            if ((_commandWriter == null) || (_commandReader == null))
            {
                Debug.WriteLine("Exception@MpdSendCommandSet: " + "_commandWriter or _commandReader is null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "_commandWriter or _commandReader is null";
                DebugCommandOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                return ret;
            }

            try
            {
                if (cmd.Trim().StartsWith("idle"))
                {
                    DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");

                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");

                    return ret;
                }
                else if (cmd.Trim() == "noidle")
                {
                    DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");

                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");

                }
                else
                {
                    string cmdlist = "command_list_begin" + "\n";
                    cmdlist = cmdlist + cmd.Trim() + "\n";
                    cmdlist = cmdlist + "command_list_end";

                    DebugCommandOutput?.Invoke(this, ">>>>" + cmdlist.Trim().Replace("\n", "\n" + ">>>>") + "\n" + "\n");

                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommandSet: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                DebugCommandOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));

                return ret;
            }

            try
            {
                StringBuilder stringBuilder = new StringBuilder();

                while (true)
                {
                    string line = await _commandReader.ReadLineAsync();

                    if (line != null)
                    {
                        if (line.StartsWith("ACK"))
                        {
                            Debug.WriteLine("ACK line @MpdSendCommandSet: " + cmd.Trim() + " and " + line);

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            break;
                        }
                        else if (line.StartsWith("OK"))
                        {
                            ret.IsSuccess = true;

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            break;
                        }
                        else if (line.StartsWith("changed: "))
                        {
                            // noidleでついてくるかもしれないchanged. idleConnectionで見ているからここでは無視。

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(line))
                            {
                                stringBuilder.Append(line + "\n"); // << has to be \n
                            }
                            else
                            {
                                Debug.WriteLine("line == IsNullOrEmpty");
                                break;
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("line != null");
                        break;
                    }
                }

                ret.ErrorMessage = "";

                DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommandSet: " + cmd.Trim() + " ReadLineAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                DebugCommandOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));

                return ret;
            }

            return ret;
        }

        public async Task<CommandResult> MpdSendIdle()
        {
            return await MpdSendCommand("idle player");
        }

        public async Task<CommandResult> MpdSendNoIdle()
        {
            return await MpdSendCommand("noidle");
        }

        public async Task<CommandResult> MpdQueryStatus(bool idling = true)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (idling)
                await MpdSendNoIdle();
            CommandResult result = await MpdSendCommand("status");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParseStatus(result.ResultText);
                if (result.IsSuccess)
                {
                    //Debug.WriteLine("@MpdQueryStatus: IsSuccess.");
                }
                else
                {
                    //Debug.WriteLine("@MpdQueryStatus: NOT IsSuccess.");
                }
            }
            if (idling)
                await MpdSendIdle();
            return result;
        }

        public async Task<CommandResult> MpdQueryCurrentSong(bool idling = true)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            // TODO:
            // Currently not used. So do nothing.

            if (idling)
                await MpdSendNoIdle();
            CommandResult result = await MpdSendCommand("currentsong");
            if (result.IsSuccess)
            {
                //
            }
            if (idling)
                await MpdSendIdle();
            return result;
        }

        public async Task<CommandResult> MpdQueryCurrentQueue(bool idling = true)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (idling)
                await MpdSendNoIdle();
            CommandResult result = await MpdSendCommand("playlistinfo");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParsePlaylistInfo(result.ResultText);
            }
            if (idling)
                await MpdSendIdle();
            return result;
        }

        public async Task<CommandResult> MpdQueryPlaylists(bool idling = true)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (idling)
                await MpdSendNoIdle();
            CommandResult result = await MpdSendCommand("listplaylists");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParsePlaylists(result.ResultText);
            }
            if (idling)
                await MpdSendIdle();
            return result;
        }

        public async Task<CommandResult> MpdQueryListAll(bool idling = true)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (idling)
                await MpdSendNoIdle();
            CommandResult result = await MpdSendCommand("listall");
            if (result.IsSuccess)
            {
                result.IsSuccess = ParseListAll(result.ResultText);
            }
            if (idling)
                await MpdSendIdle();
            return result;
        }

        public async Task<CommandSearchResult> MpdSearch(string queryTag, string queryShiki, string queryValue)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            Application.Current.Dispatcher.Invoke(() =>
            {
                SearchResult.Clear();
            });
            
            CommandSearchResult result = new CommandSearchResult();

            if (string.IsNullOrEmpty(queryTag) || string.IsNullOrEmpty(queryValue) || string.IsNullOrEmpty(queryShiki))
            {
                result.IsSuccess = false;
                return result;
            }

            var expression = queryTag + " " + queryShiki + " \'" + Regex.Escape(queryValue) + "\'";

            string cmd = "search \"(" + expression + ")\"\n";

            /*
            await MpdSendNoIdle();
            CommandResult cm = await MpdSendCommand(cmd);
            if (cm.IsSuccess)
            {
                if (ParseSearchResult(cm.ResultText))
                {
                    result.IsSuccess = true;

                    result.SearchResult = this.SearchResult;
                }
            }
            await MpdSendIdle();
            */
            CommandResult cm = await MpdSendCommand(cmd, true);
            if (cm.IsSuccess)
            {
                Debug.WriteLine("ffffffffffffff");

                if (ParseSearchResult(cm.ResultText))
                {
                    result.IsSuccess = true;

                    result.SearchResult = this.SearchResult;
                }
            }

            return result;
        }

        public async Task<CommandPlaylistResult> MpdQueryPlaylistSongs(string playlistName)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            CommandPlaylistResult result = new CommandPlaylistResult();

            if (string.IsNullOrEmpty(playlistName))
            {
                result.IsSuccess = false;
                return result;
            }

            playlistName = Regex.Escape(playlistName);

            await MpdSendNoIdle();
            CommandResult cm = await MpdSendCommand("listplaylistinfo \"" + playlistName + "\"");
            if (cm.IsSuccess)
            {
                result.IsSuccess = cm.IsSuccess;
                result.PlaylistSongs = ParsePlaylistSongsResult(cm.ResultText);

            }
            await MpdSendIdle();

            return result;
        }

        #region == Response parser methods ==

        private bool ParseStatus(string result)
        {
            if (MpdStop) { return false; }
            if (string.IsNullOrEmpty(result)) return false;

            if (result.Trim() == "OK")
            {
                DebugCommandOutput?.Invoke(this, "################(Error) " + "An empty result (OK) returened for a status command." + Environment.NewLine + Environment.NewLine);
                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ParseStatus", "An empty result (OK) returened for a status command.", "", Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                Debug.WriteLine("@ParseStatus: An empty result (OK)  returened for a status command.");

                return false;
            }

            List<string> resultLines = result.Split('\n').ToList();

            if (resultLines.Count == 0) return false;

            var comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> MpdStatusValues = new Dictionary<string, string>(comparer);

            try
            {

                Application.Current.Dispatcher.Invoke(() =>
                {
                    _status.Reset();

                    foreach (string line in resultLines)
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

                            _status.MpdVolumeIdSet = true;
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
                    //TODO:;
                    /*
                    if (MpdStatusValues.ContainsKey("error"))
                    {
                        ErrorReturned?.Invoke(this, MpdErrorTypes.StatusError, MpdStatusValues["error"]);
                    }
                    else
                    {
                        ErrorReturned?.Invoke(this, MpdErrorTypes.ErrorClear, "");
                    }
                    */

                    // TODO: more?
                });

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParseStatusResponse:" + ex.Message);

                IsBusy?.Invoke(this, false);
            }

            return true;
        }

        private bool ParsePlaylistInfo(string result)
        {
            if (MpdStop) return false;

            // TODO: warning?
            if (string.IsNullOrEmpty(result)) return false;

            if (result.Trim() == "OK") return true;

            List<string> resultLines = result.Split('\n').ToList();
            if (resultLines == null) return true;
            if (resultLines.Count == 0) return true;

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
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentQueue.Clear();
                });

                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new Dictionary<string, string>(comparer);

                int i = 0;

                foreach (string value in resultLines)
                {
                    string[] StatusValuePair = value.Trim().Split(':');
                    if (StatusValuePair.Length > 1)
                    {
                        if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                        {
                            if (SongValues.ContainsKey("Id"))
                            {
                                SongInfo sng = FillSongInfo(SongValues, i);

                                if (sng != null)
                                {
                                    Application.Current.Dispatcher.Invoke(() =>
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

                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentQueue.Add(sng);
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParsePlaylistInfoResponse: " + ex.Message);

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

        private bool ParsePlaylists(string result)
        {
            if (string.IsNullOrEmpty(result)) return false;

            List<string> resultLines = result.Split('\n').ToList();

            if (resultLines.Count == 0) return false;

            try
            {
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Playlists.Clear();
                });

                // Tmp list for sorting.
                List<string> slTmp = new List<string>();

                foreach (string value in resultLines)
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
                    Application.Current.Dispatcher.Invoke(() =>
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

        private bool ParseListAll(string result)
        {
            if (MpdStop) return false;

            if (string.IsNullOrEmpty(result)) return false;

            List<string> resultLines = result.Split('\n').ToList();

            if (resultLines.Count == 0) return false;

            try
            {
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalFiles.Clear();
                    LocalDirectories.Clear();
                });

                foreach (string value in resultLines)
                {
                    if (value.StartsWith("directory:"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            LocalDirectories.Add(value.Replace("directory: ", ""));
                        });
                    }
                    else if (value.StartsWith("file:"))
                    {
                        Application.Current.Dispatcher.Invoke(() =>
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

        // TODO: 不要？
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
                if (Application.Current == null) { return false; }
                Application.Current.Dispatcher.Invoke(() =>
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

                                Application.Current.Dispatcher.Invoke(() =>
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
                    Song sng = FillSong(SongValues, i);

                    SongValues.Clear();

                    Application.Current.Dispatcher.Invoke(() =>
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

        private bool ParseSearchResult(string result)
        {
            Debug.WriteLine("asdfasdf");
            if (MpdStop) return false;

            if (Application.Current == null) { return false; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                SearchResult.Clear();
            });

            if (string.IsNullOrEmpty(result)) return false;

            //if (result.Trim() == "OK") return true;

            List<string> resultLines = result.Split('\n').ToList();
            if (resultLines == null) return true;
            if (resultLines.Count == 0) return true;
            


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
                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new Dictionary<string, string>(comparer);

                int i = 0;

                foreach (string line in resultLines)
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

                                Application.Current.Dispatcher.Invoke(() =>
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
                    Song sng = FillSong(SongValues, i);

                    SongValues.Clear();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SearchResult.Add(sng);
                    });
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParseSearchResult: " + ex.Message);

                return false;
            }

            return true;
        }

        private ObservableCollection<Song> ParsePlaylistSongsResult(string result)
        {
            ObservableCollection<Song> songList = new ObservableCollection<Song>();

            if (MpdStop) return songList;

            if (string.IsNullOrEmpty(result)) return songList;

            if (result.Trim() == "OK") return songList;

            List<string> resultLines = result.Split('\n').ToList();
            if (resultLines == null) return songList;
            if (resultLines.Count == 0) return songList;


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
                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new Dictionary<string, string>(comparer);

                int i = 0;

                foreach (string line in resultLines)
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

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    songList.Add(sng);
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
                    Song sng = FillSong(SongValues, i);

                    SongValues.Clear();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        songList.Add(sng);
                    });
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParseSearchResult: " + ex.Message);

                return songList;
            }

            return songList;
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

        #region == MPD Commands ==

        public async Task<CommandResult> MpdPlaybackPlay(string songId = "")
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            string cmd;

            if (songId != "")
            {
                cmd = "playid " + songId;
            }
            else
            {
                cmd = "play";
            }

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackPause()
        {
            Task<bool> check = CheckCommandQueue();
            await check;
            
            CommandResult result = await MpdSendCommand("pause 1", true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackResume(int volume)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            /*
            await MpdSendNoIdle();
            CommandResult result = await MpdSendCommand("pause 0");
            if (result.IsSuccess)
            {
                //
            }
            await MpdSendIdle();
            */
            

            if (MpdStatus.MpdVolumeIdSet)
            {
                CommandResult result = await MpdSendCommand("pause 0", true);

                return result;
            }
            else
            {
                string cmd = "command_list_begin" + "\n";
                cmd = cmd + "pause 0\n";
                cmd = cmd + "setvol " + volume.ToString() + "\n";
                cmd = cmd + "command_list_end" + "\n";

                CommandResult result = await MpdSendCommand(cmd, true);

                return result;
            }
        }

        public async Task<CommandResult> MpdPlaybackStop()
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            CommandResult result = await MpdSendCommand("stop", true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackNext()
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            CommandResult result = await MpdSendCommand("next", true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackPrev()
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            CommandResult result = await MpdSendCommand("previous", true);

            return result;
        }

        public async Task<CommandResult> MpdSetVolume(int v)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (v == _status.MpdVolume) 
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = true;
                return f;
            }

            CommandResult result = await MpdSendCommand("setvol " + v.ToString(), true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackSeek(string songId, int seekTime)
        {
            if ((songId == "") || (seekTime == 0)) 
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = true;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            CommandResult result = await MpdSendCommand("seekid " + songId + " " + seekTime.ToString(), true);

            return result;
        }

        public async Task<CommandResult> MpdSetRepeat(bool on)
        {
            if (_status.MpdRepeat == on)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = true;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            string cmd = "";
            if (on)
            {
                cmd = "repeat 1";
            }
            else
            {
                cmd = "repeat 0";
            }

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdSetRandom(bool on)
        {
            if (_status.MpdRandom == on)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = true;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            string cmd = "";
            if (on)
            {
                cmd = "random 1";
            }
            else
            {
                cmd = "random 0";
            }

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdSetConsume(bool on)
        {
            if (_status.MpdConsume == on)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = true;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            string cmd = "";
            if (on)
            {
                cmd = "consume 1";
            }
            else
            {
                cmd = "consume 0";
            }

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdSetSingle(bool on)
        {
            if (_status.MpdSingle == on)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = true;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            string cmd = "";
            if (on)
            {
                cmd = "single 1";
            }
            else
            {
                cmd = "single 0";
            }

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdClear()
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            CommandResult result = await MpdSendCommand("clear", true);

            return result;
        }

        public async Task<CommandResult> MpdSave(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            playlistName = Regex.Escape(playlistName);

            CommandResult result = await MpdSendCommand("save \"" + playlistName + "\"", true);

            return result;
        }

        public async Task<CommandResult> MpdAdd(string uri)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (string.IsNullOrEmpty(uri))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            uri = Regex.Escape(uri);

            CommandResult result = await MpdSendCommand("add \"" + uri + "\"", true);

            return result;
        }

        public async Task<CommandResult> MpdAdd(List<string> uris)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (uris.Count < 1)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            string cmd = "";
            cmd = "command_list_begin" + "\n";
            foreach (var uri in uris)
            {
                var urie = Regex.Escape(uri);
                cmd = cmd + "add \"" + urie + "\"\n";
            }
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdDeleteId(List<string> ids)
        {
            Task<bool> check = CheckCommandQueue();
            await check;

            if (ids.Count < 1)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            string cmd = "";
            cmd = "command_list_begin" + "\n";
            foreach (var id in ids)
            {
                cmd = cmd + "deleteid " + id + "\n";
            }
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdMoveId(Dictionary<string, string> IdToNewPosPair)
        {
            if (IdToNewPosPair == null)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }
            if (IdToNewPosPair.Count < 1)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            string cmd = "";
            cmd = "command_list_begin" + "\n";
            foreach (KeyValuePair<string, string> pair in IdToNewPosPair)
            {
                cmd = cmd + "moveid " + pair.Key + " " + pair.Value + "\n";
            }
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdChangePlaylist(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            playlistName = Regex.Escape(playlistName);

            string cmd = "command_list_begin" + "\n";
            cmd = cmd + "clear" + "\n";
            cmd = cmd + "load \"" + playlistName + "\"\n";
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdLoadPlaylist(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            playlistName = Regex.Escape(playlistName);

            string cmd = "load \"" + playlistName + "\"";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdRenamePlaylist(string playlistName, string newPlaylistName)
        {
            if (string.IsNullOrEmpty(playlistName) || string.IsNullOrEmpty(newPlaylistName))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            playlistName = Regex.Escape(playlistName);
            newPlaylistName = Regex.Escape(newPlaylistName);

            string cmd = "rename \"" + playlistName + "\" \"" + newPlaylistName + "\"";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdRemovePlaylist(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            playlistName = Regex.Escape(playlistName);

            string cmd = "rm \"" + playlistName + "\"";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdPlaylistAdd(string playlistName, List<string> uris)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }
            if (uris == null)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }
            if (uris.Count < 1)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            Task<bool> check = CheckCommandQueue();
            await check;

            playlistName = Regex.Escape(playlistName);


            string cmd = "command_list_begin" + "\n";
            foreach (var uri in uris)
            {
                var urie = Regex.Escape(uri);
                cmd = cmd + "playlistadd " + "\"" + playlistName + "\"" + " " + "\"" + urie + "\"\n";
            }
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        /*
        public async void MpdQueryAlbumArt(string uri, string songId)
        {
            if (string.IsNullOrEmpty(uri))
                return;

            // wait for a second. 
            await Task.Delay(100);

            if (_albumCover.IsDownloading)
            {
                //System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.IsDownloading. Ignoring.");
                //return;
            }

            if (_albumCover.SongFilePath == uri)
            {
                //System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.SongFilePath == uri. Ignoring.");
                //return;
            }

            if (songId != MpdStatus.MpdSongID)
            {
                // probably you double clicked on "Next song".
                System.Diagnostics.Debug.WriteLine("Error@MpdQueryAlbumArt: songId != MpdStatus.MpdSongID. Ignoring.");
                return;
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                _albumCover = new AlbumCover();
                _albumCover.IsDownloading = true;
                _albumCover.SongFilePath = uri;
                _albumCover.AlbumImageSource = null;
                _albumCover.BinaryData =  new byte[0];
                _albumCover.BinarySize = 0;
            });

            uri = Regex.Escape(uri);

            try
            {
                string mpdCommand = "albumart \"" + uri + "\" 0" + "\n";
                //string mpdCommand = "readpicture \"" + uri + "\" 0" + "\n";
                 
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

            // wait for a bit. 
            await Task.Delay(100);


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

            uri = Regex.Escape(uri);

            try
            {
                string mpdCommand = "albumart \"" + uri + "\" " + offset.ToString() + "\n";
                //string mpdCommand = "readpicture \"" + uri + "\" " + offset.ToString() + "\n";

                _asyncClient.Send("noidle" + "\n");
                _asyncClient.Send(mpdCommand);
                _asyncClient.Send("idle player mixer options playlist stored_playlist" + "\n");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@MpdReQueryAlbumArt: " + ex.Message);
            }
        }


        */

        #endregion

        #endregion

        public void MpdDisconnect()
        {
            try
            {
                ConnectionState = ConnectionStatus.Disconnecting;

                _commandConnection.Client.Shutdown(SocketShutdown.Both);
                _commandConnection.Close();
            }
            catch { }
            finally
            {
                ConnectionState = ConnectionStatus.DisconnectedByUser;
            }

            try
            {
                ConnectionState = ConnectionStatus.Disconnecting;

                _idleConnection.Client.Shutdown(SocketShutdown.Both);
                _idleConnection.Close();
            }
            catch { }
            finally
            {
                ConnectionState = ConnectionStatus.DisconnectedByUser;
            }

        }

    }
}

