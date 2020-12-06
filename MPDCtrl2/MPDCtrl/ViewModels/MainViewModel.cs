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
using System.Windows.Threading;
using System.Net;
using MPDCtrl.Common;
using MPDCtrl.Views;

namespace MPDCtrl.ViewModels
{
    /// TODO: 
    /// 
    /// v2.0.0
    /// スライダー等のデザイン。
    /// 
    /// v2.0.1
    /// queueの項目を表示・非表示を項目毎に選択・保存できるように。
    /// queueのplaylistとしての保存。
    /// playListの削除、作成・保存。
    /// queueの曲順並べ替え
    /// 
    /// v2.0.2
    /// アルバムカバー対応。
    /// Local Files の TreeView化
    /// 
    /// [enhancement]
    /// スライダーの上でスクロールして音量変更。
    /// 


    /// 更新履歴：
    /// 
    /// v2.0.0.6: 設定画面とりあえず完成。i19nとりあえず完了。
    /// v2.0.0.5: DebugWindowがオンの時だけテキストを追加するようにした（consumeで激重になる）。
    /// v2.0.0.4: Consumeオプションを追加。
    /// v2.0.0.3: Current Queueの項目を増やしたり、IsPlayingとか。
    /// v2.0.0.2: DebugWindowの追加とかProfile関係とか色々。
    /// 


    public class Profile : ViewModelBase
    {
        private string _host;
        public string Host
        {
            get { return _host; }
            set
            {
                if (_host == value)
                    return;

                _host = value;
                NotifyPropertyChanged("Host");
            }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port == value)
                    return;

