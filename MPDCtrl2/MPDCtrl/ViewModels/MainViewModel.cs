using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Media;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Input;
using System.IO;
using System.ComponentModel;
using MPDCtrl.Common;
using System.Windows.Threading;

namespace MPDCtrl.ViewModels
{

    /// <summary>
    /// https://www.musicpd.org/doc/html/protocol.html
    /// </summary>
    /// 

    public class MainViewModel : ViewModelBase
    {
        // Application name
        const string _appName = "MPDCtrl";

        // Application version
        const string _appVer = "0.0.0";
        public string AppVer
        {
            get
            {
                return _appVer;
            }
        }

        // Application config file folder
        const string _appDeveloper = "torum";

        // Application Window Title
        public string AppTitle
        {
            get
            {
                return _appName + " " + _appVer;
            }
        }

        public bool DebugMode
        {
            get
            {
                #if DEBUG
                return true;
                #else
                return false; 
                #endif

            }
        }


        private string _envDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private string _appDataFolder;
        private string _appConfigFilePath;

        private MPC _MPC;
        private string _defaultHost;
        private int _defaultPort;
        private string _defaultPassword;

        private bool _isConnected;
        public bool IsConnected
        {
            get
            {
                return this._isConnected;
            }
            set
            {
                this._isConnected = value;
                this.NotifyPropertyChanged("IsConnected");
            }
        }

        private string _statusMessage;
        public string StatusMessage
        {
            get
            {
                return this._statusMessage;
            }
            set
            {
                this._statusMessage = value;
                this.NotifyPropertyChanged("StatusMessage");
            }
        }

        private string _debugString;
        public string DebugString
        {
            get
            {
                return _debugString;
            }
            set
            {
                _debugString = value;
                NotifyPropertyChanged("DebugString");
            }
        }

        private static string _pathConnectingButton = "M11 14H9C9 9.03 13.03 5 18 5V7C14.13 7 11 10.13 11 14M18 11V9C15.24 9 13 11.24 13 14H15C15 12.34 16.34 11 18 11M7 4C7 2.89 6.11 2 5 2S3 2.89 3 4 3.89 6 5 6 7 5.11 7 4M11.45 4.5H9.45C9.21 5.92 8 7 6.5 7H3.5C2.67 7 2 7.67 2 8.5V11H8V8.74C9.86 8.15 11.25 6.5 11.45 4.5M19 17C20.11 17 21 16.11 21 15S20.11 13 19 13 17 13.89 17 15 17.89 17 19 17M20.5 18H17.5C16 18 14.79 16.92 14.55 15.5H12.55C12.75 17.5 14.14 19.15 16 19.74V22H22V19.5C22 18.67 21.33 18 20.5 18Z";
        private static string _pathConnectedButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";
        private static string _pathDisconnectedButton = "";
        private static string _pathErrorInfoButton = "M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z";
        private string _statusButton = _pathDisconnectedButton;
        public string StatusButton
        {
            get
            {
                return _statusButton;
            }
            set
            {
                if (_statusButton == value)
                    return;

                _statusButton = value;
                this.NotifyPropertyChanged("StatusButton");
            }
        }

        private static string _pathPlayButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathPauseButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathStopButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private string _playButton;
        public string PlayButton
        {
            get
            {
                return _playButton;
            }
            set
            {
                if (_playButton == value)
                    return;

                _playButton = value;
                this.NotifyPropertyChanged("PlayButton");
            }
        }

