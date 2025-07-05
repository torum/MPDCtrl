using MPDCtrl.Contracts;
using MPDCtrl.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace MPDCtrl.Services
{
    public class BinaryDownloader : IBinaryDownloader
    {
        private static TcpClient _binaryConnection = new();
        private StreamReader? _binaryReader;
        private StreamWriter? _binaryWriter;

        private readonly AlbumImage _albumCover = new();
        public AlbumImage AlbumCover { get => _albumCover; }

        private string _host = "";
        private int _port = 6600;

        private string? MpdVersion { get; set; }

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

            _binaryConnection = new TcpClient();

            _host = host;
            _port = port;

            try
            {
                await _binaryConnection.ConnectAsync(_host, _port);

                if (_binaryConnection.Client is null)
                {
                    Debug.WriteLine("_binaryConnection.Client is null. " + host + " " + port.ToString());

                    result.ErrorMessage = "_binaryConnection.Client is null";

                    return result;
                }

                if (_binaryConnection.Client.Connected)
                {
                    var tcpStream = _binaryConnection.GetStream();

                    tcpStream.ReadTimeout = 3000;

                    _binaryReader = new(tcpStream);
                    _binaryWriter = new(tcpStream)
                    {
                        AutoFlush = true
                    };

                    string? response = await _binaryReader.ReadLineAsync();
                    if (response is not null)
                    {
                        if (response.StartsWith("OK MPD "))
                        {
                            MpdVersion = response.Replace("OK MPD ", string.Empty).Trim();

                            result.IsSuccess = true;

                            // Done for now.
                        }
                        else
                        {
                            Debug.WriteLine("**** TCP Binary Connection: MPD did not respond with proper respose.@MpdBinaryConnect");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("**** TCP Binary Connection: MPD did not respond with proper respose. @MpdBinaryConnect");
                    }
                }
                else
                {
                    Debug.WriteLine("**** !client.Client.Connected@MpdBinaryConnect");
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine("**** Exception@MpdBinaryConnect: " + e.Message);
            }

            return result;
        }

        private async Task<CommandResult> MpdBinarySendPassword(string password = "")
        {
            CommandResult ret = new();

            if (string.IsNullOrEmpty(password))
            {
                ret.IsSuccess = true;
                ret.ResultText = "OK";
                ret.ErrorMessage = "";

                return ret;
            }

            string cmd = "password " + password + "\n";

            return await MpdBinarySendCommand(cmd);
        }

        private async Task<CommandResult> MpdBinarySendCommand(string cmd, bool isAutoIdling = false)
        {
            isAutoIdling = false;

            CommandResult ret = new();

            if (_binaryConnection.Client is null)
            {
                return ret;
            }

            if ((_binaryWriter is null) || (_binaryReader is null))
            {
                Debug.WriteLine("@MpdBinarySendCommand: " + "_commandWriter or _commandReader is null");

                return ret;
            }

            if (!_binaryConnection.Client.Connected)
            {
                Debug.WriteLine("@MpdBinarySendCommand: " + "NOT IsMpdCommandConnected");

                return ret;
            }

            // WriteAsync
            try
            {
                //IsBusy?.Invoke(this, true);

                if (cmd.Trim().StartsWith("idle"))
                {
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

                    if (isAutoIdling)
                        await _binaryWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n" + "idle player\n");
                    else
                        await _binaryWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection
                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //IsBusy?.Invoke(this, false);
                return ret;
            }

            // ReadLineAsync
            try
            {
                //IsBusy?.Invoke(this, true);

                StringBuilder stringBuilder = new();

                bool isDoubleOk = false;
                string ackText = "";
                bool isNullReturn = false;

                while (true)
                {
                    string? line = await _binaryReader.ReadLineAsync();

                    if (line is not null)
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

                    //IsBusy?.Invoke(this, false);
                    return ret;
                }
                else
                {
                    ret.ResultText = stringBuilder.ToString();

                    //IsBusy?.Invoke(this, false);
                    return ret;
                }

            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdBinarySendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection

                Debug.WriteLine("IOException@MpdBinarySendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //IsBusy?.Invoke(this, false);
                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                //IsBusy?.Invoke(this, false);
                return ret;
            }

        }

        private async Task<CommandBinaryResult> MpdBinarySendBinaryCommand(string cmd, bool isAutoIdling = false)
        {
            CommandBinaryResult ret = new();

            if (_binaryConnection.Client is null)
            {
                Debug.WriteLine("@MpdBinarySendBinaryCommand: " + "TcpClient.Client is null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "TcpClient.Client is null";

                return ret;
            }

            if ((_binaryWriter is null) || (_binaryReader is null))
            {
                Debug.WriteLine("@MpdBinarySendBinaryCommand: " + "_binaryWriter or _binaryReader is null");

                ret.IsSuccess = false;
                ret.ErrorMessage = "_binaryWriter or _binaryReader is null";

                return ret;
            }

            if (!_binaryConnection.Client.Connected)
            {
                Debug.WriteLine("@MpdBinarySendBinaryCommand: " + "NOT IsMpdCommandConnected");

                ret.IsSuccess = false;
                ret.ErrorMessage = "NOT IsMpdCommandConnected";

                return ret;
            }

            // WriteAsync
            try
            {
                if (cmd.Trim().StartsWith("idle"))
                {
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

                    if (isAutoIdling)
                        await _binaryWriter.WriteAsync("noidle\n" + cmd.Trim() + "\n" + "idle player\n");
                    else
                        await _binaryWriter.WriteAsync(cmd.Trim() + "\n");
                }
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection

                Debug.WriteLine("Exception@MpdBinarySendBinaryCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendBinaryCommand: " + cmd.Trim() + " WriteAsync " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

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
                        while ((readSize = await _binaryReader.BaseStream.ReadAsync(buffer)) > 0)
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

                        string res = Encoding.Default.GetString(bindata, 0, bindata.Length);

                        List<string> values = res.Split("\n").ToList();

                        foreach (var line in values)
                        {
                            if (line is not null)
                            {
                                if (line.StartsWith("ACK"))
                                {
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
                                else if (line == "OK")
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

                    ret.ErrorMessage = "ReadAsync@MpdBinarySendBinaryCommand received null data";
                    ret.IsSuccess = false;
                    return ret;
                }
                else
                {
                    if (isAck)
                    {
                        // Ignore "file not exists" for now.
                        //MpdAckError?.Invoke(this, ackText + " (@MCGB)");

                        ret.ErrorMessage = ackText + " (@MpdBinarySendBinaryCommand)";
                        ret.IsSuccess = false;
                        return ret;
                    }
                    else if (isBinaryFound)
                    {
                        return ParseAlbumImageData(bin);
                    }
                    else
                    {
                        ret.ErrorMessage = "No binary data(size) found. Could be a readpicture command? (@MpdBinarySendBinaryCommand)";
                        ret.IsSuccess = false;
                        return ret;
                    }
                }
            }
            catch (System.InvalidOperationException e)
            {
                // The stream is currently in use by a previous operation on the stream.

                Debug.WriteLine("InvalidOperationException@MpdBinarySendBinaryCommand: " + cmd.Trim() + " ReadAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                return ret;
            }
            catch (System.IO.IOException e)
            {
                // IOException : Unable to write data to the transport connection

                Debug.WriteLine("IOException@MpdBinarySendBinaryCommand: " + cmd.Trim() + " ReadAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

                return ret;
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@MpdBinarySendBinaryCommand: " + cmd.Trim() + " ReadAsync ---- " + e.Message);

                ret.IsSuccess = false;
                ret.ErrorMessage = e.Message;

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
                    else if (val == "OK")
                    {
                        if (found)
                        {
                            gabEnd = gabEnd + val.Length + 1;
                        }
                        else
                        {
                            gabStart = gabStart + val.Length + 1;
                        }
                    }
                    else if (val.StartsWith("ACK"))
                    {
                        // ACK shouldn't be here.

                        if (found)
                        {
                            gabEnd = gabEnd + val.Length + 1;
                        }
                        else
                        {
                            gabStart = gabStart + val.Length + 1;
                        }
                    }
                    else if (val.StartsWith("changed:"))
                    {
                        if (found)
                        {
                            gabEnd = gabEnd + val.Length + 1;
                        }
                        else
                        {
                            gabStart = gabStart + val.Length + 1;
                        }
                    }
                    else
                    {
                        // should be binary...
                    }
                }

                gabEnd++;

                if (binSize > 1000000)
                {
                    Debug.WriteLine("binary file too big: " + binSize.ToString());

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

                    _albumCover.IsDownloading = false;

                    return r;
                }

                r.WholeSize = binSize;
                r.ChunkSize = binResSize;

                byte[] resBinary = new byte[data.Length - gabStart - gabEnd];
                Array.Copy(data, gabStart, resBinary, 0, resBinary.Length);

                r.BinaryData = resBinary;

                r.IsSuccess = true;

                return r;

            }
            catch (Exception ex)
            {
                Debug.WriteLine("Error@ParseAlbumImageData (l): " + ex.Message);

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
                CommandResult f = new()
                {
                    ErrorMessage = "IsNullOrEmpty(uri)",
                    IsSuccess = false
                };
                return f;
            }

            _albumCover.SongFilePath = uri;
            _albumCover.IsDownloading = true;

            uri = Regex.Escape(uri);

            string cmd = "albumart \"" + uri + "\" 0" + "\n";
            if (isUsingReadpicture && (!string.IsNullOrEmpty(MpdVersion)))
                if (CompareVersionString(MpdVersion, "0.22.0") >= 0)
                    cmd = "readpicture \"" + uri + "\" 0" + "\n";

            CommandBinaryResult result = await MpdBinarySendBinaryCommand(cmd, false);

            bool r = false;

            if (result.IsSuccess)
            {
                _albumCover.BinaryData = result.BinaryData;

                if ((result.WholeSize != 0) && (result.WholeSize == result.ChunkSize))
                {
                    /*
                    Application.Current.Dispatcher.Invoke(() =>
                    {

                    });
                    */
                    if (_albumCover.BinaryData is not null)
                    {
                        _albumCover.AlbumImageSource = BitmaSourceFromByteArray(_albumCover.BinaryData);
                    }

                    if (_albumCover.AlbumImageSource is not null)
                    {
                        _albumCover.IsSuccess = true;
                    }
                    else
                    {
                        _albumCover.IsSuccess = false;
                    }
                    _albumCover.IsDownloading = false;

                    r = _albumCover.IsSuccess;
                }
                else
                {
                    if ((result.WholeSize != 0) && (result.WholeSize > result.ChunkSize))
                    {
                        if (result.IsSuccess && (_albumCover.BinaryData is not null))
                        {
                            // TODO:
                            while ((result.WholeSize > _albumCover.BinaryData.Length) && result.IsSuccess)
                            {
                                result = await MpdReQueryAlbumArt(_albumCover.SongFilePath, _albumCover.BinaryData.Length, isUsingReadpicture);

                                if (result.IsSuccess && (result.BinaryData is not null))
                                    _albumCover.BinaryData = CombineByteArray(_albumCover.BinaryData, result.BinaryData);
                                else
                                    break;
                            }

                            if (result.IsSuccess)
                            {
                                /*
                                Application.Current.Dispatcher.Invoke(() =>
                                {

                                });
                                */
                                if (_albumCover.BinaryData is not null)
                                {
                                    _albumCover.AlbumImageSource = BitmaSourceFromByteArray(_albumCover.BinaryData);
                                }

                                if (_albumCover.AlbumImageSource is not null)
                                {
                                    _albumCover.IsSuccess = true;
                                }
                                else
                                {
                                    _albumCover.IsSuccess = false;
                                }
                                _albumCover.IsDownloading = false;

                                r = true;
                            }
                            else
                            {
                                /*
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                });
                                */
                                _albumCover.IsSuccess = false;
                                _albumCover.IsDownloading = false;
                            }
                        }
                    }
                }
            }

            CommandResult b = new()
            {
                IsSuccess = r
            };
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
                CommandBinaryResult f = new()
                {
                    ErrorMessage = "IsNullOrEmpty(uri)",
                    IsSuccess = false
                };
                return f;
            }

            if (!_albumCover.IsDownloading)
            {
                CommandBinaryResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            if (_albumCover.SongFilePath != uri)
            {
                _albumCover.IsDownloading = false;

                CommandBinaryResult f = new()
                {
                    IsSuccess = false
                };
                return f;
            }

            uri = Regex.Escape(uri);

            string cmd = "albumart \"" + uri + "\" " + offset.ToString() + "\n";
            if (isUsingReadpicture && (!string.IsNullOrEmpty(MpdVersion)))
                if (CompareVersionString(MpdVersion, "0.22.0") >= 0)
                    cmd = "readpicture \"" + uri + "\" " + offset.ToString() + "\n";

            return await MpdBinarySendBinaryCommand(cmd, false);
        }

        private static int CompareVersionString(string a, string b)
        {
            return (new System.Version(a)).CompareTo(new System.Version(b));
        }

        private static BitmapSource? BitmaSourceFromByteArray(byte[] buffer)
        {
            // Bug in MPD 0.23.5 
            if (buffer?.Length > 0)
            {
                using var stream = new MemoryStream(buffer);
                try
                {
                    return BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);

                }
                catch
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public void MpdBinaryConnectionDisconnect()
        {
            try
            {
                _binaryConnection.Client?.Shutdown(SocketShutdown.Both);
                _binaryConnection.Close();
            }
            catch { }
        }
    }
}
