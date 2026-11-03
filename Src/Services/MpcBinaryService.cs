using MPDCtrl.Models;
using MPDCtrl.Services.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MPDCtrl.Services;

public sealed partial class MpcBinaryService : IMpcBinaryService
{
    private CancellationTokenSource? _cts;
    private readonly Lock _connectionLock = new();
    private TcpClient _binaryConnection = new();
    private StreamReader? _binaryReader;
    private StreamWriter? _binaryWriter;

    //private AlbumImage _albumCover = new();
    //public AlbumImage AlbumCover { get => _albumCover; }

    private string _host = "";
    private int _port = 6600;

    private string? MpdVersion { get; set; }

    public MpcBinaryService()
    {

    }

    public async Task<bool> MpdBinaryConnectionStart(string host, int port, string password, CancellationToken cancellationToken = default)
    {
        _cts?.Dispose();
        //_cts = new CancellationTokenSource();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;

        ConnectionResult r = await MpdBinaryConnect(host, port, token);

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

    private async Task<ConnectionResult> MpdBinaryConnect(string host, int port, CancellationToken cancellationToken)
    {
        ConnectionResult result = new();

        // TODO: Currently there is no way to re-connect to the same TcpClient instance, so we need to create a new one.
        if (_binaryConnection.Client?.Connected == true)
        {
            lock (_connectionLock)
            {
                DisposeConnection(
                    ref _binaryConnection,
                    ref _binaryReader,
                    ref _binaryWriter);
            }
        }
        else
        {
            _binaryConnection = new TcpClient();
        }

        _host = host;
        _port = port;

        try
        {
            await _binaryConnection.ConnectAsync(_host, _port, cancellationToken);

            //TODO: always false
            if (_binaryConnection.Client is null)
            {
                Debug.WriteLine("_binaryConnection.Client is null. " + host + " " + port.ToString(CultureInfo.InvariantCulture));

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

                //string? response = await _binaryReader.ReadLineAsync(cancellationToken);
                string? response = await ReadMpdLineAsync(tcpStream, cancellationToken);
                if (response is not null)
                {
                    if (response.StartsWith("OK MPD ", StringComparison.InvariantCulture))
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
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _binaryConnection.Dispose();
            throw;
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

    private async Task<CommandResult> MpdBinarySendCommand(string cmd)
    {
        CommandResult ret = new();

        if (_cts is null)
        {
            return ret;
        }

        if (_cts.Token.IsCancellationRequested)
        {
            Debug.WriteLine("IsCancellationRequested returning @MpdBinarySendCommand");
            return ret;
        }

        // TODO: always false
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

            await _binaryWriter.WriteAsync(cmd.Trim() + "\n");
        }
        catch (IOException e)
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

            string ackText = "";
            string errText = "";
            bool isNullReturn = false;

            while (true)
            {
                if (_cts is null)
                    break;
                var token = _cts.Token;
                if (token.IsCancellationRequested)
                    break;

                //string? line = await _binaryReader.ReadLineAsync(token);
                string? line = await ReadMpdLineAsync(_binaryReader.BaseStream, token);

                if (line is not null)
                {
                    if (line.StartsWith("ACK", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Debug.WriteLine("ACK line @MpdBinarySendCommand: " + cmd.Trim() + " and " + line);

                        if (!string.IsNullOrEmpty(line))
                            stringBuilder.Append(line + "\n");

                        ret.ErrorMessage = line;
                        ackText = line;
                        //isAck = true;

                        break;
                    }
                    else if (line.StartsWith("OK", StringComparison.InvariantCulture))
                    {
                        ret.IsSuccess = true;

                        if (!string.IsNullOrEmpty(line))
                            stringBuilder.Append(line + "\n");

                        break;
                    }
                    else if (line.StartsWith("error", StringComparison.InvariantCultureIgnoreCase))
                    {
                        Debug.WriteLine("error line @MpdBinarySendCommand: " + cmd.Trim() + " and " + line);

                        //isErr = true;
                        errText = line;
                        ret.ErrorMessage = line;

                        if (!string.IsNullOrEmpty(line))
                            stringBuilder.Append(line + "\n");
                    }
                    else if (line.StartsWith("changed: ", StringComparison.InvariantCultureIgnoreCase))
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
        catch (InvalidOperationException e)
        {
            // The stream is currently in use by a previous operation on the stream.

            Debug.WriteLine("InvalidOperationException@MpdBinarySendCommand: " + cmd.Trim() + " ReadLineAsync ---- " + e.Message);

            ret.IsSuccess = false;
            ret.ErrorMessage = e.Message;

            //IsBusy?.Invoke(this, false);
            return ret;
        }
        catch (IOException e)
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

    private async Task<CommandBinaryResult> MpdBinarySendBinaryCommand(string cmd, AlbumImage albumCover)
    {
        CommandBinaryResult result = new();

        CommandBinaryResult Fail(string message, bool isTimeout = false)
        {
            result.IsSuccess = false;
            result.ErrorMessage = message;
            result.IsTimeOut = isTimeout;
            albumCover.IsDownloading = false;
            return result;
        }

        if (_cts is null) return Fail("Connection cancellation token is not available.");

        CancellationToken token = _cts.Token;

        if (token.IsCancellationRequested) return Fail("The operation was canceled.");

        if (_binaryWriter is null || _binaryReader is null) return Fail("The binary connection is not initialized.");

        if (!_binaryConnection.Connected) return Fail("The binary connection is not connected.");

        Stream stream = _binaryReader.BaseStream;

        try
        {
            await _binaryWriter.WriteAsync(cmd.Trim() + "\n");

            bool binaryFound = false;

            // Read response headers up to "binary: N" or the final "OK"/"ACK".
            while (true)
            {
                token.ThrowIfCancellationRequested();

                string? line = await ReadMpdLineAsync(stream, token);
                if (line is null) return Fail("Unexpected end of stream while reading the MPD response.", isTimeout: true);

                if (line.StartsWith("ACK", StringComparison.OrdinalIgnoreCase)) return Fail(line);

                if (line.Equals("OK", StringComparison.Ordinal))
                {
                    result.IsNoBinaryFound = true;
                    return Fail("No binary data was returned for the requested artwork.");
                }

                if (line.StartsWith("size:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(
                            line["size:".Length..].Trim(),
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out int wholeSize) ||
                        wholeSize < 0)
                    {
                        return Fail("Invalid size header in the MPD response.");
                    }

                    result.WholeSize = wholeSize;

                    if (wholeSize > 20_000_000) return Fail($"Artwork is too large: {wholeSize} bytes.");
                }
                else if (line.StartsWith("type:", StringComparison.OrdinalIgnoreCase))
                {
                    result.Type = line["type:".Length..].Trim();
                }
                else if (line.StartsWith("binary:", StringComparison.OrdinalIgnoreCase))
                {
                    if (!int.TryParse(
                            line["binary:".Length..].Trim(),
                            NumberStyles.None,
                            CultureInfo.InvariantCulture,
                            out int chunkSize) ||
                        chunkSize <= 0)
                    {
                        return Fail("Invalid binary size header in the MPD response.");
                    }

                    if (chunkSize > 20_000_000) return Fail($"Artwork chunk is too large: {chunkSize} bytes.");

                    result.ChunkSize = chunkSize;
                    result.BinaryData = new byte[chunkSize];

                    // TCP reads are arbitrary-sized; read exactly the advertised payload length.
                    await stream.ReadExactlyAsync(result.BinaryData.AsMemory(), token);
                    binaryFound = true;
                    break;
                }
            }

            // The payload is followed by a line break and then the command status.
            // Read lines until the response's final status, without interpreting payload bytes as text.
            while (true)
            {
                token.ThrowIfCancellationRequested();

                string? line = await ReadMpdLineAsync(stream, token);
                if (line is null)
                    return Fail("Unexpected end of stream after the MPD binary payload.", isTimeout: true);

                if (string.IsNullOrEmpty(line))
                    continue;

                if (line.StartsWith("ACK", StringComparison.OrdinalIgnoreCase))
                    return Fail(line);

                if (line.Equals("OK", StringComparison.Ordinal))
                {
                    result.IsSuccess = binaryFound;
                    if (!result.IsSuccess)
                    {
                        result.IsNoBinaryFound = true;
                        return Fail("No binary data was returned for the requested artwork.");
                    }

                    return result;
                }
            }
        }
        catch (OperationCanceledException e)
        {
            return Fail(e.Message);
        }
        catch (EndOfStreamException e)
        {
            return Fail(e.Message, isTimeout: true);
        }
        catch (IOException e)
        {
            return Fail(e.Message, isTimeout: true);
        }
        catch (Exception e)
        {
            return Fail(e.Message);
        }
    }

    private static async Task<string?> ReadMpdLineAsync(Stream stream, CancellationToken cancellationToken)
    {
        using MemoryStream lineBytes = new();
        byte[] oneByte = new byte[1];

        while (true)
        {
            int bytesRead = await stream.ReadAsync(oneByte.AsMemory(), cancellationToken);

            if (bytesRead == 0) return null;

            if (oneByte[0] == (byte)'\n')
            {
                return Encoding.UTF8
                    .GetString(lineBytes.GetBuffer(), 0, checked((int)lineBytes.Length))
                    .TrimEnd('\r');
            }

            lineBytes.WriteByte(oneByte[0]);

            if (lineBytes.Length > 64 * 1024)
            {
                throw new InvalidDataException("MPD response line exceeded the maximum allowed length.");
            }
        }
    }

    private static byte[] CombineByteArray(byte[] first, byte[] second)
    {
        byte[] ret = new byte[first.Length + second.Length];
        System.Buffer.BlockCopy(first, 0, ret, 0, first.Length);
        System.Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
        return ret;
    }

    public async Task<CommandImageResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture)
    {
        if (_cts is null)
        {
            CommandImageResult f = new()
            {
                ErrorMessage = "(_cts is null)",
                IsSuccess = false
            };
            return f;
        }

        if (_cts.Token.IsCancellationRequested)
        {
            Debug.WriteLine("IsCancellationRequested returning @MpdQueryAlbumArt (Binary)");
            CommandImageResult f = new()
            {
                ErrorMessage = "(IsCancellationRequested)",
                IsSuccess = false
            };
            return f;
        }

        if (string.IsNullOrEmpty(uri))
        {
            CommandImageResult f = new()
            {
                ErrorMessage = "IsNullOrEmpty(uri)",
                IsSuccess = false
            };
            return f;
        }

        /*
        if (_albumCover.IsDownloading)
        {
            CommandImageResult f = new()
            {
                ErrorMessage = "_albumCover.IsDownloading",
                IsSuccess = false
            };
            return f;
        }
        
        _albumCover.SongFilePath = uri;
        _albumCover.IsDownloading = true;
        */

        var albm = new AlbumImage
        {
            SongFilePath = uri,
            IsDownloading = true
        };

        uri = Regex.Escape(uri);

        string cmd = "albumart \"" + uri + "\" 0" + "\n";
        if (isUsingReadpicture && (!string.IsNullOrEmpty(MpdVersion)))
            if (CompareVersionString(MpdVersion, "0.22.0") >= 0)
                cmd = "readpicture \"" + uri + "\" 0" + "\n";

        CommandBinaryResult result = await MpdBinarySendBinaryCommand(cmd, albm);

        CommandImageResult b = new();
        bool r = false;

        if (result.IsSuccess)
        {
            albm.BinaryData = result.BinaryData;

            if ((result.WholeSize != 0) && (result.WholeSize == result.ChunkSize))
            {
                /*
                Dispatcher.UIThread.Post(async () =>
                {

                });
                */

                if (albm.BinaryData is not null)
                {
                    //_albumCover.AlbumImageSource = BitmaSourceFromByteArray(_albumCover.BinaryData);
                    //b.AlbumCover.AlbumImageSource = await BitmapSourceFromByteArray(albm.BinaryData);

                    // For winui3
                    b.AlbumCover.BinaryData = albm.BinaryData;
                    albm.IsSuccess = true;
                }
                /*
                if (b.AlbumCover.AlbumImageSource is not null)
                {
                    albm.IsSuccess = true;
                }
                else
                {
                    Debug.WriteLine("_albumCover.IsSuccess == false; BitmaSourceFromByteArray@MpdQueryAlbumArt");
                    albm.IsSuccess = false;
                }
                */

                albm.IsDownloading = false;

                r = albm.IsSuccess;
            }
            else
            {
                if ((result.WholeSize != 0) && (result.WholeSize > result.ChunkSize))
                {
                    if (result.IsSuccess && (albm.BinaryData is not null))
                    {
                        // TODO:
                        while ((result.WholeSize > albm.BinaryData.Length) && result.IsSuccess)
                        {
                            result = await MpdReQueryAlbumArt(albm.SongFilePath, albm.BinaryData.Length, isUsingReadpicture, albm);

                            if (result.IsSuccess && (result.BinaryData is not null))
                                albm.BinaryData = CombineByteArray(albm.BinaryData, result.BinaryData);
                            else
                                break;
                        }

                        if (result.IsSuccess)
                        {
                            /*
                            Dispatcher.UIThread.Post(async () =>
                            {

                            });
                            */
                            if (albm.BinaryData is not null)
                            {
                                //b.AlbumCover.AlbumImageSource = await BitmapSourceFromByteArray(albm.BinaryData);

                                // For winui3
                                b.AlbumCover.BinaryData = albm.BinaryData;
                                albm.IsSuccess = true;
                            }

                            /*
                            if (b.AlbumCover.AlbumImageSource is not null)
                            {
                                albm.IsSuccess = true;
                            }
                            else
                            {
                                result.ErrorMessage = "BitmaSourceFromByteArray(_albumCover.BinaryData) returned null.";
                                Debug.WriteLine("BitmaSourceFromByteArray(_albumCover.BinaryData) returned null.");
                                albm.IsSuccess = false;
                            }
                            */
                            albm.IsDownloading = false;

                            r = true;
                        }
                        else
                        {
                            Debug.WriteLine("Not IsSuccess MpdReQueryAlbumArt @MpdQueryAlbumArt: " + result.ErrorMessage);
                            /*
                            Dispatcher.UIThread.Post(() =>
                            {
                            });
                            */
                            albm.IsSuccess = false;
                            albm.IsDownloading = false;
                        }
                    }
                    else
                    {

                        Debug.WriteLine("if (result.IsSuccess && (_albumCover.BinaryData is not null)) @MpdQueryAlbumArt");
                    }
                }
                else
                {

                    Debug.WriteLine("if ((result.WholeSize != 0) && (result.WholeSize > result.ChunkSize)) @MpdQueryAlbumArt");
                }
            }
        }
        else
        {
            b.IsNoBinaryFound = result.IsNoBinaryFound;
            b.IsTimeOut = result.IsTimeOut;
        }

        b.IsSuccess = r;
        if (!r)
        {
            b.ErrorMessage = result.ErrorMessage;
        }
        b.AlbumCover.SongFilePath = albm.SongFilePath;
        b.AlbumCover.IsDownloading = false;
        b.AlbumCover.IsSuccess = r;

        return b;
    }

    private async Task<CommandBinaryResult> MpdReQueryAlbumArt(string uri, int offset, bool isUsingReadpicture, AlbumImage albumCover)
    {
        if (_cts is null)
        {
            CommandBinaryResult f = new()
            {
                ErrorMessage = "(_cts is null)",
                IsSuccess = false
            };
            return f;
        }

        if (_cts.Token.IsCancellationRequested)
        {
            Debug.WriteLine("IsCancellationRequested returning @MpdReQueryAlbumArt (Binary)");
            CommandBinaryResult f = new()
            {
                ErrorMessage = "(IsCancellationRequested)",
                IsSuccess = false
            };
            return f;
        }

        if (string.IsNullOrEmpty(uri))
        {
            CommandBinaryResult f = new()
            {
                ErrorMessage = "IsNullOrEmpty(uri)",
                IsSuccess = false
            };
            return f;
        }

        if (!albumCover.IsDownloading)
        {
            CommandBinaryResult f = new()
            {
                ErrorMessage = "!IsDownloading @MpdReQueryAlbumArt",
                IsSuccess = false
            };
            return f;
        }

        if (albumCover.SongFilePath != uri)
        {
            albumCover.IsDownloading = false;

            CommandBinaryResult f = new()
            {
                ErrorMessage = "_albumCover.SongFilePath != uri @MpdReQueryAlbumArt",
                IsSuccess = false
            };
            return f;
        }

        uri = Regex.Escape(uri);

        string cmd = "albumart \"" + uri + "\" " + offset.ToString(CultureInfo.InvariantCulture) + "\n";
        if (isUsingReadpicture && (!string.IsNullOrEmpty(MpdVersion)))
            if (CompareVersionString(MpdVersion, "0.22.0") >= 0)
                cmd = "readpicture \"" + uri + "\" " + offset.ToString(CultureInfo.InvariantCulture) + "\n";

        return await MpdBinarySendBinaryCommand(cmd, albumCover);
    }

    private static int CompareVersionString(string a, string b)
    {
        return (new System.Version(a)).CompareTo(new System.Version(b));
    }

    public void MpdBinaryConnectionDisconnect(bool isReconnect)
    {
        _cts?.Cancel();

        lock (_connectionLock)
        {
            DisposeConnection(
                ref _binaryConnection,
                ref _binaryReader,
                ref _binaryWriter);

        }

        if (!isReconnect)
        {
            _cts?.Dispose();
            _cts = null;
        }

        /*
        try
        {
            _binaryConnection.Client?.Shutdown(SocketShutdown.Both);
            _binaryConnection.Close();

            if (!isReconnect)
            {
                _cts?.Dispose();
            }
        }
        catch// (Exception ex)
        {
            //Debug.WriteLine($"Exception @MpdBinaryConnectionDisconnect {ex}");
        }
        */
    }

    private static void DisposeConnection(ref TcpClient connection,ref StreamReader? reader,ref StreamWriter? writer)
    {
        try
        {
            if (connection.Client?.Connected == true)
            {
                connection.Client?.Shutdown(SocketShutdown.Both);
            }
        }
        catch
        {
            // The connection may already be closed.
        }

        // Dispose can flush and throw on a broken socket. Teardown must never throw,
        // and a failure in one step must not skip the others.
        try { writer?.Dispose(); } catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException) { }
        try { reader?.Dispose(); } catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException) { }
        try { connection.Dispose(); } catch (Exception ex) when (ex is IOException or ObjectDisposedException or SocketException) { }

        writer = null;
        reader = null;
        connection = new TcpClient();
    }

    public void Dispose()
    {
        _cts?.Dispose();
        _cts = null;
        Debug.WriteLine("MpcBinaryService.Dispose() completed.");

        GC.SuppressFinalize(this);
    }
}