        private double _volume;
        public double Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                if (_volume != value)
                {
                    this._volume = value;
                    this.NotifyPropertyChanged("Volume");

                    if (_MPC != null)
                    {
                        // If we have a timer and we are in this event handler, a user is still interact with the slider
                        // we stop the timer
                        if (_volumeDelayTimer != null)
                            _volumeDelayTimer.Stop();

                        System.Diagnostics.Debug.WriteLine("Volume value is still changing. Skipping.");

                        // we always create a new instance of DispatcherTimer
                        _volumeDelayTimer = new System.Timers.Timer();
                        _volumeDelayTimer.AutoReset = false;

                        // if one second passes, that means our user has stopped interacting with the slider
                        // we do real event
                        _volumeDelayTimer.Interval = (double)1000;
                        _volumeDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeVolume);

                        _volumeDelayTimer.Start();
                    }
                }
            }
        }
        
        private System.Timers.Timer _volumeDelayTimer = null;
        private void DoChangeVolume(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_MPC != null)
            {
                if (Convert.ToDouble(_MPC.MpdStatus.MpdVolume) != this._volume)
                {
                    if (SetVolumeCommand.CanExecute(null))
                    {
                        SetVolumeCommand.Execute(null);
                    }
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Volume value is the same. Skipping.");
                }
            }
        }

        private bool _repeat;
        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                this._repeat = value;
                this.NotifyPropertyChanged("Repeat");

                if (_MPC != null)
                {
                    if (_MPC.MpdStatus.MpdRepeat != value)
                    {
                        if (SetRpeatCommand.CanExecute(null))
                        {
                            SetRpeatCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _random;
        public bool Random
        {
            get { return _random; }
            set
            {
                this._random = value;
                this.NotifyPropertyChanged("Random");

                if (_MPC != null)
                {

                    if (_MPC.MpdStatus.MpdRandom != value)
                    {
                        if (SetRandomCommand.CanExecute(null))
                        {
                            SetRandomCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private double _time;
        public double Time
        {
            get
            {
                return this._time;
            }
            set
            {
                this._time = value;
                this.NotifyPropertyChanged("Time");
            }
        }

        private double _elapsed;
        public double Elapsed
        {
            get
            {
                return this._elapsed;
            }
            set
            {
                if ((value < this._time) && _elapsed != value)
                {
                    this._elapsed = value;
                    this.NotifyPropertyChanged("Elapsed");
                    /*
                    if (SetSeekCommand.CanExecute(null))
                    {
                        SetSeekCommand.Execute(null);
                    }
                    */

                    // If we have a timer and we are in this event handler, a user is still interact with the slider
                    // we stop the timer
                    if (_elapsedDelayTimer != null)
                        _elapsedDelayTimer.Stop();

                    System.Diagnostics.Debug.WriteLine("Elapsed value is still changing. Skipping.");

                    // we always create a new instance of DispatcherTimer
                    _elapsedDelayTimer = new System.Timers.Timer();
                    _elapsedDelayTimer.AutoReset = false;

                    // if one second passes, that means our user has stopped interacting with the slider
                    // we do real event
                    _elapsedDelayTimer.Interval = (double)1000;
                    _elapsedDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeElapsed);

                    _elapsedDelayTimer.Start();
                }
            }
        }
        
        private System.Timers.Timer _elapsedDelayTimer = null;
        private void DoChangeElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_MPC != null)
            {
                if ((this._elapsed < this._time))
                {
                    if (SetSeekCommand.CanExecute(null))
                    {
                        SetSeekCommand.Execute(null);
                    }
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Volume value is the same. Skipping.");
                }
            }
        }

        private DispatcherTimer _elapsedTimer;
        
        public ObservableCollection<MPC.Song> Songs
        {
            get
            {
                if (_MPC != null)
                {
                    return _MPC.CurrentQueue;
                }
                else
                {
                    return null;
                }
            }
        }

        private MPC.Song _selectedSong;
        public MPC.Song SelectedSong
        {
            get
            {
                return _selectedSong;
            }
            set
            {
                _selectedSong = value;
                this.NotifyPropertyChanged("SelectedSong");
            }
        }

        public ObservableCollection<string> Playlists
        {
            get
            {
                if (_MPC != null)
                {
                    return _MPC.Playlists;
                }
                else
                {
                    return null;
                }
            }
        }

        private string _selecctedPlaylist;
        public string SelectedPlaylist
        {
            get
            {
                return _selecctedPlaylist;
            }
            set
            {
                if (_selecctedPlaylist != value)
                {
                    _selecctedPlaylist = value;
                    this.NotifyPropertyChanged("SelectedPlaylist");
                }
            }
        }

        private string _currentSongTitle;
        public string CurrentSongTitle
        {
            get
            {
                return _currentSongTitle;
            }
            set
            {
                if (_currentSongTitle == value) 
                    return;

                _currentSongTitle = value;
                this.NotifyPropertyChanged("CurrentSongTitle");
            }
        }

        private string _currentSongArtist;
        public string CurrentSongArtist
        {
            get
            {
                return _currentSongArtist;
            }
            set
            {
                if (_currentSongArtist == value)
                    return;

                _currentSongArtist = value;
                this.NotifyPropertyChanged("CurrentSongArtist");
            }
        }

        public MainViewModel()
        {
            #region == データ保存フォルダ ==
            // データ保存フォルダの取得
            _appDataFolder = _envDataFolder + System.IO.Path.DirectorySeparatorChar + _appDeveloper + System.IO.Path.DirectorySeparatorChar + _appName;
            // 設定ファイルのパス
            _appConfigFilePath = _appDataFolder + System.IO.Path.DirectorySeparatorChar + _appName + ".config";
            // 存在していなかったら作成
            System.IO.Directory.CreateDirectory(_appDataFolder);
            #endregion

            #region == コマンド ==
            ListAllCommand = new RelayCommand(ListAllCommand_ExecuteAsync, ListAllCommand_CanExecute);
            PlayCommand = new RelayCommand(PlayCommand_ExecuteAsync, PlayCommand_CanExecute);
            PlayNextCommand = new RelayCommand(PlayNextCommand_ExecuteAsync, PlayNextCommand_CanExecute);
            PlayPrevCommand = new RelayCommand(PlayPrevCommand_ExecuteAsync, PlayPrevCommand_CanExecute);
            SetRpeatCommand = new RelayCommand(SetRpeatCommand_ExecuteAsync, SetRpeatCommand_CanExecute);
            SetRandomCommand = new RelayCommand(SetRandomCommand_ExecuteAsync, SetRandomCommand_CanExecute);
            SetVolumeCommand = new RelayCommand(SetVolumeCommand_ExecuteAsync, SetVolumeCommand_CanExecute);
            SetSeekCommand = new RelayCommand(SetSeekCommand_ExecuteAsync, SetSeekCommand_CanExecute);
            ChangeSongCommand = new RelayCommand(ChangeSongCommand_ExecuteAsync, ChangeSongCommand_CanExecute);
            ChangePlaylistCommand = new RelayCommand(ChangePlaylistCommand_ExecuteAsync, ChangePlaylistCommand_CanExecute);
            SongListViewEnterKeyCommand = new RelayCommand(SongListViewEnterKeyCommand_ExecuteAsync, SongListViewEnterKeyCommand_CanExecute);
            SongListViewLeftDoubleClickCommand = new GenericRelayCommand<MPC.Song>(param => SongListViewLeftDoubleClickCommand_ExecuteAsync(param), param => SongListViewLeftDoubleClickCommand_CanExecute());
            PlaylistListviewEnterKeyCommand = new RelayCommand(PlaylistListviewEnterKeyCommand_ExecuteAsync, PlaylistListviewEnterKeyCommand_CanExecute);
            PlaylistListviewLeftDoubleClickCommand = new GenericRelayCommand<String>(param => PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(param), param => PlaylistListviewLeftDoubleClickCommand_CanExecute());
            
            #endregion

            // Initialize connection setting.
            _defaultHost = "127.0.0.1";//"127.0.0.1"//"192.168.3.123"
            _defaultPort = 6600;
            _defaultPassword = "";

            // Create MPC instance.
            _MPC = new MPC(_defaultHost, _defaultPort, _defaultPassword);

            #region == イベント ==
            _MPC.Connected += new MPC.MpdConnected(OnConnected);
            _MPC.StatusChanged += new MPC.MpdStatusChanged(OnStatusChanged);
            _MPC.StatusUpdate += new MPC.MpdStatusUpdate(OnStatusUpdate);
            _MPC.DataReceived += new MPC.MpdDataReceived(OnDataReceived);
            _MPC.DataSent += new MPC.MpdDataSent(OnDataSent);
            _MPC.ErrorReturned += new MPC.MpdError(OnError);
            _MPC.ErrorConnected += new MPC.MpdConnectionError(OnConnectionError);
            _MPC.ConnectionStatusChanged += new MPC.MpdConnectionStatusChanged(OnConnectionStatusChanged);
            #endregion

            // Init Song's time elapsed timer.
            _elapsedTimer = new DispatcherTimer();
            _elapsedTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _elapsedTimer.Tick += new EventHandler(ElapsedTimer);


            // 
            Start();
        }

        #region == システムイベント ==

        // 起動時の処理
        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            #region == アプリ設定のロード  ==

            try
            {
                // アプリ設定情報の読み込み
                if (File.Exists(_appConfigFilePath))
                {
                    XDocument xdoc = XDocument.Load(_appConfigFilePath);

                    #region == ウィンドウ関連 ==

                    if (sender is Window)
                    {
                        // Main Window element
                        var mainWindow = xdoc.Root.Element("MainWindow");
                        if (mainWindow != null)
                        {
                            var hoge = mainWindow.Attribute("top");
                            if (hoge != null)
                            {
                                (sender as Window).Top = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("left");
                            if (hoge != null)
                            {
                                (sender as Window).Left = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("height");
                            if (hoge != null)
                            {
                                (sender as Window).Height = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("width");
                            if (hoge != null)
                            {
                                (sender as Window).Width = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("state");
                            if (hoge != null)
                            {
                                if (hoge.Value == "Maximized")
                                {
                                    (sender as Window).WindowState = WindowState.Maximized;
                                }
                                else if (hoge.Value == "Normal")
                                {
                                    (sender as Window).WindowState = WindowState.Normal;
                                }
                                else if (hoge.Value == "Minimized")
                                {
                                    (sender as Window).WindowState = WindowState.Normal;
                                }
                            }

                        }

                    }

                    #endregion

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                System.Diagnostics.Debug.WriteLine("■■■■■ Error  設定ファイルのロード中 - FileNotFoundException : " + _appConfigFilePath);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("■■■■■ Error  設定ファイルのロード中: " + ex + " while opening : " + _appConfigFilePath);
            }

            #endregion
        }

        // 終了時の処理
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
 
            #region == アプリ設定の保存 ==

            // 設定ファイル用のXMLオブジェクト
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

            // Root Document Element
            XmlElement root = doc.CreateElement(string.Empty, "App", string.Empty);
            doc.AppendChild(root);

            XmlAttribute attrs = doc.CreateAttribute("Version");
            attrs.Value = _appVer;
            root.SetAttributeNode(attrs);

            #region == ウィンドウ関連 ==

            if (sender is Window)
            {
                // Main Window element
                XmlElement mainWindow = doc.CreateElement(string.Empty, "MainWindow", string.Empty);

                // Main Window attributes
                attrs = doc.CreateAttribute("height");
                if ((sender as Window).WindowState == WindowState.Maximized)
                {
                    attrs.Value = (sender as Window).RestoreBounds.Height.ToString();
                }
                else
                {
                    attrs.Value = (sender as Window).Height.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("width");
                if ((sender as Window).WindowState == WindowState.Maximized)
                {
                    attrs.Value = (sender as Window).RestoreBounds.Width.ToString();
                }
                else
                {
                    attrs.Value = (sender as Window).Width.ToString();

                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("top");
                if ((sender as Window).WindowState == WindowState.Maximized)
                {
                    attrs.Value = (sender as Window).RestoreBounds.Top.ToString();
                }
                else
                {
                    attrs.Value = (sender as Window).Top.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("left");
                if ((sender as Window).WindowState == WindowState.Maximized)
                {
                    attrs.Value = (sender as Window).RestoreBounds.Left.ToString();
                }
                else
                {
                    attrs.Value = (sender as Window).Left.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("state");
                if ((sender as Window).WindowState == WindowState.Maximized)
                {
                    attrs.Value = "Maximized";
                }
                else if ((sender as Window).WindowState == WindowState.Normal)
                {
                    attrs.Value = "Normal";

                }
                else if ((sender as Window).WindowState == WindowState.Minimized)
                {
                    attrs.Value = "Minimized";
                }
                mainWindow.SetAttributeNode(attrs);


                // set Main Window element to root.
                root.AppendChild(mainWindow);

            }

            #endregion

            try
            {
                // 設定ファイルの保存
                doc.Save(_appConfigFilePath);
            }
            //catch (System.IO.FileNotFoundException) { }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("■■■■■ Error  設定ファイルの保存中: " + ex + " while opening : " + _appConfigFilePath);
            }

            #endregion

        }

        #endregion

        #region == メソッド ==

        private async void Start()
        {
            ConnectionResult r = await StartConnection();

            if (r.isSuccess)
            {
                _MPC.MpdQueryStatus();

                //
                _MPC.MpdListAll();

                _MPC.MpdQueryCurrentPlaylist();

                _MPC.MpdQueryPlaylists();
            }
            else
            {
                //
            }
        }

        private async Task<ConnectionResult> StartConnection()
        {
            IsConnected = false;
            StatusButton = _pathConnectingButton;

            //TODO: i18n
            //StatusMessage = MPDCtrl.Properties.Resources.Connecting; //"Connecting...";
            StatusMessage = "Connecting...";

            return await _MPC.MpdConnect();
        }

        private void UpdateButtonStatus()
        {
            try
            {
                //Play button
                switch (_MPC.MpdStatus.MpdState)
                {
                    case MPC.Status.MpdPlayState.Play:
                        {
                            PlayButton = _pathPauseButton;
                            break;
                        }
                    case MPC.Status.MpdPlayState.Pause:
                        {
                            PlayButton = _pathPlayButton;
                            break;
                        }
                    case MPC.Status.MpdPlayState.Stop:
                        {
                            PlayButton = _pathPlayButton;
                            break;
                        }

                        //_pathStopButton
                }

                // "quietly" update view.
                this._volume = Convert.ToDouble(_MPC.MpdStatus.MpdVolume);
                this.NotifyPropertyChanged("Volume");

                this._random = _MPC.MpdStatus.MpdRandom;
                this.NotifyPropertyChanged("Random");

                this._repeat = _MPC.MpdStatus.MpdRepeat;
                this.NotifyPropertyChanged("Repeat");

                // no need to care about "double" updates for time.
                this.Time = _MPC.MpdStatus.MpdSongTime;

                this._elapsed = _MPC.MpdStatus.MpdSongElapsed;
                this.NotifyPropertyChanged("Elapsed");

                //
                Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());

                //start elapsed timer.
                if (_MPC.MpdStatus.MpdState == MPC.Status.MpdPlayState.Play)
                {
                    _elapsedTimer.Start();
                }
                else
                {
                    _elapsedTimer.Stop();
                }

            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Error@UpdateButtonStatus");
            }
        }

        private void ElapsedTimer(object sender, EventArgs e)
        {
            if ((_elapsed < _time) && (_MPC.MpdStatus.MpdState == MPC.Status.MpdPlayState.Play))
            {
                this._elapsed += 1;
                this.NotifyPropertyChanged("Elapsed");
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        #endregion

        #region == イベント ==

        private void OnConnected(MPC sender)
        {
            IsConnected = true;
            StatusButton = _pathConnectedButton;
            StatusMessage = "";
        }

        private void OnStatusChanged(MPC sender, object data)
        {
            System.Diagnostics.Debug.WriteLine("OnStatusChanged " + data);

            List<string> SubSystems = (data as string).Split('\n').ToList();

            foreach (string line in SubSystems)
            {
                if (line == "changed: playlist")
                {
                    //StatusMessage = "Loading...";
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

            UpdateButtonStatus();
        }

        private void OnStatusUpdate(MPC sender, object data)
        {
            //System.Diagnostics.Debug.WriteLine("OnStatusUpdate " + data);

            if ((data as string) == "isPlayer")
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        var item = _MPC.CurrentQueue.FirstOrDefault(i => i.ID == _MPC.MpdStatus.MpdSongID);
                        if (item != null)
                        {
                            sender.MpdCurrentSong = (item as MPC.Song); // just in case.
                            SelectedSong = (item as MPC.Song);

                            CurrentSongTitle = (item as MPC.Song).Title;
                            CurrentSongArtist = (item as MPC.Song).Artist;
                        }
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine("_MPC.CurrentQueue.FirstOrDefault@(isPlayer) failed: " + ex.Message);
                }
            }
            else if ((data as string) == "isPlaylist")
            {
                if (sender.MpdCurrentSong != null)
                {
                    ///this._selectedSong = sender.MpdCurrentSong;
                    this.NotifyPropertyChanged("SelectedSong");
                    ///System.Diagnostics.Debug.WriteLine("OnStatusUpdate isPlaylist SelectedSong is : " + this._selectedSong.Title);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusUpdate isPlaylist MpdCurrentSong is : null");
                }
                // Update UI
                this.NotifyPropertyChanged("SelectedSong");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ///UpdateButtonStatus();
                });

            }
            else if ((data as string) == "isStoredPlaylist")
            {
                //
            }

            UpdateButtonStatus();

        }

        private void OnDataReceived(MPC sender, object data)
        {
            DebugString += (data as string);
        }

        private void OnDataSent(MPC sender, object data)
        {
            DebugString += (data as string);
        }

        private void OnError(MPC sender, object data)
        {
            if (data == null) { return; }
            string s = (data as string);
            string patternStr = @"[{\[].+?[}\]]";//@"[.*?]";
            s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
            s = s.Replace("ACK ", string.Empty);
            s = s.Replace("{} ", string.Empty);
            s = s.Replace("[] ", string.Empty);

            StatusMessage = s;
        }

        private void OnConnectionError(MPC sender, object data)
        {
            if (data == null) { return; }
            string s = (data as string);

            IsConnected = false;
            StatusButton = _pathErrorInfoButton;

            StatusMessage = "Connection Error: " + s;
        }

        private void OnConnectionStatusChanged(MPC sender, AsynchronousTCPClient.ConnectionStatus status)
        {
            
            if (status == AsynchronousTCPClient.ConnectionStatus.Connected)
            {
                //Handled at elsewhare.
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.Connecting)
            {
                IsConnected = false;
                StatusMessage = " - Connecting... ";
                StatusButton = _pathConnectingButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.AutoReconnecting)
            {
                IsConnected = false;
                StatusMessage = " - Reconnecting... ";
                StatusButton = _pathConnectingButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.ConnectFail_Timeout)
            {
                IsConnected = false;
                StatusMessage = " - Connection terminated. (Fail_Timeout) ";
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.DisconnectedByHost)
            {
                IsConnected = false;
                StatusMessage = " - Connection terminated. (DisconnectedByHost) ";
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.DisconnectedByUser)
            {
                IsConnected = false;
                StatusMessage = " - Connection terminated. (DisconnectedByUser) ";
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.Error)
            {
                //Handled at OnConnectionError
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.SendFail_NotConnected)
            {
                //IsConnected = false;
                StatusMessage = " - Connection terminated. (SendFail_NotConnected) ";
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.SendFail_Timeout)
            {
                //IsConnected = false;
                StatusMessage = " - Connection terminated. (SendFail_Timeout) ";
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.NeverConnected)
            {
                IsConnected = false;
                StatusMessage = " - Disconnected. (NeverConnected) ";
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.MpdOK)
            {
                //
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.MpdAck)
            {
                //
            }
        }

        #endregion

        #region == コマンド ==

        public ICommand PlayCommand { get; }
        public bool PlayCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void PlayCommand_ExecuteAsync()
        {
            switch (_MPC.MpdStatus.MpdState)
            {
                case MPC.Status.MpdPlayState.Play:
                    {
                        //State>>Play: So, send Pause command
                        _MPC.MpdPlaybackPause();
                        break;
                    }
                case MPC.Status.MpdPlayState.Pause:
                    {
                        //State>>Pause: So, send Resume command
                        _MPC.MpdPlaybackResume();
                        break;
                    }
                case MPC.Status.MpdPlayState.Stop:
                    {
                        //State>>Stop: So, send Play command
                        _MPC.MpdPlaybackPlay();
                        break;
                    }
            }
        }

        public ICommand PlayNextCommand { get; }
        public bool PlayNextCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            //if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }
        public void PlayNextCommand_ExecuteAsync()
        {
            _MPC.MpdPlaybackNext();
        }

        public ICommand PlayPrevCommand { get; }
        public bool PlayPrevCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            //if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }
        public void PlayPrevCommand_ExecuteAsync()
        {
            _MPC.MpdPlaybackPrev();
        }

        public ICommand SetRandomCommand { get; }
        public bool SetRandomCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void SetRandomCommand_ExecuteAsync()
        {
            _MPC.MpdSetRandom(_random);
        }

        public ICommand SetRpeatCommand { get; }
        public bool SetRpeatCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void SetRpeatCommand_ExecuteAsync()
        {
            _MPC.MpdSetRepeat(_repeat);
        }

        public ICommand SetVolumeCommand { get; }
        public bool SetVolumeCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void SetVolumeCommand_ExecuteAsync()
        {
            _MPC.MpdSetVolume(Convert.ToInt32(_volume));
        }

        public ICommand SetSeekCommand { get; }
        public bool SetSeekCommand_CanExecute()
        {
            //TODO: if (this.IsBusy) { return false; }  //!
            if (_MPC == null) { return false; }
            return true;
        }
        public void SetSeekCommand_ExecuteAsync()
        {
            // TODO:
            _MPC.MpdPlaybackSeek(_MPC.MpdStatus.MpdSongID, Convert.ToInt32(_elapsed));
        }

        public ICommand ListAllCommand { get; }
        public bool ListAllCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void ListAllCommand_ExecuteAsync()
        {
            _MPC.MpdListAll();
        }

        public ICommand ChangeSongCommand { get; set; }
        public bool ChangeSongCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }
        public void ChangeSongCommand_ExecuteAsync()
        {
            _MPC.MpdPlaybackPlay(_selectedSong.ID);
        }

        public ICommand ChangePlaylistCommand { get; set; }
        public bool ChangePlaylistCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            if (this._selecctedPlaylist == "") { return false; }
            return true;
        }
        public void ChangePlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            _MPC.MpdChangePlaylist(_selecctedPlaylist);
         }

        public ICommand SongListViewEnterKeyCommand { get; set; }
        public bool SongListViewEnterKeyCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }
        public void SongListViewEnterKeyCommand_ExecuteAsync()
        {
            //
            _MPC.MpdPlaybackPlay(_selectedSong.ID);
        }

        public ICommand SongListViewLeftDoubleClickCommand { get; set; }
        public bool SongListViewLeftDoubleClickCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }
        public void SongListViewLeftDoubleClickCommand_ExecuteAsync(MPC.Song song)
        {
            //
            _MPC.MpdPlaybackPlay(song.ID);
        }
        
        public ICommand PlaylistListviewLeftDoubleClickCommand { get; set; }
        public bool PlaylistListviewLeftDoubleClickCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(String playlist)
        {
            _MPC.MpdChangePlaylist(playlist);
        }

        public ICommand PlaylistListviewEnterKeyCommand { get; set; }
        public bool PlaylistListviewEnterKeyCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void PlaylistListviewEnterKeyCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            _MPC.MpdChangePlaylist(_selecctedPlaylist);
        }


        #endregion

    }
}
