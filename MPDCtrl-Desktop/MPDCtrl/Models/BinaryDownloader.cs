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
using System.Windows.Media.Imaging;

namespace MPDCtrl.Models
{
    public class BinaryDownloader
    {
        private static TcpClient _binaryConnection = new();
        private StreamReader _binaryReader;
        private StreamWriter _binaryWriter;

        private AlbumImage _albumCover = new();
        public AlbumImage AlbumCover { get => _albumCover; }

        private string _host;
        private int _port;
        //private string _password;

        private string MpdVersion { get; set; }

        public BinaryDownloader()
        {

        }

        public async Task<bool> MpdBinaryConnectionStart(string host, int port, string password)
        {
            ConnectionResult r = await MpdBinaryConnect(host, port);

            if (r.IsSuccess)
            {
                CommandResult d = await MpdBinarySendPassword(password);

                if (d.IsSuccess)
                {
                    // ここでIdleにして、以降はnoidle + cmd + idleの組み合わせでやる。
                    // ただし、実際にはidleのあとReadしていないからタイムアウトで切断されてしまう模様。

                    // awaitが必要だった。
                    //d = await MpdSendIdle();

                    return d.IsSuccess;
                }
            }

            return false;
        }

        private async Task<ConnectionResult> MpdBinaryConnect(string host, int port)
        {
            ConnectionResult result = new();

            //IsMpdCommandConnected = false;

            _binaryConnection = new TcpClient();

            _host = host;
            _port = port;

            //DebugCommandOutput?.Invoke(this, "TCP Command Connection: Connecting." + "\n" + "\n");

            //ConnectionState = ConnectionStatus.Connecting;

            //Debug.WriteLine("TCP Binary Connection: Connecting. " + host + " " + port.ToString());

            try
            {
                await _binaryConnection.ConnectAsync(_host, _port);

                if (_binaryConnection.Client == null)
                {
                    Debug.WriteLine("_binaryConnection.Client == null. " + host + " " + port.ToString());

                    result.ErrorMessage = "_binaryConnection.Client == null";

                    return result;
                }

                if (_binaryConnection.Client.Connected)
                {
                    //DebugCommandOutput?.Invoke(this, "TCP Command Connection: Connection established." + "\n" + "\n");

                    //ConnectionState = ConnectionStatus.Connected;

                    var tcpStream = _binaryConnection.GetStream();
                    //tcpStream.ReadTimeout = System.Threading.Timeout.Infinite;
                    //
                    tcpStream.ReadTimeout = 3000;

                    _binaryReader = new(tcpStream);
                    _binaryWriter = new(tcpStream);
                    _binaryWriter.AutoFlush = true;

                    string response = await _binaryReader.ReadLineAsync();

                    if (response.StartsWith("OK MPD "))
                    {
                        MpdVersion = response.Replace("OK MPD ", string.Empty).Trim();

                        //Debug.WriteLine("TCP Binary Connection: Connected. MPD " + VerText);

                        //DebugCommandOutput?.Invoke(this, "<<<<" + response.Trim() + "\n" + "\n");

                        //IsMpdCommandConnected = true;

                        result.IsSuccess = true;

                        // Done for now.
                    }
                    else
                    {
                        Debug.WriteLine("**** TCP Binary Connection: MPD did not respond with proper respose@MpdBinaryConnect");

                        //DebugCommandOutput?.Invoke(this, "TCP Command Connection: MPD did not respond with proper respose." + "\n" + "\n");

                        //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        //ConnectionError?.Invoke(this, "TCP connection error: MPD did not respond with proper respose.");
                    }
                }
                else
                {
                    //?

                    Debug.WriteLine("**** !client.Client.Connected@MpdBinaryConnect");

                    //DebugCommandOutput?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected." + "\n" + "\n");

                    //ConnectionState = ConnectionStatus.NeverConnected;

                    //ConnectionError?.Invoke(this, "TCP Command Connection: FAIL to established... Client not connected.");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("**** Exception@MpdBinaryConnect: " + e.Message);
                //DebugCommandOutput?.Invoke(this, "TCP Command Connection: Error while connecting. Fail to connect (Exception): " + e.Message + "\n" + "\n");

                //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                //ConnectionError?.Invoke(this, "TCP connection error: " + e.Message);
            }

            return result;
        }

        private async Task<CommandResult> MpdBinarySendPassword(string password = "")
        {
            //_password = password;

            CommandResult ret = new();

            if (string.IsNullOrEmpty(password))
            {
                ret.IsSuccess = true;
                ret.ResultText = "OK";//Or OK
                ret.ErrorMessage = "";

                return ret;
            }

            string cmd = "password " + password + "\n";

            return await MpdBinarySendCommand(cmd);

        }

        private async Task<CommandResult> MpdBinarySendCommand(string cmd, bool isAutoIdling = false)
        {
            CommandResult ret = new();

            if (_binaryConnection.Client == null)
            {
                return ret;
            }

            if ((_binaryWriter == null) || (_binaryReader == null))
            {
                Debug.WriteLine("@MpdBinarySendCommand: " + "_commandWriter or _commandReader is null");

                //ret.IsSuccess = false;
                //ret.ErrorMessage = "_commandWriter or _commandReader is null";

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error :@{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdSendCommand", "_commandWriter or _commandReader is null", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            if (!_binaryConnection.Client.Connected)
            {
                Debug.WriteLine("@MpdBinarySendCommand: " + "NOT IsMpdCommandConnected");

                //ret.IsSuccess = false;
                //ret.ErrorMessage = "NOT IsMpdCommandConnected";

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdSendCommand", "!CommandConnection.Client.Connected", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            // WriteAsync
            try
            {
                //IsBusy?.Invoke(this, true);

                if (cmd.Trim().StartsWith("idle"))
                {
                    //DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");

                    await _binaryWriter.WriteAsync(cmd.Trim() + "\n");

                    if (!isAutoIdling)
                    {
                        ret.IsSuccess = true;

                        //IsBusy?.Invoke(this, false);
                        return ret;
                    }
                }
                else
                {
                    string cmdDummy = cmd;
                    if (cmd.StartsWith("password "))
                        cmdDummy = "password ****";

                    cmdDummy = cmdDummy.Trim().Replace("\n", "\n" + ">>>>");

                    //if (isAutoIdling)
                    //DebugCommandOutput?.Invoke(this, ">>>>" + "noidle\n>>>>" + cmdDummy.Trim() + "\n>>>>idle player" + "\n" + "\n");
                    //else
                    //DebugCommandOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");

                    if (isAutoIdling)
                        await _binaryWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n" + "idle player\n");
                    else
                        await _binaryWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                /*
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
                        _commandConnection.Client.Shutdown(SocketShutdown.Both);
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
                */

                //IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                /*
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "WriteAsync@MpdSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    //ConnectionError?.Invoke(this, "The connection (command) has been terminated (Exception): " + e.Message);
                }
                */
                //IsBusy?.Invoke(this, false);
                return ret;
            }

            // ReadLineAsync
            try
            {
                //IsBusy?.Invoke(this, true);

                StringBuilder stringBuilder = new();

                bool isDoubleOk = false;
                //bool isAck = false;
                string ackText = "";
                bool isNullReturn = false;

                while (true)
                {
                    string line = await _binaryReader.ReadLineAsync();

                    if (line != null)
                    {
                        if (line.StartsWith("ACK"))
                        {
                            Debug.WriteLine("ACK line @MpdBinarySendCommand: " + cmd.Trim() + " and " + line);

                            if (!string.IsNullOrEmpty(line))
                                stringBuilder.Append(line + "\n");

                            ret.ErrorMessage = line;
                            ackText = line;
                            //isAck = true;

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
                    Debug.WriteLine("@MpdBinarySendCommand ReadLineAsync isNullReturn");
                    /*
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "ReadLineAsync received null data", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                    ret.ResultText = stringBuilder.ToString();
                    ret.ErrorMessage = "ReadLineAsync@MpdSendCommand received null data";

                    // タイムアウトしていたらここで「も」エラーになる模様。

                    IsMpdCommandConnected = false;

                    DebugCommandOutput?.Invoke(this, string.Format("Reconnecting... " + Environment.NewLine + Environment.NewLine));

                    try
                    {
                        _commandConnection.Client.Shutdown(SocketShutdown.Both);
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
                    */
                    //IsBusy?.Invoke(this, false);
                    return ret;
                }
                else
                {
                    //DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");

                    //if (isAck)
                    //    MpdAckError?.Invoke(this, ackText + " (@MSC)");

                    ret.ResultText = stringBuilder.ToString();

                    //IsBusy?.Invoke(this, false);
                    return ret;
                }

            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdBinarySendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "InvalidOperationException (Most likely the connection is overloaded)", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。

                Debug.WriteLine("IOException@MpdBinarySendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                /*
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
                        _commandConnection.Client.Shutdown(SocketShutdown.Both);
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
                */
                //IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadLineAsync@MpdSendCommand", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                //IsBusy?.Invoke(this, false);
                return ret;
            }

        }

        private async Task<CommandBinaryResult> MpdBinarySendBinaryCommand(string cmd, bool isAutoIdling = false)
        {
            CommandBinaryResult ret = new();

            if (_binaryConnection.Client == null)
            {
                Debug.WriteLine("@MpdBinarySendBinaryCommand: " + "TcpClient.Client == null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "TcpClient.Client == null";

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdCommandGetBinary", "TcpClient.Client == null", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            if ((_binaryWriter == null) || (_binaryReader == null))
            {
                Debug.WriteLine("@MpdBinarySendBinaryCommand: " + "_binaryWriter or _binaryReader is null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "_binaryWriter or _binaryReader is null";

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error :@{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdCommandGetBinary", "_commandWriter or _commandReader is null", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            if (!_binaryConnection.Client.Connected)
            {
                Debug.WriteLine("@MpdBinarySendBinaryCommand: " + "NOT IsMpdCommandConnected");

                ret.IsSuccess = false;
                ret.ErrorMessage = "NOT IsMpdCommandConnected";

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "MpdCommandGetBinary", "!CommandConnection.Client.Connected", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                return ret;
            }

            // WriteAsync
            try
            {
                if (cmd.Trim().StartsWith("idle"))
                {
                    //DebugCommandOutput?.Invoke(this, ">>>>" + cmd.Trim() + "\n" + "\n");

                    await _binaryWriter.WriteAsync(cmd.Trim() + "\n");

                    if (!isAutoIdling)
                        return ret;
                }
                else
                {
                    string cmdDummy = cmd;
                    if (cmd.StartsWith("password "))
                        cmdDummy = "password ****";

                    cmdDummy = cmdDummy.Trim().Replace("\n", "\n" + ">>>>");

                    //if (isAutoIdling)
                    //    DebugCommandOutput?.Invoke(this, ">>>>" + "noidle\n>>>>" + cmdDummy.Trim() + "\n>>>>idle player" + "\n" + "\n");
                    //else
                    //    DebugCommandOutput?.Invoke(this, ">>>>" + cmdDummy.Trim() + "\n" + "\n");

                    if (isAutoIdling)
                        await _binaryWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n" + "idle player\n");
                    else
                        await _binaryWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。

                Debug.WriteLine("Exception@MpdBinarySendBinaryCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                /*
                // Could be application shutdopwn.
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error@{0}, Reason:{1}, Data:{2}, {3} Exception: {4} {5}", "WriteAsync@MpdCommandGetBinary", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    // タイムアウトしていたらここで「も」エラーになる模様。

                    IsMpdCommandConnected = false;

                    DebugCommandOutput?.Invoke(this, string.Format("Reconnecting... " + Environment.NewLine + Environment.NewLine));

                    _commandConnection.Close();

                    ConnectionResult newCon = await MpdCommandConnect(_host, _port);

                    if (newCon.IsSuccess)
                    {
                        CommandResult d = await MpdCommandSendPassword(_password);

                        if (d.IsSuccess)
                        {
                            DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Success. " + Environment.NewLine + Environment.NewLine));

                            ret = await MpdCommandGetBinary(cmd, isAutoIdling);
                        }
                    }
                    else
                    {
                        DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Failed. " + Environment.NewLine + Environment.NewLine));

                        ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                        ConnectionError?.Invoke(this, "The connection (command) has been terminated (IOException): " + e.Message);
                    }
                }
                */
                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendBinaryCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                /*
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "WriteAsync@MpdCommandGetBinary", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    //ConnectionState = ConnectionStatus.SeeConnectionErrorEvent;

                    //ConnectionError?.Invoke(this, "The connection (command) has been terminated (Exception): " + e.Message);
                }
                */
                return ret;
            }

            // ReadAsync
            try
            {
                StringBuilder stringBuilder = new();

                byte[] bin = Array.Empty<byte>();

                bool isDoubleOk = false;
                bool isAck = false;
                string ackText = "";
                bool isNullReturn = false;
                bool isBinaryFound = false;

                bool isWaitForOK = true;

                while (isWaitForOK)
                {

                    int readSize = 0;
                    int bufferSize = 5000;
                    byte[] buffer = new byte[bufferSize];
                    byte[] bindata = Array.Empty<byte>();

                    using (MemoryStream ms = new())
                    {
                        while ((readSize = await _binaryReader.BaseStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                        {
                            ms.Write(buffer, 0, readSize);

                            if (readSize < bufferSize)
                            {
                                break;
                            }
                        }

                        bindata = ms.ToArray();
                    }

                    if (bindata.Length == 0)
                    {
                        isNullReturn = true;

                        isWaitForOK = false;
                    }
                    else
                    {

                        bin = CombineByteArray(bin, bindata);

                        //Debug.WriteLine("Done .Write:" + bin.Length.ToString());

                        string res = Encoding.Default.GetString(bindata, 0, bindata.Length);

                        List<string> values = res.Split("\n").ToList();

                        foreach (var line in values)
                        {
                            if (line != null)
                            {
                                if (line.StartsWith("ACK"))
                                {
                                    //Debug.WriteLine("ACK line @MpdCommandGetBinary: " + cmd.Trim() + " and " + line);

                                    if (!string.IsNullOrEmpty(line))
                                        stringBuilder.Append(line + "\n");

                                    ret.ErrorMessage = line;
                                    ackText = line;
                                    isAck = true;

                                    isWaitForOK = false;

                                    break;
                                }
                                else if (line.StartsWith("changed: "))
                                {
                                    // noidleでついてくるかもしれないchanged. idleConnectionで見ているからここでは無視したいが・・・。

                                    if (!string.IsNullOrEmpty(line))
                                        stringBuilder.Append(line + "\n");
                                }
                                else if (line.StartsWith("size: "))
                                {
                                    if (!string.IsNullOrEmpty(line))
                                        stringBuilder.Append(line + "\n");

                                    List<string> s = line.Split(':').ToList();
                                    if (s.Count > 1)
                                    {
                                        if (Int32.TryParse(s[1], out int i))
                                        {
                                            ret.WholeSize = i;
                                        }
                                    }
                                }
                                else if (line.StartsWith("type: "))
                                {
                                    if (!string.IsNullOrEmpty(line))
                                        stringBuilder.Append(line + "\n");

                                    List<string> s = line.Split(':').ToList();
                                    if (s.Count > 1)
                                    {
                                        ret.Type = s[1].Trim();
                                    }
                                }
                                else if (line.StartsWith("binary: "))
                                {
                                    isBinaryFound = true;

                                    if (!string.IsNullOrEmpty(line))
                                        stringBuilder.Append(line + "\n");

                                    stringBuilder.Append("{binary data}" + "\n");

                                    List<string> s = line.Split(':').ToList();
                                    if (s.Count > 1)
                                    {
                                        if (Int32.TryParse(s[1], out int i))
                                        {
                                            ret.ChunkSize = i;
                                        }
                                    }

                                }
                                else if (line == "OK") // ==
                                {
                                    if (isAutoIdling)
                                    {
                                        if (isDoubleOk)
                                        {
                                            if (!string.IsNullOrEmpty(line))
                                                stringBuilder.Append(line + "\n");

                                            ret.IsSuccess = true;

                                            isWaitForOK = false;
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

                                        isWaitForOK = false;
                                        break;
                                    }
                                }
                                else
                                {
                                    // This should be binary data if not reading correctly above.

                                    //Debug.WriteLine(line);
                                }
                            }
                            else
                            {
                                isNullReturn = true;

                                isWaitForOK = false;
                                break;
                            }
                        }

                    }
                }

                if (isNullReturn)
                {
                    Debug.WriteLine("@MpdBinarySendBinaryCommand ReadAsync isNullReturn");

                    //DebugCommandOutput?.Invoke(this, string.Format("################ Error @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadAsync@MpdCommandGetBinary", "ReadLineAsync received null data", cmd.Trim(), Environment.NewLine, "", Environment.NewLine + Environment.NewLine));

                    ret.ErrorMessage = "ReadAsync@MpdBinarySendBinaryCommand received null data";

                    return ret;
                }
                else
                {
                    //DebugCommandOutput?.Invoke(this, "<<<<" + stringBuilder.ToString().Trim().Replace("\n", "\n" + "<<<<") + "\n" + "\n");

                    if (isAck)
                    {
                        // とりあえず今の所、アルバムカバーのfile not existsは無視するようにしている。
                        //MpdAckError?.Invoke(this, ackText + " (@MCGB)");

                        ret.ErrorMessage = ackText + " (@MpdBinarySendBinaryCommand)";

                        return ret;
                    }
                    else if (isBinaryFound)
                    {
                        //return ret;
                        //return ParseAlbumArtResponse(bin);
                        return ParseAlbumImageData(bin);
                    }
                    else
                    {
                        //Debug.WriteLine("No binary data(size) found. Could be a readpicture command?");

                        // TODO: 

                        //DebugCommandOutput?.Invoke(this, "No binary data(size) found. Could be a readpicture command?" + "\n" + "\n");

                        ret.ErrorMessage = "No binary data(size) found. Could be a readpicture command? (@MpdBinarySendBinaryCommand)";

                        return ret;
                    }
                }
            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdBinarySendBinaryCommand: " + cmd.Trim() + " ReadAsync ---- " + e.Message);

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadAsync@MpdCommandGetBinary", "InvalidOperationException (Most likely the connection is overloaded)", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                return ret;
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection: 確立された接続がホスト コンピューターのソウトウェアによって中止されました。

                Debug.WriteLine("IOException@MpdBinarySendBinaryCommand: " + cmd.Trim() + " ReadAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;
                /*
                // Could be application shutdopwn.
                if ((ConnectionState == ConnectionStatus.Disconnecting) || (ConnectionState == ConnectionStatus.DisconnectedByUser))
                {

                }
                else
                {
                    DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadAsync@MpdCommandGetBinary", "IOException", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                    // タイムアウトしていたらここで「も」エラーになる模様。

                    IsMpdCommandConnected = false;

                    DebugCommandOutput?.Invoke(this, string.Format("Reconnecting... " + Environment.NewLine + Environment.NewLine));

                    _commandConnection.Close();

                    ConnectionResult newCon = await MpdCommandConnect(_host, _port);

                    if (newCon.IsSuccess)
                    {
                        CommandResult d = await MpdCommandSendPassword(_password);

                        if (d.IsSuccess)
                        {
                            DebugCommandOutput?.Invoke(this, string.Format("Reconnecting Success. " + Environment.NewLine + Environment.NewLine));

                            ret = await MpdCommandGetBinary(cmd, isAutoIdling);
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
                */
                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendBinaryCommand: " + cmd.Trim() + " ReadAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //DebugCommandOutput?.Invoke(this, string.Format("################ Error: @{0}, Reason: {1}, Data: {2}, {3} Exception: {4} {5}", "ReadAsync@MpdCommandGetBinary", "Exception", cmd.Trim(), Environment.NewLine, e.Message, Environment.NewLine + Environment.NewLine));

                return ret;
            }

        }

        private CommandBinaryResult ParseAlbumImageData(byte[] data)
        {
            CommandBinaryResult r = new();

            //if (MpdStop) return r;

            if (data.Length > 1000000) //2000000000
            {
                Debug.WriteLine("**ParseAlbumImageData: binary file size too big: " + data.Length.ToString());

                _albumCover.IsDownloading = false;
                return r;
            }

            if (string.IsNullOrEmpty(_albumCover.SongFilePath))
            {
                Debug.WriteLine("**ParseAlbumImageData: File path is not set.");

                _albumCover.IsDownloading = false;
                return r;
            }

            if (!_albumCover.IsDownloading)
            {
                Debug.WriteLine("**ParseAlbumImageData: IsDownloading = false. Downloading canceld? .");

                _albumCover.IsDownloading = false;
                return r;
            }

            try
            {
                //int gabStart = gabPre;
                //int gabEnd = gabAfter;
                int gabStart = 0;
                int gabEnd = 0;

                string res = Encoding.Default.GetString(data, 0, data.Length);

                int binSize = 0;
                int binResSize = 0;

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

                                r.WholeSize = i;
                            }
                        }

                    }
                    else if (val.StartsWith("type: "))
                    {
                        gabStart = gabStart + val.Length + 1;

                        List<string> s = val.Split(':').ToList();
                        if (s.Count > 1)
                        {
                            r.Type = s[1];
                        }

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

                                r.ChunkSize = binResSize;
                            }
                        }

                    }
                    else if (val == "OK") // ==
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

                    }
                    else if (val.StartsWith("ACK"))
                    {
                        // ACK 応答はここまで到達しないはず。

                        if (found)
                        {
                            gabEnd = gabEnd + val.Length + 1;
                            //System.Diagnostics.Debug.WriteLine("changed:after " + val);
                        }
                        else
                        {
                            gabStart = gabStart + val.Length + 1;
                            //System.Diagnostics.Debug.WriteLine("changed:before " + val);
                        }
                    }
                    else if (val.StartsWith("changed:"))
                    {
                        // Song is changed...so should skip ??
                        //DataReceived_ParseData(val);

                        if (found)
                        {
                            gabEnd = gabEnd + val.Length + 1;
                            //System.Diagnostics.Debug.WriteLine("changed:after " + val);
                        }
                        else
                        {
                            gabStart = gabStart + val.Length + 1;
                            //System.Diagnostics.Debug.WriteLine("changed:before " + val);
                        }

                    }
                    else
                    {
                        // should be binary...
                    }
                }

                gabEnd++; //

                // test
                //gabEnd = 4; // \n O K \n


                if (binSize > 1000000)
                {
                    Debug.WriteLine("binary file too big: " + binSize.ToString());

                    //DebugCommandOutput?.Invoke(this, "binary file too big: " + binSize.ToString() + "\n" + "\n");

                    _albumCover.IsDownloading = false;

                    return r;
                }

                if ((binSize == 0))
                {
                    Debug.WriteLine("binary file size is Zero: " + binSize.ToString() + ", " + binResSize.ToString() + ", " + data.Length.ToString());

                    _albumCover.IsDownloading = false;

                    return r;
                }

                if (binResSize != ((data.Length - gabStart) - gabEnd))
                {
                    Debug.WriteLine("binary file size mismatch: " + binSize.ToString() + ", [" + binResSize.ToString() + ", " + (data.Length - gabStart - gabEnd) + "], " + data.Length.ToString());

                    //DebugCommandOutput?.Invoke(this, "binary file size mismatch." + "\n" + "\n");

                    //DebugCommandOutput?.Invoke(this, "raw text data:\n" + res  + "\n" + "\n");

                    _albumCover.IsDownloading = false;

                    return r;
                }

                r.WholeSize = binSize;
                r.ChunkSize = binResSize;

                // 今回受け取ったバイナリ用にバイトアレイをイニシャライズ
                byte[] resBinary = new byte[data.Length - gabStart - gabEnd];
                // 今回受け取ったバイナリをresBinaryへコピー
                Array.Copy(data, gabStart, resBinary, 0, resBinary.Length);

                r.BinaryData = resBinary;

                r.IsSuccess = true;

                return r;

            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error@ParseAlbumImageData (l): " + ex.Message);

                _albumCover.IsDownloading = false;
                return r;
            }
        }

        private static byte[] CombineByteArray(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
        }

        public async Task<CommandResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture)
        {
            if (string.IsNullOrEmpty(uri))
            {
                CommandResult f = new();
                f.ErrorMessage = "IsNullOrEmpty(uri)";
                f.IsSuccess = false;
                return f;
            }


            /*
            if (songId != MpdStatus.MpdSongID)
            {
                // probably you clicked on "Next" too farst or double clicked.
                Debug.WriteLine("Error@MpdQueryAlbumArt: songId != MpdStatus.MpdSongID. Ignoring.");

                CommandResult f = new CommandResult();
                f.IsSuccess = false;
                return f;
            }
            */

            //Debug.WriteLine("Downloading..." + uri);

            _albumCover.SongFilePath = uri;
            _albumCover.IsDownloading = true;
            /*
            Application.Current.Dispatcher.Invoke(() =>
            {
                _albumCover = new AlbumImage();
                _albumCover.IsDownloading = true;
                _albumCover.SongFilePath = uri;
                _albumCover.AlbumImageSource = null;
                _albumCover.BinaryData = new byte[0];
                _albumCover.BinarySize = 0;
            });
            */

            uri = Regex.Escape(uri);

            string cmd = "albumart \"" + uri + "\" 0" + "\n";
            if (isUsingReadpicture)
                if (CompareVersionString(MpdVersion,"0.22.0") >= 0)
                    cmd = "readpicture \"" + uri + "\" 0" + "\n";

            CommandBinaryResult result = await MpdBinarySendBinaryCommand(cmd, false);

            bool r = false;

            if (result.IsSuccess)
            {
                _albumCover.BinaryData = result.BinaryData;

                if ((result.WholeSize != 0) && (result.WholeSize == result.ChunkSize))
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _albumCover.AlbumImageSource = BitmaSourceFromByteArray(_albumCover.BinaryData);
                        _albumCover.IsSuccess = true;
                        _albumCover.IsDownloading = false;

                        //MpdAlbumArtChanged?.Invoke(this);
                    });

                    r = true;
                    //Debug.WriteLine("一発できた");
                }
                else
                {
                    //Debug.WriteLine("何回かみにいくみたい");

                    if ((result.WholeSize != 0) && (result.WholeSize > result.ChunkSize))
                    {
                        while ((result.WholeSize > _albumCover.BinaryData.Length) && result.IsSuccess)
                        {
                            result = await MpdReQueryAlbumArt(_albumCover.SongFilePath, _albumCover.BinaryData.Length, isUsingReadpicture);

                            if (result.IsSuccess && (result.BinaryData != null))
                                _albumCover.BinaryData = CombineByteArray(_albumCover.BinaryData, result.BinaryData);
                        }

                        //Debug.WriteLine("何回かできた");

                        if (result.IsSuccess && (_albumCover.BinaryData != null))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                _albumCover.AlbumImageSource = BitmaSourceFromByteArray(_albumCover.BinaryData);
                                _albumCover.IsSuccess = true;
                                _albumCover.IsDownloading = false;

                                //MpdAlbumArtChanged?.Invoke(this);
                            });

                            r = true;
                        }
                    }
                }
            }

            CommandResult b = new();
            b.IsSuccess = r;
            if (!r)
            {
                b.ErrorMessage = result.ErrorMessage;
            }

            return b;
        }

        private async Task<CommandBinaryResult> MpdReQueryAlbumArt(string uri, int offset, bool isUsingReadpicture)
        {
            if (string.IsNullOrEmpty(uri))
            {
                CommandBinaryResult f = new();
                f.ErrorMessage = "IsNullOrEmpty(uri)";
                f.IsSuccess = false;
                return f;
            }

            if (!_albumCover.IsDownloading)
            {
                Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.IsDownloading == false. Ignoring.");

                CommandBinaryResult f = new();
                f.IsSuccess = false;
                return f;
            }

            if (_albumCover.SongFilePath != uri)
            {
                Debug.WriteLine("Error@MpdQueryAlbumArt: _albumCover.SongFilePath != uri. Ignoring.");

                _albumCover.IsDownloading = false;

                CommandBinaryResult f = new();
                f.IsSuccess = false;
                return f;
            }

            uri = Regex.Escape(uri);

            string cmd = "albumart \"" + uri + "\" " + offset.ToString() + "\n";
            if (isUsingReadpicture)
                if (CompareVersionString(MpdVersion, "0.22.0") >= 0)
                    cmd = "readpicture \"" + uri + "\" " + offset.ToString() + "\n";

            return await MpdBinarySendBinaryCommand(cmd, false);
        }

        private static int CompareVersionString(string a, string b)
        {
            return (new System.Version(a)).CompareTo(new System.Version(b));
        }

        private static BitmapSource BitmaSourceFromByteArray(byte[] buffer)
        {
            using var stream = new MemoryStream(buffer);
            return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
        }

        public void MpdBinaryConnectionDisconnect()
        {
            //Debug.WriteLine("TCP Binary Connection: Disconnecting.");

            try
            {
                if (_binaryConnection.Client != null)
                    _binaryConnection.Client.Shutdown(SocketShutdown.Both);
                _binaryConnection.Close();
            }
            catch { }

        }

    }
}
