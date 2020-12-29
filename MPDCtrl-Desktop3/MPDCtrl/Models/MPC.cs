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


        public bool IsMpdCommandConnected { get; set; }

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
        private StreamReader _idleReader;
        private StreamWriter _idleWriter;


        public MPC()
        {
            _connectionStatus = ConnectionStatus.NeverConnected;

            IsMpdCommandConnected = false;
        }

        public async void MpdConnect(string host, int port)
        {
            IsMpdCommandConnected = false;

            _host = host;
            _port = port;

            DebugOutput?.Invoke(this, "TCP Command Connection: Trying to connect." + Environment.NewLine + Environment.NewLine);

            _connectionStatus = ConnectionStatus.Connecting;

            try
            {
                await _commandConnection.ConnectAsync(_host, _port);

                if (_commandConnection.Client.Connected)
                {
                    DebugOutput?.Invoke(this, "TCP Command Connection: Connection established." + Environment.NewLine + Environment.NewLine);

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

                        DebugOutput?.Invoke(this, response.Trim() + Environment.NewLine + Environment.NewLine);

                        IsMpdCommandConnected = true;

                        MpdConnected?.Invoke(this);

                        // Done for now.
                    }
                    else
                    {
                        DebugOutput?.Invoke(this, "TCP Command Connection: MPD did not respond with proper respose." + Environment.NewLine + Environment.NewLine);

                        _connectionStatus = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "TCP connection error: MPD did not respond with proper respose.");
                    }
                }
                else
                {
                    //?

                    Debug.WriteLine("**** !client.Client.Connected");

                    DebugOutput?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected." + Environment.NewLine + Environment.NewLine);

                    _connectionStatus = ConnectionStatus.NeverConnected;

                    ConnectionError?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected.");
                }
            }
            catch (Exception e)
            {
                // TODO: Test.

                DebugOutput?.Invoke(this, "TCP Command Connection: Error while connecting. Fail to connect: " + e.Message + Environment.NewLine + Environment.NewLine);

                _connectionStatus = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }

        }

        public async Task<CommandResult> MpdSendPassword(string password = "")
        {
            CommandResult ret = new CommandResult();

            if (string.IsNullOrEmpty(password))
            {
                ret.IsSuccess = true;
                ret.ResultText = "OK";//Or OK
                ret.ErrorMessage = "";

                return ret;
            }

            string cmd = "password " + password + "\n";

            DebugOutput?.Invoke(this, ">>>>Password ******" + Environment.NewLine + Environment.NewLine);

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
                                    ret.IsSuccess = true;
                                    ret.ErrorMessage = "";

                                    DebugOutput?.Invoke(this, "<<<<" + response.Trim().Replace("\r\n", Environment.NewLine + "<<<<")  + Environment.NewLine + Environment.NewLine);

                                }
                                catch (Exception e)
                                {
                                    DebugOutput?.Invoke(this, "<<<<(Exception)" + Environment.NewLine + e.Message.Trim() + Environment.NewLine + Environment.NewLine);

                                    ret.IsSuccess = false;
                                    ret.ErrorMessage = e.Message;

                                    return ret;
                                }
                            }
                            catch (Exception e)
                            {
                                DebugOutput?.Invoke(this, "<<<<(Exception)" + Environment.NewLine + e.Message.Trim() + Environment.NewLine + Environment.NewLine);

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
        }

        public async Task<CommandResult> MpdSendCommand(string cmd)
        {
            CommandResult ret = new CommandResult();

            if (cmd.Trim() == "idle")
            {
                DebugOutput?.Invoke(this, ">>>>" + cmd.Trim() + Environment.NewLine + Environment.NewLine);
            }
            else
            {
                DebugOutput?.Invoke(this, ">>>>" + "noidle" + Environment.NewLine);
                DebugOutput?.Invoke(this, ">>>>" + cmd.Trim() + Environment.NewLine + Environment.NewLine);
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
                                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}", cmd.Trim(), e.Message));

                                        return ret;
                                    }

                                    ret.IsSuccess = true;
                                    ret.ErrorMessage = "";
                                    return ret;
                                }
                                else
                                {
                                    // noidleを付けて送る。
                                    await _commandWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n");
                                    //await _commandWriter.WriteAsync(cmd.Trim() + "\n");

                                    try
                                    {
                                        bool noidleOK = false;

                                        StringBuilder stringBuilder = new StringBuilder();

                                        while (true)
                                        {
                                            string line = await _commandReader.ReadLineAsync();

                                            if (line != null)
                                            {
                                                if (line.StartsWith("ACK"))
                                                {
                                                    if (!string.IsNullOrEmpty(line))
                                                        stringBuilder.Append(line + Environment.NewLine);

                                                    break;
                                                }
                                                else if (line.StartsWith("OK"))
                                                {
                                                    // noidle分のOKは既に受け取ったのでブレイク。
                                                    if (noidleOK)
                                                    {
                                                        if (!string.IsNullOrEmpty(line))
                                                            stringBuilder.Append(line + Environment.NewLine);

                                                        break;
                                                    }

                                                    // noidle分のOKを受け取った。
                                                    noidleOK = true;
                                                }
                                                else if (line.StartsWith("changed: "))
                                                {
                                                    // noidleでついてくるかもしれないchanged. idleConnectionで見ているからここでは無視。
                                                    Debug.WriteLine("changed: " + line);

                                                }
                                                else
                                                {
                                                    if (!string.IsNullOrEmpty(line))
                                                    {
                                                        //Debug.WriteLine("ELSE: " + line);

                                                        stringBuilder.Append(line + Environment.NewLine);
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


                                        Debug.WriteLine(Environment.NewLine + Environment.NewLine);

                                        ret.ResultText = stringBuilder.ToString();
                                        ret.IsSuccess = true;
                                        ret.ErrorMessage = "";

                                        //DebugOutput?.Invoke(this, string.Format("<<<<Received (cmd={0}):\n{1}", cmd.Trim(), ret.ResultText));
                                        DebugOutput?.Invoke(this, "<<<<" + ret.ResultText.Trim().Replace("\r\n", Environment.NewLine + "<<<<") + Environment.NewLine + Environment.NewLine);

                                        // cmdの結果を受け取り終わったので、idle送信。結果は待たない。
                                        try
                                        {
                                            DebugOutput?.Invoke(this, ">>>>idle" + Environment.NewLine + Environment.NewLine);

                                            await _commandWriter.WriteAsync("idle\n");
                                        }
                                        catch (Exception e)
                                        {
                                            ret.IsSuccess = false;
                                            ret.ErrorMessage = e.Message;
                                            DebugOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}", cmd.Trim(), e.Message));

                                            return ret;
                                        }

                                    }
                                    catch (Exception e)
                                    {
                                        ret.IsSuccess = false;
                                        ret.ErrorMessage = e.Message;
                                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}", cmd.Trim(), e.Message));

                                        return ret;
                                    }
                                }
                            }
                            catch (Exception e)
                            {
                                ret.IsSuccess = false;
                                ret.ErrorMessage = e.Message;
                                DebugOutput?.Invoke(this, string.Format("<<<<Received ({0}):\n{1}", cmd.Trim(), e.Message));

                                return ret;
                            }
                        }
                        else
                        {
                            ret.IsSuccess = false;
                            ret.ErrorMessage = "_commandWriter or _commandReader is null";
                            DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}", cmd.Trim(), ret.ErrorMessage));
                        }
                    }
                    else
                    {
                        ret.IsSuccess = false;
                        ret.ErrorMessage = "NOT IsMpdCommandConnected";
                        DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}", cmd.Trim(), ret.ErrorMessage));
                    }

                }
                else
                {
                    ret.IsSuccess = false;
                    ret.ErrorMessage = "NOT _tcpCommandClient.Client.Connected";
                    DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}", cmd.Trim(), ret.ErrorMessage));
                }
            }
            catch (Exception e)
            {
                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                DebugOutput?.Invoke(this, string.Format("<<<<Received ({0} ):\n{1}", cmd.Trim(), e.Message));
            }

            return ret;
        }

        public async Task<CommandResult> MpdQueryStatus()
        {
            return await MpdSendCommand("status");
        }

        public async Task<CommandResult> MpdQueryCurrentSong()
        {
            return await MpdSendCommand("currentsong");
        }

        public async Task<CommandResult> MpdQueryCurrentQueue()
        {
            return await MpdSendCommand("playlistinfo");
        }

        public async Task<CommandResult> MpdQueryPlaylists()
        {
            return await MpdSendCommand("listplaylists");
        }

        public async Task<CommandResult> MpdQueryListAll()
        {
            return await MpdSendCommand("listall");
        }
        
    }
}

