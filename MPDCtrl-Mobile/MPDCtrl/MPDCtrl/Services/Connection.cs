using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Xml;
using System.Xml.Linq;
using System.Net;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Services
{
    public class Connection : BaseViewModel
    {

        private MPC _mpc = new MPC("192.168.3.123", 6600, "");

        public MPC Mpc
        {
            get => _mpc;
        }

        #region == ステータス系 ==

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

        #endregion

        #region == Current song info ==

        private SongInfo _currentSong;
        public SongInfo CurrentSong
        {
            get => _currentSong;
            set
            {
                SetProperty(ref _currentSong, value);

                NotifyPropertyChanged("CurrentSongTitle");
                NotifyPropertyChanged("CurrentSongArtist");
                NotifyPropertyChanged("CurrentSongAlbum");
                NotifyPropertyChanged("CurrentSongArtistAndAlbum");
            }
        }

        public string CurrentSongTitle
        {
            get
            {
                if (_currentSong != null)
                {
                    return _currentSong.Title;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string CurrentSongArtist
        {
            get
            {
                if (_currentSong != null)
                {
                    if (!string.IsNullOrEmpty(_currentSong.Artist))
                        return _currentSong.Artist.Trim();
                    else
                        return "";
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public string CurrentSongAlbum
        {
            get
            {
                if (_currentSong != null)
                {
                    if (!string.IsNullOrEmpty(_currentSong.Album))
                        return _currentSong.Album.Trim();
                    else
                        return "";
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        public string CurrentSongArtistAndAlbum
        {
            get
            {
                if (!string.IsNullOrEmpty(CurrentSongAlbum))
                {
                    return CurrentSongArtist + " - " + CurrentSongAlbum;
                }
                else
                {
                    return CurrentSongArtist;
                }
            }
        }

        #endregion

        #region == AlbumArt == 

        private ImageSource _albumArtDefault = "DefaultAlbumCoverGray.png";
        private ImageSource _albumArt = "DefaultAlbumCoverGray.png";
        public ImageSource AlbumArt
        {
            get
            {
                return _albumArt;
            }
            set
            {
                if (_albumArt == value)
                    return;

                _albumArt = value;
                NotifyPropertyChanged("AlbumArt");
            }
        }

        #endregion

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

        #region == Profiles ==

        private ObservableCollection<Profile> _profiles = new ObservableCollection<Profile>();
        public ObservableCollection<Profile> Profiles
        {
            get { return _profiles; }
        }

        private Profile _currentProfile;
        public Profile CurrentProfile
        {
            get { return _currentProfile; }
            set
            {
                if (_currentProfile == value)
                    return;

                _currentProfile = value;

                //SelectedProfile = _currentProfile;

                NotifyPropertyChanged("CurrentProfile");

                NotifyPropertyChanged("IsCurrentProfileNull");
            }
        }


        private string _host = "";
        public string Host
        {
            get { return _host; }
            set
            {
                ClearErrror("Host");
                _host = value;

                // Validate input.
                if (value == "")
                {
                    SetError("Host", "Settings_ErrorHostMustBeSpecified");

                }
                else if (value == "localhost")
                {
                    _host = "127.0.0.1";
                }
                else
                {
                    IPAddress ipAddress = null;
                    try
                    {
                        ipAddress = IPAddress.Parse(value);
                        if (ipAddress != null)
                        {
                            _host = value;
                        }
                    }
                    catch
                    {
                        //System.FormatException
                        SetError("Host", "Settings_ErrorHostInvalidAddressFormat");
                    }
                }

                NotifyPropertyChanged("Host");
            }
        }

        private int _port = 6600;
        public string Port
        {
            get { return _port.ToString(); }
            set
            {
                ClearErrror("Port");

                if (value == "")
                {
                    SetError("Port", "Settings_ErrorPortMustBeSpecified");
                    _port = 0;
                }
                else
                {
                    // Validate input. Test with i;
                    if (Int32.TryParse(value, out int i))
                    {
                        //Int32.TryParse(value, out _defaultPort)
                        // Change the value only when test was successfull.
                        _port = i;
                        ClearErrror("Port");
                    }
                    else
                    {
                        SetError("Port", "Resources.Settings_ErrorInvalidPortNaN");
                        _port = 0;
                    }
                }

                NotifyPropertyChanged("Port");
            }
        }

        private string DummyPassword(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }
            string e = "";
            for (int i = 1; i <= s.Length; i++)
            {
                e = e + "*";
            }
            return e;
        }

        private string _password = "";
        public string Password
        {
            get
            {
                return DummyPassword(_password);
            }
            set
            {
                // Don't. if (_password == value) ...

                _password = value;

                NotifyPropertyChanged("IsNotPasswordSet");
                NotifyPropertyChanged("IsPasswordSet");
                NotifyPropertyChanged("Password");
            }
        }

        private bool _setIsDefault = true;
        public bool SetIsDefault
        {
            get { return _setIsDefault; }
            set
            {
                if (_setIsDefault == value)
                    return;

                if (value == false)
                {
                    if (Profiles.Count == 1)
                    {
                        return;
                    }
                }

                _setIsDefault = value;

                NotifyPropertyChanged("SetIsDefault");
            }
        }

        private string _settingProfileEditMessage;
        public string SettingProfileEditMessage
        {
            get
            {
                return _settingProfileEditMessage;
            }
            set
            {
                _settingProfileEditMessage = value;
                NotifyPropertyChanged("SettingProfileEditMessage");
            }
        }

        #endregion

        #region == イベント ==

        public delegate void PlayerStatusChanged(MPC sender, bool isCurrentSongChanged);
        public event PlayerStatusChanged OnPlayerStatusChanged;

        public delegate void CurrentQueueStatusChanged(MPC sender, bool isCurrentSongChanged);
        public event CurrentQueueStatusChanged OnCurrentQueueStatusChanged;

        public delegate void AlbumArtStatusChanged(MPC sender);
        public event AlbumArtStatusChanged OnAlbumArtStatusChanged;

        #endregion

        public bool IsProfileSet { get; set; }

        public Connection()
        {
            _mpc.Connected += new MPC.MpdConnected(OnMpdConnected);
            _mpc.StatusUpdate += new MPC.MpdStatusUpdate(OnMpdStatusUpdate);
            _mpc.ErrorReturned += new MPC.MpdError(OnError);
            _mpc.ErrorConnected += new MPC.MpdConnectionError(OnConnectionError);
            _mpc.ConnectionStatusChanged += new MPC.MpdConnectionStatusChanged(OnConnectionStatusChanged);
            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);

            Debug.WriteLine("Loading Profiles");
            LoadProfile();

            if (CurrentProfile != null)
            {
                Debug.WriteLine("Found Deault Profiles");

                _mpc.MpdHost = CurrentProfile.Host;
                _mpc.MpdPort = CurrentProfile.Port;
                _mpc.MpdPassword = CurrentProfile.Password;

                IsProfileSet = true;
            }
            else
            {
                Debug.WriteLine("Found No Deault Profiles");
                IsProfileSet = false;
            }
        }

        public void SaveProfile()
        {
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

            // Root Document Element
            XmlElement root = doc.CreateElement(string.Empty, "App", string.Empty);
            doc.AppendChild(root);

            #region == プロファイル設定  ==

            XmlElement xProfiles = doc.CreateElement(string.Empty, "Profiles", string.Empty);

            XmlElement xProfile;
            XmlAttribute xAttrs;

            foreach (var p in Profiles)
            {
                xProfile = doc.CreateElement(string.Empty, "Profile", string.Empty);

                xAttrs = doc.CreateAttribute("Name");
                xAttrs.Value = p.Name;
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Host");
                xAttrs.Value = p.Host;
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Port");
                xAttrs.Value = p.Port.ToString();
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Password");
                xAttrs.Value = System.Security.Cryptography.Crypto.Encrypt(p.Password, "hoge");
                xProfile.SetAttributeNode(xAttrs);

                if (p.IsDefault)
                {
                    xAttrs = doc.CreateAttribute("IsDefault");
                    xAttrs.Value = "True";
                    xProfile.SetAttributeNode(xAttrs);
                }

                xProfiles.AppendChild(xProfile);
            }

            root.AppendChild(xProfiles);

            #endregion

            Preferences.Set("Profiles", doc.OuterXml);

        }

        private void LoadProfile()
        {
            if (Preferences.ContainsKey("Profiles"))
            {
                try
                {
                    XDocument xdoc = XDocument.Parse(Preferences.Get("Profiles", ""));

                    #region == プロファイル設定  ==

                    var xProfiles = xdoc.Root.Element("Profiles");
                    if (xProfiles != null)
                    {
                        var profileList = xProfiles.Elements("Profile");

                        foreach (var p in profileList)
                        {
                            Profile pro = new Profile();

                            if (p.Attribute("Name") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Name").Value))
                                    pro.Name = p.Attribute("Name").Value;
                            }
                            if (p.Attribute("Host") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Host").Value))
                                    pro.Host = p.Attribute("Host").Value;
                            }
                            if (p.Attribute("Port") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Port").Value))
                                {
                                    try
                                    {
                                        pro.Port = Int32.Parse(p.Attribute("Port").Value);
                                    }
                                    catch
                                    {
                                        pro.Port = 6600;
                                    }
                                }
                            }
                            if (p.Attribute("Password") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Password").Value))
                                    pro.Password = System.Security.Cryptography.Crypto.Decrypt(p.Attribute("Password").Value, "hoge");
                            }
                            if (p.Attribute("IsDefault") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("IsDefault").Value))
                                {
                                    if (p.Attribute("IsDefault").Value == "True")
                                    {
                                        pro.IsDefault = true;

                                        CurrentProfile = pro;
                                        //SelectedProfile = pro;
                                    }
                                }
                            }

                            Profiles.Add(pro);
                        }
                    }
                    #endregion

                }
                catch(Exception e)
                {
                    Debug.WriteLine("Exception at LoadProfile(): "+e.Message);
                }

            }

        }


        public async void Start()
        {
            if (!IsProfileSet)
                return;

            ConnectionResult ret = await _mpc.MpdConnect();
            if (ret.isSuccess)
            {

            }
            else
            {

            }
        }

        private async void OnMpdConnected(MPC sender)
        {
            Debug.WriteLine("OnMpdConnected");

            IsMpdConnected = true;

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

            await Task.Delay(200);

            _mpc.MpdQueryStatus();

            await Task.Delay(200);

            _mpc.MpdQueryCurrentQueue();

            await Task.Delay(200);

            _mpc.MpdQueryPlaylists();

            await Task.Delay(200);

            // heavy stuff should be the last.
            //_mpc.MpdQueryListAll();

        }

        // MPD updated information
        private void OnMpdStatusUpdate(MPC sender, object data)
        {
            if ((data as string) == "isPlayer")
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    ///UpdateButtonStatus();

                    bool isSongChanged = false;
                    bool isCurrentSongWasNull = false;

                    if (CurrentSong != null)
                    {
                        if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                        {
                            isSongChanged = true;

                            // Clear IsPlaying icon
                            CurrentSong.IsPlaying = false;

                            //Device.BeginInvokeOnMainThread(() =>
                            //{
                            AlbumArt = _albumArtDefault;
                            //});
                        }
                    }
                    else
                    {
                        isCurrentSongWasNull = true;
                    }

                    // Sets Current Song
                    var item = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (item != null)
                    {
                        CurrentSong = (item as SongInfo);
                        CurrentSong.IsPlaying = true;

                        // AlbumArt
                        if (isSongChanged)
                        {
                            if (!String.IsNullOrEmpty(CurrentSong.file))
                            {
                                //Debug.WriteLine("AlbumArt isPlayer: " + CurrentSong.file);
                                _mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                            }
                        }
                        else
                        {
                            if (isCurrentSongWasNull)
                            {
                                if (!String.IsNullOrEmpty(CurrentSong.file))
                                {
                                    //Debug.WriteLine("AlbumArt isPlayer: isCurrentSongWasNull");
                                    _mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                }
                            }
                            else
                            {
                                //Debug.WriteLine("AlbumArt isPlayer: !isSongChanged.");
                            }
                        }

                        ///_selectedItem = CurrentSong;
                        ///NotifyPropertyChanged("SelectedItem");

                        ///ScrollIntoView?.Invoke(this, CurrentSong);
                    }
                    else
                    {
                        CurrentSong = null;

                        //Debug.WriteLine("AlbumArt isPlayer: CurrentSong null");

                        //Device.BeginInvokeOnMainThread(() =>
                        //{
                        AlbumArt = _albumArtDefault;
                        //});
                    }

                    OnPlayerStatusChanged?.Invoke(sender, isSongChanged);
                });
            }
            else if ((data as string) == "isCurrentQueue")
            {
                IsBusy = true;

                Device.BeginInvokeOnMainThread(() =>
                {
                    bool isSongChanged = false;

                    if (Queue.Count > 0)
                    {
                        // 削除する曲の一時リスト
                        List<SongInfo> _tmpQueue = new List<SongInfo>();

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

                        if (CurrentSong == null)
                        {
                            isSongChanged = true;
                        }
                        else
                        {
                            if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                            {
                                isSongChanged = true;
                            }
                        }

                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem != null)
                        {
                            CurrentSong = (curitem as SongInfo);
                            CurrentSong.IsPlaying = true;
                        }
                        else
                        {
                            CurrentSong = null;
                        }

                        // AlbumArt
                        if (CurrentSong != null)
                        {
                            if (_mpc.AlbumArt.SongFilePath != CurrentSong.file)
                            {
                                //Device.BeginInvokeOnMainThread(() =>
                                //{
                                AlbumArt = _albumArtDefault;
                                //});

                                // a hack for iOS.
                                //await Task.Delay(100);

                                if (!String.IsNullOrEmpty(CurrentSong.file))
                                {
                                    //Debug.WriteLine("AlbumArt isCurrentQueue: " + CurrentSong.file);

                                    _mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                }
                            }
                        }
                        else
                        {
                            //Debug.WriteLine("AlbumArt isCurrentQueue: CurrentSong null");

                            //Device.BeginInvokeOnMainThread(() =>
                            //{
                            AlbumArt = _albumArtDefault;
                            //});
                        }
                    }
                    else
                    {
                        IsBusy = true;

                        foreach (var sng in _mpc.CurrentQueue)
                        {
                            Queue.Add(sng);
                        }

                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem != null)
                        {
                            CurrentSong = (curitem as SongInfo);
                            CurrentSong.IsPlaying = true;

                            // AlbumArt
                            if (_mpc.AlbumArt.SongFilePath != curitem.file)
                            {
                                //IsAlbumArtVisible = false;
                                AlbumArt = _albumArtDefault;

                                //Debug.WriteLine("AlbumArt isCurrentQueue from Clear: " + CurrentSong.file);
                                if (!String.IsNullOrEmpty((curitem as SongInfo).file))
                                {
                                    _mpc.MpdQueryAlbumArt((curitem as SongInfo).file, CurrentSong.Id);
                                }
                            }
                        }
                        else
                        {
                            // just in case.
                            CurrentSong = null;

                            //IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;
                        }

                        //
                        isSongChanged = true;
                    }

                    OnCurrentQueueStatusChanged?.Invoke(sender, isSongChanged);
                });

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
                Device.BeginInvokeOnMainThread(() =>
                {
                    if (CurrentSong != null)
                    {
                        if (CurrentSong.file == _mpc.AlbumArt.SongFilePath)
                        {
                            // a hack for iOS.
                            AlbumArt = null;

                            AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                        }
                    }

                    OnAlbumArtStatusChanged?.Invoke(sender);
                });
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

        private void OnConnectionError(MPC sender, object data)
        {
            if (data == null) { return; }

            IsConnected = false;
            //IsConnectionSettingShow = true;

            //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + (data as string);
            //StatusButton = _pathErrorInfoButton;

            //await Shell.Current.GoToAsync("//ConnectPage");
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

                //await Shell.Current.GoToAsync("//ConnectPage");
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

                //await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.Error)
            {
                // TODO: OnConnectionErrorと被る。

                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = "Error..";
                //StatusButton = _pathErrorInfoButton;

                //await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.SendFail_NotConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
                //StatusButton = _pathErrorInfoButton;

                //await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.SendFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_Timeout;
                //StatusButton = _pathErrorInfoButton;

                //await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.NeverConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                //StatusButton = _pathErrorInfoButton;

                //await Shell.Current.GoToAsync("//ConnectPage");
            }
            else if (status == TCPC.ConnectionStatus.Disconnecting)
            {
                IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_Disconnecting");
                //ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                //StatusButton = _pathErrorInfoButton;

                //await Shell.Current.GoToAsync("//ConnectPage");
            }

        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }

    }
}
