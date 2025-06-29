using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using MPDCtrlX.Models;

namespace MPDCtrlX.Services.Contracts;

public interface IMpcService
{
    //AlbumImage AlbumCover { get; }
    MpcService.ConnectionStatus ConnectionState { get; }
    ObservableCollection<SongInfoEx> CurrentQueue { get; }
    bool IsMpdCommandConnected { get; set; }
    bool IsMpdIdleConnected { get; set; }
    ObservableCollection<string> LocalDirectories { get; }
    ObservableCollection<SongFile> LocalFiles { get; }
    ObservableCollection<AlbumArtist> AlbumArtists { get; }
    ObservableCollection<AlbumEx> Albums { get; }
    SongInfoEx? MpdCurrentSong { get; }
    string MpdHost { get; }
    string MpdPassword { get; }
    int MpdPort { get; }
    Status MpdStatus { get; }
    bool MpdStop { get; set; }
    string MpdVerText { get; set; }
    ObservableCollection<Playlist> Playlists { get; }
    //ObservableCollection<SongInfo> SearchResult { get; }

    event MpcService.ConnectionErrorEvent ConnectionError;
    event MpcService.ConnectionStatusChangedEvent ConnectionStatusChanged;
    event MpcService.DebugCommandOutputEvent DebugCommandOutput;
    event MpcService.DebugIdleOutputEvent DebugIdleOutput;
    event MpcService.IsBusyEvent IsBusy;
    event MpcService.MpcProgressEvent MpcProgress;
    event MpcService.MpdAckErrorEvent MpdAckError;
    event MpcService.MpdAlbumArtChangedEvent MpdAlbumArtChanged;
    event MpcService.MpdCurrentQueueChangedEvent MpdCurrentQueueChanged;
    event MpcService.IsMpdIdleConnectedEvent MpdIdleConnected;
    event MpcService.MpdPlayerStatusChangedEvent MpdPlayerStatusChanged;
    event MpcService.MpdPlaylistsChangedEvent MpdPlaylistsChanged;

    Task<CommandResult> MpdAdd(List<string> uris);
    Task<CommandResult> MpdAdd(string uri);
    Task<CommandResult> MpdChangePlaylist(string playlistName);
    Task<CommandResult> MpdClear();
    Task<ConnectionResult> MpdCommandConnect(string host, int port);
    Task<bool> MpdCommandConnectionStart(string host, int port, string password);
    Task<CommandResult> MpdCommandSendPassword(string password = "");
    Task<CommandResult> MpdDeleteId(List<string> ids);
    Task<CommandResult> MpdDeleteId(string id);
    void MpdDisconnect();
    Task<ConnectionResult> MpdIdleConnect(string host, int port);
    Task<bool> MpdIdleConnectionStart(string host, int port, string password);
    Task<CommandResult> MpdIdleQueryCurrentQueue();
    Task<CommandResult> MpdIdleQueryCurrentSong();
    Task<CommandResult> MpdIdleQueryListAll();
    Task<CommandResult> MpdIdleQueryPlaylists();
    Task<CommandResult> MpdIdleQueryStatus();
    Task<CommandResult> MpdIdleSendPassword(string password = "");
    void MpdIdleStart();
    Task<CommandResult> MpdLoadPlaylist(string playlistName);
    Task<CommandResult> MpdMoveId(Dictionary<string, string> IdToNewPosPair);
    Task<CommandResult> MpdPlaybackNext(int volume);
    Task<CommandResult> MpdPlaybackPause();
    Task<CommandResult> MpdPlaybackPlay(int volume, string songId = "");
    Task<CommandResult> MpdPlaybackPrev(int volume);
    Task<CommandResult> MpdPlaybackResume(int volume);
    Task<CommandResult> MpdPlaybackSeek(string songId, int seekTime);
    Task<CommandResult> MpdPlaybackStop();
    Task<CommandResult> MpdPlaylistAdd(string playlistName, List<string> uris);
    Task<CommandResult> MpdPlaylistClear(string playlistName);
    Task<CommandResult> MpdPlaylistDelete(string playlistName, int pos);
    Task<CommandImageResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture);
    Task<CommandImageResult> MpdQueryAlbumArtForAlbumView(string uri, bool isUsingReadpicture);
    Task<CommandResult> MpdQueryCurrentQueue(bool autoIdling = true);
    Task<CommandResult> MpdQueryCurrentSong(bool autoIdling = true);
    Task<CommandResult> MpdQueryListAll(bool autoIdling = true);
    Task<CommandResult> MpdQueryListAlbumArtists(bool autoIdling = true);
    Task<CommandResult> MpdQueryPlaylists(bool autoIdling = true);
    Task<CommandPlaylistResult> MpdQueryPlaylistSongs(string playlistName, bool autoIdling = true);
    Task<CommandResult> MpdQueryStatus(bool autoIdling = true);
    Task<CommandResult> MpdRemovePlaylist(string playlistName);
    Task<CommandResult> MpdRenamePlaylist(string playlistName, string newPlaylistName);
    Task<CommandResult> MpdSave(string playlistName);
    Task<CommandSearchResult> MpdSearch(string queryTag, string queryShiki, string queryValue, bool autoIdling = true);
    Task<CommandResult> MpdSendIdle();
    Task<CommandResult> MpdSendNoIdle();
    Task<CommandResult> MpdSendUpdate();
    Task<CommandResult> MpdSetConsume(bool on);
    Task<CommandResult> MpdSetRandom(bool on);
    Task<CommandResult> MpdSetRepeat(bool on);
    Task<CommandResult> MpdSetSingle(bool on);
    Task<CommandResult> MpdSetVolume(int v);
}
