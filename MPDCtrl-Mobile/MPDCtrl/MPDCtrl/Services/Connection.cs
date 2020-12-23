using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Services
{
    public class Connection : BaseViewModel
    {

        private MPC _mpc = new MPC("192.168.3.2", 6600, "asdf");

        public MPC Mpc
        {
            get => _mpc;
        }

        private bool _isConnected;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                SetProperty(ref _isConnected, value);

                if (_isConnected)
                {
                    IsConnecting = false;
                }
            }
        }

        private bool _isMpdConnected;
        public bool IsMpdConnected
        {
            get
            {
                return _isMpdConnected;
            }
            set
            {
                SetProperty(ref _isMpdConnected, value);

            }
        }

        private bool _isConnecting;
        public bool IsConnecting
        {
            get
            {
                return _isConnecting;
            }
            set
            {
                SetProperty(ref _isConnecting, value);
            }
        }


        private string _connectionStatusMessage = "";
        public string ConnectionStatusMessage
        {
            get
            {
                return _connectionStatusMessage;
            }
            set
            {
                SetProperty(ref _connectionStatusMessage, value);
            }
        }

        public ObservableCollection<SongInfo> Queue { get; set; } = new ObservableCollection<SongInfo>();


        public ObservableCollection<String> Playlists
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.Playlists;
                }
                else
                {
                    return null;
                }
            }
        }

        public Connection()
        {
            _mpc.Connected += new MPC.MpdConnected(OnMpdConnected);
            _mpc.StatusChanged += new MPC.MpdStatusChanged(OnStatusChanged);
            _mpc.StatusUpdate += new MPC.MpdStatusUpdate(OnMpdStatusUpdate);
            _mpc.ErrorReturned += new MPC.MpdError(OnError);
            _mpc.ErrorConnected += new MPC.MpdConnectionError(OnConnectionError);
            _mpc.ConnectionStatusChanged += new MPC.MpdConnectionStatusChanged(OnConnectionStatusChanged);
            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);

        }

        public async void Start()
        {
            await _mpc.MpdConnect();
        }

        private void OnMpdConnected(MPC sender)
        {

            IsMpdConnected = true;


            Debug.WriteLine("OnMpdConnected");
            // got MPD ver.
            //MpdVersion = _mpc.MpdVer;

            //MpdStatusMessage = "MPD OK " + _mpc.MpdVer;
            //MpdStatusButton = _pathConnectedButton;

            _mpc.MpdSendPassword();
            /*
            if (IsUpdateOnStartup)
            {
                _mpc.MpdSendUpdate();
            }
            */

            //
            _mpc.MpdQueryCurrentQueue();

            // Call Status "after" MpdQueryCurrentQueue() in order to get "current song" in the queue.
            _mpc.MpdQueryStatus();

            _mpc.MpdQueryPlaylists();

            // heavy stuff should be the last.
            //_mpc.MpdQueryListAll();

        }

        // MPD changed nortifiation
        private void OnStatusChanged(MPC sender, object data)
        {
            //System.Diagnostics.Debug.WriteLine("OnStatusChanged " + data);

            //UpdateButtonStatus();
            /*
            List<string> SubSystems = (data as string).Split('\n').ToList();

            foreach (string line in SubSystems)
            {
                if (line == "changed: playlist")
                {

                }
                else if (line == "changed: player")
                {

                }
                else if (line == "changed: options")
                {

                }
                else if (line == "changed: mixer")
                {

                }
                else if (line == "changed: stored_playlist")
                {

                }
            }
            */
        }

        // MPD updated information
        private void OnMpdStatusUpdate(MPC sender, object data)
        {
            if ((data as string) == "isPlayer")
            {
                //
            }
            else if ((data as string) == "isCurrentQueue")
            {
                IsBusy = true;

                if (Queue.Count > 0)
                {
                    // 削除する曲の一時リスト
                    List<SongInfo> _tmpQueue = new List<SongInfo>();

                    Device.BeginInvokeOnMainThread(() =>
                    {
                        IsBusy = true;
                        // 既存のリストの中で新しいリストにないものを削除
                        foreach (var sng in Queue)
                        {
                            var queitem = _mpc.CurrentQueue.FirstOrDefault(i => i.Id == sng.Id);
                            if (queitem == null)
                            {
                                // 削除リストに追加
                                _tmpQueue.Add(sng);
                            }
                        }

                        // 削除リストをループ
                        foreach (var hoge in _tmpQueue)
                        {
                            Queue.Remove(hoge);
                        }

                        // 新しいリストの中から既存のリストにないものを追加または更新
                        foreach (var sng in _mpc.CurrentQueue)
                        {
                            var fuga = Queue.FirstOrDefault(i => i.Id == sng.Id);
                            if (fuga != null)
                            {
                                // TODO:
                                fuga.Pos = sng.Pos;
                                //fuga.Id = sng.Id; // 流石にIDは変わらないだろう。
                                fuga.LastModified = sng.LastModified;
                                //fuga.Time = sng.Time; // format exception が煩い。
                                fuga.Title = sng.Title;
                                fuga.Artist = sng.Artist;
                                fuga.Album = sng.Album;
                                fuga.AlbumArtist = sng.AlbumArtist;
                                fuga.Composer = sng.Composer;
                                fuga.Date = sng.Date;
                                fuga.duration = sng.duration;
                                fuga.file = sng.file;
                                fuga.Genre = sng.Genre;
                                fuga.Track = sng.Track;

                                //Queue.Move(fuga.Index, sng.Index);
                                fuga.Index = sng.Index;
                            }
                            else
                            {
                                Queue.Add(sng);
                                //Queue.Insert(sng.Index, sng);
                            }
                        }
                    });
                }
                else
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        foreach (var sng in _mpc.CurrentQueue)
                        {
                            Queue.Add(sng);
                        }
                    });
                }

                IsBusy = false;
            }
            else if ((data as string) == "isSongs")
            {
                // Find の結果か playlistinfoの結果
            }
            else if ((data as string) == "isStoredPlaylist")
            {
                // MPCが自動で見に行ってるから特にアクションなし。
            }
            else if ((data as string) == "isLocalFiles")
            {
                IsBusy = true;
                /*
                Application.Current.Dispatcher.Invoke(() =>
                {
                    LocalFiles.Clear();

                    foreach (var songfile in _mpc.LocalFiles)
                    {
                        try
                        {
                            Uri uri = new Uri(@"file:///" + songfile);
                            if (uri.IsFile)
                            {
                                string filename = System.IO.Path.GetFileName(songfile);//System.IO.Path.GetFileName(uri.LocalPath);
                                NodeEntry hoge = new NodeEntry(filename, uri, songfile);

                                LocalFiles.Add(hoge);
                            }
                        }
                        catch { }
                    }
                });

                IsBusy = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MusicDirectories.Clear();

                    _musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());

                    if (MusicDirectories.Count > 0)
                    {
                        SelectedNode = _musicDirectories.Children[0];
                    }

                });

                IsBusy = true;
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MusicEntries.Clear();

                    foreach (var songfile in _mpc.LocalFiles)
                    {
                        try
                        {
                            Uri uri = new Uri(@"file:///" + songfile);
                            if (uri.IsFile)
                            {
                                string filename = System.IO.Path.GetFileName(songfile);//System.IO.Path.GetFileName(uri.LocalPath);
                                NodeEntry hoge = new NodeEntry(filename, uri, songfile);

                                MusicEntries.Add(hoge);
                            }

                        }
                        catch { }
                    }
                });
                */
                IsBusy = false;

            }
            else if ((data as string) == "isUpdating_db")
            {
                /*
                System.Diagnostics.Debug.WriteLine("OnMpdStatusUpdate: isUpdating_db");
                ConnectionStatusMessage = "Updating db...";

                IsUpdatingMpdDb = true;
                */
            }
            else if ((data as string) == "isAlbumart")
            {
                /*
                if ((!_mpc.AlbumArt.IsDownloading) && _mpc.AlbumArt.IsSuccess)
                {
                    if ((CurrentSong != null) && (_mpc.AlbumArt.AlbumImageSource != null))
                    {
                        // AlbumArt
                        if (!String.IsNullOrEmpty(CurrentSong.file))
                        {
                            if (CurrentSong.file == _mpc.AlbumArt.SongFilePath)
                            {
                                AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                                IsAlbumArtVisible = true;
                            }
                        }
                    }
                }
                */
            }
        }

        private void OnError(MPC sender, MPC.MpdErrorTypes errType, object data)
        {
            if (data == null) { return; }

            if (errType == MPC.MpdErrorTypes.CommandError)
            {
                string s = (data as string);
                string patternStr = @"[{\[].+?[}\]]";//@"[.*?]";
                s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
                s = s.Replace("ACK ", string.Empty);
                s = s.Replace("{} ", string.Empty);
                s = s.Replace("[] ", string.Empty);

                //MpdStatusMessage = MPDCtrl.Properties.Resources.MPD_CommandError + ": " + s;
                //MpdStatusButton = _pathErrorMpdAckButton;
            }
            else if (errType == MPC.MpdErrorTypes.ConnectionError)
            {
                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + (data as string);
                //StatusButton = _pathErrorInfoButton;

                //IsConnected = false;
                //IsConnectionSettingShow = true;
            }
            else if (errType == MPC.MpdErrorTypes.StatusError)
            {
                //MpdStatusMessage = MPDCtrl.Properties.Resources.MPD_StatusError + ": " + (data as string);
                //MpdStatusButton = _pathErrorMpdAckButton;
            }
            else if (errType == MPC.MpdErrorTypes.ErrorClear)
            {
                //MpdStatusMessage = "";
                //MpdStatusButton = _pathDefaultNoneButton;
            }
            else
            {
                // TODO:
                //ConnectionStatusMessage = "Unknown error: " + (data as string);
                //StatusButton = _pathErrorInfoButton;
            }
        }

        private async void OnConnectionError(MPC sender, object data)
        {
            if (data == null) { return; }

            IsConnected = false;
            //IsConnectionSettingShow = true;

            //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + (data as string);
            //StatusButton = _pathErrorInfoButton;

            await Shell.Current.GoToAsync("//ConnectPage");
        }

        private async void OnConnectionStatusChanged(MPC sender, TCPC.ConnectionStatus status)
        {

            if (status == TCPC.ConnectionStatus.Connected)
            {
                IsConnected = true;
                IsConnecting = false;
                //IsConnectionSettingShow = false;

                Debug.WriteLine("ConnectionStatus_Connected");
                ConnectionStatusMessage = "Connected ";// MPDCtrl.Properties.Resources.ConnectionStatus_Connected;
                //StatusButton = _pathConnectedButton;
            }
            else if (status == TCPC.ConnectionStatus.Connecting)
            {
                IsConnected = false;
                IsConnecting = true;

                Debug.WriteLine("ConnectionStatus_Connecting");
                ConnectionStatusMessage = "ConnectionStatus_Connecting"; //MPDCtrl.Properties.Resources.ConnectionStatus_Connecting;
                //StatusButton = _pathConnectingButton;
            }
            else if (status == TCPC.ConnectionStatus.AutoReconnecting)
            {
                //IsConnected = false;
                //IsConnecting = true;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Reconnecting;
                //StatusButton = _pathConnectingButton;
            }
            else if (status == TCPC.ConnectionStatus.ConnectFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectFail_Timeout;
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.DisconnectedByHost)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_DisconnectedByHost");
                ConnectionStatusMessage = "ConnectionStatus_DisconnectedByHost";//MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByHost;
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.DisconnectedByUser)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByUser;
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.Error)
            {
                // TODO: OnConnectionErrorと被る。

                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = "Error..";
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.SendFail_NotConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.SendFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_Timeout;
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.NeverConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.Disconnecting)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_Disconnecting");
                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                //StatusButton = _pathErrorInfoButton;

                await Shell.Current.GoToAsync("//ConnectPage");
            }

        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }




    }
}
