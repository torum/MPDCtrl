namespace MPDCtrl.Services.Contracts;

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

public delegate void IsBusyEvent(IMpcService sender, bool on);
public delegate void DebugCommandOutputEvent(IMpcService sender, string data);
public delegate void DebugIdleOutputEvent(IMpcService sender, string data);
public delegate void ConnectionStatusChangedEvent(IMpcService sender, ConnectionStatus status);
public delegate void ConnectionErrorEvent(IMpcService sender, string data);
public delegate void IsMpdIdleConnectedEvent(IMpcService sender);
public delegate void MpdAckErrorEvent(IMpcService sender, string data, string origin);
public delegate void MpdFatalErrorEvent(IMpcService sender, string data, string origin);
public delegate void MpdPlayerStatusChangedEvent(IMpcService sender);
public delegate void MpdCurrentQueueChangedEvent(IMpcService sender);
public delegate void MpdCurrentSongChangedEvent(IMpcService sender);
public delegate void MpdPlaylistsChangedEvent(IMpcService sender);
public delegate void MpdOutputChangedEvent(IMpcService sender);
public delegate void MpdAlbumArtChangedEvent(IMpcService sender);
public delegate void MpcProgressEvent(IMpcService sender, string msg);