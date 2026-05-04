using MPDCtrl.Models;
using MPDCtrl.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Path = System.IO.Path;

namespace MPDCtrl.Services;

#pragma warning disable IDE0079 //
#pragma warning disable CA1854
#pragma warning disable IDE0305
#pragma warning disable CA1862
#pragma warning disable IDE0290
#pragma warning restore IDE0079 //
public partial class MpcService : IMpcService
{
    #region == Consts, Properties, etc == 

    public string MpdHost { get; private set; } = "";

    public int MpdPort { get; private set; } = 6600;

    public string MpdPassword { get; private set; } = "";

    public string MpdVerText { get; set; } = "";

    public Status MpdStatus { get; private set; } = new();

    public bool MpdStop { get; set; }

    // You need to either get "status" and "queue" before hand, or "currentsong". 
    public SongInfoEx? MpdCurrentSong { get; private set; }

    public ObservableCollection<SongInfoEx> CurrentQueue { get; private set; } = [];

    public ObservableCollection<Playlist> Playlists { get; private set; } = [];

    public ObservableCollection<SongFile> LocalFiles { get; private set; } = [];

    public ObservableCollection<String> LocalDirectories { get; private set; } = [];

    public ObservableCollection<AlbumArtist> AlbumArtists { get; private set; } = [];

    public ObservableCollection<AlbumEx> Albums { get; private set; } = [];

    public ObservableCollection<AudioOutput> AudioOutputs { get; private set; } = [];

    public List<string> Commands { get; private set; } = [];

    #endregion

    #region == Connections ==

    private static TcpClient _commandConnection = new();
    private static StreamReader? _commandReader;
    private static StreamWriter? _commandWriter;

    private static TcpClient _idleConnection = new();
    private static StreamReader? _idleReader;
    private static StreamWriter? _idleWriter;

    public enum ConnectionStatus
    {
        NeverConnected,
        Connecting,
        Connected,
        DisconnectedByUser,
        DisconnectedByHost,
        ConnectFailTimeout,
        ReceiveFailTimeout,
        SendFailTimeout,
        SendFailNotConnected,
        Disconnecting,
        Disconnected,
        SeeConnectionErrorEvent
    }

    public ConnectionStatus ConnectionState
    {
        get;
        private set
        {
            if (value == field)
                return;

            field = value;

            ConnectionStatusChanged?.Invoke(this, field);
        }
    } = ConnectionStatus.NeverConnected;

    private CancellationTokenSource? _cts;// = new();

    // TODO: Not really used...
    public bool IsMpdCommandConnected { get; set; }
    public bool IsMpdIdleConnected { get; set; }

    //private bool IsMpdCommandInIdle = false;

    #endregion

    #region == Events == 

    public delegate void IsBusyEvent(MpcService sender, bool on);
    public event IsBusyEvent? IsBusy;

    public delegate void DebugCommandOutputEvent(MpcService sender, string data);
    public event DebugCommandOutputEvent? DebugCommandOutput;

    public delegate void DebugIdleOutputEvent(MpcService sender, string data);
    public event DebugIdleOutputEvent? DebugIdleOutput;

    public delegate void ConnectionStatusChangedEvent(MpcService sender, ConnectionStatus status);
    public event ConnectionStatusChangedEvent? ConnectionStatusChanged;

    public delegate void ConnectionErrorEvent(MpcService sender, string data);
    public event ConnectionErrorEvent? ConnectionError;

    public delegate void IsMpdIdleConnectedEvent(MpcService sender);
    public event IsMpdIdleConnectedEvent? MpdIdleConnected;

    public delegate void MpdAckErrorEvent(MpcService sender, string data, string origin);
    public event MpdAckErrorEvent? MpdAckError;

    public delegate void MpdFatalErrorEvent(MpcService sender, string data, string origin);
    public event MpdFatalErrorEvent? MpdFatalError;

    public delegate void MpdPlayerStatusChangedEvent(MpcService sender);
    public event MpdPlayerStatusChangedEvent? MpdPlayerStatusChanged;

    public delegate void MpdCurrentQueueChangedEvent(MpcService sender);
    public event MpdCurrentQueueChangedEvent? MpdCurrentQueueChanged;

    public delegate void MpdPlaylistsChangedEvent(MpcService sender);
    public event MpdPlaylistsChangedEvent? MpdPlaylistsChanged;

    public delegate void MpdOutputChangedEvent(MpcService sender);
    public event MpdOutputChangedEvent? MpdOutputChanged;

    public delegate void MpdAlbumArtChangedEvent(MpcService sender);
    public event MpdAlbumArtChangedEvent? MpdAlbumArtChanged;

    public delegate void MpcProgressEvent(MpcService sender, string msg);
    public event MpcProgressEvent? MpcProgress;

    #endregion

    private static readonly System.Threading.SemaphoreSlim SemaphoreCommand = new(1, 1);
    private static readonly System.Threading.SemaphoreSlim SemaphoreBinary = new(1, 1);

    private readonly IMpcBinaryService _binaryDownloader;
    private readonly IDispatcherService _dispatcherService;

    public MpcService(IMpcBinaryService binaryDownloader, IDispatcherService dispatcherService)
    {
        _binaryDownloader = binaryDownloader;
        _dispatcherService = dispatcherService;
    }

    #region == Idle Connection ==

    /*
    public async Task<ConnectionResult> MpdIdleConnectionStart(string host, int port, string password)
    {
        ConnectionResult r = await MpdIdleConnect(host, port);

        if (r.IsSuccess)
        {
            if (!string.IsNullOrEmpty(password))
            {
                CommandResult d = await MpdIdleSendPassword(password);

                r.IsSuccess = d.IsSuccess;
                r.ErrorMessage = d.ErrorMessage;
            }
        }
        return r;
    }
    */