                _port = value;
                NotifyPropertyChanged("Port");
            }
        }

        private string _password;
        public string Password 
        {
            get { return _password; }
            set
            {
                if (_password == value)
                    return;

                _password = value;
                NotifyPropertyChanged("Password");
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                    return;

                _name = value;
                NotifyPropertyChanged("Name");
            }
        }

        private bool _isDefault;
        public bool IsDefault
        {
            get { return _isDefault; }
            set
            {
                if (_isDefault == value)
                    return;

                _isDefault = value;

                NotifyPropertyChanged("IsDefault");
            }
        }

    }

    public class ShowDebugEventArgs : EventArgs
    {
        public bool WindowVisibility = true;
        public double Top = 100;
        public double Left = 100;
        public double Height = 240;
        public double Width = 450;
    }

    public class MainViewModel : ViewModelBase
    {
        #region == 基本 ==  

        // Application name
        const string _appName = "MPDCtrl";

        // Application version
        const string _appVer = "2.0.0.6";

        public string AppVer
        {
            get
            {
                return _appVer;
            }
        }

        // Application Window Title
        public string AppTitle
        {
            get
            {
                return _appName;
            }
        }

        public string AppTitleVer
        {
            get
            {
                return _appName + " " + _appVer;
            }
        }

        // For the application config file folder
        const string _appDeveloper = "torum";

        #endregion

        #region == 設定フォルダ関連 ==  

        private string _envDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private string _appDataFolder;
        private string _appConfigFilePath;

        private bool _isFullyLoaded;
        public bool IsFullyLoaded
        {
            get
            {
                return _isFullyLoaded;
            }
            set
            {
                if (_isFullyLoaded == value)
                    return;

                _isFullyLoaded = value;
                this.NotifyPropertyChanged("IsFullyLoaded");
            }
        }

        #endregion

        #region == MPC ==  

        private MPC _MPC;


        #endregion

        #region == 画面表示周り ==  

        private bool _isConnected;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                if (_isConnected == value)
                    return;

                _isConnected = value;
                NotifyPropertyChanged("IsConnected");

                if (_isConnected)
                {
                    IsMainShow = true;
                    IsConnectionSettingShow = false;
                    IsConnecting = false;
                    IsNotConnectingNorConnected = false;
                }
                else
                {
                    IsMainShow = false;
                    IsConnectionSettingShow = true;
                    if (!IsConnecting)
                    {
                        IsNotConnectingNorConnected = true;
                    }
                }
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
                if (_isConnecting == value)
                    return;

                _isConnecting = value;
                NotifyPropertyChanged("IsConnecting");
                NotifyPropertyChanged("IsNotConnecting");
                

                if (_isConnecting)
                {
                    IsNotConnectingNorConnected = false;
                }
                else
                {
                    if (!IsConnected)
                    {
                        IsNotConnectingNorConnected = true;
                    }
                }
            }
        }

        private bool _isNotConnectingNorConnected = true;
        public bool IsNotConnectingNorConnected
        {
            get
            {
                return _isNotConnectingNorConnected;
            }
            set
            {
                if (_isNotConnectingNorConnected == value)
                    return;

                _isNotConnectingNorConnected = value;
                NotifyPropertyChanged("IsNotConnectingNorConnected");
            }
        }

        public bool IsNotConnecting
        {
            get
            {
                return !_isConnecting;
            }
        }

        private bool _isMainShow;
        public bool IsMainShow
        {
            get { return _isMainShow; }
            set
            {
                if (_isMainShow == value)
                    return;

                _isMainShow = value;
                NotifyPropertyChanged("IsMainShow");
            }
        }

        private bool _isSettingsShow;
        public bool IsSettingsShow
        {
            get { return _isSettingsShow; }
            set
            {
                if (_isSettingsShow == value)
                    return;

                _isSettingsShow = value;

                if (_isSettingsShow)
                {
                    if (CurrentProfile == null)
                    {
                        IsConnectionSettingShow = false;
                    }
                    else
                    {
                        IsConnectionSettingShow = false;
                    }
                }
                else
                {
                    if (CurrentProfile == null)
                    {
                        IsConnectionSettingShow = true;
                    }
                    else
                    {
                        if (!IsConnected)
                        {
                            IsConnectionSettingShow = true;
                        }
                    }
                }

                NotifyPropertyChanged("IsSettingsShow");

            }
        }

        private bool _isConnectionSettingShow;
        public bool IsConnectionSettingShow
        {
            get { return _isConnectionSettingShow; }
            set
            {
                if (_isConnectionSettingShow == value)
                    return;

                _isConnectionSettingShow = value;
                NotifyPropertyChanged("IsConnectionSettingShow");
            }
        }

        #endregion

        #region == コントロール関連 ==  

        private string _statusMessage;
        public string StatusMessage
        {
            get
            {
                return _statusMessage;
            }
            set
            {
                _statusMessage = value;
                NotifyPropertyChanged("StatusMessage");
            }
        }

        private MPC.Song _currentSong;
        public MPC.Song CurrentSong
        {
            get
            {
                return _currentSong;
            }
            set
            {
                if (_currentSong == value)
                    return;

                _currentSong = value;
                NotifyPropertyChanged("CurrentSong");
                NotifyPropertyChanged("CurrentSongTitle");
                NotifyPropertyChanged("CurrentSongArtist");
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
                    return _currentSong.Artist;
                }
                else
                {
                    return string.Empty;
                }
            }
        }

        public ObservableCollection<MPC.Song> Queue
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
                if (_selectedSong == value)
                    return;

                _selectedSong = value;
                NotifyPropertyChanged("SelectedSong");
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
                    NotifyPropertyChanged("SelectedPlaylist");
                }
            }
        }

        public ObservableCollection<string> Localfiles
        {
            get
            {
                if (_MPC != null)
                {
                    return _MPC.LocalFiles;
                }
                else
                {
                    return null;
                }
            }
        }

        private string _selecctedLocalfile;
        public string SelectedLocalfile
        {
            get
            {
                return _selecctedLocalfile;
            }
            set
            {
                if (_selecctedLocalfile != value)
                {
                    _selecctedLocalfile = value;
                    NotifyPropertyChanged("SelectedLocalfile");
                }
            }
        }

        private static string _pathConnectingButton = "M11 14H9C9 9.03 13.03 5 18 5V7C14.13 7 11 10.13 11 14M18 11V9C15.24 9 13 11.24 13 14H15C15 12.34 16.34 11 18 11M7 4C7 2.89 6.11 2 5 2S3 2.89 3 4 3.89 6 5 6 7 5.11 7 4M11.45 4.5H9.45C9.21 5.92 8 7 6.5 7H3.5C2.67 7 2 7.67 2 8.5V11H8V8.74C9.86 8.15 11.25 6.5 11.45 4.5M19 17C20.11 17 21 16.11 21 15S20.11 13 19 13 17 13.89 17 15 17.89 17 19 17M20.5 18H17.5C16 18 14.79 16.92 14.55 15.5H12.55C12.75 17.5 14.14 19.15 16 19.74V22H22V19.5C22 18.67 21.33 18 20.5 18Z";
        //private static string _pathConnectedButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";
        private static string _pathConnectedButton = "";
        private static string _pathDisconnectedButton = "";
        //private static string _pathErrorInfoButton = "M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z";
        private static string _pathErrorInfoButton = "M23,12L20.56,14.78L20.9,18.46L17.29,19.28L15.4,22.46L12,21L8.6,22.47L6.71,19.29L3.1,18.47L3.44,14.78L1,12L3.44,9.21L3.1,5.53L6.71,4.72L8.6,1.54L12,3L15.4,1.54L17.29,4.72L20.9,5.54L20.56,9.22L23,12M20.33,12L18.5,9.89L18.74,7.1L16,6.5L14.58,4.07L12,5.18L9.42,4.07L8,6.5L5.26,7.09L5.5,9.88L3.67,12L5.5,14.1L5.26,16.9L8,17.5L9.42,19.93L12,18.81L14.58,19.92L16,17.5L18.74,16.89L18.5,14.1L20.33,12M11,15H13V17H11V15M11,7H13V13H11V7";

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
                NotifyPropertyChanged("StatusButton");
            }
        }

        private static string _pathPlayButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathPauseButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        //private static string _pathStopButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private string _playButton = _pathPlayButton;
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
                NotifyPropertyChanged("PlayButton");
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
                    _volume = value;
                    NotifyPropertyChanged("Volume");

                    if (_MPC != null)
                    {
                        // If we have a timer and we are in this event handler, a user is still interact with the slider
                        // we stop the timer
                        if (_volumeDelayTimer != null)
                            _volumeDelayTimer.Stop();

                        //System.Diagnostics.Debug.WriteLine("Volume value is still changing. Skipping.");

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
                if (Convert.ToDouble(_MPC.MpdStatus.MpdVolume) != _volume)
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
                _repeat = value;
                NotifyPropertyChanged("Repeat");

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
                _random = value;
                NotifyPropertyChanged("Random");

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

        private bool _consume;
        public bool Consume
        {
            get { return _consume; }
            set
            {
                _consume = value;
                NotifyPropertyChanged("Consume");

                if (_MPC != null)
                {
                    if (_MPC.MpdStatus.MpdConsume != value)
                    {
                        if (SetConsumeCommand.CanExecute(null))
                        {
                            SetConsumeCommand.Execute(null);
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
                return _time;
            }
            set
            {
                _time = value;
                NotifyPropertyChanged("Time");
            }
        }

        private double _elapsed;
        public double Elapsed
        {
            get
            {
                return _elapsed;
            }
            set
            {
                if ((value < _time) && _elapsed != value)
                {
                    _elapsed = value;
                    NotifyPropertyChanged("Elapsed");
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

                    //System.Diagnostics.Debug.WriteLine("Elapsed value is still changing. Skipping.");

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
                if ((_elapsed < _time))
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

        #endregion

        #region == 設定画面 ==

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

                SelectedProfile = _currentProfile;

                NotifyPropertyChanged("CurrentProfile");
            }
        }

        private Profile _selectedProfile;
        public Profile SelectedProfile
        {
            get
            {
                return _selectedProfile;
            }
            set
            {
                if (_selectedProfile == value)
                    return;

                _selectedProfile = value;

                if (_selectedProfile != null)
                {
                    ClearErrror("Host");
                    ClearErrror("Port");
                    Host = SelectedProfile.Host;
                    Port = SelectedProfile.Port.ToString();
                    _password = SelectedProfile.Password;
                    NotifyPropertyChanged("Password");
                    SetIsDefault = SelectedProfile.IsDefault;
                }
                else
                {
                    ClearErrror("Host");
                    ClearErrror("Port");
                    Host = "";
                    Port = "6600";
                    _password = "";
                    NotifyPropertyChanged("Password");
                }

                NotifyPropertyChanged("SelectedProfile");

                // Clear message. > Don't.
                //SettingProfileEditMessage = "";
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
                    SetError("Host", MPDCtrl.Properties.Resources.Settings_ErrorHostMustBeSpecified); 
                    
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
                        SetError("Host", MPDCtrl.Properties.Resources.Settings_ErrorHostInvalidAddressFormat);
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
                    SetError("Port", MPDCtrl.Properties.Resources.Settings_ErrorPortMustBeSpecified); 
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
                        SetError("Port", MPDCtrl.Properties.Resources.Settings_ErrorInvalidPortNaN);
                        _port = 0;
                    }
                }

                NotifyPropertyChanged("Port");
            }
        }

        private string _password = "";
        public string Password
        {
            // Dummy "*"'s for PasswordBox.
            get
            {
                return DummyPassword(_password);
            }
        }

        private string Encrypt(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }

            byte[] entropy = new byte[] { 0x72, 0xa2, 0x12, 0x04 };

            try
            {
                byte[] userData = System.Text.Encoding.UTF8.GetBytes(s);

                byte[] encryptedData = ProtectedData.Protect(userData, entropy, DataProtectionScope.CurrentUser);

                return System.Convert.ToBase64String(encryptedData);
            }
            catch
            {
                return "";
            }
        }

        private string Decrypt(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }

            byte[] entropy = new byte[] { 0x72, 0xa2, 0x12, 0x04 };

            try
            {
                byte[] encryptedData = System.Convert.FromBase64String(s);

                byte[] userData = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);

                return System.Text.Encoding.UTF8.GetString(userData);
            }
            catch
            {
                return "";
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
        
        private bool _isUpdateOnStartup = true;
        public bool IsUpdateOnStartup
        {
            get { return _isUpdateOnStartup; }
            set
            {
                if (_isUpdateOnStartup == value)
                    return;

                _isUpdateOnStartup = value;

                NotifyPropertyChanged("IsUpdateOnStartup");
            }
        }

        private bool _isShowDebugWindow;
        public bool IsShowDebugWindow
        {
            get { return _isShowDebugWindow; }
            set
            {
                if (_isShowDebugWindow == value)
                    return;

                _isShowDebugWindow = value;

                ShowDebug.WindowVisibility = _isShowDebugWindow;
                ShowDebugView?.Invoke(this, ShowDebug);

                NotifyPropertyChanged("IsShowDebugWindow");
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

        public delegate void DebugWindowOutput(String data);
        public event DebugWindowOutput OnDebugWindowOutput;

        public event EventHandler<ShowDebugEventArgs> ShowDebugView;
        public ShowDebugEventArgs ShowDebug = new ShowDebugEventArgs();

        #endregion

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

            #region == コマンド初期化 ==

            ListAllCommand = new RelayCommand(ListAllCommand_ExecuteAsync, ListAllCommand_CanExecute);
            PlayCommand = new RelayCommand(PlayCommand_ExecuteAsync, PlayCommand_CanExecute);
            PlayNextCommand = new RelayCommand(PlayNextCommand_ExecuteAsync, PlayNextCommand_CanExecute);
            PlayPrevCommand = new RelayCommand(PlayPrevCommand_ExecuteAsync, PlayPrevCommand_CanExecute);
            SetRpeatCommand = new RelayCommand(SetRpeatCommand_ExecuteAsync, SetRpeatCommand_CanExecute);
            SetRandomCommand = new RelayCommand(SetRandomCommand_ExecuteAsync, SetRandomCommand_CanExecute);
            SetConsumeCommand = new RelayCommand(SetConsumeCommand_ExecuteAsync, SetConsumeCommand_CanExecute);
            SetVolumeCommand = new RelayCommand(SetVolumeCommand_ExecuteAsync, SetVolumeCommand_CanExecute);
            SetSeekCommand = new RelayCommand(SetSeekCommand_ExecuteAsync, SetSeekCommand_CanExecute);
            ChangeSongCommand = new RelayCommand(ChangeSongCommand_ExecuteAsync, ChangeSongCommand_CanExecute);
            ChangePlaylistCommand = new RelayCommand(ChangePlaylistCommand_ExecuteAsync, ChangePlaylistCommand_CanExecute);
            PlaylistListviewEnterKeyCommand = new RelayCommand(PlaylistListviewEnterKeyCommand_ExecuteAsync, PlaylistListviewEnterKeyCommand_CanExecute);
            PlaylistListviewLoadPlaylistCommand = new RelayCommand(PlaylistListviewLoadPlaylistCommand_ExecuteAsync, PlaylistListviewLoadPlaylistCommand_CanExecute);
            PlaylistListviewClearLoadPlaylistCommand = new RelayCommand(PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync, PlaylistListviewClearLoadPlaylistCommand_CanExecute);
            PlaylistListviewLeftDoubleClickCommand = new GenericRelayCommand<String>(param => PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(param), param => PlaylistListviewLeftDoubleClickCommand_CanExecute());
            LocalfileListviewAddCommand = new GenericRelayCommand<object>(param => LocalfileListviewAddCommand_Execute(param), param => LocalfileListviewAddCommand_CanExecute());
            SongListViewEnterKeyCommand = new RelayCommand(SongListViewEnterKeyCommand_ExecuteAsync, SongListViewEnterKeyCommand_CanExecute);
            SongListViewLeftDoubleClickCommand = new GenericRelayCommand<MPC.Song>(param => SongListViewLeftDoubleClickCommand_ExecuteAsync(param), param => SongListViewLeftDoubleClickCommand_CanExecute());
            SongListviewClearCommand = new RelayCommand(SongListviewClearCommand_ExecuteAsync, SongListviewClearCommand_CanExecute);
            SongListviewDeleteCommand = new GenericRelayCommand<object>(param => SongListviewDeleteCommand_Execute(param), param => SongListviewDeleteCommand_CanExecute());

            VolumeMuteCommand = new RelayCommand(VolumeMuteCommand_Execute, VolumeMuteCommand_CanExecute);
            PlayStopCommand = new RelayCommand(PlayStopCommand_Execute, PlayStopCommand_CanExecute);
            PlayPauseCommand = new RelayCommand(PlayPauseCommand_Execute, PlayPauseCommand_CanExecute);
            VolumeUpCommand = new RelayCommand(VolumeUpCommand_Execute, VolumeUpCommand_CanExecute);
            VolumeDownCommand = new RelayCommand(VolumeDownCommand_Execute, VolumeDownCommand_CanExecute);

            ShowSettingsCommand = new RelayCommand(ShowSettingsCommand_Execute, ShowSettingsCommand_CanExecute);
            SettingsOKCommand = new RelayCommand(SettingsOKCommand_Execute, SettingsOKCommand_CanExecute);

            NewProfileCommand = new RelayCommand(NewProfileCommand_Execute, NewProfileCommand_CanExecute);
            DeleteProfileCommand = new RelayCommand(DeleteProfileCommand_Execute, DeleteProfileCommand_CanExecute);
            SaveProfileCommand = new GenericRelayCommand<object>(param => SaveProfileCommand_Execute(param), param => SaveProfileCommand_CanExecute());
            UpdateProfileCommand = new RelayCommand(UpdateProfileCommand_Execute, UpdateProfileCommand_CanExecute);
            ConnectCommand = new GenericRelayCommand<object>(param => ConnectCommand_Execute(param), param => ConnectCommand_CanExecute());
            ChangeConnectionProfileCommand = new GenericRelayCommand<object> (param => ChangeConnectionProfileCommand_Execute(param), param => ChangeConnectionProfileCommand_CanExecute());

            #endregion

            #region == MPC ==  

            _MPC = new MPC(_host, _port, _password);

            #endregion

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

            #region == タイマー ==  

            // Init Song's time elapsed timer.
            _elapsedTimer = new DispatcherTimer();
            _elapsedTimer.Interval = TimeSpan.FromMilliseconds(1000);
            _elapsedTimer.Tick += new EventHandler(ElapsedTimer);

            #endregion

            #region == DebugWindow ==  

            // Window hack for the DebugWindow.
            App app = App.Current as App;
            if (app != null) 
            {
                DebugViewModel dvm = new DebugViewModel();

                OnDebugWindowOutput += new DebugWindowOutput(dvm.OnDebugOutput);

                ShowDebugView += (sender, arg) => { app.ShowDebugWindow(arg); };

                app.CreateDebugWindow(dvm);
            }

            #endregion


#if DEBUG
            IsMainShow = true; // To show Main pain in the XAML designer.
            //IsSettingsShow = true;
#else
            IsMainShow = false;
#endif
        }

        #region == システムイベント ==

        // 起動時の処理
        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {

#if DEBUG
            IsMainShow = false;
#endif

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

                    var debugWindow = xdoc.Root.Element("DebugWindow");
                    if (debugWindow != null)
                    {
                        var hoge = debugWindow.Attribute("top");
                        if (hoge != null)
                        {
                            ShowDebug.Top = double.Parse(hoge.Value);
                        }

                        hoge = debugWindow.Attribute("left");
                        if (hoge != null)
                        {
                            ShowDebug.Left = double.Parse(hoge.Value);
                        }

                        hoge = debugWindow.Attribute("height");
                        if (hoge != null)
                        {
                            ShowDebug.Height = double.Parse(hoge.Value);
                        }

                        hoge = debugWindow.Attribute("width");
                        if (hoge != null)
                        {
                            ShowDebug.Width = double.Parse(hoge.Value);
                        }

                    }

                    #endregion

                    #region == オプション設定 ==

                    var opts = xdoc.Root.Element("Options");
                    if (opts != null)
                    {
                        var hoge = opts.Attribute("UpdateOnStartup");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsUpdateOnStartup = true;
                            }
                            else
                            {
                                IsUpdateOnStartup = false;
                            }
                        }

                        hoge = opts.Attribute("ShowDebugWindow");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsShowDebugWindow = true;

                            }
                            else
                            {
                                IsShowDebugWindow = false;
                            }
                        }
                    }

                    #endregion

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
                                    pro.Password = Decrypt(p.Attribute("Password").Value);
                            }
                            if (p.Attribute("IsDefault") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("IsDefault").Value))
                                {
                                    if (p.Attribute("IsDefault").Value == "True")
                                    {
                                        pro.IsDefault = true;

                                        CurrentProfile = pro;
                                        SelectedProfile = pro;
                                    }
                                }
                            }

                            Profiles.Add(pro);
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

            IsFullyLoaded = true;

            if (CurrentProfile == null)
            {
                StatusMessage = "Welcome";

                IsConnectionSettingShow = true;
            }
            else
            {
                IsConnectionSettingShow = false;

                //
                _MPC.MpdHost = CurrentProfile.Host;
                _MPC.MpdPort = CurrentProfile.Port;
                _MPC.MpdPassword = CurrentProfile.Password;

                Start();
            }
        }

        // 終了時の処理
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            if (!IsFullyLoaded)
                return;

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

            // MainWindow
            if (sender is Window)
            {
                // Main Window element
                XmlElement mainWindow = doc.CreateElement(string.Empty, "MainWindow", string.Empty);

                Window w = (sender as Window);
                // Main Window attributes
                attrs = doc.CreateAttribute("height");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Height.ToString();
                }
                else
                {
                    attrs.Value = w.Height.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("width");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Width.ToString();
                }
                else
                {
                    attrs.Value = w.Width.ToString();

                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("top");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Top.ToString();
                }
                else
                {
                    attrs.Value = w.Top.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("left");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Left.ToString();
                }
                else
                {
                    attrs.Value = w.Left.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("state");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = "Maximized";
                }
                else if (w.WindowState == WindowState.Normal)
                {
                    attrs.Value = "Normal";

                }
                else if (w.WindowState == WindowState.Minimized)
                {
                    attrs.Value = "Minimized";
                }
                mainWindow.SetAttributeNode(attrs);

                // set MainWindow element to root.
                root.AppendChild(mainWindow);

            }

            // DebugWindow
            App app = App.Current as App;
            if (app != null)
            {
                foreach (var w in app.Windows)
                {
                    if (w is DebugWindow)
                    {
                        DebugWindow dw = (w as DebugWindow);

                        if ((dw.WindowState == WindowState.Normal || dw.WindowState == WindowState.Maximized))
                        {
                            // Main Window element
                            XmlElement debugWindow = doc.CreateElement(string.Empty, "DebugWindow", string.Empty);

                            // Main Window attributes
                            attrs = doc.CreateAttribute("height");
                            if (dw.WindowState == WindowState.Maximized)
                            {
                                attrs.Value = dw.RestoreBounds.Height.ToString();
                            }
                            else
                            {
                                attrs.Value = dw.Height.ToString();
                            }
                            debugWindow.SetAttributeNode(attrs);

                            attrs = doc.CreateAttribute("width");
                            if (dw.WindowState == WindowState.Maximized)
                            {
                                attrs.Value = dw.RestoreBounds.Width.ToString();
                            }
                            else
                            {
                                attrs.Value = dw.Width.ToString();

                            }
                            debugWindow.SetAttributeNode(attrs);

                            attrs = doc.CreateAttribute("top");
                            if (dw.WindowState == WindowState.Maximized)
                            {
                                attrs.Value = dw.RestoreBounds.Top.ToString();
                            }
                            else
                            {
                                attrs.Value = dw.Top.ToString();
                            }
                            debugWindow.SetAttributeNode(attrs);

                            attrs = doc.CreateAttribute("left");
                            if (dw.WindowState == WindowState.Maximized)
                            {
                                attrs.Value = dw.RestoreBounds.Left.ToString();
                            }
                            else
                            {
                                attrs.Value = dw.Left.ToString();
                            }
                            debugWindow.SetAttributeNode(attrs);

                            // set DebugWindow element to root.
                            root.AppendChild(debugWindow);

                        }

                        /////
                        // Tell it to close, don't hide.
                        dw.SetClose();
                        // Close it.
                        dw.Close();

                        break;
                    }
                }
            }

            #endregion

            #region == オプション設定の保存 ==

            XmlElement opts = doc.CreateElement(string.Empty, "Options", string.Empty);

            // 
            attrs = doc.CreateAttribute("UpdateOnStartup");
            if (IsUpdateOnStartup)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("ShowDebugWindow");
            if (IsShowDebugWindow)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            // 
            root.AppendChild(opts);

            #endregion

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
                xAttrs.Value = Encrypt(p.Password);
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
                if (IsUpdateOnStartup)
                {
                    _MPC.MpdSendUpdate();
                }

                //
                _MPC.MpdQueryCurrentQueue();

                // Call Status "after" MpdQueryCurrentQueue() in order to get "current song" in the queue.
                _MPC.MpdQueryStatus();

                _MPC.MpdQueryPlaylists();

                // heavy stuff should be the last.
                _MPC.MpdQueryListAll();

            }
            else
            {
                IsConnected = false;
                IsMainShow = false;
                IsConnectionSettingShow = true;
            }
        }

        private async Task<ConnectionResult> StartConnection()
        {
            
            StatusButton = _pathConnectingButton;

            StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Connecting;

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
                _volume = Convert.ToDouble(_MPC.MpdStatus.MpdVolume);
                NotifyPropertyChanged("Volume");

                _random = _MPC.MpdStatus.MpdRandom;
                NotifyPropertyChanged("Random");

                _repeat = _MPC.MpdStatus.MpdRepeat;
                NotifyPropertyChanged("Repeat");

                _consume = _MPC.MpdStatus.MpdConsume;
                NotifyPropertyChanged("Consume");

                // no need to care about "double" updates for time.
                Time = _MPC.MpdStatus.MpdSongTime;

                _elapsed = _MPC.MpdStatus.MpdSongElapsed;
                NotifyPropertyChanged("Elapsed");

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

        #endregion

        #region == イベント・タイマー ==

        private DispatcherTimer _elapsedTimer;
        private void ElapsedTimer(object sender, EventArgs e)
        {
            if ((_elapsed < _time) && (_MPC.MpdStatus.MpdState == MPC.Status.MpdPlayState.Play))
            {
                _elapsed += 1;
                NotifyPropertyChanged("Elapsed");
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        private void OnConnected(MPC sender)
        {
            IsConnected = true;

            StatusButton = _pathConnectedButton;
            StatusMessage = "";
        }

        // MPD changed nortifiation
        private void OnStatusChanged(MPC sender, object data)
        {
            //System.Diagnostics.Debug.WriteLine("OnStatusChanged " + data);

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

            UpdateButtonStatus();
        }

        // MPD updated information
        private void OnStatusUpdate(MPC sender, object data)
        {
            //System.Diagnostics.Debug.WriteLine("OnStatusUpdate " + data);

            if ((data as string) == "isPlayer")
            {

                // Clear IsPlaying icon
                if (CurrentSong != null)
                {
                    if (CurrentSong.Id != _MPC.MpdStatus.MpdSongID)
                    {
                        CurrentSong.IsPlaying = false;
                    }
                }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // Sets Current Song
                    var item = _MPC.CurrentQueue.FirstOrDefault(i => i.Id == _MPC.MpdStatus.MpdSongID);
                    if (item != null)
                    {
                        //SelectedSong = (item as MPC.Song);
                        CurrentSong = (item as MPC.Song);

                        (item as MPC.Song).IsPlaying = true;
                    }
                    else
                    {
                        //SelectedSong = null;
                        CurrentSong = null;
                    }
                });

            }
            else if ((data as string) == "isCurrentQueue")
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    var item = _MPC.CurrentQueue.FirstOrDefault(i => i.Id == _MPC.MpdStatus.MpdSongID);
                    if (item != null)
                    {
                        //sender.MpdCurrentSong = (item as MPC.Song); // just in case.

                        //SelectedSong = (item as MPC.Song);
                        CurrentSong = (item as MPC.Song);

                        (item as MPC.Song).IsPlaying = true;
                    }
                    else
                    {
                        //SelectedSong = null;
                        CurrentSong = null;
                    }
                });

            }
            else if ((data as string) == "isStoredPlaylist")
            {
                // TODO: 
            }
            else if ((data as string) == "isLocalFiles")
            {
                // TODO: 
            }
            else if ((data as string) == "isUpdating_db")
            {
                // TODO:
            }

            UpdateButtonStatus();

        }

        private void OnDataReceived(MPC sender, object data)
        {
            if (IsShowDebugWindow)
                OnDebugWindowOutput?.Invoke((data as string));
        }

        private void OnDataSent(MPC sender, object data)
        {
            if (IsShowDebugWindow)
                OnDebugWindowOutput?.Invoke((data as string));
        }

        private void OnError(MPC sender, MPC.MpdErrorTypes errType, object data)
        {
            if (data == null) { return; }

            if (errType == MPC.MpdErrorTypes.ProtocolError)
            {
                string s = (data as string);
                string patternStr = @"[{\[].+?[}\]]";//@"[.*?]";
                s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
                s = s.Replace("ACK ", string.Empty);
                s = s.Replace("{} ", string.Empty);
                s = s.Replace("[] ", string.Empty);

                StatusMessage = MPDCtrl.Properties.Resources.MPD_ProtocolError + ": " + s; 
                 StatusButton = _pathErrorInfoButton;
            }
            else if (errType == MPC.MpdErrorTypes.StatusError)
            {
                StatusMessage = MPDCtrl.Properties.Resources.MPD_StatusError + ": " + (data as string);
                StatusButton = _pathErrorInfoButton;
            }
            else if (errType == MPC.MpdErrorTypes.ErrorClear)
            {
                StatusMessage = "";
                StatusButton = _pathConnectedButton;
            }
            else
            {
                StatusMessage = (data as string);
                StatusButton = _pathErrorInfoButton;
            }
        }

        private void OnConnectionError(MPC sender, object data)
        {
            if (data == null) { return; }

            IsConnected = false;
            IsConnectionSettingShow = true;

            StatusButton = _pathErrorInfoButton;

            StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + (data as string);
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
                IsConnecting = true;
                //IsConnectionSettingShow = true;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Connecting;
                StatusButton = _pathConnectingButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.AutoReconnecting)
            {
                IsConnected = false;
                IsConnecting = true;
                //IsConnectionSettingShow = false;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Reconnecting;
                StatusButton = _pathConnectingButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.ConnectFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Reconnecting;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.DisconnectedByHost)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByHost;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.DisconnectedByUser)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByUser;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.Error)
            {
                //Handled at OnConnectionError

                IsConnecting = false;

            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.SendFail_NotConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.SendFail_Timeout)
            {
                //IsConnected = false;
                IsConnecting = false;
                //IsConnectionSettingShow = true;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_Timeout;
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == AsynchronousTCPClient.ConnectionStatus.NeverConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsConnectionSettingShow = true;

                StatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
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

        public ICommand SetConsumeCommand { get; }
        public bool SetConsumeCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void SetConsumeCommand_ExecuteAsync()
        {
            _MPC.MpdSetConsume(_consume);
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
            if (_MPC == null) { return false; }
            return true;
        }
        public void SetSeekCommand_ExecuteAsync()
        {
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
            _MPC.MpdQueryListAll();
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
            _MPC.MpdPlaybackPlay(_selectedSong.Id);
        }

        public ICommand ChangePlaylistCommand { get; set; }
        public bool ChangePlaylistCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            if (_selecctedPlaylist == "") { return false; }
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
            _MPC.MpdPlaybackPlay(_selectedSong.Id);
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
            _MPC.MpdPlaybackPlay(song.Id);
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

        public ICommand PlaylistListviewLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewLoadPlaylistCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void PlaylistListviewLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            _MPC.MpdLoadPlaylist(_selecctedPlaylist);
        }

        public ICommand PlaylistListviewClearLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewClearLoadPlaylistCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == "")
                return;

            _MPC.MpdChangePlaylist(_selecctedPlaylist);
        }

        public ICommand LocalfileListviewAddCommand { get; }
        public bool LocalfileListviewAddCommand_CanExecute()
        {
            return true;
        }
        public void LocalfileListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<String>();

                List<string> uriList = new List<string>();

                foreach (var item in collection)
                {
                    uriList.Add(item);
                }

                _MPC.MpdAdd(uriList);
            }
            else
            {
                _MPC.MpdAdd(items[0] as string);
            }
        }

        public ICommand SongListviewClearCommand { get; }
        public bool SongListviewClearCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void SongListviewClearCommand_ExecuteAsync()
        {
            // Clear queue here because "playlistinfo" returns nothing.
            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            _MPC.MpdClear();
        }

        public ICommand SongListviewDeleteCommand { get; }
        public bool SongListviewDeleteCommand_CanExecute()
        {
            return true;
        }
        public void SongListviewDeleteCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<MPC.Song> selectedList = new List<MPC.Song>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<MPC.Song>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as MPC.Song);
                }
            });

            List<string> deleteIdList = new List<string>();

            foreach (var item in selectedList)
            {
                deleteIdList.Add(item.Id);
            }

            _MPC.MpdDeleteId(deleteIdList);

        }

        public ICommand VolumeMuteCommand { get; }
        public bool VolumeMuteCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void VolumeMuteCommand_Execute()
        {
            _MPC.MpdSetVolume(0);
        }

        public ICommand PlayPauseCommand { get; }
        public bool PlayPauseCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void PlayPauseCommand_Execute()
        {
            _MPC.MpdPlaybackPause();
        }

        public ICommand PlayStopCommand { get; } 
        public bool PlayStopCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void PlayStopCommand_Execute()
        {
            _MPC.MpdPlaybackStop();
        }

        public ICommand VolumeDownCommand { get;}
        public bool VolumeDownCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void VolumeDownCommand_Execute()
        {
            if (_volume >= 10)
            {
                _MPC.MpdSetVolume(Convert.ToInt32(_volume - 10));
            }
        }

        public ICommand VolumeUpCommand { get;}
        public bool VolumeUpCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }
        public void VolumeUpCommand_Execute()
        {
            if (_volume <= 90)
            {
                _MPC.MpdSetVolume(Convert.ToInt32(_volume + 10));
            }
        }

        public ICommand ShowSettingsCommand { get; }
        public bool ShowSettingsCommand_CanExecute()
        {
            if (IsConnecting) return false;
            return true;
        }
        public void ShowSettingsCommand_Execute()
        {
            if (IsConnecting) return;

            StatusMessage = "";

            if (IsSettingsShow)
            {
                IsSettingsShow = false;
            }
            else
            {
                IsSettingsShow = true;
            }
        }

        public ICommand SettingsOKCommand { get; }
        public bool SettingsOKCommand_CanExecute()
        {
            return true;
        }
        public void SettingsOKCommand_Execute()
        {
            IsSettingsShow = false;
        }
        
        public ICommand NewProfileCommand { get; }
        public bool NewProfileCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            return true;
        }
        public void NewProfileCommand_Execute()
        {
            SelectedProfile = null;
        }

        public ICommand DeleteProfileCommand { get; }
        public bool DeleteProfileCommand_CanExecute()
        {
            if (Profiles.Count < 2) return false;
            if (SelectedProfile == null) return false;
            return true;
        }
        public void DeleteProfileCommand_Execute()
        {
            if (SelectedProfile == null) return;
            if (Profiles.Count < 2) return;

            var tmpNama = SelectedProfile.Name;
            var tmpIsDefault = SelectedProfile.IsDefault;

            if (Profiles.Remove(SelectedProfile))
            {
                SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileDeleted + " ("+ tmpNama+")" ;

                SelectedProfile = Profiles[0];

                if (tmpIsDefault)
                    Profiles[0].IsDefault = tmpIsDefault;
            }
        }

        public ICommand SaveProfileCommand { get; }
        public bool SaveProfileCommand_CanExecute()
        {
            if (SelectedProfile != null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void SaveProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (SelectedProfile != null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (_port == 0) return;

            Profile pro = new Profile();
            pro.Host = _host;
            pro.Port = _port;

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                _password = passwordBox.Password;
            }

            if (SetIsDefault)
            {
                foreach (var p in Profiles)
                {
                    p.IsDefault = false;
                }
                pro.IsDefault = true;
            }
            else
            {
                pro.IsDefault = false;
            }

            pro.Name = Host + ":" + _port.ToString();

            Profiles.Add(pro);

            SelectedProfile = pro;

            SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileSaved;

            if (CurrentProfile == null)
            {
                SetIsDefault = true;
                pro.IsDefault = true;
                CurrentProfile = pro;
            }
                
        }

        public ICommand UpdateProfileCommand { get; }
        public bool UpdateProfileCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            return true;
        }
        public void UpdateProfileCommand_Execute()
        {
            if (SelectedProfile == null) return;

            SelectedProfile.Host = _host;
            SelectedProfile.Port = _port;
            SelectedProfile.Password = _password;

            if (SetIsDefault)
            {
                foreach (var p in Profiles)
                {
                    p.IsDefault = false;
                }
                SelectedProfile.IsDefault = true;
            }
            else
            {
                if (SelectedProfile.IsDefault)
                {
                    SelectedProfile.IsDefault = false;
                    Profiles[0].IsDefault = true;
                }
                else
                {
                    SelectedProfile.IsDefault = false;
                }
            }

            SelectedProfile.Name = Host + ":" + _port.ToString();

            SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileUpdated;
        }

        public ICommand ConnectCommand { get; }
        public bool ConnectCommand_CanExecute()
        {
            return true;
        }
        public async void ConnectCommand_Execute(object obj)
        {
            if (obj == null) return;

            // Validate Host input.
            if (Host == "")
            {
                SetError("Host", "Error: Host must be epecified."); //TODO: translate
                NotifyPropertyChanged("Host");
                return;
            }
            else
            {
                if (Host == "localhost")
                {
                    Host = "127.0.0.1";
                }

                IPAddress ipAddress = null;
                try
                {
                    ipAddress = IPAddress.Parse(Host);
                    if (ipAddress != null)
                    {
                        ClearErrror("Host");
                    }
                }
                catch
                {
                    //System.FormatException
                    SetError("Host", "Error: Invalid address format."); //TODO: translate

                    return;
                }
            }

            if (_port == 0)
            {
                SetError("Port", "Error: Port must be epecified."); //TODO: translate.
                return;
            }

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                _password = passwordBox.Password;
            }


            //
            _MPC.MpdHost = _host;
            _MPC.MpdPort = _port;
            _MPC.MpdPassword = _password;


            //
            ConnectionResult r = await StartConnection();

            if (r.isSuccess)
            {
                IsConnectionSettingShow = false;
                IsSettingsShow = false;


                if (CurrentProfile == null)
                {
                    //
                    Profile prof = new Profile();
                    prof.Name = _host + ":" + _port.ToString();
                    prof.Host = _host;
                    prof.Port = _port;
                    prof.Password = _password;
                    prof.IsDefault = true;

                    CurrentProfile = prof;
                    SelectedProfile = prof;

                    Profiles.Add(prof);

                    // 初回だからUpdateしておく。
                    _MPC.MpdSendUpdate();
                }
                else
                {
                    CurrentProfile.Name = _host + ":" + _port.ToString();
                    CurrentProfile.Host = _host;
                    CurrentProfile.Port = _port;
                    CurrentProfile.Password = _password;
                }

                //
                _MPC.MpdQueryCurrentQueue();
                _MPC.MpdQueryStatus();
                _MPC.MpdQueryPlaylists();
                _MPC.MpdQueryListAll();
                //
            }
        }

        public ICommand ChangeConnectionProfileCommand { get; }
        public bool ChangeConnectionProfileCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            return true;
        }
        public async void ChangeConnectionProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (SelectedProfile == null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (_port == 0) return;
            if (IsConnecting) return;

            if (IsConnected)
            {
                //_MPC.MpdDisconnect();
            }

            SelectedProfile.Host = _host;
            SelectedProfile.Port = _port;

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                SelectedProfile.Password = passwordBox.Password;
            }

            if (SetIsDefault)
            {
                foreach (var p in Profiles)
                {
                    p.IsDefault = false;
                }
                SelectedProfile.IsDefault = true;
            }
            else
            {
                SelectedProfile.IsDefault = false;
            }

            SelectedProfile.Name = Host + ":" + _port.ToString();

            CurrentProfile = SelectedProfile;


            _MPC.MpdHost = CurrentProfile.Host;
            _MPC.MpdPort = CurrentProfile.Port;
            _MPC.MpdPassword = CurrentProfile.Password;

            ConnectionResult r = await StartConnection();

            if (r.isSuccess)
            {
                IsConnectionSettingShow = false;
                IsSettingsShow = false;

                //
                _MPC.MpdQueryCurrentQueue();
                _MPC.MpdQueryStatus();
                _MPC.MpdQueryPlaylists();
                _MPC.MpdQueryListAll();
                //
            }

        }

        #endregion

    }

}
