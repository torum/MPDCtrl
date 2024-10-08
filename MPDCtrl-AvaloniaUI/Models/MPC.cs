﻿using Avalonia.Threading;
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
using ReactiveUI;

namespace MPDCtrlX.Models
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

        private string _mpdVerText;
        public string MpdVerText
        {
            get { return _mpdVerText; }
            set
            {
                if (value == _mpdVerText)
                    return;

                _mpdVerText = value;

            }
        }

        private readonly Status _status = new();
        public Status MpdStatus
        {
            get { return _status; }
        }

        public bool MpdStop { get; set; }

        private SongInfoEx _currentSong;
        public SongInfoEx MpdCurrentSong
        {
            // You need to either get "status" and "queue" before hand, or "currentsong". 
            get
            {
                return _currentSong;
            }
        }

        private ObservableCollection<SongInfoEx> _queue = new();
        public ObservableCollection<SongInfoEx> CurrentQueue
        {
            get { return _queue; }
        }

        private ObservableCollection<Playlist> _playlists = new();
        public ObservableCollection<Playlist> Playlists
        {
            get { return _playlists; }
        }

        private readonly ObservableCollection<SongFile> _localFiles = new();
        public ObservableCollection<SongFile> LocalFiles
        {
            get { return _localFiles; }
        }

        private readonly ObservableCollection<String> _localDirectories = new();
        public ObservableCollection<String> LocalDirectories
        {
            get { return _localDirectories; }
        }

        private readonly ObservableCollection<SongInfo> _searchResult = new();
        public ObservableCollection<SongInfo> SearchResult
        {
            get { return _searchResult; }
        }

        // TODO:
        private AlbumImage _albumCover = new();
        public AlbumImage AlbumCover
        {
            get { return _albumCover; }
        }

        #endregion

        #region == Connections ==

        private static TcpClient _commandConnection = new();
        private StreamReader _commandReader;
        private StreamWriter _commandWriter;

        private static TcpClient _idleConnection = new();
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

        private ConnectionStatus _connectionState = ConnectionStatus.NeverConnected;
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

        // TODO: Not really used...
        public bool IsMpdCommandConnected { get; set; }
        public bool IsMpdIdleConnected { get; set; }

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

        public delegate void MpdAckErrorEvent(MPC sender, string data);
        public event MpdAckErrorEvent MpdAckError;

        public delegate void MpdPlayerStatusChangedEvent(MPC sender);
        public event MpdPlayerStatusChangedEvent MpdPlayerStatusChanged;

        public delegate void MpdCurrentQueueChangedEvent(MPC sender);
        public event MpdCurrentQueueChangedEvent MpdCurrentQueueChanged;

        public delegate void MpdPlaylistsChangedEvent(MPC sender);
        public event MpdPlaylistsChangedEvent MpdPlaylistsChanged;

        public delegate void MpdAlbumArtChangedEvent(MPC sender);
        public event MpdAlbumArtChangedEvent MpdAlbumArtChanged;

        public delegate void MpcProgressEvent(MPC sender, string msg);
        public event MpcProgressEvent MpcProgress;

        #endregion

        public MPC()
        {

        }

        #region == Idle Connection ==

        public async Task<bool> MpdIdleConnectionStart(string host, int port, string password)
        {
            // TODO: not used for now.

            ConnectionResult r = await MpdIdleConnect(host, port);

            bool ret = false;

            if (r.IsSuccess)
            {
                CommandResult d = await MpdIdleSendPassword(password);

                if (d.IsSuccess)
                {
                    ret = true;

                }
            }

            return ret;
        }

        public async Task<ConnectionResult> MpdIdleConnect(string host, int port)
        {
            ConnectionResult result = new();

            IsMpdIdleConnected = false;

            _idleConnection = new TcpClient();

            _host = host;
            _port = port;

            DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Connecting." + "\n" + "\n");
            MpcProgress?.Invoke(this, "Connecting...");

            ConnectionState = ConnectionStatus.Connecting;

            try
            {
                IsBusy?.Invoke(this, true);

                await _idleConnection.ConnectAsync(_host, _port);

                // TODO:
                if (_idleConnection.Client == null)
                {
                    Debug.WriteLine("_idleConnection.Client == null. " + host + " " + port.ToString());

                    result.ErrorMessage = "_idleConnection.Client == null";

                    DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Error while connecting. Fail to connect... "+ "\n" + "\n");

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "TCP connection error...");

                    IsBusy?.Invoke(this, false);
                    return result;
                }

                if (_idleConnection.Client.Connected)
                {
                    DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Connection established." + "\n" + "\n");
                    MpcProgress?.Invoke(this, "Connection established....");

                    ConnectionState = ConnectionStatus.Connected;

                    var tcpStream = _idleConnection.GetStream();
                    tcpStream.ReadTimeout = System.Threading.Timeout.Infinite;

                    _idleReader = new StreamReader(tcpStream);
                    _idleWriter = new StreamWriter(tcpStream)
                    {
                        AutoFlush = true
                    };

                    string response = await _idleReader.ReadLineAsync();

                    if (response.StartsWith("OK MPD "))
                    {
                        MpdVerText = response.Replace("OK MPD ", string.Empty).Trim();

                        DebugIdleOutput?.Invoke(this, "<<<<" + response.Trim() + "\n" + "\n");
                        MpcProgress?.Invoke(this, response.Trim());

                        IsMpdIdleConnected = true;

                        result.IsSuccess = true;

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
            catch (SocketException e)
            {
                // TODO: Test.

                //e.SocketErrorCode

                DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Error while connecting. Fail to connect: " + e.Message + "\n" + "\n");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }
            catch (Exception e)
            {
                // TODO: Test.

                DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Error while connecting. Fail to connect: " + e.Message + "\n" + "\n");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }


            IsBusy?.Invoke(this, false);
            return result;
        }

        public async Task<CommandResult> MpdIdleSendPassword(string password = "")
        {
            //MpcProgress?.Invoke(this, "Sending password...");

            _password = password;

            CommandResult ret = new();

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

        private async Task<CommandResult> MpdIdleSendCommand(string cmd)
        {
            CommandResult ret = new();

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

            //DebugIdleOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");
            Task nowait = Task.Run(() => DebugIdleOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n"));

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

                    ConnectionError?.Invoke(this, "The connection (idle) has been terminated (IOException): " + e.Message);
                }

                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdIdleSendCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "WriteAsync@MpdIdleSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "The connection (idle) has been terminated (Exception): " + e.Message);
                }

                return ret;
            }

            try
            {
                StringBuilder stringBuilder = new();

                bool isAck = false;
                string ackText = "";

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

                            isAck = true;
                            ackText = line;
                            ret.ErrorMessage = line;

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
                        Debug.WriteLine("@MpdIdleSendCommand ReadLineAsync line != null");

                        DebugIdleOutput?.Invoke(this, string.Format("################ Error @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdleSendCommand", "ReadLineAsync received null data", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "The connection (idle) has been terminated. ");

                        ret.ResultText = stringBuilder.ToString();
                        ret.ErrorMessage = "ReadLineAsync@MpdIdleSendCommand received null data";

                        break;
                    }
                }

                nowait = Task.Run(() => DebugIdleOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n"));
                
                if (isAck)
                    nowait = Task.Run(() => MpdAckError?.Invoke(this, ackText + " (@MISC)")); 

                ret.ResultText = stringBuilder.ToString();
                
                return ret;
            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdIdleSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                DebugIdleOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdIdleSendCommand", "InvalidOperationException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                //ConnectionError?.Invoke(this, "The connection (idle) has been terminated. Most likely the connection has been overloaded.");


                //await _idleWriter.WriteAsync("noidle\n");
                //await _idleWriter.WriteAsync("idle player\n");
                //await Task.Delay(1000);

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

        }

        public async Task<CommandResult> MpdIdleQueryStatus()
        {
            MpcProgress?.Invoke(this, "[Background] Querying status...");

            CommandResult result = await MpdIdleSendCommand("status");
            if (result.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing status...");
                result.IsSuccess = await ParseStatus(result.ResultText);

                MpcProgress?.Invoke(this, "[Background] Status updated.");
            }

            return result;
        }

        public async Task<CommandResult> MpdIdleQueryCurrentSong()
        {
            MpcProgress?.Invoke(this, "[Background] Querying current song info...");

            CommandResult result = await MpdIdleSendCommand("currentsong");
            if (result.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing current song info...");
                result.IsSuccess = await ParseCurrentSong(result.ResultText);

                MpcProgress?.Invoke(this, "[Background] Current song info updated.");
            }

            return result;
        }

        public async Task<CommandResult> MpdIdleQueryCurrentQueue()
        {
            MpcProgress?.Invoke(this, "[Background] Querying queue...");

            CommandResult result = await MpdIdleSendCommand("playlistinfo");
            if (result.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing queue...");
                result.IsSuccess = await ParsePlaylistInfo(result.ResultText);

                MpcProgress?.Invoke(this, "[Background] Queue updated.");
            }

            return result;
        }

        public async Task<CommandResult> MpdIdleQueryPlaylists()
        {
            MpcProgress?.Invoke(this, "[Background] Querying playlists...");

            CommandResult result = await MpdIdleSendCommand("listplaylists");
            if (result.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing playlists...");
                result.IsSuccess = await ParsePlaylists(result.ResultText);

                MpcProgress?.Invoke(this, "[Background] Playlists updated.");
            }

            return result;
        }

        public async Task<CommandResult> MpdIdleQueryListAll()
        {
            MpcProgress?.Invoke(this, "[Background] Querying files and directories...");

            CommandResult result = await MpdIdleSendCommand("listall");
            if (result.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing files and directories...");
                result.IsSuccess = await ParseListAll(result.ResultText);

                MpcProgress?.Invoke(this, "[Background] Files and directories updated.");
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
                StringBuilder stringBuilder = new();

                bool isAck = false;
                string ackText = "";

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

                            isAck = true;
                            ackText = line;

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

                if (isAck)
                {
                    MpdAckError?.Invoke(this, ackText + " (@idle)");
                }
                else
                {
                    await ParseSubSystemsAndRaiseChangedEvent(result);
                }
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

        private async Task<bool> ParseSubSystemsAndRaiseChangedEvent(string result)
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

                //MpcProgress?.Invoke(this, "");

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

        public async Task<bool> MpdCommandConnectionStart(string host, int port, string password)
        {
            ConnectionResult r = await MpdCommandConnect(host, port);

            if (r.IsSuccess)
            {
                CommandResult d = await MpdCommandSendPassword(password);

                if (d.IsSuccess)
                {
                    // ここでIdleにして、以降はnoidle + cmd + idleの組み合わせでやる。
                    // ただし、実際にはidleのあとReadしていないからタイムアウトで切断されてしまう模様。

                    d = await MpdSendIdle();

                    return d.IsSuccess;
                }
            }

            return false;
        }

        public async Task<ConnectionResult> MpdCommandConnect(string host, int port)
        {
            ConnectionResult result = new();

            IsMpdCommandConnected = false;

            _commandConnection = new TcpClient();

            _host = host;
            _port = port;

            DebugCommandOutput?.Invoke(this, "TCP Command Connection: Connecting." + "\n" + "\n");

            ConnectionState = ConnectionStatus.Connecting;

            try
            {
                await _commandConnection.ConnectAsync(_host, _port);

                // TODO:
                if (_commandConnection.Client == null)
                {
                    Debug.WriteLine("_commandConnection.Client == null. " + host + " " + port.ToString());

                    result.ErrorMessage = "_commandConnection.Client == null";

                    DebugCommandOutput?.Invoke(this, "TCP Command Connection: Error while connecting. Fail to connect... " + "\n" + "\n");

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "TCP connection error...");

                    return result;
                }

                if (_commandConnection.Client.Connected)
                {
                    DebugCommandOutput?.Invoke(this, "TCP Command Connection: Connection established." + "\n" + "\n");

                    ConnectionState = ConnectionStatus.Connected;

                    var tcpStream = _commandConnection.GetStream();
                    //tcpStream.ReadTimeout = System.Threading.Timeout.Infinite;
                    //
                    tcpStream.ReadTimeout = 2000;

                    _commandReader = new StreamReader(tcpStream);
                    _commandWriter = new StreamWriter(tcpStream)
                    {
                        AutoFlush = true
                    };

                    string response = await _commandReader.ReadLineAsync();

                    if (response.StartsWith("OK MPD "))
                    {
                        MpdVerText = response.Replace("OK MPD ", string.Empty).Trim();

                        DebugCommandOutput?.Invoke(this, "<<<<" + response.Trim() + "\n" + "\n");

                        IsMpdCommandConnected = true;

                        result.IsSuccess = true;

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
            catch (SocketException e)
            {
                // TODO: Test.

                //e.SocketErrorCode

                DebugCommandOutput?.Invoke(this, "TCP Command Connection: Error while connecting. Fail to connect: " + e.Message + "\n" + "\n");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }
            catch (Exception e)
            {
                DebugCommandOutput?.Invoke(this, "TCP Command Connection: Error while connecting. Fail to connect (Exception): " + e.Message + "\n" + "\n");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }

            return result;
        }

        public async Task<CommandResult> MpdCommandSendPassword(string password = "")
        {
            _password = password;

            CommandResult ret = new();

            if (string.IsNullOrEmpty(password))
            {
                ret.IsSuccess = true;
                ret.ResultText = "OK";
                ret.ErrorMessage = "";

                return ret;
            }

            string cmd = "password " + password + "\n";

            return await MpdSendCommand(cmd);

        }

        // TODO: Rename MpdSendCommand to MpdCommandSendCommand
        private async Task<CommandResult> MpdSendCommand(string cmd, bool isAutoIdling = false)
        {
            CommandResult ret = new();

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

            // WriteAsync
            try
            {
                IsBusy?.Invoke(this, true);

                if (cmd.Trim().StartsWith("idle"))
                {
                    //DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");
                    Task nowait = Task.Run(() => DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n"));

                    await _commandWriter.WriteAsync(cmd.Trim() + "\n");

                    if (!isAutoIdling)
                    {
                        ret.IsSuccess = true;

                        IsBusy?.Invoke(this, false);
                        return ret;
                    }
                }
                else
                {
                    string cmdDummy = cmd;
                    if (cmd.StartsWith("password "))
                        cmdDummy = "password ****";

                    cmdDummy = cmdDummy.Trim().Replace("\n", "\n" + ">>>>");

                    Task nowait;
                    if (isAutoIdling)
                        //DebugCommandOutput?.Invoke(this, ">>>>" + "noidle\n>>>>" + cmdDummy.Trim() + "\n>>>>idle player" + "\n" + "\n");
                        nowait = Task.Run(() => DebugCommandOutput?.Invoke(this, ">>>>" + "noidle\n>>>>" + cmdDummy.Trim() + "\n>>>>idle player" + "\n" + "\n"));
                    else
                        //DebugCommandOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");
                        nowait = Task.Run(() => DebugCommandOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n"));

                    if (isAutoIdling)
                        await _commandWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n" + "idle player\n");
                    else
                        await _commandWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                // Could be application shutdopwn.
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error@{0}, Reason:{1}, Data:{2}, {3} Exception: {4} {5}", "WriteAsync@MpdSendCommand", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    // タイムアウトしていたらここで「も」エラーになる模様。

                    IsMpdCommandConnected = false;

                    DebugCommandOutput?.Invoke(this, string.Format("Reconnecting... " + Environment.NewLine + Environment.NewLine));

                    try
                    {
                        //_commandConnection.Client.Shutdown(SocketShutdown.Both);
                        _commandConnection.Close();
                    }
                    catch { }

                    ConnectionResult newCon = await MpdCommandConnect(_host,_port);

                    if (newCon.IsSuccess)
                    {
                        CommandResult d = await MpdCommandSendPassword(_password);

                        if (d.IsSuccess)
                        {
                            d = await MpdSendCommand("idle player");

                            if (d.IsSuccess)
                            {
                                DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Success. @IOExceptionOfWriteAsync" + Environment.NewLine + Environment.NewLine));

                                ret = await MpdSendCommand(cmd, isAutoIdling);
                            }
                        }
                    }
                    else
                    {
                        DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Failed. " + Environment.NewLine + Environment.NewLine));

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "The connection (command) has been terminated (IOException): " + e.Message);
                    }
                }

                IsBusy?.Invoke(this, false);
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

                IsBusy?.Invoke(this, false);
                return ret;
            }

            if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
            {
                return ret;
            }

            // ReadLineAsync
            try
            {
                IsBusy?.Invoke(this, true);

                StringBuilder stringBuilder = new();

                bool isDoubleOk = false;
                bool isAck = false;
                string ackText = "";
                bool isNullReturn = false;

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

                            ret.ErrorMessage = line;
                            ackText = line;
                            isAck = true;

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
                        }
                    }
                    else
                    {
                        isNullReturn = true;

                        break;
                    }
                }

                if (isNullReturn)
                {
                    Debug.WriteLine("@MpdSendCommand ReadLineAsync isNullReturn");

                    DebugCommandOutput?.Invoke(this, string.Format("################ Error @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "ReadLineAsync received null data", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                    ret.ResultText = stringBuilder.ToString();
                    ret.ErrorMessage = "ReadLineAsync@MpdSendCommand received null data";

                    // タイムアウトしていたらここで「も」エラーになる模様。

                    if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                    {
                        IsBusy?.Invoke(this, false);
                        return ret;
                    }

                    IsMpdCommandConnected = false;

                    DebugCommandOutput?.Invoke(this, string.Format("Reconnecting... " + Environment.NewLine + Environment.NewLine));

                    try
                    {
                        //_commandConnection.Client.Shutdown(SocketShutdown.Both);
                        _commandConnection.Close();
                    }
                    catch { }

                    ConnectionResult newCon = await MpdCommandConnect(_host, _port);

                    if (newCon.IsSuccess)
                    {
                        CommandResult d = await MpdCommandSendPassword(_password);

                        if (d.IsSuccess)
                        {
                            d = await MpdSendCommand("idle player");

                            if (d.IsSuccess)
                            {
                                DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Success. @isNullReturn" + Environment.NewLine + Environment.NewLine));

                                ret = await MpdSendCommand(cmd, isAutoIdling);
                            }
                        }
                    }
                    else
                    {
                        // 
                        DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Failed. " + Environment.NewLine + Environment.NewLine));

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "The connection (command) has been terminated (null return).");
                    }

                    IsBusy?.Invoke(this, false);
                    return ret;
                }
                else
                {
                    //DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");
                    Task nowait = Task.Run(() => DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n"));

                    if (isAck)
                        nowait = Task.Run(() => MpdAckError?.Invoke(this, ackText + " (@MSC)"));

                    ret.ResultText = stringBuilder.ToString();

                    IsBusy?.Invoke(this, false);
                    return ret;
                }

            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "InvalidOperationException (Most likely the connection is overloaded)", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                //ConnectionError?.Invoke(this, "The connection (command) has been terminated. Most likely the connection has been overloaded.");

                //TODO: testing. null return?
                //_commandReader.DiscardBufferedData();


                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。
                Debug.WriteLine("IOException@MpdSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                // Could be application shutdopwn.
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    // タイムアウトしていたらここで「も」エラーになる模様。

                    IsMpdCommandConnected = false;

                    DebugCommandOutput?.Invoke(this, string.Format("Reconnecting... " + Environment.NewLine + Environment.NewLine));

                    try
                    {
                        //_commandConnection.Client.Shutdown(SocketShutdown.Both);
                        _commandConnection.Close();
                    }
                    catch { }

                    ConnectionResult newCon = await MpdCommandConnect(_host, _port);

                    if (newCon.IsSuccess)
                    {
                        CommandResult d = await MpdCommandSendPassword(_password);

                        if (d.IsSuccess)
                        {
                            d = await MpdSendCommand("idle player");

                            if (d.IsSuccess)
                            {
                                DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Success. @IOExceptionOfReadLineAsync" + Environment.NewLine + Environment.NewLine));

                                ret = await MpdSendCommand(cmd, isAutoIdling);
                            }
                        }
                    }
                    else
                    {
                        // Unable to read data from the transport connection: 既に接続済みのソケットに対して接続を要求しました。.

                        DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Failed. " + Environment.NewLine + Environment.NewLine));

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "The connection (command) has been terminated (IOException): " + e.Message);
                    }
                }

                IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                IsBusy?.Invoke(this, false);
                return ret;
            }

        }

        #endregion

        #region == Command Connection's MPD Commands with results other than OK == 

        public async Task<CommandResult> MpdSendIdle()
        {
            return await MpdSendCommand("idle player");
        }

        public async Task<CommandResult> MpdSendNoIdle()
        {
            return await MpdSendCommand("noidle");
        }

        public async Task<CommandResult> MpdQueryStatus(bool autoIdling = true)
        {
            CommandResult result = await MpdSendCommand("status", autoIdling);
            if (result.IsSuccess)
            {
                result.IsSuccess = await ParseStatus(result.ResultText);
            }

            return result;
        }

        public async Task<CommandResult> MpdQueryCurrentSong(bool autoIdling = true)
        {
            CommandResult result = await MpdSendCommand("currentsong", autoIdling);
            if (result.IsSuccess)
            {
                result.IsSuccess = await ParseCurrentSong(result.ResultText);
            }

            return result;
        }

        public async Task<CommandResult> MpdQueryCurrentQueue(bool autoIdling = true)
        {
            CommandResult result = await MpdSendCommand("playlistinfo", autoIdling);
            if (result.IsSuccess)
            {
                result.IsSuccess = await ParsePlaylistInfo(result.ResultText);
            }

            return result;
        }

        public async Task<CommandResult> MpdQueryPlaylists(bool autoIdling = true)
        {
            CommandResult result = await MpdSendCommand("listplaylists", autoIdling);
            if (result.IsSuccess)
            {
                result.IsSuccess = await ParsePlaylists(result.ResultText);
            }

            return result;
        }

        public async Task<CommandResult> MpdQueryListAll(bool autoIdling = true)
        {
            MpcProgress?.Invoke(this, "[Background] Querying files and directories...");

            CommandResult result = await MpdSendCommand("listall", autoIdling);
            if (result.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing files and directories...");
                result.IsSuccess = await ParseListAll(result.ResultText);
                MpcProgress?.Invoke(this, "[Background] Files and directories updated.");
            }

            return result;
        }

        public async Task<CommandSearchResult> MpdSearch(string queryTag, string queryShiki, string queryValue, bool autoIdling = true)
        {
            MpcProgress?.Invoke(this, "[Background] Searching...");

            CommandSearchResult result = new();

            Dispatcher.UIThread.Post(() =>
            {
                SearchResult.Clear();
            });
            
            if (string.IsNullOrEmpty(queryTag) || string.IsNullOrEmpty(queryValue) || string.IsNullOrEmpty(queryShiki))
            {
                result.IsSuccess = false;
                return result;
            }

            var expression = queryTag + " " + queryShiki + " \'" + Regex.Escape(queryValue) + "\'";

            string cmd = "search \"(" + expression + ")\"\n";

            CommandResult cm = await MpdSendCommand(cmd, autoIdling);
            if (cm.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing search result...");
                if (await ParseSearchResult(cm.ResultText))
                {
                    result.IsSuccess = true;

                    result.SearchResult = this.SearchResult;

                    MpcProgress?.Invoke(this, "[Background] Search result updated.");
                }
            }

            return result;
        }

        public async Task<CommandPlaylistResult> MpdQueryPlaylistSongs(string playlistName, bool autoIdling = true)
        {
            CommandPlaylistResult result = new();

            if (string.IsNullOrEmpty(playlistName))
            {
                result.IsSuccess = false;
                return result;
            }

            MpcProgress?.Invoke(this, "[Background] Querying playlist info...");

            playlistName = Regex.Escape(playlistName);

            CommandResult cm = await MpdSendCommand("listplaylistinfo \"" + playlistName + "\"", autoIdling);
            if (cm.IsSuccess)
            {
                MpcProgress?.Invoke(this, "[Background] Parsing playlist info...");

                result.IsSuccess = cm.IsSuccess;
                result.PlaylistSongs = ParsePlaylistSongsResult(cm.ResultText);

                MpcProgress?.Invoke(this, "[Background] Playlist info is updated.");
            }

            return result;
        }

        public async Task<CommandResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture)
        {
            CommandResult f = new();
            if (string.IsNullOrEmpty(uri))
            {
                f.IsSuccess = false;
                return f;
            }

            BinaryDownloader hoge = new();

            bool asdf = await hoge.MpdBinaryConnectionStart(MpdHost,MpdPort,MpdPassword);

            CommandResult b = new();

            if (asdf)
            {
                b =  await hoge.MpdQueryAlbumArt(uri, isUsingReadpicture);

                if (b.IsSuccess)
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        _albumCover = hoge.AlbumCover;
                    });

                    await Task.Delay(1300);

                    //Debug.WriteLine("AlbumArt Donwloaded... ");

                    MpdAlbumArtChanged?.Invoke(this);
                }
                else
                {
                    //Debug.WriteLine("why... " + b.ErrorMessage);
                }
            }
            else
            {
                Debug.WriteLine("damn... ");
            }

            hoge.MpdBinaryConnectionDisconnect();

            return b;
        }
        
        #endregion

        #region == Command Connection's MPD Commands with boolean result ==

        public async Task<CommandResult> MpdSendUpdate()
        {
            CommandResult result = await MpdSendCommand("update", true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackPlay(int volume, string songId = "")
        {
            string cmd = "play";

            if (songId != "")
            {
                cmd = "playid " + songId;
            }

            if (MpdStatus.MpdVolumeIsSet)
            {
                CommandResult result = await MpdSendCommand(cmd, true);

                return result;
            }
            else
            {
                string cmdList = "command_list_begin" + "\n";
                cmdList = cmdList + cmd + "\n";
                cmdList = cmdList + "setvol " + volume.ToString() + "\n";
                cmdList = cmdList + "command_list_end" + "\n";

                CommandResult result = await MpdSendCommand(cmdList, true);

                return result;
            }
        }

        public async Task<CommandResult> MpdPlaybackPause()
        {
            CommandResult result = await MpdSendCommand("pause 1", true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackResume(int volume)
        {
            if (MpdStatus.MpdVolumeIsSet)
            {
                CommandResult result = await MpdSendCommand("pause 0", true);

                return result;
            }
            else
            {
                string cmd = "command_list_begin" + "\n";
                cmd += "pause 0\n";
                cmd = cmd + "setvol " + volume.ToString() + "\n";
                cmd = cmd + "command_list_end" + "\n";

                CommandResult result = await MpdSendCommand(cmd, true);

                return result;
            }
        }

        public async Task<CommandResult> MpdPlaybackStop()
        {
            CommandResult result = await MpdSendCommand("stop", true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackNext(int volume)
        {
            if (MpdStatus.MpdVolumeIsSet)
            {
                CommandResult result = await MpdSendCommand("next", true);

                return result;
            }
            else
            {
                string cmd = "command_list_begin" + "\n";
                cmd += "next\n";
                cmd = cmd + "setvol " + volume.ToString() + "\n";
                cmd = cmd + "command_list_end" + "\n";

                CommandResult result = await MpdSendCommand(cmd, true);

                return result;
            }
        }

        public async Task<CommandResult> MpdPlaybackPrev(int volume)
        {
            if (MpdStatus.MpdVolumeIsSet)
            {
                CommandResult result = await MpdSendCommand("previous", true);

                return result;
            }
            else
            {
                string cmd = "command_list_begin" + "\n";
                cmd += "previous\n";
                cmd = cmd + "setvol " + volume.ToString() + "\n";
                cmd = cmd + "command_list_end" + "\n";

                CommandResult result = await MpdSendCommand(cmd, true);

                return result;
            }
        }

        public async Task<CommandResult> MpdSetVolume(int v)
        {
            if (v == _status.MpdVolume) 
            {
                CommandResult f = new()
                {
                    IsSuccess = true
                };
                return f;
            }

            CommandResult result = await MpdSendCommand("setvol " + v.ToString(), true);

            return result;
        }

        public async Task<CommandResult> MpdPlaybackSeek(string songId, int seekTime)
        {
            if ((songId == "") || (seekTime == 0)) 
            {
                CommandResult f = new()
                {
                    IsSuccess = true
                };
                return f;
            }

            CommandResult result = await MpdSendCommand("seekid " + songId + " " + seekTime.ToString(), true);

            return result;
        }

        public async Task<CommandResult> MpdSetRepeat(bool on)
        {
            if (_status.MpdRepeat == on)
            {
                CommandResult f = new()
                {
                    IsSuccess = true
                };
                return f;
            }

            string cmd;
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
                CommandResult f = new()
                {
                    IsSuccess = true
                };
                return f;
            }

            string cmd;
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
                CommandResult f = new()
                {
                    IsSuccess = true
                };
                return f;
            }

            string cmd;
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
                CommandResult f = new()
                {
                    IsSuccess = true
                };
                return f;
            }

            string cmd;
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
            CommandResult result = await MpdSendCommand("clear", true);

            return result;
        }

        public async Task<CommandResult> MpdSave(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            playlistName = Regex.Escape(playlistName);

            CommandResult result = await MpdSendCommand("save \"" + playlistName + "\"", true);

            return result;
        }

        public async Task<CommandResult> MpdAdd(string uri)
        {
            if (string.IsNullOrEmpty(uri))
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            uri = Regex.Escape(uri);

            CommandResult result = await MpdSendCommand("add \"" + uri + "\"", true);

            return result;
        }

        public async Task<CommandResult> MpdAdd(List<string> uris)
        {
            if (uris.Count < 1)
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            string cmd = "command_list_begin" + "\n";
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
            if (ids.Count < 1)
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            string cmd = "command_list_begin" + "\n";
            foreach (var id in ids)
            {
                cmd = cmd + "deleteid " + id + "\n";
            }
            cmd = cmd + "command_list_end" + "\n";

            return await MpdSendCommand(cmd, true);
        }

        public async Task<CommandResult> MpdDeleteId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            string cmd = "deleteid " + id + "\n";

            return await MpdSendCommand(cmd, true);
        }

        public async Task<CommandResult> MpdMoveId(Dictionary<string, string> IdToNewPosPair)
        {
            if (IdToNewPosPair == null)
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }
            if (IdToNewPosPair.Count < 1)
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            string cmd = "command_list_begin" + "\n";
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
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

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
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            playlistName = Regex.Escape(playlistName);

            string cmd = "load \"" + playlistName + "\"";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdRenamePlaylist(string playlistName, string newPlaylistName)
        {
            if (string.IsNullOrEmpty(playlistName) || string.IsNullOrEmpty(newPlaylistName))
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

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
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            playlistName = Regex.Escape(playlistName);

            string cmd = "rm \"" + playlistName + "\"";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        public async Task<CommandResult> MpdPlaylistAdd(string playlistName, List<string> uris)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }
            if (uris == null)
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }
            if (uris.Count < 1)
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

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

        public async Task<CommandResult> MpdPlaylistDelete(string playlistName, int pos)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            playlistName = Regex.Escape(playlistName);

            //playlistdelete {NAME} {SONGPOS}
            string cmd = "playlistdelete \"" + playlistName + "\"" + " " + pos.ToString();

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        /* NOT GOOD. Multiple deletion with SONGPO causes pos to shift.
        public async Task<CommandResult> MpdPlaylistDelete(string playlistName, List<int> posList)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }
            if (posList == null)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }
            if (posList.Count < 1)
            {
                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }

            playlistName = Regex.Escape(playlistName);

            string cmd = "command_list_begin" + "\n";
            foreach (var pos in posList)
            {
                cmd = cmd + "playlistdelete " + "\"" + playlistName + "\"" + " " + pos.ToString() + "\n";
            }
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }
        */
 
        public async Task<CommandResult> MpdPlaylistClear(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
            {
                CommandResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            playlistName = Regex.Escape(playlistName);

            //playlistclear {NAME}
            string cmd = "playlistclear \"" + playlistName + "\"";

            CommandResult result = await MpdSendCommand(cmd, true);

            return result;
        }

        #endregion

        #region == Response parser methods ==

        private Task<bool> ParseStatus(string result)
        {
            if (MpdStop) { return Task.FromResult(false); }
            if (string.IsNullOrEmpty(result)) return Task.FromResult(false);

            if (result.Trim() == "OK")
            {
                DebugCommandOutput?.Invoke(this, "################(Error) " + "An empty result (OK) returened for a status command." + Environment.NewLine + Environment.NewLine);
                DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ParseStatus", "An empty result (OK) returened for a status command.", "", Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                Debug.WriteLine("@ParseStatus: An empty result (OK)  returened for a status command.");

                return Task.FromResult(false);
            }

            List<string> resultLines = result.Split('\n').ToList();

            if (resultLines.Count == 0) return Task.FromResult(false);

            var comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> MpdStatusValues = new(comparer);

            try
            {
                IsBusy?.Invoke(this, true);

                Dispatcher.UIThread.Post(() =>
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

                            _status.MpdVolumeIsSet = true;
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
                        _status.MpdError = MpdStatusValues["error"];
                    }
                    else
                    {
                        _status.MpdError = "";
                    }

                    // TODO: more?
                });

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception@ParseStatus:" + ex.Message);

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@ParseStatus", ex.Message);
                });

                IsBusy?.Invoke(this, false);
                return Task.FromResult(false);
            }
            finally
            {
                IsBusy?.Invoke(this, false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> ParseCurrentSong(string result)
        {
            if (MpdStop) return Task.FromResult(false);

            bool isEmptyResult = false;

            if (string.IsNullOrEmpty(result))
                isEmptyResult = true;

            if (result.Trim() == "OK")
                isEmptyResult = true;

            List<string> resultLines = result.Split('\n').ToList();
            if (resultLines == null)
                isEmptyResult = true;
            if (resultLines.Count == 0)
                isEmptyResult = true;

            if (isEmptyResult)
            {
                return Task.FromResult(true);
            }

            IsBusy?.Invoke(this, true);

            try
            {
                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new(comparer);

                foreach (string value in resultLines)
                {
                    string[] StatusValuePair = value.Trim().Split(':');
                    if (StatusValuePair.Length > 1)
                    {
                        if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                        {
                            // Shouldn't be happening here except "Genre"
                            if (StatusValuePair[0].Trim() == "Genre")
                            {
                                SongValues["Genre"] = SongValues["Genre"] + "/" + value.Replace(StatusValuePair[0].Trim() + ": ", "");
                            }
                        }
                        else
                        {
                            SongValues.Add(StatusValuePair[0].Trim(), value.Replace(StatusValuePair[0].Trim() + ": ", ""));
                        }
                    }
                }

                if ((SongValues.Count > 0) && SongValues.ContainsKey("Id"))
                {
                    SongInfoEx sng = FillSongInfoEx(SongValues, -1);

                    if (sng != null)
                    {
                        if (_currentSong != null)
                        {
                            if (_currentSong.Id != sng.Id)
                                _currentSong = sng;
                        }
                    }

                    SongValues.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParseCurrentSong: " + ex.Message);

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@ParseCurrentSong", ex.Message);
                });

                return Task.FromResult(false);
            }
            finally
            {
                IsBusy?.Invoke(this, false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> ParsePlaylistInfo(string result)
        {
            if (MpdStop) return Task.FromResult(false);

            bool isEmptyResult = false;

            if (string.IsNullOrEmpty(result)) 
                isEmptyResult = true;

            if (result.Trim() == "OK") 
                isEmptyResult = true;

            List<string> resultLines = result.Split('\n').ToList();
            if (resultLines == null)
                isEmptyResult = true;
            if (resultLines.Count == 0)
                isEmptyResult = true;

            if (isEmptyResult)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    CurrentQueue.Clear();
                });

                return Task.FromResult(true);
            }

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

            /*
            try
            {

                resultLines.Clear();
                resultLines.Add("file: Slamhaus/Slamwave/06 The Trial.flac");
                resultLines.Add("Last-Modified: 2021-05-27T19:20:50Z");
                resultLines.Add("Title: The Trial");
                resultLines.Add("Artist: Slamhaus");
                resultLines.Add("Date: 2021");
                resultLines.Add("Comment: Visit https://slamhaus.bandcamp.com");
                resultLines.Add("Album: Slamwave");
                resultLines.Add("Track: 6");
                resultLines.Add("AlbumArtist: Slamhaus");
                resultLines.Add("Time: 340");
                resultLines.Add("duration: 339.504");
                resultLines.Add("Pos: 5");
                resultLines.Add("Id: 1438");
                resultLines.Add("file: Harris Heller/Synthwave/Sunset/01 - Zig the Zag.flac");
                resultLines.Add("Last-Modified: 2021-06-03T16:52:10Z");
                resultLines.Add("Album: Sunset");
                resultLines.Add("AlbumArtist: Harris Heller");
                resultLines.Add("Artist: Harris Heller");
                resultLines.Add("Date: 2021-03-05");
                resultLines.Add("Disc: 1");
                resultLines.Add("Genre: Electro");
                resultLines.Add("Genre: Dance");
                resultLines.Add("Title: Zig the Zag");
                resultLines.Add("Track: 1");
                resultLines.Add("Time: 126");
                resultLines.Add("duration: 126.250");
                resultLines.Add("Pos: 6");
                resultLines.Add("Id: 1439");
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParsePlaylistInfo: " + ex.Message);
            }
            */

            try
            {
                IsBusy?.Invoke(this, true);

                ObservableCollection<SongInfoEx> tmpQueue = new();
                /*
                if (Application.Current == null) { return Task.FromResult(false); }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    CurrentQueue.Clear();
                });
                */

                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new(comparer);

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
                                SongInfoEx sng = FillSongInfoEx(SongValues, i);

                                if (sng != null)
                                {
                                    /*
                                    if (Application.Current == null) { return Task.FromResult(false); }
                                    Application.Current.Dispatcher.Invoke(() =>
                                    {
                                        CurrentQueue.Add(sng);
                                    });
                                    */
                                    //CurrentQueue.Add(sng);
                                    tmpQueue.Add(sng);

                                    i++;

                                    MpcProgress?.Invoke(this, string.Format("[Background] Parsing queue item... ({0})", i));

                                    SongValues.Clear();
                                }
                            }
                            /*
                            if (!SongValues.ContainsKey(StatusValuePair[0].Trim()))
                            {
                                SongValues.Add(StatusValuePair[0].Trim(), value.Replace(StatusValuePair[0].Trim() + ": ", ""));
                            }
                            else
                            {
                                if (StatusValuePair[0].Trim() == "Genre")
                                {
                                    SongValues["Genre"] = SongValues["Genre"] + "/" + value.Replace(StatusValuePair[0].Trim() + ": ", "");
                                } 
                            }
                            */
                        }

                        if (!SongValues.ContainsKey(StatusValuePair[0].Trim()))
                        {
                            SongValues.Add(StatusValuePair[0].Trim(), value.Replace(StatusValuePair[0].Trim() + ": ", ""));
                        }
                        else
                        {
                            if (StatusValuePair[0].Trim() == "Genre")
                            {
                                SongValues["Genre"] = SongValues["Genre"] + "/" + value.Replace(StatusValuePair[0].Trim() + ": ", "");
                            }
                        }
                    }
                }

                if ((SongValues.Count > 0) && SongValues.ContainsKey("Id"))
                {
                    SongInfoEx sng = FillSongInfoEx(SongValues, i);

                    if (sng != null)
                    {
                        SongValues.Clear();
                        /*
                        if (Application.Current == null) { return Task.FromResult(false); }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            CurrentQueue.Add(sng);
                        });
                        */
                        //CurrentQueue.Add(sng);
                        tmpQueue.Add(sng);

                        MpcProgress?.Invoke(this, string.Format("[Background] Parsing queue item... ({0})", i+1));
                    }
                }

                // test
                MpcProgress?.Invoke(this, "[Background] Updating internal queue list...");
                
                Dispatcher.UIThread.Post(() =>
                {
                    //CurrentQueue.Clear();
                    //_queue = tmpQueue;
                    _queue = new ObservableCollection<SongInfoEx>(tmpQueue);
                });
                MpcProgress?.Invoke(this, "[Background] Internal queue list is updated.");

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception@ParsePlaylistInfo: " + ex.Message);

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@ParsePlaylistInfo", ex.Message);
                });

                return Task.FromResult(false);
            }
            finally
            {
                IsBusy?.Invoke(this, false);
            }

            return Task.FromResult(true);
        }

        private SongInfoEx FillSongInfoEx(Dictionary<string, string> SongValues, int i)
        {
            try
            {
                SongInfoEx sng = new();

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
                    sng.Duration = SongValues["duration"];
                }

                if (SongValues.ContainsKey("Pos"))
                {
                    sng.Pos = SongValues["Pos"];
                }

                if (SongValues.ContainsKey("file"))
                {
                    sng.File = SongValues["file"];
                }

                // for sorting.
                sng.Index = i;
                /*
                if (i < 0) 
                { 
                    // -1 means we are not parsing queue but parsing currentsong.<Oppes currentsong does not return pos..
                    if (string.IsNullOrEmpty(sng.Pos))
                    {
                        int tmpi = -1;
                        try
                        {
                            tmpi = Int32.Parse(sng.Pos);
                            sng.Index = tmpi;
                        }
                        catch{}
                    }
                }
                */

                //
                if (sng.Id == _status.MpdSongID)
                {
                    _currentSong = sng;
                }

                return sng;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error@FillSongInfoEx: " + e.ToString());

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@FillSongInfoEx", e.Message);
                });

                return null;
            }
        }

        private Task<bool> ParsePlaylists(string result)
        {
            bool isEmptyResult = false;

            if (string.IsNullOrEmpty(result)) 
                isEmptyResult = true;

            List<string> resultLines = result.Split('\n').ToList();

            if (resultLines.Count == 0) 
                isEmptyResult = true;

            if (isEmptyResult)
            {
                Dispatcher.UIThread.Post(() =>
                {
                    Playlists.Clear();
                });

                return Task.FromResult(true);
            }

            try
            {
                IsBusy?.Invoke(this, true);

                ObservableCollection<Playlist> tmpPlaylists = new();

                Playlist pl = null;

                foreach (string value in resultLines)
                {
                    if (value.StartsWith("playlist:"))
                    {

                        pl = new Playlist
                        {
                            Name = value.Replace("playlist: ", "")
                        };

                        tmpPlaylists.Add(pl);
                    }
                    else if (value.StartsWith("Last-Modified: "))
                    {
                        if (pl != null)
                            pl.LastModified = value.Replace("Last-Modified: ", "");
                    }
                    else if (value.StartsWith("OK"))
                    {
                        // Ignoring.
                    }
                }

                Dispatcher.UIThread.Post(() =>
                {
                    _playlists = new ObservableCollection<Playlist>(tmpPlaylists);
                });

                /*
                if (Application.Current == null) { return Task.FromResult(false); }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Playlists.Clear();

                    // Tmp list for sorting.
                    //List<string> slTmp = new List<string>();

                    Playlist pl = null;

                    foreach (string value in resultLines)
                    {
                        if (value.StartsWith("playlist:"))
                        {

                            pl = new Playlist
                            {
                                Name = value.Replace("playlist: ", "")
                            };

                            Playlists.Add(pl);
                        }
                        else if (value.StartsWith("Last-Modified: "))
                        {
                            if (pl != null)
                                pl.LastModified = value.Replace("Last-Modified: ", "");
                        }
                        else if (value.StartsWith("OK"))
                        {
                            // Ignoring.
                        }
                    }
                });
                */
            }
            catch (Exception e)
            {
                Debug.WriteLine("Error@ParsePlaylists: " + e.ToString());

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@ParsePlaylists", e.Message);
                });

                IsBusy?.Invoke(this, false);
                return Task.FromResult(false);
            }
            finally
            {
                IsBusy?.Invoke(this, false);
            }

            return Task.FromResult(true);
        }

        private Task<bool> ParseListAll(string result)
        {
            if (MpdStop) return Task.FromResult(false);

            if (string.IsNullOrEmpty(result)) return Task.FromResult(false);

            List<string> resultLines = result.Split('\n').ToList();

            if (resultLines.Count == 0) return Task.FromResult(false);

            try
            {
                IsBusy?.Invoke(this, true);

                Dispatcher.UIThread.Post(() =>
                {
                    LocalFiles.Clear();
                    LocalDirectories.Clear();
                });

                SongFile song = null;

                int i = 0;

                foreach (string value in resultLines)
                {
                    if (value.StartsWith("directory:"))
                    {
                        Dispatcher.UIThread.Post(() =>
                        {
                            LocalDirectories.Add(value.Replace("directory: ", ""));
                        });

                        //LocalDirectories.Add(value.Replace("directory: ", ""));

                        MpcProgress?.Invoke(this, string.Format("[Background] Parsing files and directories ({0})...", i));
                    }
                    else if (value.StartsWith("file:"))
                    {
                        song = new SongFile
                        {
                            File = value.Replace("file: ", "")
                        };

                        Dispatcher.UIThread.Post(() =>
                        {
                            LocalFiles.Add(song);
                        });

                        //LocalFiles.Add(song);

                        i++;

                        MpcProgress?.Invoke(this, string.Format("[Background] Parsing files and directories ({0})...", i));
                    }
                    else if ((value.StartsWith("OK")))
                    {
                        // Ignoring.
                    }
                    else
                    {
                        //Debug.WriteLine(value);
                    }
                }

                MpcProgress?.Invoke(this, "[Background] Parsing files and directories is done.");

            }
            catch (Exception e)
            {
                Debug.WriteLine("Error@ParseListAll: " + e.ToString());

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@ParseListAll", e.Message);
                });

                IsBusy?.Invoke(this, false);
                return Task.FromResult(false); ;
            }
            finally
            {
                IsBusy?.Invoke(this, false);
            }

            return Task.FromResult(true); ;
        }

        private Task<bool> ParseSearchResult(string result)
        {
            if (MpdStop) return Task.FromResult(false);

            Dispatcher.UIThread.Post(() =>
            {
                SearchResult.Clear();
            });

            if (string.IsNullOrEmpty(result)) return Task.FromResult(false);

            //if (result.Trim() == "OK") return true;

            List<string> resultLines = result.Split('\n').ToList();
            if (resultLines == null) return Task.FromResult(true);
            if (resultLines.Count == 0) return Task.FromResult(true);



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
                IsBusy?.Invoke(this, true);

                var comparer = StringComparer.OrdinalIgnoreCase;
                Dictionary<string, string> SongValues = new(comparer);

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
                                SongInfo sng = FillSongInfo(SongValues, i);

                                i++;

                                SongValues.Clear();

                                Dispatcher.UIThread.Post(() =>
                                {
                                    SearchResult.Add(sng);
                                });

                                MpcProgress?.Invoke(this, string.Format("[Background] Parsing search result item... ({0})", i));
                            }

                            // start over
                            //SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }
                        /*
                        else
                        {
                            SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }
                        */
                        if (!SongValues.ContainsKey(ValuePair[0].Trim()))
                        {
                            SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }
                        else
                        {
                            if (ValuePair[0].Trim() == "Genre")
                            {
                                SongValues["Genre"] = SongValues["Genre"] + "/" + line.Replace(ValuePair[0].Trim() + ": ", "");
                            }
                        }
                    }
                }

                // last one
                if ((SongValues.Count > 0) && SongValues.ContainsKey("file"))
                {
                    SongInfo sng = FillSongInfo(SongValues, i);

                    SongValues.Clear();

                    Dispatcher.UIThread.Post(() =>
                    {
                        SearchResult.Add(sng);
                    });

                    MpcProgress?.Invoke(this, string.Format("[Background] Parsing search result item... ({0})", i+1));
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParseSearchResult: " + ex.Message);

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@ParseSearchResult", ex.Message);
                });

                IsBusy?.Invoke(this, false);
                return Task.FromResult(false);
            }
            finally
            {
                IsBusy?.Invoke(this, false);

                MpcProgress?.Invoke(this, "[Background] Search result loaded.");
            }

            return Task.FromResult(true);
        }

        private ObservableCollection<SongInfo> ParsePlaylistSongsResult(string result)
        {
            ObservableCollection<SongInfo> songList = new();

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
                Dictionary<string, string> SongValues = new(comparer);

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
                                SongInfo sng = FillSongInfo(SongValues, i);

                                i++;

                                SongValues.Clear();

                                //if (Application.Current == null) { return songList; }
                                //Application.Current.Dispatcher.Invoke(() =>
                                //{
                                    songList.Add(sng);
                                //});

                                MpcProgress?.Invoke(this, string.Format("[Background] Parsing playlist item... ({0})", i));
                            }

                            // start over
                            //SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }
                        /*
                        else
                        {
                            SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }
                        */
                        if (!SongValues.ContainsKey(ValuePair[0].Trim()))
                        {
                            SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].Trim() + ": ", ""));
                        }
                        else
                        {
                            if (ValuePair[0].Trim() == "Genre")
                            {
                                SongValues["Genre"] = SongValues["Genre"] + "/" + line.Replace(ValuePair[0].Trim() + ": ", "");
                            }
                        }
                    }
                }

                // last one
                if ((SongValues.Count > 0) && SongValues.ContainsKey("file"))
                {
                    SongInfo sng = FillSongInfo(SongValues, i);

                    SongValues.Clear();

                    //if (Application.Current == null) { return songList; }
                    //Application.Current.Dispatcher.Invoke(() =>
                    //{
                        songList.Add(sng);
                    //});
                    MpcProgress?.Invoke(this, string.Format("[Background] Parsing playlist item... ({0})", i+1));
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParsePlaylistSongsResult: " + ex.Message);

                Dispatcher.UIThread.Post(() =>
                {
                    App.AppendErrorLog("Exception@MPC@ParsePlaylistSongsResult", ex.Message);
                });

                return songList;
            }

            return songList;
        }

        private static SongInfo FillSongInfo(Dictionary<string, string> SongValues, int i)
        {

            SongInfo sng = new();

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
                sng.Duration = SongValues["duration"];
            }

            if (SongValues.ContainsKey("file"))
            {
                sng.File = SongValues["file"];
            }

            // for sorting. (and playlist pos)
            sng.Index = i;

            return sng;
        }

        #endregion

        public void MpdDisconnect()
        {
            try
            {
                IsBusy?.Invoke(this, true);

                ConnectionState = ConnectionStatus.Disconnecting;

                if (_commandConnection.Client != null)
                    _commandConnection.Client.Shutdown(SocketShutdown.Both);
                _commandConnection.Close();
            }
            catch { }
            finally
            {
                IsBusy?.Invoke(this, false);
                ConnectionState = ConnectionStatus.DisconnectedByUser;
            }

            try
            {
                IsBusy?.Invoke(this, true);

                ConnectionState = ConnectionStatus.Disconnecting;

                if (_idleConnection.Client != null)
                    _idleConnection.Client.Shutdown(SocketShutdown.Both);
                _idleConnection.Close();
            }
            catch { }
            finally
            {
                IsBusy?.Invoke(this, false);
                ConnectionState = ConnectionStatus.DisconnectedByUser;
            }

        }

    }
}