    public async Task<ConnectionResult> MpdIdleConnect(string host, int port)
    {
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        MpdStop = false;

        ConnectionResult result = new();

        IsMpdIdleConnected = false;

        _idleConnection = new TcpClient();

        MpdHost = host;
        MpdPort = port;

        DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Connecting." + "\n" + "\n");
        MpcProgress?.Invoke(this, "Connecting...");

        ConnectionState = ConnectionStatus.Connecting;

        try
        {
            IsBusy?.Invoke(this, true);

            await _idleConnection.ConnectAsync(MpdHost, MpdPort);

            // TODO: always false
            if (_idleConnection.Client is null)
            {
                Debug.WriteLine("_idleConnection.Client is null. " + host + " " + port.ToString());

                result.ErrorMessage = "_idleConnection.Client is null";

                DebugIdleOutput?.Invoke(this, "TCP Idle Connection: Error while connecting. Fail to connect... " + "\n" + "\n");

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

                string? response = await _idleReader.ReadLineAsync();

                if (response is not null)
                {
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

            ConnectionError?.Invoke(this, "TCP connection failed to establish (SocketException): " + e.Message);
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

        MpdPassword = password;

        CommandResult ret = new();

        if (string.IsNullOrEmpty(password))
        {
            ret.IsSuccess = true;
            ret.ResultText = "OK";
            ret.ErrorMessage = "";

            return ret;
        }

        string cmd = "password " + password;

        return await MpdIdleSendCommand(cmd);

    }

    private async Task<CommandResult> MpdIdleSendCommand(string cmd)
    {
        CommandResult ret = new();

        if (MpdStop)
        {
            Debug.WriteLine("@MpdIdleSendCommand: MpdStop1");
            return ret;
        }

        if (_cts is null)
        {
            Debug.WriteLine("@MpdIdleSendCommand: _cts is null)");
            return ret;
        }

        MpcProgress?.Invoke(this, $"[Background] MpdIdleSendCommand...");

        //TODO: aways false
        if (_idleConnection.Client is null)
        {
            Debug.WriteLine("@MpdIdleSendCommand: TcpClient.Client is null");

            ret.IsSuccess = false;
            ret.ErrorMessage = "TcpClient.Client is null";

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @MpdIdleSendCommand, Reason: TcpClient.Client is null, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return ret;
        }

        if ((_idleWriter is null) || (_idleReader is null))
        {
            Debug.WriteLine("@MpdIdleSendCommand: " + "_idleWriter or _idleReader is null");

            ret.IsSuccess = false;
            ret.ErrorMessage = "_idleWriter or _idleReader is null";

            DebugIdleOutput?.Invoke(this,
                $"################ Error :@MpdIdleSendCommand, Reason: _idleWriter or _idleReader is null, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return ret;
        }

        if (!_idleConnection.Client.Connected)
        {
            Debug.WriteLine("Exception@MpdIdleSendCommand: " + "NOT IsMpdIdleConnected");

            ret.IsSuccess = false;
            ret.ErrorMessage = "NOT IsMpdIdleConnected";

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @MpdIdleSendCommand, Reason: !CommandConnection.Client.Connected, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return ret;
        }

        string cmdDummy = cmd;
        if (cmd.StartsWith("password "))
            cmdDummy = "password ****";

        //DebugIdleOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");
        DebugIdleOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");

        try
        {
            /*
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
            */

            MpcProgress?.Invoke(this, $"[Background] MpdIdleSendCommand::WriteAsync...");
            await _idleWriter.WriteAsync(cmd.Trim() + "\n");
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
                DebugIdleOutput?.Invoke(this,
                    $"################ Error@WriteAsync@MpdIdleSendCommand, Reason:IOException, Data:{cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

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
                DebugIdleOutput?.Invoke(this,
                    $"################ Error: @WriteAsync@MpdIdleSendCommand, Reason: Exception, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "The connection (idle) has been terminated (Exception): " + e.Message);
            }

            return ret;
        }

        try
        {
            MpcProgress?.Invoke(this, $"[Background] MpdIdleSendCommand::ReadLineAsync...");

            StringBuilder stringBuilder = new();

            var isAck = false;
            var isErr = false;
            var ackText = "";
            var errText = "";

            while (true)
            {
                var line = await _idleReader.ReadLineAsync(_cts.Token);

                //if (_cts.Token.IsCancellationRequested)
                _cts.Token.ThrowIfCancellationRequested();

                if (line is not null)
                {
                    MpcProgress?.Invoke(this, $"[Background] MpdIdleSendCommand::ReadLineAsync...{line}");

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
                    else if (line.StartsWith("error"))
                    {
                        Debug.WriteLine("error line @MpdIdleSendCommand: " + cmd.Trim() + " and " + line);

                        isErr = true;
                        errText = line;
                        ret.ErrorMessage = line;

                        if (!string.IsNullOrEmpty(line))
                            stringBuilder.Append(line + "\n");
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
                            MpcProgress?.Invoke(this, $"[Background] MpdIdleSendCommand::ReadLineAsync...line == IsEmpty");

                            break;
                        }
                    }
                }
                else
                {
                    Debug.WriteLine("@MpdIdleSendCommand ReadLineAsync line is not null");
                    MpcProgress?.Invoke(this, $"[Background] MpdIdleSendCommand::ReadLineAsync...line == IsNull");

                    DebugIdleOutput?.Invoke(this,
                        $"################ Error @ReadLineAsync@MpdIdleSendCommand, Reason: ReadLineAsync received null data, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "The connection (idle) has been terminated. ");

                    ret.ResultText = stringBuilder.ToString();
                    ret.ErrorMessage = "ReadLineAsync@MpdIdleSendCommand received null data";

                    break;
                }
            }

            //_ = Task.Run(() => );
            DebugIdleOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");

            if (isAck)
            {
                //_ = Task.Run(() => );
                MpdAckError?.Invoke(this, ackText, "Idle");
            }

            if (isErr)
            {
                //_ = Task.Run(() => );
                MpdFatalError?.Invoke(this, errText, "Idle");

                //ret.IsSuccess = false;

                //return ret;
            }

            if (isAck || isErr)
            {
                // Not good. If "{clearerror} you don't have permission for "clearerror"", then this gonna go forever.
                //await MpdIdleSendCommand("clearerror");
            }

            ret.ResultText = stringBuilder.ToString();

            return ret;
        }
        catch (System.InvalidOperationException e)
        {
            // The stream is currently in use by a previous operation on the stream.

            Debug.WriteLine("InvalidOperationException@MpdIdleSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @ReadLineAsync@MpdIdleSendCommand, Reason: InvalidOperationException, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

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

            MpcProgress?.Invoke(this, $"[Background] Exception @MpdIdleSendCommand::ReadLineAsync...");

            ret.IsSuccess = false;
            ret.ErrorMessage = e.Message;

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @ReadLineAsync@MpdIdleSendCommand, Reason: Exception, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

            return ret;
        }
    }

    public async Task<CommandResult> MpdIdleQueryProtocol()
    {
        // Not working with MPD 0.24.4?

        MpcProgress?.Invoke(this, "[Background] Querying available protocol features...");
        CommandResult result = await MpdIdleSendCommand("protocol available");
        if (result.IsSuccess)
        {
            MpcProgress?.Invoke(this, "[Background] Parsing protocol features...");
            result.IsSuccess = await ParseProtocolFeatures(result.ResultText);
            MpcProgress?.Invoke(this, "[Background] Protocol features updated.");
        }
        return result;
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
        //CommandResult result = await MpdIdleSendCommand("listallinfo");
        if (result.IsSuccess)
        {
            MpcProgress?.Invoke(this, "[Background] Parsing files and directories...");
            result.IsSuccess = await ParseListAll(result.ResultText);

            MpcProgress?.Invoke(this, "[Background] Files and directories updated.");
        }

        return result;
    }

    public async Task<CommandResult> MpdIdleQueryOutputs()
    {
        MpcProgress?.Invoke(this, "[Background] Querying output...");

        CommandResult result = await MpdIdleSendCommand("outputs");
        if (result.IsSuccess)
        {
            MpcProgress?.Invoke(this, "[Background] Parsing outputs...");
            result.IsSuccess = await ParseOutputs(result.ResultText);

            MpcProgress?.Invoke(this, "[Background] outputs updated.");
        }

        return result;
    }

    public async Task<CommandResult> MpdIdleQueryListAlbumArtists()
    {
        MpcProgress?.Invoke(this, "[Background] Querying artists...");

        CommandResult result = await MpdIdleSendCommand("list album group albumartist");
        if (result.IsSuccess)
        {
            MpcProgress?.Invoke(this, "[Background] Parsing artists...");
            result.IsSuccess = await ParseListAlbumGroupAlbumArtist(result.ResultText);
            MpcProgress?.Invoke(this, "[Background] Artists updated.");
        }
        MpcProgress?.Invoke(this, "");

        return result;
    }

    public void MpdIdleStart()
    {
        MpdIdle();
    }

    private async void MpdIdle()
    {
        if (MpdStop)
        {
            Debug.WriteLine("@MpdIdle: MpdStop1");
            return;
        }

        if (_cts is null)
        {
            Debug.WriteLine("@MpdIdle: _cts is null)");
            return;
        }

        //TODO:always false
        if (_idleConnection.Client is null)
        {
            Debug.WriteLine("@MpdIdle: TcpClient.Client is null");

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @MpdIdle, Reason: TcpClient.Client is null, Data: , {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return;
        }

        if ((_idleWriter is null) || (_idleReader is null))
        {
            Debug.WriteLine("@MpdIdle: " + "_idleWriter or _idleReader is null");

            DebugIdleOutput?.Invoke(this,
                $"################ Error :@MpdIdle, Reason: _idleWriter or _idleReader is null, Data: , {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return;
        }

        if (!_idleConnection.Client.Connected)
        {
            Debug.WriteLine("@MpdIdle: " + "!_idleConnection.Client.Connected");

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @MpdIdle, Reason: !_idleConnection.Client.Connected, Data: , {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return;
        }

        string cmd = "idle player mixer options playlist stored_playlist output\n";

        DebugIdleOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");

        try
        {
            await _idleWriter.WriteAsync(cmd);
        }
        catch (IOException e)
        {
            // connection terminated by ... something.

            Debug.WriteLine("[IOException@MpdIdle] ({0} ):\n{1}", "WriteAsync", e.Message);

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @WriteAsync@MpdIdle, Reason: IOException, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

            ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

            ConnectionError?.Invoke(this, "The connection (idle) has been terminated. IOException: " + e.Message);

            return;
        }
        catch (Exception e)
        {
            Debug.WriteLine("[Exception@MpdIdle] ({0} ):\n{1}", "WriteAsync", e.Message);

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @WriteAsync@MpdIdle, Reason: Exception, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

            ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

            ConnectionError?.Invoke(this, "The connection (idle) has been terminated. Exception: " + e.Message);

            return;
        }

        try
        {
            StringBuilder stringBuilder = new();

            var isAck = false;
            var isErr = false;
            var ackText = "";
            var errText = "";

            while (true)
            {
                if (MpdStop)
                {
                    Debug.WriteLine("@MpdIdle: MpdStop in while loop.");
                    break;
                }

                var line = await _idleReader.ReadLineAsync(_cts.Token);

                if (line is not null)
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
                    else if (line.StartsWith("error"))
                    {
                        Debug.WriteLine("error line @MpdIdle(): " + cmd.Trim() + " and " + line);

                        isErr = true;
                        errText = line;

                        if (!string.IsNullOrEmpty(line))
                            stringBuilder.Append(line + "\n");
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
                        Debug.WriteLine("ReadLineAsync null return due to cancellation by ConnectionStatus.Disconnecting, now exiting.");
                    }
                    else
                    {
                        Debug.WriteLine("TCP Idle Connection: ReadLineAsync @MpdIdle received null data.");

                        DebugIdleOutput?.Invoke(this,
                            $"################ Error: @ReadLineAsync@MpdIdle, Reason: ReadLineAsync@MpdIdle received null data, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {""} {Environment.NewLine + Environment.NewLine}");

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
                //_ = Task.Run(() => MpdAckError?.Invoke(this, ackText, "Idle"));
                MpdAckError?.Invoke(this, ackText, "Idle");
            }

            if (isErr)
            {
                //_ = Task.Run(() => MpdFatalError?.Invoke(this, errText, "Idle"));
                MpdFatalError?.Invoke(this, errText, "Idle");
                //ret.IsSuccess = false;

                //return ret;
            }
            else
            {
                //await ParseSubSystemsAndRaiseChangedEvent(result);
            }

            if (isAck || isErr)
            {
                // Not good. If "{clearerror} you don't have permission for "clearerror"", then this gonna go forever.
                //await MpdIdleSendCommand("clearerror");
            }

            await ParseSubSystemsAndRaiseChangedEvent(result);
        }
        catch (System.IO.IOException e)
        {
            // Could be application shutdopwn.

            if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser) || (ConnectionState == ConnectionStatus.Connecting))
            {
                // no problem
                Debug.WriteLine("ReadLineAsync IOException due to ConnectionStatus.Disconnecting, now exiting.");
            }
            else
            {
                Debug.WriteLine("[IOException@MpdIdle] ({0}):\n{1}", "ReadLineAsync: " + ConnectionState.ToString(), e.Message);

                DebugIdleOutput?.Invoke(this,
                    $"################ Error: @ReadLineAsync@MpdIdle, Reason: IOException, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

                ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                ConnectionError?.Invoke(this, "The connection (idle) has been terminated. Exception: " + e.Message);
            }
        }
        catch (System.OperationCanceledException e)
        {
            if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser) || (ConnectionState == ConnectionStatus.Connecting))
            {
                // no problem
                Debug.WriteLine("ReadLineAsync canceled due to OperationCanceledException. Disconnecting, now exiting. @MpdIdle()");
            }
            else
            {
                Debug.WriteLine("[OperationCanceledException@MpdIdle] ({0}):\n{1}", "ReadLineAsync @MpdIdle(): " + ConnectionState.ToString(), e.Message);
            }
        }
        catch (System.InvalidOperationException e)
        {
            // The stream is currently in use by a previous operation on the stream.

            DebugIdleOutput?.Invoke(this,
                $"################ Error: @ReadLineAsync@MpdIdle, Reason: InvalidOperationException, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

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

                DebugIdleOutput?.Invoke(this,
                    $"################ Error: @ReadLineAsync@MpdIdle, Reason: Exception, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

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

        List<string> subSystems = result.Split('\n').ToList();

        try
        {
            bool isPlayer = false;
            bool isCurrentQueue = false;
            bool isStoredPlaylist = false;
            var isOutput = false;

            foreach (string line in subSystems)
            {
                //if (line.ToLower() == "changed: playlist")
                if (string.Equals(line, "changed: playlist", StringComparison.OrdinalIgnoreCase))
                {
                    // playlist: the queue (i.e.the current playlist) has been modified
                    isCurrentQueue = true;
                }
                //if (line.ToLower() == "changed: player")
                if (string.Equals(line, "changed: player", StringComparison.OrdinalIgnoreCase))
                {
                    // player: the player has been started, stopped or seeked
                    isPlayer = true;
                }
                //if (line.ToLower() == "changed: options")
                if (string.Equals(line, "changed: options", StringComparison.OrdinalIgnoreCase))
                {
                    // options: options like repeat, random, crossfade, replay gain
                    isPlayer = true;
                }
                //if (line.ToLower() == "changed: mixer")
                if (string.Equals(line, "changed: mixer", StringComparison.OrdinalIgnoreCase))
                {
                    // mixer: the volume has been changed
                    isPlayer = true;
                }
                if (line.ToLower() == "changed: output")
                {
                    // output: Audio output has been added, removed or modified(e.g.renamed, enabled or disabled)
                    isOutput = true;
                }
                //if (line.ToLower() == "changed: stored_playlist")
                if (string.Equals(line, "changed: stored_playlist", StringComparison.OrdinalIgnoreCase))
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

            if (isOutput)
            {
                var idleResult = await MpdIdleQueryOutputs();
                if (idleResult.IsSuccess)
                {
                    MpdOutputChanged?.Invoke(this);
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
                // BinaryConnection start.
                await _binaryDownloader.MpdBinaryConnectionStart(MpdHost, MpdPort, MpdPassword);

                // Get available commands
                await MpdCommands();

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

        MpdHost = host;
        MpdPort = port;

        DebugCommandOutput?.Invoke(this, "TCP Command Connection: Connecting." + "\n" + "\n");

        ConnectionState = ConnectionStatus.Connecting;

        try
        {
            await _commandConnection.ConnectAsync(MpdHost, MpdPort);

            // TODO: always false
            if (_commandConnection.Client is null)
            {
                Debug.WriteLine("_commandConnection.Client is null. " + host + " " + port.ToString());

                result.ErrorMessage = "_commandConnection.Client is null";

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

                string? response = await _commandReader.ReadLineAsync();

                if (response is not null)
                {
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
        MpdPassword = password;

        CommandResult ret = new();

        if (string.IsNullOrEmpty(password))
        {
            ret.IsSuccess = true;
            ret.ResultText = "OK";
            ret.ErrorMessage = "";

            return ret;
        }

        string cmd = "password " + password;

        return await MpdCommandSendCommandProtected(cmd);

    }

    private async Task<CommandResult> MpdCommandSendCommand(string cmd)
    {
        CommandResult ret = new();

        if (_cts is null)
        {
            ret.IsSuccess = false;
            return ret;
        }

        try
        {
            if (await SemaphoreCommand.WaitAsync(TimeSpan.FromSeconds(3), _cts.Token))
            {
                if (MpdStop)
                {
                    Debug.WriteLine("@MpdCommandSendCommand: MpdStop");
                    ret.IsWaitFailed = true;
                    ret.ErrorMessage = "WaitAsync failed due to MpdStop. @MpdCommandSendCommand";
                    SemaphoreCommand.Release();
                    return ret;
                }

                try
                {
                    ret = await MpdCommandSendCommandProtected(cmd, false);
                }
                finally
                {
                    SemaphoreCommand.Release();
                }
            }
            else
            {
                Debug.WriteLine("WaitAsync failed. @MpdCommandSendCommand");
                ret.IsWaitFailed = true;
                ret.ErrorMessage = "WaitAsync failed. @MpdCommandSendCommand";
            }
        }
        catch (Exception e)
        {
            // Probably System.OperationCanceledException or System.ObjectDisposedException
            // System.ObjectDisposedException: The CancellationTokenSource has been disposed.

            Debug.WriteLine($"Exception. @MpdCommandSendCommand: {e}");
            ret.IsWaitFailed = true;
            ret.ErrorMessage = "Exception. @MpdCommandSendCommand" + e.Message;

        }

        return ret;
    }

    private async Task<CommandResult> MpdCommandSendCommandProtected(string cmd, bool isAutoIdling = false, int reTryCount = 0)
    {
        // TEST: 
        isAutoIdling = false;

        CommandResult ret = new();

        if (_cts is null)
        {
            ret.IsSuccess = false;
            return ret;
        }

        if (_cts.Token.IsCancellationRequested)
        {
            Debug.WriteLine("IsCancellationRequested @MpdCommandSendCommandProtected");
            return ret;
        }

        //TODO: always false
        if (_commandConnection.Client is null)
        {
            Debug.WriteLine("@MpdSendCommand: TcpClient.Client is null");

            ret.IsSuccess = false;
            ret.ErrorMessage = "TcpClient.Client is null";

            DebugCommandOutput?.Invoke(this,
                $"################ Error: @MpdSendCommand, Reason: TcpClient.Client is null, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return ret;
        }

        if ((_commandWriter is null) || (_commandReader is null))
        {
            Debug.WriteLine("@MpdSendCommand: " + "_commandWriter or _commandReader is null");

            ret.IsSuccess = false;
            ret.ErrorMessage = "_commandWriter or _commandReader is null";

            DebugCommandOutput?.Invoke(this,
                $"################ Error :@MpdSendCommand, Reason: _commandWriter or _commandReader is null, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return ret;
        }

        if (!_commandConnection.Client.Connected)
        {
            Debug.WriteLine("@MpdSendCommand: " + "NOT IsMpdCommandConnected");

            ret.IsSuccess = false;
            ret.ErrorMessage = "NOT IsMpdCommandConnected";

            DebugCommandOutput?.Invoke(this,
                $"################ Error: @MpdSendCommand, Reason: !CommandConnection.Client.Connected, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return ret;
        }

        if (reTryCount > 5)
        {
            Debug.WriteLine("@MpdSendCommand: " + "retryCount > 5, returning.");

            ret.IsSuccess = false;
            ret.ErrorMessage = "RetryCount > 5, returning.";

            DebugCommandOutput?.Invoke(this,
                $"################ Error: @MpdSendCommand, Reason: RetryCount > 5, Data: {cmd.Trim()}, {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            return ret;
        }

        // WriteAsync
        try
        {
            IsBusy?.Invoke(this, true);
            /*
            if (cmd.Trim().StartsWith("idle"))
            {
                //DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");
                Task nowait = Task.Run(() => DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n"));

                await _commandWriter.WriteAsync(cmd.Trim() + "\n");

                if (!isAutoIdling)
                {
                    ret.IsSuccess = true;

                    IsBusy?.Invoke(this, false);
                    ;
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
            */


            // TEST:
            /*
            if (IsMpdCommandInIdle)
            {
                if ((!cmd.StartsWith("password")) && (!cmd.StartsWith("idle")) && (!cmd.StartsWith("noidle")))
                {
                    Debug.WriteLine("MpdSendCommand: IsMpdCommandInIdle is true, cmd: " + cmd.Trim());
                    var n = await MpdCommandSendCommand("noidle");
                    if (n.IsSuccess)
                    {
                        IsMpdCommandInIdle = false;
                        return ret = await MpdCommandSendCommand(cmd);
                    }
                    else
                    {
                        Debug.WriteLine("MpdSendCommand: IsMpdCommandInIdle is true, but noidle failed: " + n.ErrorMessage);
                    }
                    return n;
                }
            }
            else
            {

            }
            */

            string cmdDummy = cmd;
            if (cmd.StartsWith("password "))
                cmdDummy = "password ****";

            DebugCommandOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");

            await _commandWriter.WriteAsync(cmd.Trim() + "\n");
        }
        catch (System.IO.IOException e)
        {
            // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。

            ret.IsSuccess = false;
            ret.ErrorMessage = e.Message;

            // Could be application shutdopwn.
            if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
            {
                //IsBusy?.Invoke(this, false);

                //return ret;
            }
            else
            {
                ConnectionState = ConnectionStatus.ConnectFailTimeout;
                DebugCommandOutput?.Invoke(this,
                    $"################ Error@WriteAsync@MpdSendCommand, Reason:{nameof(IOException)}, Data:{cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

                // タイムアウトしていたらここで「も」エラーになる模様。

                IsMpdCommandConnected = false;

                DebugCommandOutput?.Invoke(this, string.Format("Reconnecting... " + Environment.NewLine + Environment.NewLine));
                Debug.WriteLine($"Looks like Connection Timeout. Reconnecting...  @IOExceptionOfWriteAsync:  {e.Message}" + Environment.NewLine + cmd.Trim());
                try
                {
                    //_commandConnection.Client.Shutdown(SocketShutdown.Both);
                    _commandConnection.Close();
                }
                catch { }

                ConnectionResult newCon = await MpdCommandConnect(MpdHost, MpdPort);

                if (newCon.IsSuccess)
                {
                    CommandResult d = await MpdCommandSendPassword(MpdPassword);

                    if (d.IsSuccess)
                    {
                        // TEST:
                        //d = await MpdCommandSendCommand("idle player", isAutoIdling, reTryCount++);
                        //if (d.IsSuccess)
                        //{
                        DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Success. @IOExceptionOfWriteAsync" + Environment.NewLine + Environment.NewLine));
                        Debug.WriteLine("Reconnecting Success.  @IOExceptionOfWriteAsync");

                        ConnectionState = ConnectionStatus.Connected;
                        ret = await MpdCommandSendCommandProtected(cmd, isAutoIdling, reTryCount++);

                        // Fixing the MPD's volume 100% on re-connect problem once on for all...with dirty hack. 
                        //Debug.WriteLine($"setvol {MpdStatus.MpdVolume}.  @IOExceptionOfWriteAsync");
                        //ret = await MpdCommandSendCommandProtected(("setvol " + MpdStatus.MpdVolume.ToString()), isAutoIdling, reTryCount++);

                        //}
                    }

                    ConnectionState = ConnectionStatus.Connected;
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
                IsBusy?.Invoke(this, false);

                return ret;
            }
            else
            {
                DebugCommandOutput?.Invoke(this,
                    $"################ Error: @WriteAsync@MpdSendCommand, Reason: Exception, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

                // TODO:
                //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                //ConnectionError?.Invoke(this, "The connection (command) has been terminated (Exception): " + e.Message);
            }

            IsBusy?.Invoke(this, false);

            return ret;
        }

        if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
        {
            IsBusy?.Invoke(this, false);
            return ret;
        }

        // ReadLineAsync
        try
        {
            IsBusy?.Invoke(this, true);

            StringBuilder stringBuilder = new();

            var isDoubleOk = false;
            var isAck = false;
            var ackText = "";
            var isNullReturn = false;
            var isErr = false;
            var errText = "";

            while (true)
            {
                var line = await _commandReader.ReadLineAsync(_cts.Token);
                
                if (_cts.Token.IsCancellationRequested)
                {
                    Debug.WriteLine("IsCancellationRequested in while loop @MpdCommandSendCommandProtected");
                    return ret;
                }

                if (line is not null)
                {
                    if (line.StartsWith("ACK"))
                    {
                        Debug.WriteLine("ACK line @MpdCommandSendCommand: " + cmd.Trim() + " and " + line);

                        if (!string.IsNullOrEmpty(line))
                            stringBuilder.Append(line + "\n");

                        ret.ErrorMessage = line;
                        ackText = line;
                        isAck = true;

                        break;
                    }
                    else if (line.StartsWith("error"))
                    {
                        Debug.WriteLine("Error line @MpdCommandSendCommand: " + cmd.Trim() + " and " + line);

                        isErr = true;
                        errText = line;
                        ret.ErrorMessage = line;

                        if (!string.IsNullOrEmpty(line))
                            stringBuilder.Append(line + "\n");
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


                DebugCommandOutput?.Invoke(this,
                    $"################ Error @ReadLineAsync@MpdSendCommand, Reason: ReadLineAsync received null data, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {Environment.NewLine + Environment.NewLine}");

                ret.ResultText = stringBuilder.ToString();
                ret.ErrorMessage = "ReadLineAsync@MpdSendCommand received null data";

                // タイムアウトしていたらここで「も」エラーになる模様。

                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {
                    IsBusy?.Invoke(this, false);

                    return ret;
                }

                IsMpdCommandConnected = false;

                DebugCommandOutput?.Invoke(this, string.Format("Connection Timeout(NullReturn). Reconnecting... " + Environment.NewLine + Environment.NewLine));

                try
                {
                    //_commandConnection.Client.Shutdown(SocketShutdown.Both);
                    _commandConnection.Close();
                }
                catch { }

                ConnectionResult newCon = await MpdCommandConnect(MpdHost, MpdPort);

                if (newCon.IsSuccess)
                {
                    CommandResult d = await MpdCommandSendPassword(MpdPassword);

                    if (d.IsSuccess)
                    {
                        // TEST:
                        //d = await MpdCommandSendCommand("idle player", isAutoIdling, reTryCount);
                        //if (d.IsSuccess)
                        //{
                        DebugCommandOutput?.Invoke(this, "Reconnecting Success. @isNullReturn" + Environment.NewLine + Environment.NewLine);
                        Debug.WriteLine("Reconnecting Success. @isNullReturn, RetryCount=" + reTryCount.ToString() + Environment.NewLine);

                        ConnectionState = ConnectionStatus.Connected;

                        ret = await MpdCommandSendCommandProtected(cmd, isAutoIdling, reTryCount++);

                        // Fixing the MPD's volume 100% on re-connect problem once on for all...with dirty hack. 
                        //Debug.WriteLine($"setvol {MpdStatus.MpdVolume}.  @isNullReturn");
                        //ret = await MpdCommandSendCommandProtected(("setvol " + MpdStatus.MpdVolume.ToString()), isAutoIdling, reTryCount++);

                        //}
                    }

                    ConnectionState = ConnectionStatus.Connected;
                }
                else
                {
                    // 
                    DebugCommandOutput?.Invoke(this, "Reconnecting Failed. " + Environment.NewLine + Environment.NewLine);

                    ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    ConnectionError?.Invoke(this, "The connection (command) has been terminated (null return).");
                }

                IsBusy?.Invoke(this, false);

                return ret;

            }
            else
            {
                //DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");
                DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");

                if (isAck)
                {
                    MpdAckError?.Invoke(this, ackText, "Command");
                }

                if (isErr)
                {
                    MpdFatalError?.Invoke(this, errText, "Command");
                }

                ret.ResultText = stringBuilder.ToString();

                IsBusy?.Invoke(this, false);

                // TEST: 
                /*
                if (!IsMpdCommandInIdle)
                {
                    if ((!cmd.StartsWith("password")) && (!cmd.StartsWith("idle")) && (!cmd.StartsWith("noidle")))
                    {
                        Debug.WriteLine("MpdSendCommand: " + "IsMpdCommandInIdle is false, sending idle command...");
                        var y = await MpdCommandSendCommand("idle");
                        if (y.IsSuccess)
                        {
                            IsMpdCommandInIdle = true;
                        }
                    }
                }
                */

                return ret;
            }

        }
        catch (System.InvalidOperationException e)
        {
            // The stream is currently in use by a previous operation on the stream.

            Debug.WriteLine("InvalidOperationException@MpdSendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

            DebugCommandOutput?.Invoke(this,
                $"################ Error: @ReadLineAsync@MpdSendCommand, Reason: InvalidOperationException (Most likely the connection is overloaded), Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

            ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

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
            Debug.WriteLine("IOException@ReadLineAsync@MpdSendCommand: " + Environment.NewLine + cmd.Trim() + Environment.NewLine + e.Message);

            ret.IsSuccess = false;
            ret.ErrorMessage = e.Message;

            // Could be application shutdopwn.
            if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
            {
                IsBusy?.Invoke(this, false);

                return ret;
            }
            else
            {
                ConnectionState = ConnectionStatus.ConnectFailTimeout;

                DebugCommandOutput?.Invoke(this,
                    $"################ Error: @{"ReadLineAsync@MpdSendCommand" + Environment.NewLine}Reason: {"IOException" + Environment.NewLine}Data: {cmd.Trim() + Environment.NewLine}{Environment.NewLine}Exception: {e.Message} {Environment.NewLine}");

                // タイムアウトしていたらここで「も」エラーになる模様。

                IsMpdCommandConnected = false;

                DebugCommandOutput?.Invoke(this, string.Format("Connection Timeout. Reconnecting... " + Environment.NewLine + Environment.NewLine));
                Debug.WriteLine("Connection Timeout. Reconnecting...  @IOExceptionOfReadLineAsync");

                try
                {
                    //_commandConnection.Client.Shutdown(SocketShutdown.Both);
                    _commandConnection.Close();
                }
                catch { }

                ConnectionResult newCon = await MpdCommandConnect(MpdHost, MpdPort);

                if (newCon.IsSuccess)
                {
                    CommandResult d = await MpdCommandSendPassword(MpdPassword);

                    if (d.IsSuccess)
                    {
                        // TEST:
                        //d = await MpdCommandSendCommand("idle player", isAutoIdling, reTryCount);
                        //if (d.IsSuccess)
                        //{
                        DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Success. @IOExceptionOfReadLineAsync" + Environment.NewLine + Environment.NewLine));
                        Debug.WriteLine("Reconnecting Success. @IOExceptionOfReadLineAsync");

                        ConnectionState = ConnectionStatus.Connected;

                        ret = await MpdCommandSendCommandProtected(cmd, isAutoIdling, reTryCount++);

                        // Fixing the MPD's volume 100% on re-connect problem once on for all...with dirty hack. 
                        //Debug.WriteLine($"setvol {MpdStatus.MpdVolume}.  @IOExceptionOfReadLineAsync");
                        //ret = await MpdCommandSendCommandProtected(("setvol " + MpdStatus.MpdVolume.ToString()), isAutoIdling, reTryCount++);

                        //}
                    }

                    ConnectionState = ConnectionStatus.Connected;
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

            DebugCommandOutput?.Invoke(this,
                $"################ Error: @ReadLineAsync@MpdSendCommand, Reason: Exception, Data: {cmd.Trim()}, {Environment.NewLine} Exception: {e.Message} {Environment.NewLine + Environment.NewLine}");

            IsBusy?.Invoke(this, false);

            return ret;
        }

    }

    #endregion

    #region == Command Connection's MPD Commands with results other than OK == 
    
    public async Task<CommandResult> MpdCommands()
    {
        CommandResult result = await MpdCommandSendCommand("commands");
        if (result.IsSuccess)
        {
            result.IsSuccess = await ParseCommands(result.ResultText);
        }
        return result;
    }

    public async Task<CommandResult> MpdQueryStatus()
    {
        CommandResult result = await MpdCommandSendCommand("status");
        if (result.IsSuccess)
        {
            result.IsSuccess = await ParseStatus(result.ResultText);
        }

        return result;
    }

    public async Task<CommandResult> MpdQueryCurrentSong()
    {
        CommandResult result = await MpdCommandSendCommand("currentsong");
        if (result.IsSuccess)
        {
            result.IsSuccess = await ParseCurrentSong(result.ResultText);
        }

        return result;
    }

    public async Task<CommandResult> MpdQueryCurrentQueue()
    {
        CommandResult result = await MpdCommandSendCommand("playlistinfo");
        if (result.IsSuccess)
        {
            result.IsSuccess = await ParsePlaylistInfo(result.ResultText);
        }

        return result;
    }

    public async Task<CommandResult> MpdQueryPlaylists()
    {
        CommandResult result = await MpdCommandSendCommand("listplaylists");
        if (result.IsSuccess)
        {
            result.IsSuccess = await ParsePlaylists(result.ResultText);
        }

        return result;
    }

    public async Task<CommandResult> MpdQueryListAll()
    {
        MpcProgress?.Invoke(this, "[Background] Querying files and directories...");

        //CommandResult result = await MpdCommandSendCommand("listallinfo");
        CommandResult result = await MpdCommandSendCommand("listall");
        if (result.IsSuccess)
        {
            MpcProgress?.Invoke(this, "[Background] Parsing files and directories...");
            result.IsSuccess = await ParseListAll(result.ResultText);
            MpcProgress?.Invoke(this, "[Background] Files and directories updated.");
        }

        MpcProgress?.Invoke(this, "");
        return result;
    }

    [GeneratedRegex(@"[\\]+")]
    private static partial Regex EscapeRegex1();

    [GeneratedRegex(@"[\']+")]
    private static partial Regex EscapeRegex2();

    [GeneratedRegex(@"[""]+")]
    private static partial Regex EscapeRegex3();

    public async Task<CommandSearchResult> MpdSearch(string queryTag, string queryShiki, string queryValue)
    {
        //MpcProgress?.Invoke(this, "[Background] Searching...");

        CommandSearchResult result = new();

        /*
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            SearchResult.Clear();
        });
        */

        if (string.IsNullOrEmpty(queryTag) || string.IsNullOrEmpty(queryShiki)) // || string.IsNullOrEmpty(queryValue) 
        {
            result.IsSuccess = false;
            return result;
        }

        //var expression = queryTag + " " + queryShiki + " \'" + Regex.Escape(queryValue) + "\'";

        //queryValue = @"foo'bar""";
        var escapeValue = queryValue;
        //escapeValue = Regex.Replace(escapeValue, @"[\\]+", @"\");
        escapeValue = EscapeRegex1().Replace(escapeValue, @"\");
        //escapeValue = Regex.Replace(escapeValue, @"[\']+", @"\\'");
        escapeValue = EscapeRegex2().Replace(escapeValue, @"\\'");
        //escapeValue = Regex.Replace(escapeValue, @"[""]+", @"\\\""");
        escapeValue = EscapeRegex3().Replace(escapeValue, @"\\\""");

        var expression = queryTag + " " + queryShiki + " \\\"" + escapeValue + "\\\"";
        //var expression = queryTag + " " + queryShiki + " \'" + Regex.Escape(queryValue) + "\'";

        string cmd = "search \"(" + expression + ")\"\n";

        //Debug.WriteLine("MpdSearch cmd: " + cmd);

        CommandResult cm = await MpdCommandSendCommand(cmd);
        if (cm.IsSuccess)
        {
            //MpcProgress?.Invoke(this, "[Background] Parsing search result...");
            /*
            if (await ParseSearchResult(cm.ResultText))
            {
                result.IsSuccess = true;

                result.SearchResult = this.SearchResult;

                //result.ResultText = cm.ResultText;

                MpcProgress?.Invoke(this, "[Background] Search result updated.");
            }
            */
            //MpcProgress?.Invoke(this, "[Background] Search completed.");
            result.IsSuccess = true;
            result.ResultText = cm.ResultText;
            result.SearchResult = await ParseSearchResult(cm.ResultText);
        }
        else
        {
            result.IsSuccess = false;
            result.ErrorMessage = cm.ErrorMessage;
        }

        MpcProgress?.Invoke(this, "");

        return result;
    }

    public async Task<CommandPlaylistResult> MpdQueryPlaylistSongs(string playlistName)
    {
        CommandPlaylistResult result = new();

        if (string.IsNullOrEmpty(playlistName))
        {
            result.IsSuccess = false;
            return result;
        }

        MpcProgress?.Invoke(this, "[Background] Querying playlist info...");

        playlistName = Regex.Escape(playlistName);

        CommandResult cm = await MpdCommandSendCommand("listplaylistinfo \"" + playlistName + "\"");
        if (cm.IsSuccess)
        {
            MpcProgress?.Invoke(this, "[Background] Parsing playlist info...");

            result.IsSuccess = cm.IsSuccess;
            result.PlaylistSongs = ParsePlaylistSongsResult(cm.ResultText);

            MpcProgress?.Invoke(this, "[Background] Playlist info is updated.");
        }
        else
        {
            result.IsSuccess = false;
            result.ErrorMessage = cm.ErrorMessage;
        }

        return result;
    }

    public async Task<CommandImageResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture)
    {
        CommandImageResult res = new();

        if (string.IsNullOrEmpty(uri))
        {
            res.IsSuccess = false;
            return res;
        }

        if (_cts is null)
        {
            res.IsSuccess = false;
            return res;
        }

        try
        {
            if (await SemaphoreBinary.WaitAsync(TimeSpan.FromSeconds(3), _cts.Token))
            {
                try
                {
                    if (MpdStop)
                    {
                        Debug.WriteLine("MpdStop @MpdQueryAlbumArt");
                        res.IsSuccess = false;
                        return res;
                    }

                    if (_cts is null)
                    {
                        Debug.WriteLine("_cts is null returning @MpdQueryAlbumArt (Command)");
                        res.IsSuccess = false;
                        return res;
                    }

                    if (_cts.Token.IsCancellationRequested)
                    {
                        Debug.WriteLine("IsCancellationRequested returning @MpdQueryAlbumArt (Command)");
                        res.IsSuccess = false;
                        return res;
                    }

                    res = await _binaryDownloader.MpdQueryAlbumArt(uri, isUsingReadpicture);

                    if (res.IsSuccess)
                    {
                        /*
                        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                        {
                            AlbumCover = _binaryDownloader.AlbumCover;
                        });
                        */

                        //res.IsSuccess = true;
                        //res.AlbumCover = _binaryDownloader.AlbumCover;

                        //await Task.Delay(1000);
                        //await Task.Delay(200);
                        MpdAlbumArtChanged?.Invoke(this);
                    }
                    else
                    {
                        //Debug.WriteLine("MpdQueryAlbumArt failed @MpdQueryAlbumArt. Why... > " + res.ErrorMessage);

                        // need this to clear image.
                        //await Task.Delay(200);
                        MpdAlbumArtChanged?.Invoke(this);

                        /*
                        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                        {
                            AlbumCover = new();
                        });
                        */
                    }
                }
                finally
                {
                    SemaphoreBinary.Release();
                }
            }
            else
            {
                Debug.WriteLine("WaitAsync failed. @MpdQueryAlbumArt: " + uri);
                res.IsWaitFailed = true;
                res.ErrorMessage = "WaitAsync failed. @MpdQueryAlbumArt";
            }

            if ((!res.IsSuccess) && res.IsTimeOut)
            {
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {
                    Debug.WriteLine("MpdQueryAlbumArt@Timeout. Disconnecting...");
                    IsBusy?.Invoke(this, false);

                    return res;
                }
                else
                {
                    DebugCommandOutput?.Invoke(this, "MpdQueryAlbumArt@Timeout. Reconnecting...");
                    // re-connect
                    var b = await _binaryDownloader.MpdBinaryConnectionStart(MpdHost, MpdPort, MpdPassword);
                    if (b)
                    {
                        DebugCommandOutput?.Invoke(this, "MpdQueryAlbumArt@Timeout. Reconnecting success.");
                        Debug.WriteLine("MpdQueryAlbumArt@Timeout. Reconnecting success.");
                        // retry for the timeout.
                        //SemaphoreBinary.Release();
                        return await MpdQueryAlbumArt(uri, isUsingReadpicture);
                    }
                    else
                    {
                        Debug.WriteLine("MpdQueryAlbumArt@Timeout. Reconnecting failed.");
                    }
                }
            }
        }
        catch (Exception e)
        {
            // Probably System.OperationCanceledException or System.ObjectDisposedException
            // The CancellationTokenSource has been disposed.

            Debug.WriteLine($"Exception. @MpdQueryAlbumArt: {e}");

            res.ErrorMessage = "Exception. @MpdQueryAlbumArt" + e.Message;
        }

        return res;
    }

    public async Task<CommandImageResult> MpdQueryAlbumArtForAlbumView(string uri, bool isUsingReadpicture)
    {
        CommandImageResult res = new();

        if (string.IsNullOrEmpty(uri))
        {
            res.IsSuccess = false;
            return res;
        }
        
        if (_cts is null)
        {
            res.IsSuccess = false;
            Debug.WriteLine("@MpdQueryAlbumArtForAlbumView: (_cts is null)");
            return res;
        }

        try
        {
            if (await SemaphoreBinary.WaitAsync(TimeSpan.FromSeconds(2), _cts.Token))
            {
                try
                {
                    if (MpdStop)
                    {
                        Debug.WriteLine("MpdStop @MpdQueryAlbumArtForAlbumView");
                        res.IsSuccess = false;
                        return res;
                    }

                    if (_cts.Token.IsCancellationRequested)
                    {
                        Debug.WriteLine("IsCancellationRequested returning @MpdQueryAlbumArtForAlbumView (Command)");
                        res.IsSuccess = false;
                        return res;
                    }

                    res = await _binaryDownloader.MpdQueryAlbumArt(uri, isUsingReadpicture);

                    if (res.IsSuccess)
                    {
                        //await Task.Delay(200);
                        //MpdAlbumArtChanged?.Invoke(this);
                    }
                    else
                    {
                        //Debug.WriteLine("MpdQueryAlbumArt failed @MpdQueryAlbumArtForAlbumView. Why... > " + res.ErrorMessage);

                        if ((!res.IsSuccess) && res.IsTimeOut)
                        {
                            if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                            {
                                Debug.WriteLine("MpdQueryAlbumArt@Timeout. Disconnecting...");
                                IsBusy?.Invoke(this, false);

                                return res;
                            }
                            else
                            {
                                DebugCommandOutput?.Invoke(this, "MpdQueryAlbumArt@Timeout. Reconnecting...");
                                // re-connect
                                var b = await _binaryDownloader.MpdBinaryConnectionStart(MpdHost, MpdPort, MpdPassword);
                                if (b)
                                {
                                    Debug.WriteLine("MpdQueryAlbumArt@Timeout. Reconnecting success.");
                                    DebugCommandOutput?.Invoke(this, "MpdQueryAlbumArt@Timeout. Reconnecting success.");
                                    // retry for the timeout.
                                    //SemaphoreBinary.Release();
                                    return await MpdQueryAlbumArtForAlbumView(uri, isUsingReadpicture);
                                }
                                else
                                {
                                    Debug.WriteLine("MpdQueryAlbumArt@Timeout. Reconnecting failed.");
                                    DebugCommandOutput?.Invoke(this, "MpdQueryAlbumArt@Timeout. Reconnecting failed.");
                                }
                            }
                        }
                        else
                        {
                            //Debug.WriteLine("MpdQueryAlbumArt failed.");
                        }
                    }
                }
                finally
                {
                    SemaphoreBinary.Release();
                }
            }
            else
            {
                Debug.WriteLine("WaitAsync failed. @MpdQueryAlbumArtForAlbumView");
                res.IsWaitFailed = true;
                res.ErrorMessage = "WaitAsync failed. @MpdQueryAlbumArtForAlbumView";
            }
////
        }
        catch (Exception e)
        {
            // probably System.OperationCanceledException or System.ObjectDisposedException

            Debug.WriteLine($"Exception. @MpdQueryAlbumArtForAlbumView: {e}");

            res.ErrorMessage = "Exception. @MpdQueryAlbumArtForAlbumView" + e.Message;
        }

        return res;
    }

    #endregion

    #region == Command Connection's MPD Commands with boolean result ==

    public async Task<CommandResult> MpdSendIdle()
    {
        return await MpdCommandSendCommand("idle player");
    }

    public async Task<CommandResult> MpdSendNoIdle()
    {
        return await MpdCommandSendCommand("noidle");
    }

    public async Task<CommandResult> MpdSendUpdate()
    {
        CommandResult result = await MpdCommandSendCommand("update");

        return result;
    }

    public async Task<CommandResult> MpdPlaybackPlay(int volume, string songId = "")
    {
        string cmd = "play";

        if (!string.IsNullOrEmpty(songId))
        {
            cmd = "playid " + songId;
        }

        if ((MpdStatus.MpdState == Status.MpdPlayState.Play) || (MpdStatus.MpdState == Status.MpdPlayState.Pause))
        {
            // stop?
        }
        else if (MpdStatus.MpdState == Status.MpdPlayState.Stop)
        {

        }

        if (MpdStatus.MpdVolumeIsSet)
        {
            CommandResult result = await MpdCommandSendCommand(cmd);

            return result;
        }
        else
        {
            string cmdList = "command_list_begin" + "\n";
            cmdList = cmdList + cmd + "\n";
            cmdList = cmdList + "setvol " + volume.ToString() + "\n";
            cmdList = cmdList + "command_list_end" + "\n";

            CommandResult result = await MpdCommandSendCommand(cmdList);

            return result;
        }
    }

    public async Task<CommandResult> MpdPlaybackPause()
    {
        CommandResult result = await MpdCommandSendCommand("pause 1");

        return result;
    }

    public async Task<CommandResult> MpdPlaybackResume(int volume)
    {
        if (MpdStatus.MpdVolumeIsSet)
        {
            CommandResult result = await MpdCommandSendCommand("pause 0");

            return result;
        }
        else
        {
            string cmd = "command_list_begin" + "\n";
            cmd += "pause 0\n";
            cmd = cmd + "setvol " + volume.ToString() + "\n";
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdCommandSendCommand(cmd);

            return result;
        }
    }

    public async Task<CommandResult> MpdPlaybackStop()
    {
        CommandResult result = await MpdCommandSendCommand("stop");

        return result;
    }

    public async Task<CommandResult> MpdPlaybackNext(int volume)
    {
        if (MpdStatus.MpdVolumeIsSet)
        {
            CommandResult result = await MpdCommandSendCommand("next");

            return result;
        }
        else
        {
            string cmd = "command_list_begin" + "\n";
            cmd += "next\n";
            cmd = cmd + "setvol " + volume.ToString() + "\n";
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdCommandSendCommand(cmd);

            return result;
        }
    }

    public async Task<CommandResult> MpdPlaybackPrev(int volume)
    {
        if (MpdStatus.MpdVolumeIsSet)
        {
            CommandResult result = await MpdCommandSendCommand("previous");

            return result;
        }
        else
        {
            string cmd = "command_list_begin" + "\n";
            cmd += "previous\n";
            cmd = cmd + "setvol " + volume.ToString() + "\n";
            cmd = cmd + "command_list_end" + "\n";

            CommandResult result = await MpdCommandSendCommand(cmd);

            return result;
        }
    }

    public async Task<CommandResult> MpdSetVolume(int v)
    {
        /*
        if ((v == MpdStatus.MpdVolume) && (MpdStatus.MpdVolumeIsSet))
        {
            CommandResult f = new()
            {
                IsSuccess = true
            };
            return f;
        }
        */
        CommandResult result = await MpdCommandSendCommand("setvol " + v.ToString());

        return result;
    }

    public async Task<CommandResult> MpdPlaybackSeek(string songId, double seekTime)
    {
        if ((songId == "") || (seekTime == 0))
        {
            CommandResult f = new()
            {
                IsSuccess = true
            };
            return f;
        }

        CommandResult result = await MpdCommandSendCommand("seekid " + songId + " " + seekTime.ToString());

        return result;
    }

    public async Task<CommandResult> MpdSetRepeat(bool on)
    {
        if (MpdStatus.MpdRepeat == on)
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

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdSetRandom(bool on)
    {
        if (MpdStatus.MpdRandom == on)
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

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdSetConsume(bool on)
    {
        if (MpdStatus.MpdConsume == on)
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

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdSetSingle(bool on)
    {
        if (MpdStatus.MpdSingle == on)
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

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdClear()
    {
        CommandResult result = await MpdCommandSendCommand("clear");

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

        CommandResult result = await MpdCommandSendCommand("save \"" + playlistName + "\"");

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

        CommandResult result = await MpdCommandSendCommand("add \"" + uri + "\"");

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

        CommandResult result = await MpdCommandSendCommand(cmd);

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

        return await MpdCommandSendCommand(cmd);
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

        return await MpdCommandSendCommand(cmd);
    }

    public async Task<CommandResult> MpdMoveId(Dictionary<string, string> idToNewPosPair)
    {
        if (idToNewPosPair is null)
        {
            CommandResult f = new()
            {
                IsSuccess = false
            };
            return f;
        }
        if (idToNewPosPair.Count < 1)
        {
            CommandResult f = new()
            {
                IsSuccess = false
            };
            return f;
        }

        string cmd = "command_list_begin" + "\n";
        foreach (KeyValuePair<string, string> pair in idToNewPosPair)
        {
            cmd = cmd + "moveid " + pair.Key + " " + pair.Value + "\n";
        }
        cmd = cmd + "command_list_end" + "\n";

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdMultiplePlay(List<string> uris, int volume)
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
        cmd = cmd + "clear" + "\n";
        foreach (var uri in uris)
        {
            cmd = cmd + "add \"" + Regex.Escape(uri) + "\"\n";
        }
        cmd = cmd + "play" + "\n";
        if (!MpdStatus.MpdVolumeIsSet)
        {
            cmd = cmd + "setvol " + volume.ToString() + "\n";
        }
        cmd = cmd + "currentsong" + "\n";
        cmd = cmd + "command_list_end" + "\n";

        CommandResult result = await MpdCommandSendCommand(cmd);

        if (result.IsSuccess)
        {
            await ParseCurrentSong(result.ResultText);
        }

        return result;
    }

    public async Task<CommandResult> MpdSinglePlay(string uri, int volume)
    {
        if (string.IsNullOrEmpty(uri))
        {
            CommandResult f = new()
            {
                IsSuccess = false
            };
            return f;
        }

        string cmd = "command_list_begin" + "\n";
        cmd = cmd + "clear" + "\n";
        cmd = cmd + "add \"" + Regex.Escape(uri) + "\"\n";
        cmd = cmd + "play" + "\n";
        if (!MpdStatus.MpdVolumeIsSet)
        {
            cmd = cmd + "setvol " + volume.ToString() + "\n";
        }
        cmd = cmd + "currentsong" + "\n";
        cmd = cmd + "command_list_end" + "\n";

        CommandResult result = await MpdCommandSendCommand(cmd);

        if (result.IsSuccess)
        {
            await ParseCurrentSong(result.ResultText);
        }

        return result;
    }

    public async Task<CommandResult> MpdChangePlaylist(string playlistName, int volume)
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
        //cmd = cmd + "stop" + "\n";
        cmd = cmd + "clear" + "\n";
        cmd = cmd + "load \"" + playlistName + "\"\n";
        cmd = cmd + "play" + "\n";
        if (!MpdStatus.MpdVolumeIsSet)
        {
            cmd = cmd + "setvol " + volume.ToString() + "\n";
        }
        cmd = cmd + "currentsong" + "\n";
        cmd = cmd + "command_list_end" + "\n";

        CommandResult result = await MpdCommandSendCommand(cmd);

        if (result.IsSuccess)
        {
            await ParseCurrentSong(result.ResultText);
        }

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

        CommandResult result = await MpdCommandSendCommand(cmd);

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

        CommandResult result = await MpdCommandSendCommand(cmd);

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

        CommandResult result = await MpdCommandSendCommand(cmd);

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
        if (uris is null)
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

        CommandResult result = await MpdCommandSendCommand(cmd);

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

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdPlaylistMove(string playlistName, Dictionary<string, string> posToNewPosPair)
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

        //playlistmove {NAME} [{FROM} | {START:END}] {TO}
        string cmd = "playlistmove \"" + playlistName + "\"" + " " + posToNewPosPair.ToList()[0].Key.ToString() + " " + posToNewPosPair.ToList()[0].Value.ToString();

        // MPD does not support multiple move in a command list.
        /*
        string cmd = "command_list_begin" + "\n";
        foreach (var kvp in posToNewPosPair)
        {
            cmd = cmd + "playlistmove \"" + playlistName + "\"" + " " + kvp.Key.ToString() + " " + kvp.Value.ToString() + "\n";
        }
        cmd = cmd + "command_list_end" + "\n";
        */
        CommandResult result = await MpdCommandSendCommand(cmd);
        Debug.WriteLine(cmd);
        return result;
    }

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

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdToggleOutput(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            CommandResult f = new()
            {
                IsSuccess = false
            };
            return f;
        }

        string cmd = "toggleoutput " + id;

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    public async Task<CommandResult> MpdClearError()
    {
        string cmd = "clearerror";

        CommandResult result = await MpdCommandSendCommand(cmd);

        return result;
    }

    #endregion

    #region == Response parser methods ==

    private Task<bool> ParseProtocolFeatures(string result)
    {
        if (MpdStop) { return Task.FromResult(false); }
        if (string.IsNullOrEmpty(result)) return Task.FromResult(false);

        // Not working with MPD 0.24.4?

        Debug.WriteLine(result);

        return Task.FromResult(true); 
    
    }

    private Task<bool> ParseCommands(string result)
    {
        if (MpdStop) { return Task.FromResult(false); }
        if (string.IsNullOrEmpty(result)) return Task.FromResult(false);
        List<string> resultLines = result.Split('\n').ToList();
        if (resultLines.Count == 0) return Task.FromResult(false);

        //Debug.WriteLine(result);

        Commands.Clear();

        foreach (string value in resultLines)
        {
            if (value.StartsWith("command: "))
            {
                Commands.Add(value.Replace("command: ", "").Trim());
            }
        }

        return Task.FromResult(true);
    }

    private Task<bool> ParseOutputs(string result)
    {
        if (MpdStop) { return Task.FromResult(false); }
        if (string.IsNullOrEmpty(result)) return Task.FromResult(false);

        List<string> resultLines = result.Split('\n').ToList();
        if (resultLines.Count == 0) return Task.FromResult(false);

        //Debug.WriteLine(result);
        /*
            outputid: 0
            outputname: 1 - EX-LD4K321V
            plugin: winmm
            outputenabled: 1
            outputid: 1
            outputname: Speakers
            plugin: winmm
            outputenabled: 1
            OK
        */
        try
        {
            //IsBusy?.Invoke(this, true);

            AudioOutputs.Clear();

            int i = 0;

            AudioOutput? output = null;

            foreach (string value in resultLines)
            {
                if (value.StartsWith("outputid:"))
                {
                    if (output is not null)
                    {
                        if (!string.IsNullOrEmpty(output.Id))
                        {
                            AudioOutputs.Add(output);
                        }
                    }

                    output = new AudioOutput
                    {
                        Id = value.Replace("outputid: ", "")
                    };

                    i++;
                    MpcProgress?.Invoke(this, $"[Background] Parsing AudioOutpus ({i})...");
                }
                else if (value.StartsWith("outputname:"))
                {
                    output?.Name = value.Replace("outputname: ", "").Trim();
                }
                else if (value.StartsWith("outputenabled:"))
                {
                    output?.Enabled = value.Replace("outputenabled: ", "").Trim();
                }
                else if (value.StartsWith("OK"))
                {
                    if (output is not null)
                    {
                        if (!string.IsNullOrEmpty(output.Id))
                        {
                            AudioOutputs.Add(output);
                        }
                    }
                }
                else
                {
                    //Debug.WriteLine(value);
                }
            }

            MpcProgress?.Invoke(this, "[Background] Parsing outputs is done.");
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error@ParseOutputs: " + e);
            _dispatcherService.TryEnqueue(() =>
            {
                (App.Current as App)?.AppendErrorLog("Exception@MPC@ParseOutputs", e.Message);
            });
            //IsBusy?.Invoke(this, false);
            return Task.FromResult(false); ;
        }
        finally
        {
            IsBusy?.Invoke(this, false);
        }

        return Task.FromResult(true);
    }

    private Task<bool> ParseStatus(string result)
    {
        if (MpdStop) { return Task.FromResult(false); }
        if (string.IsNullOrEmpty(result)) return Task.FromResult(false);

        if (result.Trim() == "OK")
        {
            DebugCommandOutput?.Invoke(this, "################(Error) " + "An empty result (OK) returened for a status command." + Environment.NewLine + Environment.NewLine);
            DebugCommandOutput?.Invoke(this,
                $"################ Error: @ParseStatus, Reason: An empty result (OK) returened for a status command., Data: , {Environment.NewLine} Exception:  {Environment.NewLine + Environment.NewLine}");

            Debug.WriteLine("@ParseStatus: An empty result (OK)  returened for a status command.");

            return Task.FromResult(false);
        }

        List<string> resultLines = result.Split('\n').ToList();

        if (resultLines.Count == 0) return Task.FromResult(false);

        var comparer = StringComparer.OrdinalIgnoreCase;
        Dictionary<string, string> mpdStatusValues = new(comparer);

        try
        {
            IsBusy?.Invoke(this, true);
            /*
            //Application.Current.Dispatcher.Invoke(() =>
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {

            });
            */

            MpdStatus.Reset();

            foreach (string line in resultLines)
            {
                string[] statusValuePair = line.Split(':');
                if (statusValuePair.Length > 1)
                {
                    if (mpdStatusValues.ContainsKey(statusValuePair[0].Trim()))
                    {
                        // new

                        mpdStatusValues[statusValuePair[0].Trim()] = line.Replace(statusValuePair[0].Trim() + ": ", "");
                    }
                    else
                    {
                        mpdStatusValues.Add(statusValuePair[0].Trim(), line.Replace(statusValuePair[0].Trim() + ": ", ""));
                    }
                }
            }

            // Play state
            if (mpdStatusValues.TryGetValue("state", out var valueState))
            {
                switch (valueState)
                {
                    case "play":
                        {
                            MpdStatus.MpdState = Status.MpdPlayState.Play;
                            break;
                        }
                    case "pause":
                        {
                            MpdStatus.MpdState = Status.MpdPlayState.Pause;
                            break;
                        }
                    case "stop":
                        {
                            MpdStatus.MpdState = Status.MpdPlayState.Stop;
                            break;
                        }
                    default:
                        //throw new ArgumentOutOfRangeException("state");
                        break;
                }
            }

            // Volume
            if (mpdStatusValues.TryGetValue("volume", out var valueVolume))
            {
                if (!string.IsNullOrEmpty(valueVolume))
                {
                    //Debug.WriteLine("volume is set to " + valueVolume + " @ParseStatus()");
                    MpdStatus.MpdVolume = Int32.Parse(valueVolume);

                    MpdStatus.MpdVolumeIsReturned = true;
                }
                else
                {
                    MpdStatus.MpdVolumeIsReturned = false;
                }
            }
            else
            {
                MpdStatus.MpdVolumeIsReturned = false;
            }

                // songID
                MpdStatus.MpdSongID = "";
            if (mpdStatusValues.TryGetValue("songid", out string? value))
            {
                MpdStatus.MpdSongID = value;
            }

            // Repeat opt bool.
            if (mpdStatusValues.ContainsKey("repeat"))
            {
                try
                {
                    if (mpdStatusValues["repeat"] == "1")
                    {
                        MpdStatus.MpdRepeat = true;
                    }
                    else
                    {
                        MpdStatus.MpdRepeat = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            // Random opt bool.
            if (mpdStatusValues.ContainsKey("random"))
            {
                try
                {
                    if (Int32.Parse(mpdStatusValues["random"]) > 0)
                    {
                        MpdStatus.MpdRandom = true;
                    }
                    else
                    {
                        MpdStatus.MpdRandom = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            // Consume opt bool.
            if (mpdStatusValues.ContainsKey("consume"))
            {
                try
                {
                    if (Int32.Parse(mpdStatusValues["consume"]) > 0)
                    {
                        MpdStatus.MpdConsume = true;
                    }
                    else
                    {
                        MpdStatus.MpdConsume = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            // Single opt bool.
            if (mpdStatusValues.ContainsKey("single"))
            {
                try
                {
                    if (Int32.Parse(mpdStatusValues["single"]) > 0)
                    {
                        MpdStatus.MpdSingle = true;
                    }
                    else
                    {
                        MpdStatus.MpdSingle = false;
                    }

                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            // Song time. deprecated. 
            if (mpdStatusValues.ContainsKey("time"))
            {
                try
                {
                    if (mpdStatusValues["time"].Split(':').Length > 1)
                    {
                        MpdStatus.MpdSongTime = Double.Parse(mpdStatusValues["time"].Split(':')[1].Trim());
                        MpdStatus.MpdSongElapsed = Double.Parse(mpdStatusValues["time"].Split(':')[0].Trim());
                    }
                }
                catch (FormatException e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                }
            }

            // Song time elapsed.
            if (mpdStatusValues.TryGetValue("elapsed", out string? value1))
            {
                try
                {
                    MpdStatus.MpdSongElapsed = Double.Parse(value1);
                }
                catch { }
            }

            // Song duration.
            if (mpdStatusValues.TryGetValue("duration", out string? value2))
            {
                try
                {
                    MpdStatus.MpdSongTime = Double.Parse(value2);
                }
                catch { }
            }

            // Error
            if (mpdStatusValues.ContainsKey("error"))
            {
                MpdStatus.MpdError = mpdStatusValues["error"];
            }
            else
            {
                MpdStatus.MpdError = "";
            }


            // TODO: more?

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception@ParseStatus:" + ex.Message);

            _dispatcherService.TryEnqueue(() =>
            {
                (App.Current as App)?.AppendErrorLog("Exception@MPC@ParseStatus", ex.Message);
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

        if (resultLines is null)
            return Task.FromResult(true);
        if (resultLines.Count == 0)
            return Task.FromResult(true);

        if (isEmptyResult)
        {
            return Task.FromResult(true);
        }

        IsBusy?.Invoke(this, true);

        try
        {
            var comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> songValues = new(comparer);

            foreach (string value in resultLines)
            {
                string[] statusValuePair = value.Trim().Split(':');
                if (statusValuePair.Length > 1)
                {
                    if (songValues.ContainsKey(statusValuePair[0].Trim()))
                    {
                        // Shouldn't be happening here except "Genre"
                        if (statusValuePair[0].Trim() == "Genre")
                        {
                            songValues["Genre"] = songValues["Genre"] + "/" + value.Replace(statusValuePair[0].Trim() + ": ", "");
                        }
                    }
                    else
                    {
                        songValues.TryAdd(statusValuePair[0].Trim(), value.Replace(statusValuePair[0].Trim() + ": ", ""));
                    }
                }
            }

            if ((songValues.Count > 0) && songValues.ContainsKey("Id"))
            {
                SongInfoEx? sng = FillSongInfoEx(songValues, -1);

                if (sng is not null)
                {
                    if (MpdCurrentSong?.Id != sng.Id)
                        MpdCurrentSong = sng;
                }

                songValues.Clear();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error@ParseCurrentSong: " + ex.Message);

            _dispatcherService.TryEnqueue(() =>
            {
                (App.Current as App)?.AppendErrorLog("Exception@MPC@ParseCurrentSong", ex.Message);
            });

            return Task.FromResult(false);
        }
        finally
        {
            IsBusy?.Invoke(this, false);
        }

        return Task.FromResult(true);
    }

    private async Task<bool> ParsePlaylistInfo(string result)
    {
        if (MpdStop) return false;

        bool isEmptyResult = false;

        if (string.IsNullOrEmpty(result))
            isEmptyResult = true;

        if (result.Trim() == "OK")
            isEmptyResult = true;

        List<string> resultLines = result.Split('\n').ToList();

        if (resultLines is null)
            isEmptyResult = true;
        if (resultLines is not null)
        {
            if (resultLines.Count == 0)
                isEmptyResult = true;
        }

        if (isEmptyResult)
        {
            await _dispatcherService.EnqueueAsync(() =>
            {
                CurrentQueue.Clear();
            });

            return true;
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

            ObservableCollection<SongInfoEx> tmpQueue = [];
            /*
            if (Application.Current is null) { return Task.FromResult(false); }
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentQueue.Clear();
            });
            */

            var comparer = StringComparer.OrdinalIgnoreCase;
            Dictionary<string, string> songValues = new(comparer);

            int i = 0;

            if (resultLines is null)
                return true;

            foreach (string value in resultLines)
            {
                string[] statusValuePair = value.Trim().Split(':');
                if (statusValuePair.Length > 1)
                {
                    //if (SongValues.ContainsKey(StatusValuePair[0].Trim()))
                    if (statusValuePair[0].Trim().Equals("file"))
                    {
                        if (songValues.ContainsKey("Id"))
                        {
                            SongInfoEx? sng = FillSongInfoEx(songValues, i);

                            if (sng is not null)
                            {
                                //CurrentQueue.Add(sng);
                                tmpQueue.Add(sng);

                                i++;

                                MpcProgress?.Invoke(this, $"[Background] Parsing queue item... ({i})");

                                songValues.Clear();
                            }
                        }

                        songValues.Clear();
                        songValues.Add(statusValuePair[0].Trim(), value.Replace(statusValuePair[0].Trim() + ": ", ""));
                    }
                    else
                    //if (!SongValues.ContainsKey(StatusValuePair[0].Trim()))
                    {
                        songValues.TryAdd(statusValuePair[0].Trim(), value.Replace(statusValuePair[0].Trim() + ": ", ""));
                    }
                }
            }

            if ((songValues.Count > 0) && songValues.ContainsKey("Id"))
            {
                SongInfoEx? sng = FillSongInfoEx(songValues, i);

                if (sng is not null)
                {
                    songValues.Clear();
                    /*
                    if (Application.Current is null) { return Task.FromResult(false); }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CurrentQueue.Add(sng);
                    });
                    */
                    //CurrentQueue.Add(sng);
                    tmpQueue.Add(sng);

                    MpcProgress?.Invoke(this, $"[Background] Parsing queue item... ({i + 1})");
                }
            }

            // test
            MpcProgress?.Invoke(this, "[Background] Updating internal queue list...");

            await _dispatcherService.EnqueueAsync(async () =>
            {
                //CurrentQueue.Clear();
                //_queue = tmpQueue;
                this.CurrentQueue = new ObservableCollection<SongInfoEx>(tmpQueue);
            });
            MpcProgress?.Invoke(this, "[Background] Internal queue list is updated.");

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Exception@ParsePlaylistInfo: " + ex.Message);

            _dispatcherService.TryEnqueue(() =>
            {
                (App.Current as App)?.AppendErrorLog("Exception@MPC@ParsePlaylistInfo", ex.Message);
                (App.Current as App)?.SaveErrorLog();
            });

            return false;
        }
        finally
        {
            IsBusy?.Invoke(this, false);
        }

        return true;
    }

    private SongInfoEx? FillSongInfoEx(Dictionary<string, string> songValues, int i)
    {
        try
        {
            if (songValues.ContainsKey("Id"))
            {
                SongInfoEx sng = new();

                if (songValues.ContainsKey("Id"))
                {
                    sng.Id = songValues["Id"];
                }

                if (songValues.ContainsKey("Title"))
                {
                    sng.Title = songValues["Title"];
                }
                else
                {
                    sng.Title = "";
                    if (songValues.ContainsKey("file"))
                    {
                        sng.Title = Path.GetFileName(songValues["file"]);
                    }
                }

                if (songValues.ContainsKey("Artist"))
                {
                    sng.Artist = songValues["Artist"];
                }
                else
                {
                    if (songValues.ContainsKey("AlbumArtist"))
                    {
                        // TODO: Should I?
                        sng.Artist = songValues["AlbumArtist"];
                    }
                    else
                    {
                        sng.Artist = "";
                    }
                }

                if (songValues.ContainsKey("Last-Modified"))
                {
                    sng.LastModified = songValues["Last-Modified"];
                }

                if (songValues.ContainsKey("AlbumArtist"))
                {
                    sng.AlbumArtist = songValues["AlbumArtist"];
                }

                if (songValues.ContainsKey("Album"))
                {
                    sng.Album = songValues["Album"];
                }

                if (songValues.ContainsKey("Track"))
                {
                    sng.Track = songValues["Track"];
                }

                if (songValues.ContainsKey("Disc"))
                {
                    sng.Disc = songValues["Disc"];
                }

                if (songValues.ContainsKey("Date"))
                {
                    sng.Date = songValues["Date"];
                }

                if (songValues.ContainsKey("Genre"))
                {
                    sng.Genre = songValues["Genre"];
                }

                if (songValues.ContainsKey("Composer"))
                {
                    sng.Composer = songValues["Composer"];
                }

                if (songValues.ContainsKey("Time"))
                {
                    sng.Time = songValues["Time"];
                }

                if (songValues.ContainsKey("duration"))
                {
                    sng.Time = songValues["duration"];
                    sng.Duration = songValues["duration"];
                }

                if (songValues.ContainsKey("Pos"))
                {
                    sng.Pos = songValues["Pos"];
                }

                if (songValues.ContainsKey("file"))
                {
                    sng.File = songValues["file"];
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
                if (sng.Id == MpdStatus.MpdSongID)
                {
                    MpdCurrentSong = sng;
                }

                return sng;
            }
            else
            {
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error@FillSongInfoEx: " + e);

            _dispatcherService.TryEnqueue(() =>
            {
                (App.Current as App)?.AppendErrorLog("Exception@MPC@FillSongInfoEx", e.Message);
                });

            return null;
        }
    }

    private async Task<bool> ParsePlaylists(string result)
    {
        bool isEmptyResult = false;

        if (string.IsNullOrEmpty(result))
            isEmptyResult = true;

        List<string> resultLines = result.Split('\n').ToList();

        if (resultLines.Count == 0)
            isEmptyResult = true;

        if (isEmptyResult)
        {
            //Application.Current.Dispatcher.Invoke(() =>
            await _dispatcherService.EnqueueAsync(async () =>
            {
                Playlists.Clear();
            });

            return true;
        }

        try
        {
            IsBusy?.Invoke(this, true);

            ObservableCollection<Playlist> tmpPlaylists = [];

            Playlist? pl = null;

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
                    pl?.LastModified = value.Replace("Last-Modified: ", "");
                }
                else if (value.StartsWith("OK"))
                {
                    // Ignoring.
                }
            }

            await _dispatcherService.EnqueueAsync(async () =>
            {
                this.Playlists = new ObservableCollection<Playlist>(tmpPlaylists);
            });

            /*
            if (Application.Current is null) { return Task.FromResult(false); }
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
                        if (pl is not null)
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
            Debug.WriteLine("Error@ParsePlaylists: " + e);

            _dispatcherService.TryEnqueue(() => { (App.Current as App)?.AppendErrorLog("Exception@MPC@ParsePlaylists", e.Message); });

            IsBusy?.Invoke(this, false);
            return false;
        }
        finally
        {
            IsBusy?.Invoke(this, false);
        }

        return true;
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
            /*
            //Application.Current.Dispatcher.Invoke(() =>
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                LocalFiles.Clear();
                LocalDirectories.Clear();
            });
            */
            LocalFiles.Clear();
            LocalDirectories.Clear();

            SongFile? song = null;

            int i = 0;

            foreach (string value in resultLines)
            {
                //Debug.WriteLine("LocalDirectories: " + value);
                if (value.StartsWith("directory:"))
                {
                    /*
                    App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                    {
                        LocalDirectories.Add(value.Replace("directory: ", ""));
                    });
                    */
                    LocalDirectories.Add(value.Replace("directory: ", ""));

                    MpcProgress?.Invoke(this, $"[Background] Parsing files and directories ({i})...");
                }
                else if (value.StartsWith("file:"))
                {
                    song = new SongFile
                    {
                        File = value.Replace("file: ", "")
                    };
                    /*
                    App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                    {
                        LocalFiles.Add(song);
                    });
                    */
                    LocalFiles.Add(song);

                    i++;

                    MpcProgress?.Invoke(this, $"[Background] Parsing files and directories ({i})...");
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
            Debug.WriteLine("Error@ParseListAll: " + e);

            _dispatcherService.TryEnqueue(() =>{ (App.Current as App)?.AppendErrorLog("Exception@MPC@ParseListAll", e.Message); });

            IsBusy?.Invoke(this, false);
            return Task.FromResult(false); ;
        }
        finally
        {
            IsBusy?.Invoke(this, false);
        }

        return Task.FromResult(true);
    }

    private static string RemoveThePrefix(string? value, string prefix)
    {
        if (string.IsNullOrEmpty(value) || string.IsNullOrEmpty(prefix))
        {
            return string.Empty;
        }

        //string prefix = "The ";
        prefix += " ";

        if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return value[prefix.Length..];
        }
        else
        {
            return value;
        }
    }

    private Task<bool> ParseListAlbumGroupAlbumArtist(string result)
    {
        if (MpdStop) return Task.FromResult(false);

        if (string.IsNullOrEmpty(result)) return Task.FromResult(false);

        List<string> resultLines = result.Split('\n').ToList();

        if (resultLines.Count == 0) return Task.FromResult(false);

        try
        {
            IsBusy?.Invoke(this, true);

            AlbumArtists.Clear();
            Albums.Clear();

            int i = 0;

            AlbumArtist? arts = null;
            foreach (string value in resultLines)
            {
                if (value.StartsWith("AlbumArtist:"))
                {
                    if (arts is not null)
                    {
                        if (!string.IsNullOrEmpty(arts.Name))
                        {
                            arts.NameSort = RemoveThePrefix(arts.Name, "The");

                            AlbumArtists.Add(arts);
                        }
                    }

                    arts = new AlbumArtist
                    {
                        Name = value.Replace("AlbumArtist: ", "")
                    };
                    //AlbumArtists.Add(arts);

                    MpcProgress?.Invoke(this, $"[Background] Parsing AlbumArtists ({i})...");
                }
                else if (value.StartsWith("Album:"))
                {
                    var albx = new AlbumEx
                    {
                        Name = value.Replace("Album: ", ""),
                        AlbumArtist = arts?.Name ?? "",
                        AlbumArtistSort = RemoveThePrefix(arts?.Name, "The")
                    };
                    //Debug.WriteLine(albx.AlbumArtistSort);

                    if (albx.Name.StartsWith("The ", StringComparison.OrdinalIgnoreCase))
                    {
                        albx.NameSort = RemoveThePrefix(albx.Name, "The");
                    }
                    else if (albx.Name.StartsWith("A ", StringComparison.OrdinalIgnoreCase))
                    {
                        albx.NameSort = RemoveThePrefix(albx.Name, "A");
                    }
                    else if (albx.Name.StartsWith("An ", StringComparison.OrdinalIgnoreCase))
                    {
                        albx.NameSort = RemoveThePrefix(albx.Name, "An");
                    }
                    else
                    {
                        albx.NameSort = albx.Name;
                    }

                    arts?.Albums.Add(albx);

                    // Create Albums at the same time.
                    if ((!string.IsNullOrEmpty(albx.Name)))// (!string.IsNullOrEmpty(arts?.Name.Trim()))
                    {
                        Albums.Add(albx);
                    }

                    i++;

                    MpcProgress?.Invoke(this, $"[Background] Parsing albumartists and albums ({i})...");
                }
                else if ((value.StartsWith("OK")))
                {
                    if (arts is not null)
                    {
                        if (!string.IsNullOrEmpty(arts.Name))
                        {
                            arts.NameSort = RemoveThePrefix(arts.Name, "The");

                            AlbumArtists.Add(arts);
                        }
                    }
                }
                else
                {
                    //Debug.WriteLine(value);
                }
            }

            MpcProgress?.Invoke(this, "[Background] Parsing albumartists and albums is done.");
        }
        catch (Exception e)
        {
            Debug.WriteLine("Error@ParseListAlbumGroupAlbumArtist: " + e);

            _dispatcherService.TryEnqueue(() =>{ (App.Current as App)?.AppendErrorLog("Exception@MPC@ParseListAlbumGroupAlbumArtist", e.Message); });

            IsBusy?.Invoke(this, false);
            return Task.FromResult(false); ;
        }
        finally
        {
            IsBusy?.Invoke(this, false);
        }

        return Task.FromResult(true);
    }

    private Task<ObservableCollection<SongInfo>> ParseSearchResult(string result)
    {
        var res = new ObservableCollection<SongInfo>();

        if (MpdStop) return Task.FromResult(res);

        //App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        //{
        //    SearchResult.Clear();
        //});

        if (string.IsNullOrEmpty(result)) return Task.FromResult(res);

        //if (result.Trim() == "OK") return true;

        List<string> resultLines = result.Split('\n').ToList();

        if (resultLines is null) return Task.FromResult(res);
        if (resultLines.Count == 0) return Task.FromResult(res);



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
            Dictionary<string, string> songValues = new(comparer);
            //Dictionary<string, string> SongValues = new();

            int i = 0;

            foreach (string line in resultLines)
            {
                //Debug.WriteLine(line);
                string[] valuePair = line.Split(':');
                if (valuePair.Length > 1)
                {
                    if (valuePair[0].Trim().Equals("file"))
                    {
                        // save old one and clear songvalues.
                        if (songValues.ContainsKey("file"))// && SongValues.ContainsKey("duration")
                        {
                            //Debug.WriteLine(SongValues["file"]);
                            SongInfo? sng = FillSongInfo(songValues, i);

                            songValues.Clear();

                            if (sng is not null)
                            {
                                i++;

                                res.Add(sng);
                            }

                            MpcProgress?.Invoke(this, $"[Background] Parsing search result item... ({i})");

                        }


                        songValues.Clear();
                        songValues.Add(valuePair[0].Trim(), line.Replace(valuePair[0].Trim() + ": ", ""));
                    }
                    else
                    {
                        songValues.TryAdd(valuePair[0].Trim(), line.Replace(valuePair[0].Trim() + ": ", ""));

                    }
                }
            }

            // last one
            if ((songValues.Count > 0) && songValues.ContainsKey("file"))
            {
                SongInfo? sng = FillSongInfo(songValues, i);

                songValues.Clear();

                if (sng is not null)
                {
                    res.Add(sng);
                }

                MpcProgress?.Invoke(this, $"[Background] Parsing search result item... ({i + 1})");
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error@ParseSearchResult: " + ex.Message);

            _dispatcherService.TryEnqueue(() => { (App.Current as App)?.AppendErrorLog("Exception@MPC@ParseSearchResult", ex.Message); });

            IsBusy?.Invoke(this, false);
            return Task.FromResult(res);
        }
        finally
        {
            IsBusy?.Invoke(this, false);

            MpcProgress?.Invoke(this, "[Background] Search result loaded.");
        }

        return Task.FromResult(res);
    }

    private ObservableCollection<SongInfo> ParsePlaylistSongsResult(string result)
    {
        ObservableCollection<SongInfo> songList = [];

        if (MpdStop) return songList;

        if (string.IsNullOrEmpty(result)) return songList;

        if (result.Trim() == "OK") return songList;

        List<string> resultLines = result.Split('\n').ToList();

        if (resultLines is null) return songList;
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
            Dictionary<string, string> songValues = new(comparer);

            int i = 0;

            foreach (string line in resultLines)
            {
                string[] valuePair = line.Split(':');
                if (valuePair.Length > 1)
                {
                    if (valuePair[0].Trim().Equals("file"))
                    {
                        // Contains means new one.

                        // save old one and clear songvalues.
                        if (songValues.ContainsKey("file")) // && SongValues.ContainsKey("duration")
                        {
                            SongInfo? sng = FillSongInfo(songValues, i);
                            
                            songValues.Clear();

                            if (sng is not null)
                            {
                                songList.Add(sng);

                                i++;
                            }

                            MpcProgress?.Invoke(this, $"[Background] Parsing playlist item... ({i})");
                        }

                        // start over
                        songValues.Clear();
                        songValues.Add(valuePair[0].Trim(), line.Replace(valuePair[0].Trim() + ": ", ""));
                    }
                    else
                    {
                        //SongValues.Add(ValuePair[0].Trim(), line.Replace(ValuePair[0].TrimStart() + ": ", ""));
                        songValues.TryAdd(valuePair[0].Trim(), line.Replace(valuePair[0].Trim() + ": ", ""));
                    }
                    /*
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
                    */

                }
            }

            // last one
            if ((songValues.Count > 0) && songValues.ContainsKey("file"))
            {
                SongInfo? sng = FillSongInfo(songValues, i);

                songValues.Clear();

                if (sng is not null)
                {
                    songList.Add(sng);
                }

                MpcProgress?.Invoke(this, $"[Background] Parsing playlist item... ({i + 1})");
            }

        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error@ParsePlaylistSongsResult: " + ex.Message);

            _dispatcherService.TryEnqueue(() => { (App.Current as App)?.AppendErrorLog("Exception@MPC@ParsePlaylistSongsResult", ex.Message); });
            return songList;
        }

        return songList;
    }

    private static SongInfo? FillSongInfo(Dictionary<string, string> songValues, int i)
    {
        if (songValues.ContainsKey("file"))
        {
            SongInfo sng = new()
            {
                File = songValues["file"]
            };

            if (string.IsNullOrEmpty(sng.File.Trim()))
            {
                Debug.WriteLine("file is empty @FillSongInfo");
                return null;
            }

            if (songValues.ContainsKey("Title"))
            {
                sng.Title = songValues["Title"];
            }
            else
            {
                sng.Title = "";
                if (songValues.ContainsKey("file"))
                {
                    sng.Title = Path.GetFileName(songValues["file"]);
                }
            }

            if (songValues.ContainsKey("Artist"))
            {
                sng.Artist = songValues["Artist"];
            }
            /*
            else
            {
                if (SongValues.ContainsKey("AlbumArtist"))
                {
                    // TODO: Should I?
                    //sng.Artist = SongValues["AlbumArtist"];
                }
                else
                {
                    sng.Artist = "";
                }
            }
            */

            if (songValues.ContainsKey("Last-Modified"))
            {
                sng.LastModified = songValues["Last-Modified"];
            }

            if (songValues.ContainsKey("AlbumArtist"))
            {
                sng.AlbumArtist = songValues["AlbumArtist"];
            }

            if (songValues.ContainsKey("Album"))
            {
                sng.Album = songValues["Album"];
            }

            if (songValues.ContainsKey("Track"))
            {
                sng.Track = songValues["Track"];
            }

            if (songValues.ContainsKey("Disc"))
            {
                sng.Disc = songValues["Disc"];
            }

            if (songValues.ContainsKey("Date"))
            {
                sng.Date = songValues["Date"];
            }

            if (songValues.ContainsKey("Genre"))
            {
                sng.Genre = songValues["Genre"];
            }

            if (songValues.ContainsKey("Composer"))
            {
                sng.Composer = songValues["Composer"];
            }

            if (songValues.ContainsKey("Time"))
            {
                sng.Time = songValues["Time"];
            }

            if (songValues.ContainsKey("duration"))
            {
                sng.Time = songValues["duration"];
                sng.Duration = songValues["duration"];
            }
            /*
            if (SongValues.ContainsKey("file"))
            {
                sng.File = SongValues["file"];
            }
            */

            // for sorting. (and playlist pos)
            sng.Index = i;

            return sng;
        }
        else
        {
            return null;
        }
    }

    #endregion

    public void MpdDisconnect(bool isReconnect)
    {
        // This needs to be first.
        ConnectionState = ConnectionStatus.Disconnecting;

        MpdStop = true;

        _cts?.Cancel();

        try
        {
            IsBusy?.Invoke(this, true);

            ConnectionState = ConnectionStatus.Disconnecting;

            _commandConnection.Client?.Shutdown(SocketShutdown.Both);
            _commandConnection.Close();
        }
        catch (Exception ex)
        {
            _ = ex;
            //Debug.WriteLine($"Exception @MpdDisconnect on _commandConnection.Close() {ex}");
        }
        finally
        {
            IsBusy?.Invoke(this, false);
            ConnectionState = ConnectionStatus.DisconnectedByUser;
        }

        try
        {
            IsBusy?.Invoke(this, true);

            ConnectionState = ConnectionStatus.Disconnecting;

            _idleConnection.Client?.Shutdown(SocketShutdown.Both);
            _idleConnection.Close();
        }
        catch (Exception ex)
        {
            _ = ex;
            //Debug.WriteLine($"Exception @MpdDisconnect on _idleConnection.Close() {ex}");
        }
        finally
        {
            IsBusy?.Invoke(this, false);
            ConnectionState = ConnectionStatus.DisconnectedByUser;
        }

        _binaryDownloader.MpdBinaryConnectionDisconnect(isReconnect);

        ConnectionState = ConnectionStatus.DisconnectedByUser;
        IsBusy?.Invoke(this, false);

        if (!isReconnect)
        {
            _cts?.Dispose();
        }
    }
}

