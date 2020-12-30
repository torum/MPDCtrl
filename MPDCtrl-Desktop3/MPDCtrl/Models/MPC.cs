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
using MPDCtrl.Common;
using MPDCtrl.ViewModels.Classes;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace MPDCtrl.Models
{
    /// <summary>
    /// MPD client class. 
    /// </summary>
    public class MPC
    {
        #region == Consts, Properties, etc == 

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

        //private TCPC _asyncClient = new TCPC();

        #endregion

        #region == Events == 

        public delegate void IsBusyEvent(MPC sender, bool on);
        public event IsBusyEvent IsBusy;

        public delegate void DebugOutputEvent(MPC sender, string data);
        public event DebugOutputEvent DebugOutput;

        public delegate void ConnectionStatusChangedEvent(MPC sender, ConnectionStatus status);
        public event ConnectionStatusChangedEvent ConnectionStatusChanged;

        public delegate void ConnectionErrorEvent(MPC sender, string data);
        public event ConnectionErrorEvent ConnectionError;

        public delegate void IsMpdConnectedEvent(MPC sender);
        public event IsMpdConnectedEvent MpdConnected;

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

        public bool IsMpdCommandConnected { get; set; }
        public bool IsMpdIdleConnected { get; set; }

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
            SeeConnectionErrorEvent
        }

        private ConnectionStatus _connectionStatus;
        public ConnectionStatus ConnectionState
        {
            get
            {
                return _connectionStatus;
            }
            private set
            {
                if (value == _connectionStatus)
                    return;

                _connectionStatus = value;

                ConnectionStatusChanged?.Invoke(this, _connectionStatus);
            }
        }

        private static TcpClient _commandConnection = new TcpClient();
        private StreamReader _commandReader;
        private StreamWriter _commandWriter;

        private static TcpClient _idleConnection = new TcpClient();
        private static StreamReader _idleReader;
        private static StreamWriter _idleWriter;

        private static ManualResetEvent readDone =
            new ManualResetEvent(false);

        #endregion

        //ConcurrentQueue<string> commandQueue = new ConcurrentQueue<string>();


        public MPC()
        {
            _connectionStatus = ConnectionStatus.NeverConnected;

        }

        public async void MpdConnect(string host, int port)
        {
            IsMpdCommandConnected = false;

            _host = host;
            _port = port;

            DebugOutput?.Invoke(this, "TCP Command Connection: Trying to connect." + "\n" + "\n");

            _connectionStatus = ConnectionStatus.Connecting;

            try
            {
                await _commandConnection.ConnectAsync(_host, _port);

                if (_commandConnection.Client.Connected)
                {
                    DebugOutput?.Invoke(this, "TCP Command Connection: Connection established." + "\n" + "\n");

                    _connectionStatus = ConnectionStatus.Connected;

                    var tcpStream = _commandConnection.GetStream();
                    tcpStream.ReadTimeout = System.Threading.Timeout.Infinite;

                    _commandReader = new StreamReader(tcpStream);
                    _commandWriter = new StreamWriter(tcpStream);
                    _commandWriter.AutoFlush = true;

                    string response = await _commandReader.ReadLineAsync();

                    if (response.StartsWith("OK MPD "))
                    {
                        _mpdVer = response.Replace("OK MPD ", string.Empty).Trim();

                        DebugOutput?.Invoke(this, "<<<<" + response.Trim() + "\n" + "\n");

                        IsMpdCommandConnected = true;

                        MpdConnected?.Invoke(this);

                        // Done for now.
                    }
                    else
                    {
                        DebugOutput?.Invoke(this, "TCP Command Connection: MPD did not respond with proper respose." + "\n" + "\n");

                        _connectionStatus = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "TCP connection error: MPD did not respond with proper respose.");
                    }
                }
                else
                {
                    //?

                    Debug.WriteLine("**** !client.Client.Connected");

                    DebugOutput?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected." + "\n" + "\n");

                    _connectionStatus = ConnectionStatus.NeverConnected;

                    ConnectionError?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected.");
                }
            }
            catch (Exception e)
            {
                // TODO: Test.

                DebugOutput?.Invoke(this, "TCP Command Connection: Error while connecting. Fail to connect: " + e.Message + "\n" + "\n");

                _connectionStatus = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }

        }

        public void MpdDisconnect()
        {
            try
            {
                ConnectionState = ConnectionStatus.Disconnecting;

                _commandConnection.Client.Shutdown(SocketShutdown.Both);
                _commandConnection.Close();
            }
            catch { }
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

        #region == Idle connection ==

        public async void MpdIdleConnectionStart(string host, int port, string password)
        {
            ConnectionResult r = await MpdIdleConnect(host, port);

            if (r.IsSuccess)
            {
                CommandResult d = await MpdSendIdleAuth(password);

                if (d.IsSuccess)
                {
                    MpdIdle();
                }
            }

        }

        private async Task<ConnectionResult> MpdIdleConnect(string host, int port)
        {

            DebugOutput?.Invoke(this, "TCP Idle Connection: Trying to connect." + "\n" + "\n");

            _connectionStatus = ConnectionStatus.Connecting;

            ConnectionResult ret = new ConnectionResult();

            try
            {
                await _idleConnection.ConnectAsync(host, port);

                if (_idleConnection.Client.Connected)
                {
                    DebugOutput?.Invoke(this, "TCP Idle Connection: Connection established." + "\n" + "\n");

                    _connectionStatus = ConnectionStatus.Connected;

                    var tcpStream = _idleConnection.GetStream();
                    tcpStream.ReadTimeout = System.Threading.Timeout.Infinite;

                    _idleReader = new StreamReader(tcpStream);
                    _idleWriter = new StreamWriter(tcpStream);
                    _idleWriter.AutoFlush = true;

                    string response = await _idleReader.ReadLineAsync();

                    if (response.StartsWith("OK MPD "))
                    {
                        //_mpdVer = response.Replace("OK MPD ", string.Empty);

                        Debug.WriteLine(response.Trim() + " @MPC.MpdIdleConnect");

                        DebugOutput?.Invoke(this, "<<" + response.Trim() + "\n" + "\n");
                        
                        IsMpdIdleConnected = true;

                        ret.IsSuccess = true;
                        ret.ErrorMessage = "";
                    }
                    else
                    {
                        DebugOutput?.Invoke(this, "TCP Idle Connection: MPD did not respond with proper respose." + "\n" + "\n");

                        _connectionStatus = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "TCP connection error: MPD did not respond with proper respose.");
                    }
                }
                else
                {
                    //?

                    Debug.WriteLine("**** !client.Client.Connected");

                    DebugOutput?.Invoke(this, "TCP Idle Connection: FAIL to established... Client not connected." + "\n" + "\n");

                    _connectionStatus = ConnectionStatus.NeverConnected;

                    ConnectionError?.Invoke(this, "TCP Idle Connection: FAIL to established... Client not connected.");
                }
            }
            catch (Exception e)
            {
                // TODO: Test.

                DebugOutput?.Invoke(this, "TCP Idle Connection: Error while connecting. Fail to connect: " + e.Message + "\n" + "\n");

                _connectionStatus = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }

            return ret;
        }

        private async Task<CommandResult> MpdSendIdleAuth(string password = "")
        {
            CommandResult ret = new CommandResult();

            if (string.IsNullOrEmpty(password))
            {
                ret.IsSuccess = true;
                ret.ResultText = "OK";//Or OK
                ret.ErrorMessage = "";

                return ret;
            }

            DebugOutput?.Invoke(this, ">>Password ******" + "\n" + "\n");

            string cmd = "password " + password + "\n";

            try
            {
                if (_idleConnection.Client.Connected)
                {
                    if (IsMpdIdleConnected)
                    {
                        if ((_idleWriter != null) || (_idleReader != null))
                        {
                            try
                            {
                                await _idleWriter.WriteAsync(cmd);

                                try
                                {
                                    string response = await _idleReader.ReadLineAsync();

                                    ret.ResultText = response;
                                    ret.IsSuccess = true;
                                    ret.ErrorMessage = "";

                                    DebugOutput?.Invoke(this, "<<" + response.Trim().Replace("\r\n", "\n" + "<<") + "\n" + "\n");
                                }
                                catch (Exception e)
                                {
                                    DebugOutput?.Invoke(this, string.Format("[Exception@MpdSendIdleAuth] ({0} ):\n{1}", "ReadLineAsync", e.Message));

                                    Debug.WriteLine(e.Message);

                                    ret.IsSuccess = false;
                                    ret.ErrorMessage = e.Message;

                                    return ret;
                                }

                            }
                            catch (Exception e)
                            {
                                DebugOutput?.Invoke(this, string.Format("[Exception@MpdSendIdleAuth] ({0} ):\n{1}", "WriteAsync", e.Message));

                                Debug.WriteLine(e.Message);

                                ret.IsSuccess = false;
                                ret.ErrorMessage = e.Message;

                                return ret;

                            }
                        }
                        else
                        {
                            ret.IsSuccess = false;
                            ret.ErrorMessage = "_idleWriter or _idleReader is null";

                            Debug.WriteLine("_idleWriter or _idleReader is null");
                        }
                    }
                    else
                    {
                        ret.IsSuccess = false;
                        ret.ErrorMessage = "NOT IsMpdIdleConnected";

                        Debug.WriteLine("NOT IsMpdIdleConnected");
                    }

                }
                else
                {
                    ret.IsSuccess = false;
                    ret.ErrorMessage = "NOT _tcpIdleClient.Client.Connected";

                    Debug.WriteLine("NOT _tcpIdleClient.Client.Connected");
                }
            }
            catch (Exception e)
            {
                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                Debug.WriteLine(e.Message);
            }

            return ret;
        }

        public async void MpdIdle()
        {
            if (_idleConnection.Client == null)
            {
                DebugOutput?.Invoke(this, string.Format("[Exception@MpdIdle] ({0} ):\n{1}", "_idleConnection.Client.Connected", "_idleConnection.Client == null"));

                Debug.WriteLine("_idleConnection.Client == null @MpdIdle]");

                return;
            }

            if (IsMpdIdleConnected && _idleConnection.Client.Connected)
            {
                if ((_idleWriter != null) || (_idleReader != null))
                {
                    try
                    {
                        bool isMpdIdling = true;

                        string cmd = "idle player mixer options playlist stored_playlist\n";

                        DebugOutput?.Invoke(this, ">>>>>>>>>>>>>>>>" + cmd.Trim() + "\n" + "\n");

                        await _idleWriter.WriteAsync(cmd);

                        StringBuilder stringBuilder = new StringBuilder();

                        try
                        {
                            while (isMpdIdling)
                            {
                                string line = await _idleReader.ReadLineAsync();

                                if (line != null)
                                {
                                    if (line.StartsWith("ACK"))
                                    {
                                        Debug.WriteLine("ACK: " + line);

                                        if (!string.IsNullOrEmpty(line))
                                            stringBuilder.Append(line + "\n");

                                        isMpdIdling = false;
                                    }
                                    else if (line.StartsWith("OK"))
                                    {
                                        if (!string.IsNullOrEmpty(line))
                                            stringBuilder.Append(line + "\n");

                                        isMpdIdling = false;
                                    }
                                    else if (line.StartsWith("changed: "))
                                    {
                                        //Debug.WriteLine("changed line @MpdIdle " + line);

                                        if (!string.IsNullOrEmpty(line))
                                            stringBuilder.Append(line + "\n");
                                    }
                                    else
                                    {
                                        if (!string.IsNullOrEmpty(line))
                                        {
                                            Debug.WriteLine("ELSE @MpdIdle  " + line);

                                            stringBuilder.Append(line + "\n");
                                        }
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("line == null @MpdIdle]");

                                    isMpdIdling = false;

                                    DebugOutput?.Invoke(this, string.Format("[Exception@MpdIdle] ({0} ):\n{1}", "ReadLineAsync", "line == null"));

                                    return;
                                }
                            }

                            string result = stringBuilder.ToString();

                            DebugOutput?.Invoke(this, "<<<<<<<<<<<<<<<<" + result.Trim().Replace("\n", "\n" + "<<<<<<<<<<<<<<<<") + "\n" + "\n");

                            // start over
                            MpdIdle();

                            // Parse & Raise event.
                            ParseSubSystemsAndRaseChangedEvent(result);

                        }
                        catch (Exception e)
                        {
                            // Could be application shutdopwn.

                            Debug.WriteLine("[Exception@MpdIdle] ({0} ):\n{1}", "ReadLineAsync", e.Message);
                            DebugOutput?.Invoke(this, string.Format("[Exception@MpdIdle] ({0} ):\n{1}", "ReadLineAsync", e.Message));
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("[Exception@MpdIdle] ({0} ):\n{1}", "WriteAsync", e.Message);
                        DebugOutput?.Invoke(this, string.Format("[Exception@MpdIdle] ({0} ):\n{1}", "WriteAsync", e.Message));
                    }
                }
                else
                {

                    Debug.WriteLine("(_idleWriter != null) || (_idleReader != null) @MpdIdle");
                }
            }
            else
            {

                Debug.WriteLine("IsMpdIdleConnected && _idleConnection.Client.Connected @MpdIdle");
            }
        }

        private void ParseSubSystemsAndRaseChangedEvent(string result)
        {
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

                if (isCurrentQueue)
                {
                    MpdCurrentQueueChanged?.Invoke(this);
                }

                if (isStoredPlaylist)
                {
                    MpdPlaylistsChanged?.Invoke(this);
                }

                if (isPlayer)
                {
                    MpdPlayerStatusChanged?.Invoke(this);
                }

            }
            catch
            {
                Debug.WriteLine("**Error@ParseSubSystemsAndRaseChangedEvent: " + result);
            }
        }

        /*
        public async Task<IdleResult> MpdNoIdle()
        {
            IdleResult ret = new IdleResult();

            try
            {
                if (IsMpdIdleConnected && _idleConnection.Client.Connected)
                {
                    if ((_idleWriter != null) || (_idleReader != null))
                    {
                        try
                        {
                            string cmd = "noidle\n";

                            DebugOutput?.Invoke(this, ">>" + cmd.Trim() + "\n" + "\n");

                            await _idleWriter.WriteAsync(cmd);

                            StringBuilder stringBuilder = new StringBuilder();

                            try
                            {
                                while (true)
                                {
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
                                            Debug.WriteLine("OK: " + line);

                                            if (!string.IsNullOrEmpty(line))
                                                stringBuilder.Append(line + "\n");

                                            break;
                                        }
                                        else if (line.StartsWith("changed: "))
                                        {
                                            Debug.WriteLine("changed: " + line);

                                            if (!string.IsNullOrEmpty(line))
                                                stringBuilder.Append(line + "\n");
                                        }
                                        else
                                        {
                                            if (!string.IsNullOrEmpty(line))
                                            {
                                                Debug.WriteLine("ELSE: " + line);

                                                stringBuilder.Append(line + "\n");
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
                                        Debug.WriteLine("line == null");
                                        break;
                                    }
                                }

                                ret.ResultText = stringBuilder.ToString();
                                ret.IsSuccess = true;
                                ret.ErrorMessage = "";

                                DebugOutput?.Invoke(this, "<<" + ret.ResultText.Trim().Replace("\n", "\n" + "<<") + "\n" + "\n");
                            }
                            catch (Exception e)
                            {
                                ret.IsSuccess = false;
                                ret.ErrorMessage = e.Message;

                                return ret;
                            }

                        }
                        catch (Exception e)
                        {
                            ret.IsSuccess = false;
                            ret.ErrorMessage = e.Message;

                            return ret;
                        }
                    }
                    else
                    {
                        ret.IsSuccess = false;
                        ret.ErrorMessage = "_idleWriter or _idleReader is null";
                    }
                }
                else
                {
                    ret.IsSuccess = false;
                    ret.ErrorMessage = "NOT IsMpdIdleConnected";
                }
            }
            catch (Exception e)
            {
                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
            }

            return ret;
        }
        */
        
        #endregion

        #region == MPD Commands == 

        private async Task<CommandResult> MpdSendCommand(string cmd)
        {
            CommandResult ret = new CommandResult();

            if (cmd.Trim() == "idle")
            {
                DebugOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");
            }
            else
            {
                DebugOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");
            }

            try
            {
                if (_commandConnection.Client.Connected)
                {
                    if (IsMpdCommandConnected)
                    {
                        if ((_commandWriter != null) || (_commandReader != null))
                        {
                            try
                            {
                                if (cmd.Trim() == "idle")
                                {
                                    // idle の場合は結果を待た（Readし）ない。
                                    try
                                    {
                                        await _commandWriter.WriteAsync(cmd.Trim() + "\n");
                                    }
                                    catch (Exception e)
                                    {
                                        ret.IsSuccess = false;
                                        ret.ErrorMessage = e.Message;
                                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));

                                        return ret;
                                    }

                                    ret.IsSuccess = true;
                                    ret.ErrorMessage = "";
                                    return ret;
                                }
                                else
                                {
                                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");

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
                                                    Debug.WriteLine("ACK line @MpdSendCommand: " + cmd.Trim() + " and " + line);

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
                                                        //Debug.WriteLine("ELSE: " + line);

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
                                                break;
                                            }
                                        }

                                        ret.ResultText = stringBuilder.ToString();
                                        ret.ErrorMessage = "";

                                        DebugOutput?.Invoke(this, "<<<<" + ret.ResultText.Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("Exception@MpdSendCommand: " + cmd.Trim() + " ReadLineAsync " + e.Message);

                                        ret.IsSuccess = false;
                                        ret.ErrorMessage = e.Message;
                                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));

                                        return ret;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine("Exception@MpdSendCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                                ret.IsSuccess = false;
                                ret.ErrorMessage = e.Message;
                                DebugOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));

                                return ret;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("@MpdSendCommand:  _commandWriter or _commandReader is null");

                            ret.IsSuccess = false;
                            ret.ErrorMessage = "_commandWriter or _commandReader is null";
                            DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                        }
                    }
                    else
                    {
                        Debug.WriteLine("@MpdSendCommand:  NOT IsMpdCommandConnected");

                        ret.IsSuccess = false;
                        ret.ErrorMessage = "NOT IsMpdCommandConnected";
                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                    }
                }
                else
                {
                    Debug.WriteLine("@MpdSendCommand: NOT _tcpCommandClient.Client.Connected");

                    ret.IsSuccess = false;
                    ret.ErrorMessage = "NOT _tcpCommandClient.Client.Connected";
                    DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommand: " + cmd.Trim()  + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));
            }

            return ret;
        }

        private async Task<CommandResult> MpdSendCommandWithNoData(string cmd)
        {
            CommandResult ret = new CommandResult();

            if (cmd.Trim() == "idle")
            {
                DebugOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");
            }
            else
            {
                DebugOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");
            }

            try
            {
                if (_commandConnection.Client.Connected)
                {
                    if (IsMpdCommandConnected)
                    {
                        if ((_commandWriter != null) || (_commandReader != null))
                        {
                            try
                            {
                                if (cmd.Trim() == "idle")
                                {
                                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");
                                }
                                else
                                {
                                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");

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
                                                    Debug.WriteLine("ACK line @MpdSendCommand: " + cmd.Trim() + " and " + line);

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
                                                break;
                                            }
                                        }

                                        ret.ErrorMessage = "";

                                        DebugOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");
                                    }
                                    catch (Exception e)
                                    {
                                        Debug.WriteLine("Exception@MpdSendCommandWithNoData: " + cmd.Trim() + " ReadLineAsync " + e.Message);

                                        ret.IsSuccess = false;
                                        ret.ErrorMessage = e.Message;
                                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));

                                        return ret;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                Debug.WriteLine("Exception@MpdSendCommandWithNoData: " + cmd.Trim() + " WriteAsync " + e.Message);

                                ret.IsSuccess = false;
                                ret.ErrorMessage = e.Message;
                                DebugOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));

                                return ret;
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Exception@MpdSendCommandWithNoData: " + "_commandWriter or _commandReader is null");

                            ret.IsSuccess = false;
                            ret.ErrorMessage = "_commandWriter or _commandReader is null";
                            DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Exception@MpdSendCommandWithNoData: " + "NOT IsMpdCommandConnected");

                        ret.IsSuccess = false;
                        ret.ErrorMessage = "NOT IsMpdCommandConnected";
                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                    }

                }
                else
                {
                    Debug.WriteLine("Exception@MpdSendCommandWithNoData: " + "NOT _tcpCommandClient.Client.Connected");

                    ret.IsSuccess = false;
                    ret.ErrorMessage = "NOT _tcpCommandClient.Client.Connected";
                    DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), ret.ErrorMessage));
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommandWithNoData: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}" + Environment.NewLine + Environment.NewLine, cmd.Trim(), e.Message));
            }

            return ret;
        }

        public async Task<CommandResult> MpdSendPassword(string password = "")
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

            return await MpdSendCommandWithNoData(cmd);

            /*
            DebugOutput?.Invoke(this, ">>>>password ******" + "\n" + "\n");

            try
            {
                if (_commandConnection.Client.Connected)
                {
                    if (IsMpdCommandConnected)
                    {
                        if ((_commandWriter != null) || (_commandReader != null))
                        {
                            try
                            {
                                await _commandWriter.WriteAsync(cmd);

                                try
                                {
                                    string response = await _commandReader.ReadLineAsync();

                                    ret.ResultText = response;
                                    ret.ErrorMessage = "";

                                    if (response.Trim() == "OK")
                                    {
                                        ret.IsSuccess = true;
                                    }

                                    DebugOutput?.Invoke(this, "<<<<" + response.Trim().Replace("\r\n", "\n" + "<<<<")  + "\n" + "\n");

                                }
                                catch (Exception e)
                                {
                                    DebugOutput?.Invoke(this, "<<<<(Exception)" + "\n" + e.Message.Trim() + "\n" + "\n");

                                    ret.IsSuccess = false;
                                    ret.ErrorMessage = e.Message;

                                    return ret;
                                }
                            }
                            catch (Exception e)
                            {
                                DebugOutput?.Invoke(this, "<<<<(Exception)" + "\n" + e.Message.Trim() + "\n" + "\n");

                                ret.IsSuccess = false;
                                ret.ErrorMessage = e.Message;

                                return ret;
                            }
                        }
                        else
                        {
                            ret.IsSuccess = false;
                            ret.ErrorMessage = "_commandWriter or _commandReader is null";
                        }
                    }
                    else
                    {
                        ret.IsSuccess = false;
                        ret.ErrorMessage = "NOT IsMpdCommandConnected";
                    }

                }
                else
                {
                    ret.IsSuccess = false;
                    ret.ErrorMessage = "NOT _tcpCommandClient.Client.Connected";
                }
            }
            catch (Exception e)
            {
                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
            }

            return ret;

            */
        }

        public async Task<CommandResult> MpdSendIdle()
        {
            return await MpdSendCommandWithNoData("idle");
        }

        public async Task<CommandResult> MpdSendNoIdle()
        {
            return await MpdSendCommandWithNoData("noidle");
        }

        public async Task<CommandResult> MpdQueryStatus()
        {
            CommandResult r = await MpdSendCommand("status");

            if (r.IsSuccess)
            {
                r.IsSuccess = ParseStatus(r.ResultText);
            }

            return r;
        }

        public async Task<CommandResult> MpdQueryCurrentSong()
        {
            // Currently not used. So do nothing.

            return await MpdSendCommand("currentsong");
        }

        public async Task<CommandResult> MpdQueryCurrentQueue()
        {
            CommandResult r = await MpdSendCommand("playlistinfo");

            if (r.IsSuccess)
            {
                if (ParsePlaylistInfo(r.ResultText))
                {
                    //
                }
            }

            return r;
        }

        public async Task<CommandResult> MpdQueryPlaylists()
        {
            CommandResult r = await MpdSendCommand("listplaylists");

            if (r.IsSuccess)
            {
                if (ParsePlaylists(r.ResultText))
                {
                    //
                }
            }

            return r;
        }

        public async Task<CommandResult> MpdQueryListAll()
        {
            CommandResult r = await MpdSendCommand("listall");

            if (r.IsSuccess)
            {
                if (ParseListAll(r.ResultText))
                {
                    //
                }
            }

            return r;
        }

        #region == Response parser methods ==

        private bool ParseStatus(string result)
        {
            if (MpdStop) { return false; }
            if (string.IsNullOrEmpty(result)) return false;

            /*
            if (result.Trim() == "OK\nOK")
            {
                DebugOutput?.Invoke(this, "<<<<(Error) " + "An empty result (OKOK) returened for a status command." + Environment.NewLine + Environment.NewLine);

                Debug.WriteLine("@ParseStatus: An empty result (OKOK) returened for a status command.");

                return false;
            }
            */
            if (result.Trim() == "OK")
            {
                DebugOutput?.Invoke(this, "<<<<(Error) " + "An empty result (OK) returened for a status command." + Environment.NewLine + Environment.NewLine);

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
                System.Diagnostics.Debug.WriteLine("Error@ParseStatusResponse:" + ex.Message);

                IsBusy?.Invoke(this, false);
            }

            return true;
        }

        private bool ParsePlaylistInfo(string result)
        {
            if (MpdStop) return false;

            if (string.IsNullOrEmpty(result)) return false;

            List<string> resultLines = result.Split('\n').ToList();

            if (resultLines.Count == 0) return false;

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

        #region == Other Commands ==

        public async Task<CommandResult> MpdPlaybackPlay(string songId = "")
        {
            string cmd;

            if (songId != "")
            {
                cmd = "playid " + songId;
            }
            else
            {
                cmd = "play";
            }

            CommandResult result = await MpdSendNoIdle();
            if (result.IsSuccess)
            {
                result = await MpdSendCommandWithNoData(cmd);
                if (result.IsSuccess)
                {
                    result = await MpdSendIdle();
                    if (result.IsSuccess)
                    {

                    }
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdPlaybackPause()
        {
            CommandResult result = await MpdSendNoIdle();
            if (result.IsSuccess)
            {
                result = await MpdSendCommandWithNoData("pause 1");
                if (result.IsSuccess)
                {
                    result = await MpdSendIdle();
                    if (result.IsSuccess)
                    {

                    }
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdPlaybackResume()
        {
            CommandResult result = await MpdSendNoIdle();
            if (result.IsSuccess)
            {
                result = await MpdSendCommandWithNoData("pause 0");
                if (result.IsSuccess)
                {
                    result = await MpdSendIdle();
                    if (result.IsSuccess)
                    {

                    }
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdPlaybackStop()
        {
            CommandResult result = await MpdSendNoIdle();
            if (result.IsSuccess)
            {
                result = await MpdSendCommandWithNoData("stop");
                if (result.IsSuccess)
                {
                    result = await MpdSendIdle();
                    if (result.IsSuccess)
                    {

                    }
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdPlaybackNext()
        {
            CommandResult result = await MpdSendNoIdle();
            if (result.IsSuccess)
            {
                result = await MpdSendCommandWithNoData("next");
                if (result.IsSuccess)
                {
                    result = await MpdSendIdle();
                    if (result.IsSuccess)
                    {

                    }
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdPlaybackPrev()
        {
            CommandResult result = await MpdSendNoIdle();
            if (result.IsSuccess)
            {
                result = await MpdSendCommandWithNoData("previous");
                if (result.IsSuccess)
                {
                    result = await MpdSendIdle();
                }
            }

            return result;
        }

        public async Task<CommandResult> MpdSetVolume(int v)
        {
            if (v == _status.MpdVolume) 
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = true;
                return f;
            }

            CommandResult result = await MpdSendNoIdle();
            if (result.IsSuccess)
                result = await MpdSendCommandWithNoData("setvol " + v.ToString());
            if (result.IsSuccess)
                result = await MpdSendIdle();

            return result;
        }

        /*

MpdPlaybackSeek(string songId, int seekTime)



MpdSetRepeat(bool on)
MpdSetRandom(bool on)
MpdSetConsume(bool on)
MpdSetSingle(bool on)

MpdClear
MpdSave(string playlistName)
MpdAdd(string uri)
MpdAdd(List<string> uris)
MpdDeleteId(List<string> ids)
MpdMoveId(Dictionary<string, string> IdToNewPosPair)

        
MpdQueryListPlaylistinfo(string playlistName)

MpdChangePlaylist(string playlistName)
MpdLoadPlaylist(string playlistName)
MpdRenamePlaylist(string playlistName, string newPlaylistName)
MpdRemovePlaylist(string playlistName)
MpdPlaylistAdd(string playlistName, List<string> uris)


MpdSearch(string queryTag, string queryShiki, string queryValue)

MpdQueryAlbumArt(string uri, string songId)
MpdReQueryAlbumArt(string uri, int offset)





        */

        #endregion

        #endregion
    }
}

