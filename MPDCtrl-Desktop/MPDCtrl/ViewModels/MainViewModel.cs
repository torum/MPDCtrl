using MPDCtrl.Common;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;
using MPDCtrl.ViewModels.Classes;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml;
using System.Xml.Linq;
using Windows.Networking;

namespace MPDCtrl.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        #region == Basic ==  

        // Application name
        const string _appName = "MPDCtrl";

        // Application version
        const string _appVer = "v3.0.19.0";

        public static string AppVer
        {
            get
            {
                return _appVer;
            }
        }

        // Application Title (for system)
        public static string AppTitle
        {
            get
            {
                return _appName;
            }
        }

        // Application Window Title (for display)
        public static string AppTitleVer
        {
            get
            {
                return _appName + " " + _appVer;
            }
        }

        // For the application config file folder
        const string _appDeveloper = "torum";

        // TODO: no longer used.
        public static bool DeveloperMode
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

        #endregion

        #region == Config file load & save ==  

        private static readonly string _envDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string _appDataFolder;
        private static string _appConfigFilePath;

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
                this.NotifyPropertyChanged(nameof(IsFullyLoaded));
            }
        }

        private bool _isFullyRendered;
        public bool IsFullyRendered
        {
            get
            {
                return _isFullyRendered;
            }
            set
            {
                if (_isFullyRendered == value)
                    return;

                _isFullyRendered = value;
                this.NotifyPropertyChanged(nameof(IsFullyRendered));
            }
        }

        #endregion

        #region == Layout related ==

        // TODO: no longer used...
        private double _mainLeftPainActualWidth = 241;
        public double MainLeftPainActualWidth
        {
            get
            {
                return _mainLeftPainActualWidth;
            }
            set
            {
                if (value == _mainLeftPainActualWidth) return;

                _mainLeftPainActualWidth = value;

                NotifyPropertyChanged(nameof(MainLeftPainActualWidth));
            }
        }

        // TODO: no longer used...
        private double _mainLeftPainWidth = 241;
        public double MainLeftPainWidth
        {
            get
            {
                return _mainLeftPainWidth;
            }
            set
            {
                if (value == _mainLeftPainWidth) return;

                _mainLeftPainWidth = value;

                NotifyPropertyChanged(nameof(MainLeftPainWidth));
            }
        }

        #region == Queue column headers ==

        private bool _queueColumnHeaderPositionVisibility = true;
        public bool QueueColumnHeaderPositionVisibility
        {
            get
            {
                return _queueColumnHeaderPositionVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderPositionVisibility)
                    return;

                _queueColumnHeaderPositionVisibility = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderPositionVisibility));
            }
        }

        private double _queueColumnHeaderPositionWidth = 53;
        public double QueueColumnHeaderPositionWidth
        {
            get
            {
                return _queueColumnHeaderPositionWidth;
            }
            set
            {
                if (value == _queueColumnHeaderPositionWidth)
                    return;

                _queueColumnHeaderPositionWidth = value;

                if (value > 0)
                    QueueColumnHeaderPositionWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderPositionWidth));
            }
        }

        private double _queueColumnHeaderPositionWidthUser = 53;
        public double QueueColumnHeaderPositionWidthRestore
        {
            get
            {
                return _queueColumnHeaderPositionWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderPositionWidthUser)
                    return;

                _queueColumnHeaderPositionWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderPositionWidthRestore));
            }
        }

        private bool _queueColumnHeaderNowPlayingVisibility = true;
        public bool QueueColumnHeaderNowPlayingVisibility
        {
            get
            {
                return _queueColumnHeaderNowPlayingVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingVisibility)
                    return;

                _queueColumnHeaderNowPlayingVisibility = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderNowPlayingVisibility));
            }
        }

        private double _queueColumnHeaderNowPlayingWidth = 32;
        public double QueueColumnHeaderNowPlayingWidth
        {
            get
            {
                return _queueColumnHeaderNowPlayingWidth;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingWidth)
                    return;

                _queueColumnHeaderNowPlayingWidth = value;

                if (value > 0)
                    QueueColumnHeaderNowPlayingWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderNowPlayingWidth));
            }
        }

        private double _queueColumnHeaderNowPlayingWidthUser = 32;
        public double QueueColumnHeaderNowPlayingWidthRestore
        {
            get
            {
                return _queueColumnHeaderNowPlayingWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingWidthUser)
                    return;

                _queueColumnHeaderNowPlayingWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderNowPlayingWidthRestore));
            }
        }

        private double _queueColumnHeaderTitleWidth = 180;
        public double QueueColumnHeaderTitleWidth
        {
            get
            {
                return _queueColumnHeaderTitleWidth;
            }
            set
            {
                if (value == _queueColumnHeaderTitleWidth)
                    return;

                _queueColumnHeaderTitleWidth = value;

                if (value > 0)
                    QueueColumnHeaderTitleWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderTitleWidth));
            }
        }

        private double _queueColumnHeaderTitleWidthUser = 180;
        public double QueueColumnHeaderTitleWidthRestore
        {
            get
            {
                return _queueColumnHeaderTitleWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderTitleWidthUser)
                    return;

                _queueColumnHeaderTitleWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderTitleWidthRestore));
            }
        }

        private bool _queueColumnHeaderTimeVisibility = true;
        public bool QueueColumnHeaderTimeVisibility
        {
            get
            {
                return _queueColumnHeaderTimeVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderTimeVisibility)
                    return;

                _queueColumnHeaderTimeVisibility = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderTimeVisibility));
            }
        }

        private double _queueColumnHeaderTimeWidth = 62;
        public double QueueColumnHeaderTimeWidth
        {
            get
            {
                return _queueColumnHeaderTimeWidth;
            }
            set
            {
                if (value == _queueColumnHeaderTimeWidth)
                    return;

                _queueColumnHeaderTimeWidth = value;

                if (value > 0)
                    QueueColumnHeaderTitleWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderTimeWidth));
            }
        }

        private double _queueColumnHeaderTimeWidthUser = 62;
        public double QueueColumnHeaderTimeWidthRestore
        {
            get
            {
                return _queueColumnHeaderTimeWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderTimeWidthUser)
                    return;

                _queueColumnHeaderTimeWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderTimeWidthRestore));
            }
        }

        private bool _queueColumnHeaderArtistVisibility = true;
        public bool QueueColumnHeaderArtistVisibility
        {
            get
            {
                return _queueColumnHeaderArtistVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderArtistVisibility)
                    return;

                _queueColumnHeaderArtistVisibility = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderArtistVisibility));
            }
        }

        private double _queueColumnHeaderArtistWidth = 120;
        public double QueueColumnHeaderArtistWidth
        {
            get
            {
                return _queueColumnHeaderArtistWidth;
            }
            set
            {
                if (value == _queueColumnHeaderArtistWidth)
                    return;

                _queueColumnHeaderArtistWidth = value;

                if (value > 0)
                    QueueColumnHeaderArtistWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderArtistWidth));
            }
        }

        private double _queueColumnHeaderArtistWidthUser = 120;
        public double QueueColumnHeaderArtistWidthRestore
        {
            get
            {
                return _queueColumnHeaderArtistWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderArtistWidthUser)
                    return;

                _queueColumnHeaderArtistWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderArtistWidthRestore));
            }
        }

        private bool _queueColumnHeaderAlbumVisibility = true;
        public bool QueueColumnHeaderAlbumVisibility
        {
            get
            {
                return _queueColumnHeaderAlbumVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumVisibility)
                    return;

                _queueColumnHeaderAlbumVisibility = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderAlbumVisibility));
            }
        }

        private double _queueColumnHeaderAlbumWidth = 120;
        public double QueueColumnHeaderAlbumWidth
        {
            get
            {
                return _queueColumnHeaderAlbumWidth;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumWidth)
                    return;

                _queueColumnHeaderAlbumWidth = value;

                if (value > 0)
                    QueueColumnHeaderAlbumWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderAlbumWidth));
            }
        }

        private double _queueColumnHeaderAlbumWidthUser = 120;
        public double QueueColumnHeaderAlbumWidthRestore
        {
            get
            {
                return _queueColumnHeaderAlbumWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumWidthUser)
                    return;

                _queueColumnHeaderAlbumWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderAlbumWidthRestore));
            }
        }

        private bool _queueColumnHeaderGenreVisibility = true;
        public bool QueueColumnHeaderGenreVisibility
        {
            get
            {
                return _queueColumnHeaderGenreVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderGenreVisibility)
                    return;

                _queueColumnHeaderGenreVisibility = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderGenreVisibility));
            }
        }

        private double _queueColumnHeaderGenreWidth = 100;
        public double QueueColumnHeaderGenreWidth
        {
            get
            {
                return _queueColumnHeaderGenreWidth;
            }
            set
            {
                if (value == _queueColumnHeaderGenreWidth)
                    return;

                _queueColumnHeaderGenreWidth = value;

                if (value > 0)
                    QueueColumnHeaderGenreWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderGenreWidth));
            }
        }

        private double _queueColumnHeaderGenreWidthUser = 100;
        public double QueueColumnHeaderGenreWidthRestore
        {
            get
            {
                return _queueColumnHeaderGenreWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderGenreWidthUser)
                    return;

                _queueColumnHeaderGenreWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderGenreWidthRestore));
            }
        }

        private bool _queueColumnHeaderLastModifiedVisibility = true;
        public bool QueueColumnHeaderLastModifiedVisibility
        {
            get
            {
                return _queueColumnHeaderLastModifiedVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedVisibility)
                    return;

                _queueColumnHeaderLastModifiedVisibility = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderLastModifiedVisibility));
            }
        }

        private double _queueColumnHeaderLastModifiedWidth = 180;
        public double QueueColumnHeaderLastModifiedWidth
        {
            get
            {
                return _queueColumnHeaderLastModifiedWidth;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedWidth)
                    return;

                _queueColumnHeaderLastModifiedWidth = value;

                if (value > 0)
                    QueueColumnHeaderLastModifiedWidthRestore = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderLastModifiedWidth));
            }
        }

        private double _queueColumnHeaderLastModifiedWidthUser = 180;
        public double QueueColumnHeaderLastModifiedWidthRestore
        {
            get
            {
                return _queueColumnHeaderLastModifiedWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedWidthUser)
                    return;

                _queueColumnHeaderLastModifiedWidthUser = value;

                NotifyPropertyChanged(nameof(QueueColumnHeaderLastModifiedWidthRestore));
            }
        }

        #endregion

        #endregion

        #region == Themes ==

        private ObservableCollection<Theme> _themes;
        public ObservableCollection<Theme> Themes
        {
            get { return _themes; }
            set { _themes = value; }
        }
       
        private Theme _currentTheme;
        public Theme CurrentTheme
        {
            get
            {
                return _currentTheme;
            }
            set
            {
                if (_currentTheme == value) return;

                _currentTheme = value;
                this.NotifyPropertyChanged("CurrentTheme");

                (Application.Current as App).ChangeTheme(_currentTheme.Name);
            }
        }

        #endregion

        #region == Status and Visibility switch flags ==  

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
                NotifyPropertyChanged(nameof(IsConnected));
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
                NotifyPropertyChanged(nameof(IsConnecting));
                NotifyPropertyChanged(nameof(IsNotConnecting));

                NotifyPropertyChanged(nameof(IsProfileSwitchOK));
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
                NotifyPropertyChanged(nameof(IsNotConnectingNorConnected));
            }
        }

        public bool IsNotConnecting
        {
            get
            {
                return !_isConnecting;
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

                NotifyPropertyChanged(nameof(IsSettingsShow));

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
                NotifyPropertyChanged(nameof(IsConnectionSettingShow));
            }
        }

        private bool _isChangePasswordDialogShow;
        public bool IsChangePasswordDialogShow
        {
            get
            {
                return _isChangePasswordDialogShow;
            }
            set
            {
                if (_isChangePasswordDialogShow == value)
                    return;

                _isChangePasswordDialogShow = value;
                NotifyPropertyChanged(nameof(IsChangePasswordDialogShow));
            }
        }

        public bool IsCurrentProfileSet
        {
            get
            {
                if (Profiles.Count > 0)
                    return true;
                else
                    return false;
            }
        }

        private bool _isAlbumArtVisible;
        public bool IsAlbumArtVisible
        {
            get
            {
                return _isAlbumArtVisible;
            }
            set
            {
                if (_isAlbumArtVisible == value)
                    return;

                _isAlbumArtVisible = value;
                NotifyPropertyChanged(nameof(IsAlbumArtVisible));
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get
            {
                return _isBusy;
            }
            set
            {
                if (_isBusy == value)
                    return;

                _isBusy = value;
                NotifyPropertyChanged(nameof(IsBusy));

                NotifyPropertyChanged(nameof(IsProfileSwitchOK));

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
            }
        }

        private bool _isWorking;
        public bool IsWorking
        {
            get
            {
                return _isWorking;
            }
            set
            {
                if (_isWorking == value)
                    return;

                _isWorking = value;
                NotifyPropertyChanged(nameof(IsWorking));

                NotifyPropertyChanged(nameof(IsProfileSwitchOK));

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
            }
        }

        private bool _isShowAckWindow
;
        public bool IsShowAckWindow

        {
            get { return _isShowAckWindow; }
            set
            {
                if (_isShowAckWindow == value)
                    return;

                _isShowAckWindow = value;

                NotifyPropertyChanged(nameof(IsShowAckWindow));
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

                NotifyPropertyChanged(nameof(IsShowDebugWindow));

                if (_isShowDebugWindow)
                {
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        //DebugWindowShowHide?.Invoke
                        DebugWindowShowHide2?.Invoke(this, true);
                    });
                }
                else
                {
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        //DebugWindowShowHide?.Invoke();
                        DebugWindowShowHide2?.Invoke(this, false);
                    });
                }
            }
        }

        #endregion

        #region == CurrentSong, Playback controls, AlbumArt ==  

        private SongInfoEx _currentSong;
        public SongInfoEx CurrentSong
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
                NotifyPropertyChanged(nameof(CurrentSong));
                NotifyPropertyChanged(nameof(CurrentSongTitle));
                NotifyPropertyChanged(nameof(CurrentSongArtist));
                NotifyPropertyChanged(nameof(CurrentSongAlbum));

                if (value == null)
                    _elapsedTimer.Stop();
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

        #region == Playback ==  

        private static readonly string _pathPlayButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static readonly string _pathPauseButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
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
                NotifyPropertyChanged(nameof(PlayButton));
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
                    NotifyPropertyChanged(nameof(Volume));

                    if (_mpc != null)
                    {
                        if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) != _volume)
                        {
                            // If we have a timer and we are in this event handler, a user is still interact with the slider
                            // we stop the timer
                            if (_volumeDelayTimer != null)
                                _volumeDelayTimer.Stop();

                            //System.Diagnostics.Debug.WriteLine("Volume value is still changing. Skipping.");

                            // we always create a new instance of DispatcherTimer
                            _volumeDelayTimer = new System.Timers.Timer
                            {
                                AutoReset = false,

                                // if one second passes, that means our user has stopped interacting with the slider
                                // we do real event
                                Interval = (double)1000
                            };
                            _volumeDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeVolume);

                            _volumeDelayTimer.Start();
                        }
                    }
                }
            }
        }

        private System.Timers.Timer _volumeDelayTimer = null;
        private void DoChangeVolume(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_mpc != null)
            {
                if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) != _volume)
                {
                    if (SetVolumeCommand.CanExecute(null))
                    {
                        SetVolumeCommand.Execute(null);
                    }
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
                NotifyPropertyChanged(nameof(Repeat));

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdRepeat != value)
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
                NotifyPropertyChanged(nameof(Random));

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdRandom != value)
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
                NotifyPropertyChanged(nameof(Consume));

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdConsume != value)
                    {
                        if (SetConsumeCommand.CanExecute(null))
                        {
                            SetConsumeCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _single;
        public bool Single
        {
            get { return _single; }
            set
            {
                _single = value;
                NotifyPropertyChanged(nameof(Single));

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdSingle != value)
                    {
                        if (SetSingleCommand.CanExecute(null))
                        {
                            SetSingleCommand.Execute(null);
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
                NotifyPropertyChanged(nameof(Time));
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
                    NotifyPropertyChanged(nameof(Elapsed));

                    // If we have a timer and we are in this event handler, a user is still interact with the slider
                    // we stop the timer
                    if (_elapsedDelayTimer != null)
                        _elapsedDelayTimer.Stop();

                    //System.Diagnostics.Debug.WriteLine("Elapsed value is still changing. Skipping.");

                    // we always create a new instance of DispatcherTimer
                    _elapsedDelayTimer = new System.Timers.Timer
                    {
                        AutoReset = false,

                        // if one second passes, that means our user has stopped interacting with the slider
                        // we do real event
                        Interval = (double)1000
                    };
                    _elapsedDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeElapsed);

                    _elapsedDelayTimer.Start();
                }
            }
        }

        private System.Timers.Timer _elapsedDelayTimer = null;
        private void DoChangeElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_mpc != null)
            {
                if ((_elapsed < _time))
                {
                    if (SetSeekCommand.CanExecute(null))
                    {
                        SetSeekCommand.Execute(null);
                    }
                }
            }
        }


        #endregion

        #region == AlbumArt == 

        private readonly ImageSource _albumArtDefault = null;
        private ImageSource _albumArt;
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
                NotifyPropertyChanged(nameof(AlbumArt));
            }
        }

        #endregion

        #endregion

        #region == TreeView Menu (Queue, Library, Search, Playlists, Playlist) ==

        #region == MenuTree ==

        private readonly MenuTreeBuilder _mainMenuItems = new();
        public ObservableCollection<NodeTree> MainMenuItems
        {
            get { return _mainMenuItems.Children; }
            set
            {
                _mainMenuItems.Children = value;
                NotifyPropertyChanged(nameof(MainMenuItems));
            }
        }

        private NodeTree _selectedNodeMenu = new NodeMenu("root");
        public NodeTree SelectedNodeMenu
        {
            get { return _selectedNodeMenu; }
            set
            {
                if (_selectedNodeMenu == value)
                    return;

                if (value == null)
                {
                    //Debug.WriteLine("selectedNodeMenu is null");
                    return;
                }

                _selectedNodeMenu = value;
                NotifyPropertyChanged(nameof(SelectedNodeMenu));

                if (value is NodeMenuQueue)
                {
                    IsQueueVisible = true;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = false;
                    IsSearchVisible = false;
                }
                else if (value is NodeMenuPlaylists)
                {
                    IsQueueVisible = false;
                    IsPlaylistsVisible = true;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = false;
                    IsSearchVisible = false;
                }
                else if (value is NodeMenuPlaylistItem)
                {
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = true;
                    IsLibraryVisible = false;
                    IsSearchVisible = false;

                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        SelectedPlaylistSong = null;
                        //PlaylistSongs.Clear();
                        //PlaylistSongs = new ObservableCollection<SongInfo>(); // Don't Clear();
                        PlaylistSongs = (value as NodeMenuPlaylistItem).PlaylistSongs;
                        //PlaylistSongs = new ObservableCollection<SongInfo>((value as NodeMenuPlaylistItem).PlaylistSongs); 

                    });

                    if (((value as NodeMenuPlaylistItem).PlaylistSongs.Count == 0) || (value as NodeMenuPlaylistItem).IsUpdateRequied)
                        GetPlaylistSongs(value as NodeMenuPlaylistItem);
                }
                else if (value is NodeMenuLibrary)
                {
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = true;
                    IsSearchVisible = false;

                    if (!(value as NodeMenuLibrary).IsAcquired || (MusicDirectories.Count <= 1) && (MusicEntries.Count == 0))
                        GetLibrary(value as NodeMenuLibrary);
                }
                else if (value is NodeMenuSearch)
                {
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = false;
                    IsSearchVisible = true;
                }
                else if (value is NodeMenu)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenu ...unknown:" + _selectedNodeMenu.Name);

                    if (value.Name != "root")
                        throw new NotImplementedException();

                    IsQueueVisible = true;
                }
                else
                {
                    //Debug.WriteLine(value.Name);

                    throw new NotImplementedException();
                }

            }
        }

        private bool _isQueueVisible = true;
        public bool IsQueueVisible
        {
            get { return _isQueueVisible; }
            set
            {
                if (_isQueueVisible == value)
                    return;

                _isQueueVisible = value;
                NotifyPropertyChanged(nameof(IsQueueVisible));
            }
        }

        private bool _isPlaylistsVisible = true;
        public bool IsPlaylistsVisible
        {
            get { return _isPlaylistsVisible; }
            set
            {
                if (_isPlaylistsVisible == value)
                    return;

                _isPlaylistsVisible = value;
                NotifyPropertyChanged(nameof(IsPlaylistsVisible));
            }
        }

        private bool _isPlaylistItemVisible = true;
        public bool IsPlaylistItemVisible
        {
            get { return _isPlaylistItemVisible; }
            set
            {
                if (_isPlaylistItemVisible == value)
                    return;

                _isPlaylistItemVisible = value;
                NotifyPropertyChanged(nameof(IsPlaylistItemVisible));
            }
        }

        private bool _isLibraryVisible = true;
        public bool IsLibraryVisible
        {
            get { return _isLibraryVisible; }
            set
            {
                if (_isLibraryVisible == value)
                    return;

                _isLibraryVisible = value;
                NotifyPropertyChanged(nameof(IsLibraryVisible));
            }
        }

        private bool _isSearchVisible = true;
        public bool IsSearchVisible
        {
            get { return _isSearchVisible; }
            set
            {
                if (_isSearchVisible == value)
                    return;

                _isSearchVisible = value;
                NotifyPropertyChanged(nameof(IsSearchVisible));
            }
        }

        #endregion

        #region == Queue ==  

        private ObservableCollection<SongInfoEx> _queue = new();
        public ObservableCollection<SongInfoEx> Queue
        {
            get
            {
                if (_mpc != null)
                {
                    return _queue;
                    //return _mpc.CurrentQueue;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_queue == value)
                    return;

                _queue = value;
                NotifyPropertyChanged(nameof(Queue));
            }
        }

        private SongInfoEx _selectedQueueSong;
        public SongInfoEx SelectedQueueSong
        {
            get
            {
                return _selectedQueueSong;
            }
            set
            {
                if (_selectedQueueSong == value)
                    return;

                _selectedQueueSong = value;
                NotifyPropertyChanged(nameof(SelectedQueueSong));
            }
        }

        private bool _isQueueFindVisible;
        public bool IsQueueFindVisible
        {
            get
            {
                return _isQueueFindVisible;
            }
            set
            {
                if (_isQueueFindVisible == value)
                    return;

                _isQueueFindVisible = value;
                NotifyPropertyChanged(nameof(IsQueueFindVisible));
            }
        }

        private ObservableCollection<SongInfoEx> _queueForFilter = new();
        public ObservableCollection<SongInfoEx> QueueForFilter
        {
            get
            {
                return _queueForFilter;
            }
            set
            {
                if (_queueForFilter == value)
                    return;

                _queueForFilter = value;
                NotifyPropertyChanged(nameof(QueueForFilter));
            }
        }

        private SearchTags _selectedQueueFilterTags = SearchTags.Title;
        public SearchTags SelectedQueueFilterTags
        {
            get
            {
                return _selectedQueueFilterTags;
            }
            set
            {
                if (_selectedQueueFilterTags == value)
                    return;

                _selectedQueueFilterTags = value;
                NotifyPropertyChanged(nameof(SelectedQueueFilterTags));

                if (_filterQueueQuery == "")
                    return;

                var collectionView = CollectionViewSource.GetDefaultView(_queueForFilter);
                collectionView.Filter = x =>
                {
                    var entry = (SongInfoEx)x;

                    if (SelectedQueueFilterTags == SearchTags.Title)
                    {
                        return entry.Title.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else if (SelectedQueueFilterTags == SearchTags.Artist)
                    {
                        return entry.Artist.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else if (SelectedQueueFilterTags == SearchTags.Album)
                    {
                        return entry.Album.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else if (SelectedQueueFilterTags == SearchTags.Genre)
                    {
                        return entry.Genre.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else
                    {
                        return false;
                    }
                };
            }
        }

        private string _filterQueueQuery = "";
        public string FilterQueueQuery
        {
            get
            {
                return _filterQueueQuery;
            }
            set
            {
                if (_filterQueueQuery == value)
                    return;

                _filterQueueQuery = value;
                NotifyPropertyChanged(nameof(FilterQueueQuery));

                var collectionView = CollectionViewSource.GetDefaultView(_queueForFilter);
                collectionView.Filter = x =>
                {
                    if (_filterQueueQuery == "")
                    {
                        return false;
                    }
                    else
                    {
                        var entry = (SongInfoEx)x;

                        if (SelectedQueueFilterTags == SearchTags.Title)
                        {
                            return entry.Title.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else if (SelectedQueueFilterTags == SearchTags.Artist)
                        {
                            return entry.Artist.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else if (SelectedQueueFilterTags == SearchTags.Album)
                        {
                            return entry.Album.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else if (SelectedQueueFilterTags == SearchTags.Genre)
                        {
                            return entry.Genre.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else
                        {
                            return false;
                        }
                    }
                };

                //collectionView.Refresh();
            }
        }

        private SongInfoEx _selectedQueueFilterSong;
        public SongInfoEx SelectedQueueFilterSong
        {
            get
            {
                return _selectedQueueFilterSong;
            }
            set
            {
                if (_selectedQueueFilterSong == value)
                    return;

                _selectedQueueFilterSong = value;
                NotifyPropertyChanged(nameof(SelectedQueueFilterSong));
            }
        }

        #endregion

        #region == Library ==

        private readonly DirectoryTreeBuilder _musicDirectories = new();
        public ObservableCollection<NodeTree> MusicDirectories
        {
            get { return _musicDirectories.Children; }
            set
            {
                _musicDirectories.Children = value;
                NotifyPropertyChanged(nameof(MusicDirectories));
            }
        }

        private NodeTree _selectedNodeDirectory = new NodeDirectory(".",new Uri(@"file:///./"));
        public NodeTree SelectedNodeDirectory
        {
            get { return _selectedNodeDirectory; }
            set
            {
                if (_selectedNodeDirectory == value)
                    return;

                _selectedNodeDirectory = value;
                NotifyPropertyChanged(nameof(SelectedNodeDirectory));

                if (_selectedNodeDirectory == null)
                    return;

                if (MusicEntries == null)
                    return;
                if (MusicEntries.Count == 0)
                    return;

                // TODO: Make the selection option > Filtering mode or matching mode.
                bool filteringMode = true;

                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);
                if (collectionView == null)
                    return;

                try
                {
                    collectionView.Filter = x =>
                    {
                        var entry = (NodeFile)x;

                        if (entry == null)
                            return false;

                        if (entry.FileUri == null)
                            return false;

                        string path = entry.FileUri.LocalPath; //person.FileUri.AbsoluteUri;
                        if (string.IsNullOrEmpty(path))
                            return false;
                        string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);
                        if (string.IsNullOrEmpty(filename))
                            return false;

                        if ((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath == "/")
                        {
                            if (filteringMode)
                            {
                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    return (filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                // Only the matched(in the folder) items
                                path = path.Replace("/", "");

                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    return ((path == filename) && filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path == filename);
                                }
                            }
                        }
                        else
                        {
                            path = path.Replace(("/" + filename), "");

                            if (filteringMode)
                            {
                                // testing (adding "/")
                                path += "/";

                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    // testing (adding "/")
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath + "/") && filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    // This is not enough. eg. "/Hoge/Hoge" and /Hoge/Hoge2
                                    //return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath));

                                    // testing (adding "/")
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath + "/"));
                                }
                            }
                            else
                            {
                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path == (_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath);
                                }
                            }
                        }
                    };

                }
                catch (Exception e)
                {
                    Debug.WriteLine("collectionView.Filter = x => " + e.Message);

                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            App app = App.Current as App;
                            app.AppendErrorLog("Exception@SelectedNodeDirectory collectionView.Filter = x =>", e.Message);
                        });
                    }
                }

            }
        }

        private ObservableCollection<NodeFile> _musicEntries = new();
        public ObservableCollection<NodeFile> MusicEntries
        {
            get
            {
                return _musicEntries; 
            }
            set
            {
                if (value == _musicEntries)
                    return;

                _musicEntries = value;
                NotifyPropertyChanged(nameof(MusicEntries));

            }
        }

        private string _filterMusicEntriesQuery = "";
        public string FilterMusicEntriesQuery
        {
            get
            {
                return _filterMusicEntriesQuery;
            }
            set
            {
                if (_filterMusicEntriesQuery == value)
                    return;

                _filterMusicEntriesQuery = value;
                NotifyPropertyChanged(nameof(FilterMusicEntriesQuery));

                /*
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);

                collectionView.Filter = x =>
                {
                    var entry = (NodeFile)x;

                    string test = entry.FilePath + entry.Name;

                    // 絞り込み
                    return (test.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                };
                collectionView.Refresh();

                */

                if (_selectedNodeDirectory == null)
                    return;

                // TODO: 絞り込みモードか、マッチしたフォルダ内だけかの切り替え
                bool filteringMode = true;

                // Treeview で選択ノードが変更されたのでListview でフィルターを掛ける。
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);
                collectionView.Filter = x =>
                {
                    var entry = (NodeFile)x;

                    if (entry.FileUri == null)
                        return false;

                    string path = entry.FileUri.LocalPath; //person.FileUri.AbsoluteUri;
                    string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);

                    if ((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath == "/")
                    {
                        if (filteringMode)
                        {
                            // 絞り込みモード
                            if (FilterMusicEntriesQuery != "")
                            {
                                return (filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            // マッチしたフォルダ内だけ
                            path = path.Replace("/", "");

                            if (FilterMusicEntriesQuery != "")
                            {
                                return ((path == filename) && filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path == filename);
                            }
                        }
                    }
                    else
                    {
                        path = path.Replace(("/" + filename), "");

                        if (filteringMode)
                        {
                            // 絞り込みモード

                            if (FilterMusicEntriesQuery != "")
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath));
                            }
                        }
                        else
                        {
                            // マッチしたフォルダ内だけ
                            if (FilterMusicEntriesQuery != "")
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path == (_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath);
                            }
                        }
                    }
                };

                collectionView.Refresh();

            }
        }

        #endregion

        #region == Search ==

        public ObservableCollection<SongInfo> SearchResult
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.SearchResult;
                }
                else
                {
                    return null;
                }
            }
        }

        private SearchTags _selectedSearchTags = SearchTags.Title;
        public SearchTags SelectedSearchTags
        {
            get
            {
                return _selectedSearchTags;
            }
            set
            {
                if (_selectedSearchTags == value)
                    return;

                _selectedSearchTags = value;
                NotifyPropertyChanged(nameof(SelectedSearchTags));
            }
        }

        private string _searchQuery;
        public string SearchQuery
        {
            get
            {
                return _searchQuery;
            }
            set
            {
                if (_searchQuery == value)
                    return;

                _searchQuery = value;
                NotifyPropertyChanged(nameof(SearchQuery));
            }
        }

        #endregion

        #region == Playlists ==  

        private ObservableCollection<Playlist> _playlists = new();
        public ObservableCollection<Playlist> Playlists
        {
            get
            {
                return _playlists;
            }
            set
            {
                if (_playlists == value)
                    return;

                _playlists = value;
                NotifyPropertyChanged(nameof(Playlists));
            }
        }

        private Playlist _selectedPlaylist;
        public Playlist SelectedPlaylist
        {
            get
            {
                return _selectedPlaylist;
            }
            set
            {
                if (_selectedPlaylist != value)
                {
                    _selectedPlaylist = value;
                    NotifyPropertyChanged(nameof(SelectedPlaylist));
                }
            }
        }

        #endregion

        #region == Playlist Items ==

        private ObservableCollection<SongInfo> _playlistSongs = new();
        public ObservableCollection<SongInfo> PlaylistSongs
        {
            get
            {
                return _playlistSongs;
            }
            set
            {
                if (_playlistSongs != value)
                {
                    _playlistSongs = value;
                    NotifyPropertyChanged(nameof(PlaylistSongs));
                }
            }
        }

        private SongInfo _selectedPlaylistSong;
        public SongInfo SelectedPlaylistSong
        {
            get
            {
                return _selectedPlaylistSong;
            }
            set
            {
                if (_selectedPlaylistSong != value)
                {
                    _selectedPlaylistSong = value;
                    NotifyPropertyChanged(nameof(SelectedPlaylistSong));
                }
            }
        }

        #endregion

        #endregion

        #region == Status Messages == 

        private string _statusBarMessage;
        public string StatusBarMessage
        {
            get
            {
                return _statusBarMessage;
            }
            set
            {
                _statusBarMessage = value;
                NotifyPropertyChanged(nameof(StatusBarMessage));
            }
        }

        private string _connectionStatusMessage;
        public string ConnectionStatusMessage
        {
            get
            {
                return _connectionStatusMessage;
            }
            set
            {
                _connectionStatusMessage = value;
                NotifyPropertyChanged(nameof(ConnectionStatusMessage));
            }
        }

        private string _mpdStatusMessage;
        public string MpdStatusMessage
        {
            get
            {
                return _mpdStatusMessage;
            }
            set
            {
                _mpdStatusMessage = value;
                NotifyPropertyChanged(nameof(MpdStatusMessage));

                if (_mpdStatusMessage != "")
                    _isMpdStatusMessageContainsText = true;
                else
                    _isMpdStatusMessageContainsText = false;
                NotifyPropertyChanged(nameof(IsMpdStatusMessageContainsText));
            }
        }

        private bool _isMpdStatusMessageContainsText;
        public bool IsMpdStatusMessageContainsText
        {
            get
            {
                return _isMpdStatusMessageContainsText;
            }
        }

        private static readonly string _pathDefaultNoneButton = "";
        private static readonly string _pathDisconnectedButton = "M4,1C2.89,1 2,1.89 2,3V7C2,8.11 2.89,9 4,9H1V11H13V9H10C11.11,9 12,8.11 12,7V3C12,1.89 11.11,1 10,1H4M4,3H10V7H4V3M14,13C12.89,13 12,13.89 12,15V19C12,20.11 12.89,21 14,21H11V23H23V21H20C21.11,21 22,20.11 22,19V15C22,13.89 21.11,13 20,13H14M3.88,13.46L2.46,14.88L4.59,17L2.46,19.12L3.88,20.54L6,18.41L8.12,20.54L9.54,19.12L7.41,17L9.54,14.88L8.12,13.46L6,15.59L3.88,13.46M14,15H20V19H14V15Z";

        private static readonly string _pathConnectingButton = "M11 14H9C9 9.03 13.03 5 18 5V7C14.13 7 11 10.13 11 14M18 11V9C15.24 9 13 11.24 13 14H15C15 12.34 16.34 11 18 11M7 4C7 2.89 6.11 2 5 2S3 2.89 3 4 3.89 6 5 6 7 5.11 7 4M11.45 4.5H9.45C9.21 5.92 8 7 6.5 7H3.5C2.67 7 2 7.67 2 8.5V11H8V8.74C9.86 8.15 11.25 6.5 11.45 4.5M19 17C20.11 17 21 16.11 21 15S20.11 13 19 13 17 13.89 17 15 17.89 17 19 17M20.5 18H17.5C16 18 14.79 16.92 14.55 15.5H12.55C12.75 17.5 14.14 19.15 16 19.74V22H22V19.5C22 18.67 21.33 18 20.5 18Z";
        private static readonly string _pathConnectedButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";
        //private static string _pathConnectedButton = "";
        //private static string _pathDisconnectedButton = "";
        private static readonly string _pathNewConnectionButton = "M20,4C21.11,4 22,4.89 22,6V18C22,19.11 21.11,20 20,20H4C2.89,20 2,19.11 2,18V6C2,4.89 2.89,4 4,4H20M8.5,15V9H7.25V12.5L4.75,9H3.5V15H4.75V11.5L7.3,15H8.5M13.5,10.26V9H9.5V15H13.5V13.75H11V12.64H13.5V11.38H11V10.26H13.5M20.5,14V9H19.25V13.5H18.13V10H16.88V13.5H15.75V9H14.5V14A1,1 0 0,0 15.5,15H19.5A1,1 0 0,0 20.5,14Z";
        private static readonly string _pathErrorInfoButton = "M23,12L20.56,14.78L20.9,18.46L17.29,19.28L15.4,22.46L12,21L8.6,22.47L6.71,19.29L3.1,18.47L3.44,14.78L1,12L3.44,9.21L3.1,5.53L6.71,4.72L8.6,1.54L12,3L15.4,1.54L17.29,4.72L20.9,5.54L20.56,9.22L23,12M20.33,12L18.5,9.89L18.74,7.1L16,6.5L14.58,4.07L12,5.18L9.42,4.07L8,6.5L5.26,7.09L5.5,9.88L3.67,12L5.5,14.1L5.26,16.9L8,17.5L9.42,19.93L12,18.81L14.58,19.92L16,17.5L18.74,16.89L18.5,14.1L20.33,12M11,15H13V17H11V15M11,7H13V13H11V7";

        private string _statusButton = _pathDefaultNoneButton;
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
                NotifyPropertyChanged(nameof(StatusButton));
            }
        }

        private static readonly string _pathMpdOkButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";

        private static readonly string _pathMpdAckErrorButton = "M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z";

        private string _mpdStatusButton = _pathMpdOkButton;
        public string MpdStatusButton
        {
            get
            {
                return _mpdStatusButton;
            }
            set
            {
                if (_mpdStatusButton == value)
                    return;

                _mpdStatusButton = value;
                NotifyPropertyChanged(nameof(MpdStatusButton));
            }
        }

        private bool _isUpdatingMpdDb;
        public bool IsUpdatingMpdDb
        {
            get
            {
                return _isUpdatingMpdDb;
            }
            set
            {
                _isUpdatingMpdDb = value;
                NotifyPropertyChanged(nameof(IsUpdatingMpdDb));
            }
        }

        private string _mpdVersion;
        public string MpdVersion
        {
            get
            {
                if (_mpdVersion != "")
                    return "MPD protocol v" + _mpdVersion;
                else
                    return _mpdVersion;

            }
            set
            {
                if (value == _mpdVersion)
                    return;

                _mpdVersion = value;
                NotifyPropertyChanged(nameof(MpdVersion));
            }
        }

        #endregion

        #region == Profile and Settings ==

        private readonly ObservableCollection<Profile> _profiles = new();
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
                NotifyPropertyChanged(nameof(CurrentProfile));

                SelectedProfile = _currentProfile;

                _volume = _currentProfile.Volume;
                NotifyPropertyChanged(nameof(Volume));
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
                    ClearError(nameof(Host));
                    ClearError(nameof(Port));
                    Host = SelectedProfile.Host;
                    Port = SelectedProfile.Port.ToString();
                    Password = SelectedProfile.Password;
                    SetIsDefault = SelectedProfile.IsDefault;
                }
                else
                {
                    ClearError(nameof(Host));
                    ClearError(nameof(Port));
                    Host = "";
                    Port = "6600";
                    Password = "";
                }

                NotifyPropertyChanged(nameof(SelectedProfile));

                // "quietly"
                _selectedQuickProfile = _selectedProfile;
                NotifyPropertyChanged(nameof(SelectedQuickProfile));

            }
        }

        public bool IsProfileSwitchOK
        {
            get
            {
                if (IsBusy || IsConnecting || IsWorking || (Profiles.Count <= 1))
                    return false;
                else
                    return true;
            }
        }

        private Profile _selectedQuickProfile;
        public Profile SelectedQuickProfile
        {
            get
            {
                return _selectedQuickProfile;
            }
            set
            {
                if (_selectedQuickProfile == value)
                    return;

                if (IsProfileSwitchOK)
                {
                    _selectedQuickProfile = value;

                    if (_selectedQuickProfile != null)
                    {
                        SelectedProfile = _selectedQuickProfile;
                        CurrentProfile = _selectedQuickProfile;

                        ChangeConnection(_selectedQuickProfile);
                    }
                }

                NotifyPropertyChanged(nameof(SelectedQuickProfile));
            }
        }

        private string _host = "";
        public string Host
        {
            get { return _host; }
            set
            {
                ClearError(nameof(Host));
                _host = value;

                // Validate input.
                if (value == "")
                {
                    SetError(nameof(Host), MPDCtrl.Properties.Resources.Settings_ErrorHostMustBeSpecified);

                }
                /*
                else if (value == "localhost")
                {
                    _host = "127.0.0.1";
                }
                else
                {
                    IPAddress ipAddress;
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
                        SetError(nameof(Host), MPDCtrl.Properties.Resources.Settings_ErrorHostInvalidAddressFormat);
                    }
                }
                */

                NotifyPropertyChanged(nameof(Host));
            }
        }

        private IPAddress _hostIpAddress;
        public IPAddress HostIpAddress
        {
            get { return _hostIpAddress; }
            set
            {
                if (_hostIpAddress == value)
                    return;

                    _hostIpAddress = value;

                NotifyPropertyChanged(nameof(HostIpAddress));
            }
        }

        private int _port = 6600;
        public string Port
        {
            get { return _port.ToString(); }
            set
            {
                ClearError(nameof(Port));

                if (value == "")
                {
                    SetError(nameof(Port), MPDCtrl.Properties.Resources.Settings_ErrorPortMustBeSpecified);
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
                        ClearError(nameof(Port));
                    }
                    else
                    {
                        SetError(nameof(Port), MPDCtrl.Properties.Resources.Settings_ErrorInvalidPortNaN);
                        _port = 0;
                    }
                }

                NotifyPropertyChanged(nameof(Port));
            }
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

                NotifyPropertyChanged(nameof(IsNotPasswordSet));
                NotifyPropertyChanged(nameof(IsPasswordSet));
                NotifyPropertyChanged(nameof(Password));
            }
        }

        private static string Encrypt(string s)
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

        private static string Decrypt(string s)
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

        private static string DummyPassword(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }
            string e = "";
            for (int i = 1; i <= s.Length; i++)
            {
                e += "*";
            }
            return e;
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
                NotifyPropertyChanged(nameof(SettingProfileEditMessage));
            }
        }

        public bool IsPasswordSet
        {
            get
            {
                if (SelectedProfile != null)
                {
                    if (!string.IsNullOrEmpty(SelectedProfile.Password))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsNotPasswordSet
        {
            get
            {
                if (IsPasswordSet)
                    return false;
                else
                    return true;
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

                _setIsDefault = value;

                NotifyPropertyChanged(nameof(SetIsDefault));
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

                NotifyPropertyChanged(nameof(IsUpdateOnStartup));
            }
        }

        private bool _isAutoScrollToNowPlaying = false;
        public bool IsAutoScrollToNowPlaying
        {
            get { return _isAutoScrollToNowPlaying; }
            set
            {
                if (_isAutoScrollToNowPlaying == value)
                    return;

                _isAutoScrollToNowPlaying = value;

                NotifyPropertyChanged(nameof(IsAutoScrollToNowPlaying));
            }
        }

        private bool _isSaveLog;
        public bool IsSaveLog
        {
            get { return _isSaveLog; }
            set
            {
                if (_isSaveLog == value)
                    return;

                _isSaveLog = value;

                NotifyPropertyChanged(nameof(IsSaveLog));
            }
        }

        private bool _isDownloadAlbumArt = false;
        public bool IsDownloadAlbumArt
        {
            get { return _isDownloadAlbumArt; }
            set
            {
                if (_isDownloadAlbumArt == value)
                    return;

                _isDownloadAlbumArt = value;

                NotifyPropertyChanged(nameof(IsDownloadAlbumArt));
            }
        }

        private bool _isDownloadAlbumArtEmbeddedUsingReadPicture = false;
        public bool IsDownloadAlbumArtEmbeddedUsingReadPicture
        {
            get { return _isDownloadAlbumArtEmbeddedUsingReadPicture; }
            set
            {
                if (_isDownloadAlbumArtEmbeddedUsingReadPicture == value)
                    return;

                _isDownloadAlbumArtEmbeddedUsingReadPicture = value;

                NotifyPropertyChanged(nameof(IsDownloadAlbumArtEmbeddedUsingReadPicture));
            }
        }

        private bool _isSwitchingProfile;
        public bool IsSwitchingProfile
        {
            get
            {
                return _isSwitchingProfile;
            }
            set
            {
                if (_isSwitchingProfile == value)
                    return;

                _isSwitchingProfile = value;
                NotifyPropertyChanged(nameof(IsSwitchingProfile));
            }
        }


        private string _changePasswordDialogMessage;
        public string ChangePasswordDialogMessage
        {
            get { return _changePasswordDialogMessage; }
            set
            {
                if (_changePasswordDialogMessage == value)
                    return;

                _changePasswordDialogMessage = value;
                NotifyPropertyChanged(nameof(ChangePasswordDialogMessage));
            }
        }

        #endregion

        #region == Popups ==

        private List<string> queueListviewSelectedQueueSongIdsForPopup = new();
        private List<string> searchResultListviewSelectedQueueSongUriForPopup = new();
        private List<string> songFilesListviewSelectedQueueSongUriForPopup = new();

        private bool _isSaveAsPlaylistPopupVisible;
        public bool IsSaveAsPlaylistPopupVisible
        {
            get
            {
                return _isSaveAsPlaylistPopupVisible;
            }
            set
            {
                if (_isSaveAsPlaylistPopupVisible == value)
                    return;

                _isSaveAsPlaylistPopupVisible = value;
                NotifyPropertyChanged(nameof(IsSaveAsPlaylistPopupVisible));
            }
        }

        private bool _isConfirmClearQueuePopupVisible;
        public bool IsConfirmClearQueuePopupVisible
        {
            get
            {
                return _isConfirmClearQueuePopupVisible;
            }
            set
            {
                if (_isConfirmClearQueuePopupVisible == value)
                    return;

                _isConfirmClearQueuePopupVisible = value;
                NotifyPropertyChanged(nameof(IsConfirmClearQueuePopupVisible));
            }
        }

        private bool _isSelectedSaveToPopupVisible;
        public bool IsSelectedSaveToPopupVisible
        {
            get
            {
                return _isSelectedSaveToPopupVisible;
            }
            set
            {
                if (_isSelectedSaveToPopupVisible == value)
                    return;

                _isSelectedSaveToPopupVisible = value;
                NotifyPropertyChanged(nameof(IsSelectedSaveToPopupVisible));
            }
        }

        private bool _isSelectedSaveAsPopupVisible;
        public bool IsSelectedSaveAsPopupVisible
        {
            get
            {
                return _isSelectedSaveAsPopupVisible;
            }
            set
            {
                if (_isSelectedSaveAsPopupVisible == value)
                    return;

                _isSelectedSaveAsPopupVisible = value;
                NotifyPropertyChanged(nameof(IsSelectedSaveAsPopupVisible));
            }
        }

        private bool _isConfirmDeleteQueuePopupVisible;
        public bool IsConfirmDeleteQueuePopupVisible
        {
            get
            {
                return _isConfirmDeleteQueuePopupVisible;
            }
            set
            {
                if (_isConfirmDeleteQueuePopupVisible == value)
                    return;

                _isConfirmDeleteQueuePopupVisible = value;
                NotifyPropertyChanged(nameof(IsConfirmDeleteQueuePopupVisible));
            }
        }

        private bool _isConfirmDeletePlaylistPopupVisible;
        public bool IsConfirmDeletePlaylistPopupVisible
        {
            get
            {
                return _isConfirmDeletePlaylistPopupVisible;
            }
            set
            {
                if (_isConfirmDeletePlaylistPopupVisible == value)
                    return;

                _isConfirmDeletePlaylistPopupVisible = value;
                NotifyPropertyChanged(nameof(IsConfirmDeletePlaylistPopupVisible));
            }
        }

        private bool _isConfirmUpdatePlaylistSongsPopupVisible;
        public bool IsConfirmUpdatePlaylistSongsPopupVisible
        {
            get
            {
                return _isConfirmUpdatePlaylistSongsPopupVisible;
            }
            set
            {
                if (_isConfirmUpdatePlaylistSongsPopupVisible == value)
                    return;

                _isConfirmUpdatePlaylistSongsPopupVisible = value;
                NotifyPropertyChanged(nameof(IsConfirmUpdatePlaylistSongsPopupVisible));
            }
        }

        private bool _isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible;
        public bool IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible
        {
            get
            {
                return _isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible;
            }
            set
            {
                if (_isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible == value)
                    return;

                _isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible = value;
                NotifyPropertyChanged(nameof(IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible));
            }
        }

        private bool _isConfirmDeletePlaylistSongPopupVisible;
        public bool IsConfirmDeletePlaylistSongPopupVisible
        {
            get
            {
                return _isConfirmDeletePlaylistSongPopupVisible;
            }
            set
            {
                if (_isConfirmDeletePlaylistSongPopupVisible == value)
                    return;

                _isConfirmDeletePlaylistSongPopupVisible = value;
                NotifyPropertyChanged(nameof(IsConfirmDeletePlaylistSongPopupVisible));
            }
        }

        private bool _isConfirmPlaylistClearPopupVisible;
        public bool IsConfirmPlaylistClearPopupVisible
        {
            get
            {
                return _isConfirmPlaylistClearPopupVisible;
            }
            set
            {
                if (_isConfirmPlaylistClearPopupVisible == value)
                    return;

                _isConfirmPlaylistClearPopupVisible = value;
                NotifyPropertyChanged(nameof(IsConfirmPlaylistClearPopupVisible));
            }
        }

        private bool _isSearchResultSelectedSaveAsPopupVisible;
        public bool IsSearchResultSelectedSaveAsPopupVisible
        {
            get
            {
                return _isSearchResultSelectedSaveAsPopupVisible;
            }
            set
            {
                if (_isSearchResultSelectedSaveAsPopupVisible == value)
                    return;

                _isSearchResultSelectedSaveAsPopupVisible = value;
                NotifyPropertyChanged(nameof(IsSearchResultSelectedSaveAsPopupVisible));
            }
        }

        private bool _isSearchResultSelectedSaveToPopupVisible;
        public bool IsSearchResultSelectedSaveToPopupVisible
        {
            get
            {
                return _isSearchResultSelectedSaveToPopupVisible;
            }
            set
            {
                if (_isSearchResultSelectedSaveToPopupVisible == value)
                    return;

                _isSearchResultSelectedSaveToPopupVisible = value;
                NotifyPropertyChanged(nameof(IsSearchResultSelectedSaveToPopupVisible));
            }
        }

        private bool _sSongFilesSelectedSaveAsPopupVisible;
        public bool IsSongFilesSelectedSaveAsPopupVisible
        {
            get
            {
                return _sSongFilesSelectedSaveAsPopupVisible;
            }
            set
            {
                if (_sSongFilesSelectedSaveAsPopupVisible == value)
                    return;

                _sSongFilesSelectedSaveAsPopupVisible = value;
                NotifyPropertyChanged(nameof(IsSongFilesSelectedSaveAsPopupVisible));
            }
        }

        private bool _isSongFilesSelectedSaveToPopupVisible;
        public bool IsSongFilesSelectedSaveToPopupVisible
        {
            get
            {
                return _isSongFilesSelectedSaveToPopupVisible;
            }
            set
            {
                if (_isSongFilesSelectedSaveToPopupVisible == value)
                    return;

                _isSongFilesSelectedSaveToPopupVisible = value;
                NotifyPropertyChanged(nameof(IsSongFilesSelectedSaveToPopupVisible));
            }
        }

        #endregion

        #region == Events ==

        // DebugWindow
        public delegate void DebugWindowShowHideEventHandler();
        public event DebugWindowShowHideEventHandler DebugWindowShowHide;

        public event EventHandler<bool> DebugWindowShowHide2;

        public event EventHandler<string> DebugCommandOutput;

        public event EventHandler<string> DebugIdleOutput;

        public delegate void DebugCommandClearEventHandler();
        public event DebugCommandClearEventHandler DebugCommandClear;

        public delegate void DebugIdleClearEventHandler();
        public event DebugIdleClearEventHandler DebugIdleClear;

        // AckWindow
        public event EventHandler<string> AckWindowOutput;

        public delegate void AckWindowClearEventHandler();
        public event AckWindowClearEventHandler AckWindowClear;

        // Queue listview ScrollIntoView
        public event EventHandler<int> ScrollIntoView;

        // Queue listview ScrollIntoView and select (for filter and first time loading the queue)
        public event EventHandler<int> ScrollIntoViewAndSelect;

        // PlaylistSongsListview ScrollIntoView
        //public event EventHandler<int> ScrollIntoViewPlaylistSongs;

        //public delegate void QueueSelectionClearEventHandler();
        //public event QueueSelectionClearEventHandler QueueSelectionClear;

        public event EventHandler<string> UpdateProgress;

        #endregion

        #region == Lock objects ==

        // TODO: not used

        private readonly object lockQueueObject = new();
        private readonly object lockCurrentQueueObject = new();

        #endregion

        private readonly MPC _mpc = new();

        public MainViewModel()
        {
            #region == Init config folder and file path ==

            _appDataFolder = _envDataFolder + System.IO.Path.DirectorySeparatorChar + _appDeveloper + System.IO.Path.DirectorySeparatorChar + _appName;
            _appConfigFilePath = _appDataFolder + System.IO.Path.DirectorySeparatorChar + _appName + ".config";
            System.IO.Directory.CreateDirectory(_appDataFolder);

            #endregion

            #region == Init commands ==

            PlayCommand = new RelayCommand(PlayCommand_ExecuteAsync, PlayCommand_CanExecute);
            PlayStopCommand = new RelayCommand(PlayStopCommand_Execute, PlayStopCommand_CanExecute);
            PlayPauseCommand = new RelayCommand(PlayPauseCommand_Execute, PlayPauseCommand_CanExecute);
            PlayNextCommand = new RelayCommand(PlayNextCommand_ExecuteAsync, PlayNextCommand_CanExecute);
            PlayPrevCommand = new RelayCommand(PlayPrevCommand_ExecuteAsync, PlayPrevCommand_CanExecute);
            ChangeSongCommand = new RelayCommand(ChangeSongCommand_ExecuteAsync, ChangeSongCommand_CanExecute);

            SetRpeatCommand = new RelayCommand(SetRpeatCommand_ExecuteAsync, SetRpeatCommand_CanExecute);
            SetRandomCommand = new RelayCommand(SetRandomCommand_ExecuteAsync, SetRandomCommand_CanExecute);
            SetConsumeCommand = new RelayCommand(SetConsumeCommand_ExecuteAsync, SetConsumeCommand_CanExecute);
            SetSingleCommand = new RelayCommand(SetSingleCommand_ExecuteAsync, SetSingleCommand_CanExecute);

            SetVolumeCommand = new RelayCommand(SetVolumeCommand_ExecuteAsync, SetVolumeCommand_CanExecute);
            VolumeMuteCommand = new RelayCommand(VolumeMuteCommand_Execute, VolumeMuteCommand_CanExecute);
            VolumeUpCommand = new RelayCommand(VolumeUpCommand_Execute, VolumeUpCommand_CanExecute);
            VolumeDownCommand = new RelayCommand(VolumeDownCommand_Execute, VolumeDownCommand_CanExecute);

            SetSeekCommand = new RelayCommand(SetSeekCommand_ExecuteAsync, SetSeekCommand_CanExecute);

            PlaylistListviewEnterKeyCommand = new RelayCommand(PlaylistListviewEnterKeyCommand_ExecuteAsync, PlaylistListviewEnterKeyCommand_CanExecute);
            PlaylistListviewLoadPlaylistCommand = new RelayCommand(PlaylistListviewLoadPlaylistCommand_ExecuteAsync, PlaylistListviewLoadPlaylistCommand_CanExecute);
            PlaylistListviewClearLoadPlaylistCommand = new RelayCommand(PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync, PlaylistListviewClearLoadPlaylistCommand_CanExecute);
            PlaylistListviewLeftDoubleClickCommand = new GenericRelayCommand<Playlist>(param => PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(param), param => PlaylistListviewLeftDoubleClickCommand_CanExecute());
            ChangePlaylistCommand = new RelayCommand(ChangePlaylistCommand_ExecuteAsync, ChangePlaylistCommand_CanExecute);
            PlaylistListviewRemovePlaylistCommand = new GenericRelayCommand<Playlist>(param => PlaylistListviewRemovePlaylistCommand_Execute(param), param => PlaylistListviewRemovePlaylistCommand_CanExecute());

            PlaylistListviewConfirmRemovePlaylistPopupCommand = new RelayCommand(PlaylistListviewConfirmRemovePlaylistPopupCommand_Execute, PlaylistListviewConfirmRemovePlaylistPopupCommand_CanExecute);
            PlaylistListviewRenamePlaylistCommand = new GenericRelayCommand<Playlist>(param => PlaylistListviewRenamePlaylistCommand_Execute(param), param => PlaylistListviewRenamePlaylistCommand_CanExecute());

            PlaylistListviewConfirmUpdatePopupCommand = new RelayCommand(PlaylistListviewConfirmUpdatePopupCommand_Execute, PlaylistListviewConfirmUpdatePopupCommand_CanExecute);
            PlaylistListviewClearCommand = new RelayCommand(PlaylistListviewClearCommand_Execute, PlaylistListviewClearCommand_CanExecute);
            PlaylistListviewClearPopupCommand = new RelayCommand(PlaylistListviewClearPopupCommand_Execute, PlaylistListviewClearPopupCommand_CanExecute);


            QueueListviewEnterKeyCommand = new RelayCommand(QueueListviewEnterKeyCommand_ExecuteAsync, QueueListviewEnterKeyCommand_CanExecute);
            QueueListviewLeftDoubleClickCommand = new GenericRelayCommand<SongInfoEx>(param => QueueListviewLeftDoubleClickCommand_ExecuteAsync(param), param => QueueListviewLeftDoubleClickCommand_CanExecute());
            QueueListviewDeleteCommand = new GenericRelayCommand<object>(param => QueueListviewDeleteCommand_Execute(param), param => QueueListviewDeleteCommand_CanExecute());
            QueueListviewConfirmDeletePopupCommand = new RelayCommand(QueueListviewConfirmDeletePopupCommand_Execute, QueueListviewConfirmDeletePopupCommand_CanExecute);

            QueueListviewClearCommand = new RelayCommand(QueueListviewClearCommand_ExecuteAsync, QueueListviewClearCommand_CanExecute);
            QueueListviewConfirmClearPopupCommand = new RelayCommand(QueueListviewConfirmClearPopupCommand_Execute, QueueListviewConfirmClearPopupCommand_CanExecute);
            QueueListviewSaveAsCommand = new RelayCommand(QueueListviewSaveAsCommand_ExecuteAsync, QueueListviewSaveAsCommand_CanExecute);
            QueueListviewSaveAsPopupCommand = new GenericRelayCommand<String>(param => QueueListviewSaveAsPopupCommand_Execute(param), param => QueueListviewSaveAsPopupCommand_CanExecute());
            QueueListviewMoveUpCommand = new GenericRelayCommand<object>(param => QueueListviewMoveUpCommand_Execute(param), param => QueueListviewMoveUpCommand_CanExecute());
            QueueListviewMoveDownCommand = new GenericRelayCommand<object>(param => QueueListviewMoveDownCommand_Execute(param), param => QueueListviewMoveDownCommand_CanExecute());

            QueueListviewSaveSelectedAsCommand = new GenericRelayCommand<object>(param => QueueListviewSaveSelectedAsCommand_Execute(param), param => QueueListviewSaveSelectedAsCommand_CanExecute());
            QueueListviewSaveSelectedAsPopupCommand = new GenericRelayCommand<String>(param => QueueListviewSaveSelectedAsPopupCommand_Execute(param), param => QueueListviewSaveSelectedAsPopupCommand_CanExecute());

            QueueListviewSaveSelectedToCommand = new GenericRelayCommand<object>(param => QueueListviewSaveSelectedToCommand_Execute(param), param => QueueListviewSaveSelectedToCommand_CanExecute());
            QueueListviewSaveSelectedToPopupCommand = new GenericRelayCommand<Playlist>(param => QueueListviewSaveSelectedToPopupCommand_Execute(param), param => QueueListviewSaveSelectedToPopupCommand_CanExecute());

            ShowSettingsCommand = new RelayCommand(ShowSettingsCommand_Execute, ShowSettingsCommand_CanExecute);
            SettingsOKCommand = new RelayCommand(SettingsOKCommand_Execute, SettingsOKCommand_CanExecute);

            NewProfileCommand = new RelayCommand(NewProfileCommand_Execute, NewProfileCommand_CanExecute);
            DeleteProfileCommand = new RelayCommand(DeleteProfileCommand_Execute, DeleteProfileCommand_CanExecute);
            SaveProfileCommand = new GenericRelayCommand<object>(param => SaveProfileCommand_Execute(param), param => SaveProfileCommand_CanExecute());
            UpdateProfileCommand = new GenericRelayCommand<object>(param => UpdateProfileCommand_Execute(param), param => UpdateProfileCommand_CanExecute());
            ChangeConnectionProfileCommand = new GenericRelayCommand<object>(param => ChangeConnectionProfileCommand_Execute(param), param => ChangeConnectionProfileCommand_CanExecute());

            ShowChangePasswordDialogCommand = new GenericRelayCommand<object>(param => ShowChangePasswordDialogCommand_Execute(param), param => ShowChangePasswordDialogCommand_CanExecute());
            ChangePasswordDialogOKCommand = new GenericRelayCommand<object>(param => ChangePasswordDialogOKCommand_Execute(param), param => ChangePasswordDialogOKCommand_CanExecute());
            ChangePasswordDialogCancelCommand = new RelayCommand(ChangePasswordDialogCancelCommand_Execute, ChangePasswordDialogCancelCommand_CanExecute);

            EscapeCommand = new RelayCommand(EscapeCommand_ExecuteAsync, EscapeCommand_CanExecute);

            QueueColumnHeaderPositionShowHideCommand = new RelayCommand(QueueColumnHeaderPositionShowHideCommand_Execute, QueueColumnHeaderPositionShowHideCommand_CanExecute);
            QueueColumnHeaderNowPlayingShowHideCommand = new RelayCommand(QueueColumnHeaderNowPlayingShowHideCommand_Execute, QueueColumnHeaderNowPlayingShowHideCommand_CanExecute);
            QueueColumnHeaderTimeShowHideCommand = new RelayCommand(QueueColumnHeaderTimeShowHideCommand_Execute, QueueColumnHeaderTimeShowHideCommand_CanExecute);
            QueueColumnHeaderArtistShowHideCommand = new RelayCommand(QueueColumnHeaderArtistShowHideCommand_Execute, QueueColumnHeaderArtistShowHideCommand_CanExecute);
            QueueColumnHeaderAlbumShowHideCommand = new RelayCommand(QueueColumnHeaderAlbumShowHideCommand_Execute, QueueColumnHeaderAlbumShowHideCommand_CanExecute);
            QueueColumnHeaderGenreShowHideCommand = new RelayCommand(QueueColumnHeaderGenreShowHideCommand_Execute, QueueColumnHeaderGenreShowHideCommand_CanExecute);
            QueueColumnHeaderLastModifiedShowHideCommand = new RelayCommand(QueueColumnHeaderLastModifiedShowHideCommand_Execute, QueueColumnHeaderLastModifiedShowHideCommand_CanExecute);

            ShowFindCommand = new RelayCommand(ShowFindCommand_Execute, ShowFindCommand_CanExecute);

            QueueFindShowHideCommand = new RelayCommand(QueueFindShowHideCommand_Execute, QueueFindShowHideCommand_CanExecute);

            SearchExecCommand = new RelayCommand(SearchExecCommand_Execute, SearchExecCommand_CanExecute);
            FilterMusicEntriesClearCommand = new RelayCommand(FilterMusicEntriesClearCommand_Execute, FilterMusicEntriesClearCommand_CanExecute);

            FilterQueueClearCommand = new RelayCommand(FilterQueueClearCommand_Execute, FilterQueueClearCommand_CanExecute);

            SearchResultListviewSaveSelectedAsCommand = new GenericRelayCommand<object>(param => SearchResultListviewSaveSelectedAsCommand_Execute(param), param => SearchResultListviewSaveSelectedAsCommand_CanExecute());
            SearchResultListviewSaveSelectedAsPopupCommand = new GenericRelayCommand<string>(param => SearchResultListviewSaveSelectedAsPopupCommand_Execute(param), param => SearchResultListviewSaveSelectedAsPopupCommand_CanExecute());
            SearchResultListviewSaveSelectedToCommand = new GenericRelayCommand<object>(param => SearchResultListviewSaveSelectedToCommand_Execute(param), param => SearchResultListviewSaveSelectedToCommand_CanExecute());
            SearchResultListviewSaveSelectedToPopupCommand = new GenericRelayCommand<Playlist>(param => SearchResultListviewSaveSelectedToPopupCommand_Execute(param), param => SearchResultListviewSaveSelectedToPopupCommand_CanExecute());


            SongsListviewAddCommand = new GenericRelayCommand<object>(param => SongsListviewAddCommand_Execute(param), param => SongsListviewAddCommand_CanExecute());
            SongFilesListviewAddCommand = new GenericRelayCommand<object>(param => SongFilesListviewAddCommand_Execute(param), param => SongFilesListviewAddCommand_CanExecute());

            SongFilesListviewSaveSelectedAsCommand = new GenericRelayCommand<object>(param => SongFilesListviewSaveSelectedAsCommand_Execute(param), param => SongFilesListviewSaveSelectedAsCommand_CanExecute());
            SongFilesListviewSaveSelectedAsPopupCommand = new GenericRelayCommand<string>(param => SongFilesListviewSaveSelectedAsPopupCommand_Execute(param), param => SongFilesListviewSaveSelectedAsPopupCommand_CanExecute());
            SongFilesListviewSaveSelectedToCommand = new GenericRelayCommand<object>(param => SongFilesListviewSaveSelectedToCommand_Execute(param), param => SongFilesListviewSaveSelectedToCommand_CanExecute());
            SongFilesListviewSaveSelectedToPopupCommand = new GenericRelayCommand<Playlist>(param => SongFilesListviewSaveSelectedToPopupCommand_Execute(param), param => SongFilesListviewSaveSelectedToPopupCommand_CanExecute());


            ScrollIntoNowPlayingCommand = new RelayCommand(ScrollIntoNowPlayingCommand_Execute, ScrollIntoNowPlayingCommand_CanExecute);

            ClearDebugCommandTextCommand = new RelayCommand(ClearDebugCommandTextCommand_Execute, ClearDebugCommandTextCommand_CanExecute);
            ClearDebugIdleTextCommand = new RelayCommand(ClearDebugIdleTextCommand_Execute, ClearDebugIdleTextCommand_CanExecute);
            ShowDebugWindowCommand = new RelayCommand(ShowDebugWindowCommand_Execute, ShowDebugWindowCommand_CanExecute);

            ClearAckTextCommand = new RelayCommand(ClearAckTextCommand_Execute, ClearAckTextCommand_CanExecute);
            ShowAckWindowCommand = new RelayCommand(ShowAckWindowCommand_Execute, ShowAckWindowCommand_CanExecute);

            PlaylistListviewDeletePosCommand = new GenericRelayCommand<object>(param => PlaylistListviewDeletePosCommand_Execute(param), param => PlaylistListviewDeletePosCommand_CanExecute());
            PlaylistListviewDeletePosPopupCommand = new RelayCommand(PlaylistListviewDeletePosPopupCommand_Execute, PlaylistListviewDeletePosPopupCommand_CanExecute);
            PlaylistListviewConfirmDeletePosNotSupportedPopupCommand = new RelayCommand(PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_Execute, PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_CanExecute);

            QueueFilterSelectCommand = new GenericRelayCommand<object>(param => QueueFilterSelectCommand_Execute(param), param => QueueFilterSelectCommand_CanExecute());


            TreeviewMenuItemLoadPlaylistCommand = new RelayCommand(TreeviewMenuItemLoadPlaylistCommand_Execute, TreeviewMenuItemLoadPlaylistCommand_CanExecute);
            TreeviewMenuItemClearLoadPlaylistCommand = new RelayCommand(TreeviewMenuItemClearLoadPlaylistCommand_Execute, TreeviewMenuItemClearLoadPlaylistCommand_CanExecute);

            #endregion

            #region == Locks == 
            
            //TODO; not used

            BindingOperations.EnableCollectionSynchronization(Queue, lockQueueObject);
            BindingOperations.EnableCollectionSynchronization(_mpc.CurrentQueue, lockCurrentQueueObject);
            
            BindingOperations.EnableCollectionSynchronization(MusicDirectories, new object());
            BindingOperations.EnableCollectionSynchronization(MusicEntries, new object());
            BindingOperations.EnableCollectionSynchronization(SearchResult, new object());
            BindingOperations.EnableCollectionSynchronization(Playlists, new object());
            BindingOperations.EnableCollectionSynchronization(PlaylistSongs, new object());
            BindingOperations.EnableCollectionSynchronization(Profiles, new object());

            #endregion

            #region == Themes ==

            // 
            _themes = new ObservableCollection<Theme>()
            {
                new Theme() { Id = 1, Name = "DarkTheme", Label = MPDCtrl.Properties.Resources.Settings_Opts_Themes_Dark, IconData="M17.75,4.09L15.22,6.03L16.13,9.09L13.5,7.28L10.87,9.09L11.78,6.03L9.25,4.09L12.44,4L13.5,1L14.56,4L17.75,4.09M21.25,11L19.61,12.25L20.2,14.23L18.5,13.06L16.8,14.23L17.39,12.25L15.75,11L17.81,10.95L18.5,9L19.19,10.95L21.25,11M18.97,15.95C19.8,15.87 20.69,17.05 20.16,17.8C19.84,18.25 19.5,18.67 19.08,19.07C15.17,23 8.84,23 4.94,19.07C1.03,15.17 1.03,8.83 4.94,4.93C5.34,4.53 5.76,4.17 6.21,3.85C6.96,3.32 8.14,4.21 8.06,5.04C7.79,7.9 8.75,10.87 10.95,13.06C13.14,15.26 16.1,16.22 18.97,15.95M17.33,17.97C14.5,17.81 11.7,16.64 9.53,14.5C7.36,12.31 6.2,9.5 6.04,6.68C3.23,9.82 3.34,14.64 6.35,17.66C9.37,20.67 14.19,20.78 17.33,17.97Z"},
                new Theme() { Id = 2, Name = "LightTheme", Label = MPDCtrl.Properties.Resources.Settings_Opts_Themes_Light, IconData="M12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,2L14.39,5.42C13.65,5.15 12.84,5 12,5C11.16,5 10.35,5.15 9.61,5.42L12,2M3.34,7L7.5,6.65C6.9,7.16 6.36,7.78 5.94,8.5C5.5,9.24 5.25,10 5.11,10.79L3.34,7M3.36,17L5.12,13.23C5.26,14 5.53,14.78 5.95,15.5C6.37,16.24 6.91,16.86 7.5,17.37L3.36,17M20.65,7L18.88,10.79C18.74,10 18.47,9.23 18.05,8.5C17.63,7.78 17.1,7.15 16.5,6.64L20.65,7M20.64,17L16.5,17.36C17.09,16.85 17.62,16.22 18.04,15.5C18.46,14.77 18.73,14 18.87,13.21L20.64,17M12,22L9.59,18.56C10.33,18.83 11.14,19 12,19C12.82,19 13.63,18.83 14.37,18.56L12,22Z"}
            };

            // 
            _currentTheme = _themes[0];

            #endregion

            #region == Subscribe to events ==

            _mpc.IsBusy += new MPC.IsBusyEvent(OnMpcIsBusy);

            _mpc.MpdIdleConnected += new MPC.IsMpdIdleConnectedEvent(OnMpdIdleConnected);

            _mpc.DebugCommandOutput += new MPC.DebugCommandOutputEvent(OnDebugCommandOutput);
            _mpc.DebugIdleOutput += new MPC.DebugIdleOutputEvent(OnDebugIdleOutput);

            _mpc.ConnectionStatusChanged += new MPC.ConnectionStatusChangedEvent(OnConnectionStatusChanged);
            _mpc.ConnectionError += new MPC.ConnectionErrorEvent(OnConnectionError);

            _mpc.MpdPlayerStatusChanged += new MPC.MpdPlayerStatusChangedEvent(OnMpdPlayerStatusChanged);
            _mpc.MpdCurrentQueueChanged += new MPC.MpdCurrentQueueChangedEvent(OnMpdCurrentQueueChanged);
            _mpc.MpdPlaylistsChanged += new MPC.MpdPlaylistsChangedEvent(OnMpdPlaylistsChanged);

            _mpc.MpdAckError += new MPC.MpdAckErrorEvent(OnMpdAckError);

            _mpc.MpdAlbumArtChanged += new MPC.MpdAlbumArtChangedEvent(OnAlbumArtChanged);

            //_mpc.MpcInfo += new MPC.MpcInfoEvent(OnMpcInfoEvent);

            _mpc.MpcProgress += new MPC.MpcProgressEvent(OnMpcProgress);

            this.UpdateProgress += (sender, arg) => { this.OnUpdateProgress(arg); };

            #endregion

            #region == Init Song's time elapsed timer. ==  

            // Init Song's time elapsed timer.
            _elapsedTimer = new System.Timers.Timer(500);
            _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);

            #endregion

        }

        #region == Startup and Shutdown ==

        // Startup
        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            #region == Load app setting  ==

            try
            {
                // Load config file.
                if (File.Exists(_appConfigFilePath))
                {
                    XDocument xdoc = XDocument.Load(_appConfigFilePath);

                    #region == Window setting ==

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

                    #region == Theme ==
                    
                    var thm = xdoc.Root.Element("Theme");
                    if (thm != null)
                    {
                        var hoge = thm.Attribute("ThemeName");
                        if (hoge != null)
                        {
                            if ((hoge.Value == "DarkTheme") || hoge.Value == "LightTheme")
                            {
                                CurrentTheme = _themes.FirstOrDefault(x => x.Name == hoge.Value);
                            }
                        }
                    }
                    
                    #endregion

                    #region == Options ==

                    var opts = xdoc.Root.Element("Options");
                    if (opts != null)
                    {
                        var hoge = opts.Attribute("AutoScrollToNowPlaying");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsAutoScrollToNowPlaying = true;
                            }
                            else
                            {
                                IsAutoScrollToNowPlaying = false;
                            }
                        }

                        hoge = opts.Attribute("UpdateOnStartup");
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

                        hoge = opts.Attribute("SaveLog");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsSaveLog = true;

                            }
                            else
                            {
                                IsSaveLog = false;
                            }
                        }

                        hoge = opts.Attribute("DownloadAlbumArt");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsDownloadAlbumArt = true;

                            }
                            else
                            {
                                IsDownloadAlbumArt = false;
                            }
                        }

                        hoge = opts.Attribute("DownloadAlbumArtEmbeddedUsingReadPicture");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsDownloadAlbumArtEmbeddedUsingReadPicture = true;

                            }
                            else
                            {
                                IsDownloadAlbumArtEmbeddedUsingReadPicture = false;
                            }
                        }
                    }

                    #endregion

                    #region == Profiles  ==

                    var xProfiles = xdoc.Root.Element("Profiles");
                    if (xProfiles != null)
                    {
                        var profileList = xProfiles.Elements("Profile");

                        foreach (var p in profileList)
                        {
                            Profile pro = new();

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
                            if (p.Attribute("Volume") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Volume").Value))
                                {
                                    try
                                    {
                                        pro.Volume = double.Parse(p.Attribute("Volume").Value);
                                    }
                                    catch
                                    {
                                        pro.Volume = 50;
                                    }
                                }
                            }

                            Profiles.Add(pro);
                        }
                    }
                    #endregion

                    #region == Header columns ==

                    var Headers = xdoc.Root.Element("Headers");///Queue/Position
                    if (Headers != null)
                    {
                        var Que = Headers.Element("Queue");
                        if (Que != null)
                        {
                            var column = Que.Element("Position");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                        {
                                            QueueColumnHeaderPositionVisibility = true;
                                        }
                                        else
                                        {
                                            QueueColumnHeaderPositionVisibility = false;
                                        }
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderPositionWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderPositionWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderPositionWidth > 0)
                                    QueueColumnHeaderPositionWidthRestore = QueueColumnHeaderPositionWidth;
                            }
                            column = Que.Element("NowPlaying");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderNowPlayingVisibility = true;
                                        else
                                            QueueColumnHeaderNowPlayingVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderNowPlayingWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderNowPlayingWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderNowPlayingWidth > 0)
                                    QueueColumnHeaderNowPlayingWidthRestore = QueueColumnHeaderNowPlayingWidth;
                            }
                            column = Que.Element("Title");
                            if (column != null)
                            {
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderTitleWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                            if (QueueColumnHeaderTitleWidth < 120)
                                                QueueColumnHeaderTitleWidth = 160;
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderTitleWidth = 160;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderTitleWidth > 0)
                                    QueueColumnHeaderTitleWidthRestore = QueueColumnHeaderTitleWidth;
                            }
                            column = Que.Element("Time");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderTimeVisibility = true;
                                        else
                                            QueueColumnHeaderTimeVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderTimeWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderTimeWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderTimeWidth > 0)
                                    QueueColumnHeaderTimeWidthRestore = QueueColumnHeaderTimeWidth;
                            }
                            column = Que.Element("Artist");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderArtistVisibility = true;
                                        else
                                            QueueColumnHeaderArtistVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderArtistWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderArtistWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderArtistWidth > 0)
                                    QueueColumnHeaderArtistWidthRestore = QueueColumnHeaderArtistWidth;
                            }
                            column = Que.Element("Album");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderAlbumVisibility = true;
                                        else
                                            QueueColumnHeaderAlbumVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderAlbumWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderAlbumWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderAlbumWidth > 0)
                                    QueueColumnHeaderAlbumWidthRestore = QueueColumnHeaderAlbumWidth;
                            }
                            column = Que.Element("Genre");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderGenreVisibility = true;
                                        else
                                            QueueColumnHeaderGenreVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderGenreWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderGenreWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderGenreWidth > 0)
                                    QueueColumnHeaderGenreWidthRestore = QueueColumnHeaderGenreWidth;
                            }
                            column = Que.Element("LastModified");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderLastModifiedVisibility = true;
                                        else
                                            QueueColumnHeaderLastModifiedVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderLastModifiedWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderLastModifiedWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderLastModifiedWidth > 0)
                                    QueueColumnHeaderLastModifiedWidthRestore = QueueColumnHeaderLastModifiedWidth;
                            }
                        }
                    }

                    #endregion

                    #region == Layout ==

                    var lay = xdoc.Root.Element("Layout");
                    if (lay != null)
                    {
                        var leftpain = lay.Element("LeftPain");
                        if (leftpain != null)
                        {
                            if (leftpain.Attribute("Width") != null)
                            {
                                if (!string.IsNullOrEmpty(leftpain.Attribute("Width").Value))
                                {
                                    try
                                    {
                                        MainLeftPainWidth = Double.Parse(leftpain.Attribute("Width").Value.ToString());
                                    }
                                    catch
                                    {
                                        MainLeftPainWidth = 241;
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                }

                IsFullyLoaded = true;
            }
            catch (System.IO.FileNotFoundException ex)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        App app = App.Current as App;
                        app.AppendErrorLog("System.IO.FileNotFoundException@OnWindowLoaded", ex.Message);
                    });
                }
            }
            catch (Exception ex)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        App app = App.Current as App;
                        app.AppendErrorLog("Exception@OnWindowLoaded", ex.Message);
                    });
                }
            }

            #endregion

            NotifyPropertyChanged(nameof(IsCurrentProfileSet));

            if (CurrentProfile == null)
            {
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.Init_NewConnectionSetting;
                StatusButton = _pathNewConnectionButton;

                // Show connection setting
                IsConnectionSettingShow = true;
            }
            else
            {
                IsConnectionSettingShow = false;

                // set this "quietly"
                _volume = CurrentProfile.Volume;
                NotifyPropertyChanged(nameof(Volume));

                // start the connection
                Start(CurrentProfile.Host, CurrentProfile.Port);
            }

            // error log
            if (IsSaveLog)
            {
                if (App.Current != null)
                {
                    App app = App.Current as App;
                    app.IsSaveErrorLog = true;
                    app.LogFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + "MPDCtrl_errors.txt";
                }
            }
        }

        // On window's content rendered
        public void OnContentRendered(object sender, EventArgs e)
        {
            IsFullyRendered = true;
        }

        // Closing
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // Make sure Window and settings have been fully loaded and not overriding with empty data.
            if (!IsFullyLoaded)
                return;

            double windowWidth = 780;

            #region == App Setting ==

            // Config xml file
            XmlDocument doc = new();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

            // Root Document Element
            XmlElement root = doc.CreateElement(string.Empty, "App", string.Empty);
            doc.AppendChild(root);

            XmlAttribute attrs = doc.CreateAttribute("Version");
            attrs.Value = _appVer;
            root.SetAttributeNode(attrs);

            #region == Window settings ==

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
                    windowWidth = w.RestoreBounds.Width;
                }
                else
                {
                    attrs.Value = w.Width.ToString();
                    windowWidth = w.Width;

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

            #endregion

            #region == Theme ==

            XmlElement thm = doc.CreateElement(string.Empty, "Theme", string.Empty);

            attrs = doc.CreateAttribute("ThemeName");
            attrs.Value = _currentTheme.Name;
            thm.SetAttributeNode(attrs);

            /// 
            root.AppendChild(thm);

            #endregion

            #region == Options ==

            XmlElement opts = doc.CreateElement(string.Empty, "Options", string.Empty);

            //
            attrs = doc.CreateAttribute("AutoScrollToNowPlaying");
            if (IsAutoScrollToNowPlaying)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

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
            attrs = doc.CreateAttribute("SaveLog");
            if (IsSaveLog)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("DownloadAlbumArt");
            if (IsDownloadAlbumArt)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("DownloadAlbumArtEmbeddedUsingReadPicture");
            if (IsDownloadAlbumArtEmbeddedUsingReadPicture)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            /// 
            root.AppendChild(opts);

            #endregion

            #region == Profiles  ==

            XmlElement xProfiles = doc.CreateElement(string.Empty, "Profiles", string.Empty);

            XmlElement xProfile;
            XmlAttribute xAttrs;

            if (Profiles.Count == 1)
                Profiles[0].IsDefault = true;

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

                xAttrs = doc.CreateAttribute("Volume");
                if (p == CurrentProfile)
                {
                    xAttrs.Value = _volume.ToString();
                }
                else
                {
                    xAttrs.Value = p.Volume.ToString();
                }
                xProfile.SetAttributeNode(xAttrs);


                xProfiles.AppendChild(xProfile);
            }

            root.AppendChild(xProfiles);

            #endregion

            #region == Header columns ==

            XmlElement headers = doc.CreateElement(string.Empty, "Headers", string.Empty);

            XmlElement queueHeader;
            XmlElement queueHeaderColumn;

            XmlAttribute qAttrs;

            queueHeader = doc.CreateElement(string.Empty, "Queue", string.Empty);


            // Position
            queueHeaderColumn = doc.CreateElement(string.Empty, "Position", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderPositionVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderPositionWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderPositionWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Now Playing
            queueHeaderColumn = doc.CreateElement(string.Empty, "NowPlaying", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderNowPlayingVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderNowPlayingWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderNowPlayingWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Title skip visibility
            queueHeaderColumn = doc.CreateElement(string.Empty, "Title", string.Empty);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderTitleWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderTitleWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Time
            queueHeaderColumn = doc.CreateElement(string.Empty, "Time", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderTimeVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderTimeWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderTimeWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Artist
            queueHeaderColumn = doc.CreateElement(string.Empty, "Artist", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderArtistVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderArtistWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderArtistWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Album
            queueHeaderColumn = doc.CreateElement(string.Empty, "Album", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderAlbumVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderAlbumWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderAlbumWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Genre
            queueHeaderColumn = doc.CreateElement(string.Empty, "Genre", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderGenreVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderGenreWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderGenreWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Last Modified
            queueHeaderColumn = doc.CreateElement(string.Empty, "LastModified", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderLastModifiedVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderLastModifiedWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderLastModifiedWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            //
            headers.AppendChild(queueHeader);
            ////
            root.AppendChild(headers);

            #endregion

            #region == Layout - not using right now ==

            XmlElement lay = doc.CreateElement(string.Empty, "Layout", string.Empty);

            XmlElement leftpain;
            XmlAttribute lAttrs;

            // LeftPain
            leftpain = doc.CreateElement(string.Empty, "LeftPain", string.Empty);
            lAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
            {
                if (windowWidth > (MainLeftPainActualWidth - 24))
                {
                    lAttrs.Value = MainLeftPainActualWidth.ToString();
                }
                else
                {
                    lAttrs.Value = "241";
                }
            }
            else
            {
                lAttrs.Value = MainLeftPainWidth.ToString();
            }
            leftpain.SetAttributeNode(lAttrs);

            //
            lay.AppendChild(leftpain);
            ////
            root.AppendChild(lay);

            #endregion

            try
            {
                doc.Save(_appConfigFilePath);
            }
            //catch (System.IO.FileNotFoundException) { }
            catch (Exception ex)
            {
                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        App app = App.Current as App;
                        app.AppendErrorLog("Exception@OnWindowClosing", ex.Message);
                    });
                }
            }

            #endregion

            try
            {
                if (IsConnected)
                {
                    _mpc.MpdStop = true;

                    // TODO: Although it's a good thing to close...this causes anoying exception in the debug output. 
                    _mpc.MpdDisconnect();
                }
            }
            catch { }

            // Save error logs.
            if (Application.Current != null)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    App app = App.Current as App;
                    app.SaveErrorLog();
                });
            }
        }

        #endregion

        #region == Methods ==

        private async void Start(string host, int port)
        {
            HostIpAddress = null;
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(host);
                if (addresses.Count() > 0)
                {
                    HostIpAddress = addresses[0];
                }
                else
                {
                    // TODO::
                    ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
                    StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";
                    return;
                }
            }
            catch (Exception)
            {
                // TODO::
                ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
                StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";
                return;
            }

            // Start MPD connection.
            _=Task.Run(() => _mpc.MpdIdleConnect(HostIpAddress.ToString(), port));
        }

        private void UpdateButtonStatus()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                try
                {
                    //Play button
                    switch (_mpc.MpdStatus.MpdState)
                    {
                        case Status.MpdPlayState.Play:
                            {
                                PlayButton = _pathPauseButton;
                                break;
                            }
                        case Status.MpdPlayState.Pause:
                            {
                                PlayButton = _pathPlayButton;
                                break;
                            }
                        case Status.MpdPlayState.Stop:
                            {
                                PlayButton = _pathPlayButton;
                                break;
                            }

                            //_pathStopButton
                    }

                    if (_mpc.MpdStatus.MpdVolumeIsSet)
                    {
                        double tmpVol = Convert.ToDouble(_mpc.MpdStatus.MpdVolume);
                        if (_volume != tmpVol)
                        {
                            // "quietly" update.
                            _volume = tmpVol;
                            NotifyPropertyChanged(nameof(Volume));
                        }
                    }

                    _random = _mpc.MpdStatus.MpdRandom;
                    NotifyPropertyChanged(nameof(Random));

                    _repeat = _mpc.MpdStatus.MpdRepeat;
                    NotifyPropertyChanged(nameof(Repeat));

                    _consume = _mpc.MpdStatus.MpdConsume;
                    NotifyPropertyChanged(nameof(Consume));

                    _single = _mpc.MpdStatus.MpdSingle;
                    NotifyPropertyChanged(nameof(Single));

                    // no need to care about "double" updates for time.
                    Time = _mpc.MpdStatus.MpdSongTime;

                    _elapsed = _mpc.MpdStatus.MpdSongElapsed;
                    //NotifyPropertyChanged("Elapsed");

                    //start elapsed timer.
                    if (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play)
                    {
                        if (!_elapsedTimer.Enabled)
                            _elapsedTimer.Start();
                    }
                    else
                    {
                        _elapsedTimer.Stop();
                    }

                    //if (Application.Current == null) { return; }
                    //Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
                }
                catch
                {
                    Debug.WriteLine("Error@UpdateButtonStatus");
                }
            });
        }

        private async void UpdateStatus()
        {
            UpdateButtonStatus();

            bool isAlbumArtChanged = false;

            UpdateProgress?.Invoke(this, "[UI] Status updating...");

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                bool isSongChanged = false;
                bool isCurrentSongWasNull = false;

                if (CurrentSong != null)
                {
                    if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                    {
                        isSongChanged = true;

                        // Clear IsPlaying icon
                        CurrentSong.IsPlaying = false;

                        //
                        if (_mpc.MpdCurrentSong != null)
                        {
                            _mpc.MpdCurrentSong.IsPlaying = false;
                        }

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }
                }
                else
                {
                    isCurrentSongWasNull = true;
                }

                if (Queue.Count > 0)
                {
                    if (isSongChanged || isCurrentSongWasNull)
                    {
                        // Sets Current Song
                        var item = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (item != null)
                        {
                            CurrentSong = (item as SongInfoEx);
                            CurrentSong.IsPlaying = true;

                            //CurrentSong.IsSelected = true;

                            if (IsAutoScrollToNowPlaying)
                                ScrollIntoViewAndSelect?.Invoke(this, CurrentSong.Index);

                            // AlbumArt
                            if (!String.IsNullOrEmpty(CurrentSong.File))
                            {
                                isAlbumArtChanged = true;
                            }
                        }
                        else
                        {
                            // TODO:
                            CurrentSong = null;

                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;
                        }
                    }
                }
                else
                {
                    // TODO:
                    CurrentSong = null;

                    IsAlbumArtVisible = false;
                    AlbumArt = _albumArtDefault;
                }
            });

            UpdateProgress?.Invoke(this, "");

            if (IsDownloadAlbumArt)
                if (CurrentSong != null)
                    if (isAlbumArtChanged)
                        await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
        }

        private async void UpdateCurrentSong()
        {
            bool isAlbumArtChanged = false;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                bool isSongChanged = false;
                bool isCurrentSongWasNull = false;

                if (CurrentSong != null)
                {
                    if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                    {
                        isSongChanged = true;

                        // Clear IsPlaying icon
                        CurrentSong.IsPlaying = false;

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }
                }
                else
                {
                    isCurrentSongWasNull = true;
                }

                if (_mpc.MpdCurrentSong != null)
                {
                    if (_mpc.MpdCurrentSong.Id == _mpc.MpdStatus.MpdSongID)
                    {
                        CurrentSong = _mpc.MpdCurrentSong;
                        CurrentSong.IsPlaying = true;

                        if (isSongChanged || isCurrentSongWasNull)
                        {
                            /*
                            if (Queue.Count > 0)
                                if (IsAutoScrollToNowPlaying)
                                    ScrollIntoView?.Invoke(this, CurrentSong.Index);
                            */

                            // AlbumArt
                            if (!String.IsNullOrEmpty(CurrentSong.File))
                            {
                                isAlbumArtChanged = true;
                            }
                        }
                    }
                }

            });

            if (IsDownloadAlbumArt)
                if (CurrentSong != null)
                    if (isAlbumArtChanged)
                        await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);

        }

        private async void UpdateCurrentQueue()
        {
            if (IsSwitchingProfile)
                return;

            bool isAlbumArtChanged = false;

            IsQueueFindVisible = false;

            if (Queue.Count > 0)
            {
                UpdateProgress?.Invoke(this, "[UI] Updating the queue...");
                await Task.Delay(20);

                if (IsSwitchingProfile)
                    return;

                IsWorking = true;

                try
                {
                    // The simplest way, but all the selections and listview position will be cleared. Kind of annoying when moving items.
                    
                    UpdateProgress?.Invoke(this, "[UI] Loading the queue...");
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Queue = new ObservableCollection<SongInfoEx>(_mpc.CurrentQueue);

                        UpdateProgress?.Invoke(this, "[UI] Checking current song after Queue update.");

                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem != null)
                        {
                            if (CurrentSong != null)
                            {
                                if (CurrentSong.Id != curitem.Id)
                                {
                                    CurrentSong = curitem;
                                    CurrentSong.IsPlaying = true;

                                    if (IsAutoScrollToNowPlaying)
                                        // ScrollIntoView while don't change the selection 
                                        ScrollIntoView?.Invoke(this, CurrentSong.Index);

                                    // AlbumArt
                                    if (_mpc.AlbumCover.SongFilePath != curitem.File)
                                    {
                                        IsAlbumArtVisible = false;
                                        AlbumArt = _albumArtDefault;

                                        if (!String.IsNullOrEmpty(CurrentSong.File))
                                        {
                                            //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                            isAlbumArtChanged = true;
                                        }
                                    }
                                }
                                else
                                {
                                    curitem.IsPlaying = true;

                                    if (IsAutoScrollToNowPlaying)
                                        // ScrollIntoView while don't change the selection 
                                        ScrollIntoView?.Invoke(this, curitem.Index);

                                }
                            }
                        }
                        else
                        {
                            CurrentSong = null;

                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;
                        }

                        UpdateProgress?.Invoke(this, "");

                        IsWorking = false;
                    });
                    UpdateProgress?.Invoke(this, "");
                    
                    /*
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsWorking = true;

                        UpdateProgress?.Invoke(this, "[UI] Updating the queue...");

                        // tmp list of deletion
                        List<SongInfoEx> _tmpQueue = new();

                        // deletes items that does not exists in the new queue. 
                        foreach (var sng in Queue)
                        {
                            UpdateProgress?.Invoke(this, "[UI] Queue list updating...(checking deleted items)");

                            IsWorking = true;

                            var queitem = _mpc.CurrentQueue.FirstOrDefault(i => i.Id == sng.Id);
                            if (queitem == null)
                            {
                                // add to tmp deletion list.
                                _tmpQueue.Add(sng);
                            }
                        }

                        // loop the tmp deletion list and remove.
                        foreach (var hoge in _tmpQueue)
                        {
                            UpdateProgress?.Invoke(this, "[UI] Queue list updating...(deletion)");

                            IsWorking = true;

                            Queue.Remove(hoge);
                        }

                        // update or add item from the new queue list.
                        foreach (var sng in _mpc.CurrentQueue)
                        {
                            UpdateProgress?.Invoke(this, string.Format("[UI] Queue list updating...(checking and adding new items {0})", sng.Id));

                            IsWorking = true;

                            var fuga = Queue.FirstOrDefault(i => i.Id == sng.Id);
                            if (fuga != null)
                            {
                                fuga.Pos = sng.Pos;
                                //fuga.Id = sng.Id;
                                fuga.LastModified = sng.LastModified;
                                //fuga.Time = sng.Time; // format exception が煩い。
                                fuga.Title = sng.Title;
                                fuga.Artist = sng.Artist;
                                fuga.Album = sng.Album;
                                fuga.AlbumArtist = sng.AlbumArtist;
                                fuga.Composer = sng.Composer;
                                fuga.Date = sng.Date;
                                fuga.Duration = sng.Duration;
                                fuga.File = sng.File;
                                fuga.Genre = sng.Genre;
                                fuga.Track = sng.Track;

                                fuga.Index = sng.Index;
                            }
                            else
                            {
                                Queue.Add(sng);
                            }
                        }

                        UpdateProgress?.Invoke(this, "");

                        // Sorting.
                        // Sort here because Queue list may have been re-ordered.
                        UpdateProgress?.Invoke(this, "[UI] Queue list sorting...");
                        var collectionView = CollectionViewSource.GetDefaultView(Queue);
                        // no need to add because It's been added when "loading".
                        //collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                        collectionView.Refresh();
                        UpdateProgress?.Invoke(this, "");
                        
                        // Sorting.
                        // This is not good because all the selections will be cleared.
                        //
                        //UpdateProgress?.Invoke(this, "[UI] Queue list sorting...");
                        //Queue = new ObservableCollection<SongInfoEx>(Queue.OrderBy(n => n.Index));
                        //UpdateProgress?.Invoke(this, "");
                        
                        UpdateProgress?.Invoke(this, "[UI] Checking current song after Queue update.");

                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem != null)
                        {
                            if (CurrentSong != null)
                            {
                                if (CurrentSong.Id != curitem.Id)
                                {
                                    CurrentSong = curitem;
                                    CurrentSong.IsPlaying = true;

                                    if (IsAutoScrollToNowPlaying)
                                        // ScrollIntoView while don't change the selection 
                                        ScrollIntoView?.Invoke(this, CurrentSong.Index);

                                    // AlbumArt
                                    if (_mpc.AlbumCover.SongFilePath != curitem.File)
                                    {
                                        IsAlbumArtVisible = false;
                                        AlbumArt = _albumArtDefault;

                                        if (!String.IsNullOrEmpty(CurrentSong.File))
                                        {
                                            //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                            isAlbumArtChanged = true;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            CurrentSong = null;

                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;
                        }

                        UpdateProgress?.Invoke(this, "");

                        IsWorking = false;
                    });
                    */
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception@UpdateCurrentQueue: " + e.Message);

                    UpdateProgress?.Invoke(this, "Exception@UpdateCurrentQueue: " + e.Message);

                    IsWorking = false;

                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            App app = App.Current as App;
                            app.AppendErrorLog("Exception@UpdateCurrentQueue", e.Message);
                        });
                    }

                    return;
                }

                IsWorking = false;
            }
            else
            {
                UpdateProgress?.Invoke(this, "[UI] Loading the queue...");
                await Task.Delay(20);

                if (IsSwitchingProfile)
                    return;

                IsWorking = true;

                try
                {
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        IsWorking = true;

                        UpdateProgress?.Invoke(this, "[UI] Loading the queue...");

                        Queue = new ObservableCollection<SongInfoEx>(_mpc.CurrentQueue);

                        UpdateProgress?.Invoke(this, "[UI] Queue checking current song...");

                        bool isNeedToFindCurrentSong = false;

                        if (CurrentSong != null)
                        {
                            if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                            {
                                isNeedToFindCurrentSong = true;
                            }
                            else
                            {
                                if (_mpc.MpdCurrentSong != null)
                                {
                                    // This means CurrentSong is already aquired by "currentsong" command.
                                    if (_mpc.MpdCurrentSong.Id == _mpc.MpdStatus.MpdSongID)
                                    {
                                        // the reason not to use CurrentSong is that it points different instance (set by "currentsong" command and currentqueue). 
                                        _mpc.MpdCurrentSong.IsPlaying = true;

                                        // just in case.
                                        CurrentSong.IsPlaying = true;
                                        // currentsong command does not return pos, so it's needed to be set.
                                        CurrentSong.Index = _mpc.MpdCurrentSong.Index;

                                        CurrentSong.IsSelected = true;

                                        if (IsAutoScrollToNowPlaying)
                                            // use ScrollIntoViewAndSelect instead of ScrollIntoView
                                            ScrollIntoViewAndSelect?.Invoke(this, CurrentSong.Index);
                                    }
                                }
                            }
                        }
                        else
                        {
                            isNeedToFindCurrentSong = true;
                        }

                        if (isNeedToFindCurrentSong)
                        {
                            // Set Current and NowPlaying.
                            var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                            if (curitem != null)
                            {
                                CurrentSong = curitem;
                                CurrentSong.IsPlaying = true;
                                CurrentSong.IsSelected = true;

                                if (IsAutoScrollToNowPlaying)
                                    // use ScrollIntoViewAndSelect instead of ScrollIntoView
                                    ScrollIntoViewAndSelect?.Invoke(this, CurrentSong.Index);

                                // AlbumArt
                                if (_mpc.AlbumCover.SongFilePath != curitem.File)
                                {
                                    IsAlbumArtVisible = false;
                                    AlbumArt = _albumArtDefault;

                                    if (!String.IsNullOrEmpty(CurrentSong.File))
                                    {
                                        isAlbumArtChanged = true;
                                    }
                                }
                            }
                            else
                            {
                                // just in case.
                                CurrentSong = null;

                                IsAlbumArtVisible = false;
                                AlbumArt = _albumArtDefault;
                            }
                        }

                        // Add SortDescription to the Listview.
                        UpdateProgress?.Invoke(this, "[UI] Queue list sorting...");
                        var collectionView = CollectionViewSource.GetDefaultView(Queue);
                        collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                        collectionView.Refresh();
                        UpdateProgress?.Invoke(this, "");


                        UpdateProgress?.Invoke(this, "");
                    });
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception@UpdateCurrentQueue: " + e.Message);

                    StatusBarMessage = "Exception@UpdateCurrentQueue: " + e.Message;

                    IsWorking = false;

                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            App app = App.Current as App;
                            app.AppendErrorLog("Exception@UpdateCurrentQueue", e.Message);
                        });
                    }

                    return;
                }

                IsWorking = false;
            }

            if (CurrentSong != null)
                if (IsDownloadAlbumArt)
                    if (isAlbumArtChanged)
                    {
                        //UpdateProgress?.Invoke(this, "[UI] Queue QueryAlbumArt.");
                        await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
                        //UpdateProgress?.Invoke(this, "");
                    }

        }

        private async void UpdatePlaylists()
        {
            if (IsSwitchingProfile)
                return;

            //IsBusy = true;
            IsWorking = true;

            UpdateProgress?.Invoke(this, "[UI] Playlists loading...");
            await Task.Delay(10);

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {

                UpdateProgress?.Invoke(this, "[UI] Playlists loading...");
                Playlists = new ObservableCollection<Playlist>(_mpc.Playlists);
                UpdateProgress?.Invoke(this, "");

                NodeMenuPlaylists playlistDir = _mainMenuItems.PlaylistsDirectory;

                if (playlistDir != null)
                {
                    // Sort playlists.
                    List<string> slTmp = new();

                    foreach (var v in _mpc.Playlists)
                    {
                        slTmp.Add(v.Name);
                    }
                    slTmp.Sort();

                    foreach (var hoge in slTmp)
                    {
                        var fuga = playlistDir.Children.FirstOrDefault(i => i.Name == hoge);
                        if (fuga == null)
                        {
                            NodeMenuPlaylistItem playlistNode = new(hoge);
                            playlistDir.Children.Add(playlistNode);
                        }
                    }

                    List<NodeTree> tobedeleted = new();
                    foreach (var hoge in playlistDir.Children)
                    {
                        var fuga = slTmp.FirstOrDefault(i => i == hoge.Name);
                        if (fuga == null)
                        {
                            tobedeleted.Add(hoge);
                        }
                        else
                        {
                            if (hoge is NodeMenuPlaylistItem)
                                (hoge as NodeMenuPlaylistItem).IsUpdateRequied = true;
                        }
                    }

                    foreach (var hoge in tobedeleted)
                    {
                        playlistDir.Children.Remove(hoge);
                    }

                    // 通知する
                    if (SelectedNodeMenu is NodeMenuPlaylistItem)
                    {
                        if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                        {
                            IsConfirmUpdatePlaylistSongsPopupVisible = true;
                        }
                    }
                }
            });

            //IsBusy = false;
            IsWorking = false;
        }

        private void UpdateLibrary()
        {
            Task.Run(() =>
            {
                UpdateLibraryMusic();
            });

            Task.Run(() =>
            {
                UpdateLibraryDirectories();
            });
        }

        private void UpdateLibraryMusic()
        {
            // Music files

            if (IsSwitchingProfile)
                return;

            UpdateProgress?.Invoke(this, "[UI] Library songs loading...");

            //IsBusy = true;
            IsWorking = true;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MusicEntries.Clear();
            });

            var tmpMusicEntries = new ObservableCollection<NodeFile>();

            foreach (var songfile in _mpc.LocalFiles)
            {
                if (IsSwitchingProfile)
                    break;

                //await Task.Delay(5);

                //if (IsSwitchingProfile)
                //    break;

                //IsBusy = true;
                IsWorking = true;

                if (string.IsNullOrEmpty(songfile.File)) continue;

                try
                {
                    Uri uri = new(@"file:///" + songfile.File);
                    if (uri.IsFile)
                    {
                        string filename = System.IO.Path.GetFileName(songfile.File);//System.IO.Path.GetFileName(uri.LocalPath);
                        NodeFile hoge = new(filename, uri, songfile.File);
                        /*
                        if (Application.Current == null) { return; }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MusicEntries.Add(hoge);
                        });
                        */
                        tmpMusicEntries.Add(hoge);
                    }

                    if (IsSwitchingProfile)
                        break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(songfile + e.Message);

                    //IsBusy = false;
                    IsWorking = false;

                    if (Application.Current != null)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            App app = App.Current as App;
                            app.AppendErrorLog("Exception@UpdateLibraryMusic", e.Message);
                        });
                    }

                    return;
                }
            }

            if (IsSwitchingProfile)
                return;

            //IsBusy = true;
            IsWorking = true;
            
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                UpdateProgress?.Invoke(this, "[UI] Library songs loading...");
                MusicEntries = tmpMusicEntries;
                UpdateProgress?.Invoke(this, "");
            });
            
            //IsBusy = false;
            IsWorking = false;
        }

        private void UpdateLibraryDirectories()
        {
            // Directories

            if (IsSwitchingProfile)
                return;

            UpdateProgress?.Invoke(this, "[UI] Library directories loading...");

            //IsBusy = true;
            IsWorking = true;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MusicDirectories.Clear();
            });

            try
            {
                var tmpMusicDirectories = new DirectoryTreeBuilder();
                /*
                await Task.Run(() =>
                {
                    //_musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());
                    tmpMusicDirectories.Load(_mpc.LocalDirectories.ToList<String>());
                });
                */
                tmpMusicDirectories.Load(_mpc.LocalDirectories.ToList<String>());

                IsWorking = true;

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    UpdateProgress?.Invoke(this, "[UI] Library directories loading...");
                    MusicDirectories = tmpMusicDirectories.Children;
                    UpdateProgress?.Invoke(this, "");
                });
                
                //_musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());

                if (MusicDirectories.Count > 0)
                {
                    SelectedNodeDirectory = MusicDirectories[0] as NodeDirectory;
                }

                //IsBusy = false;
                IsWorking = false;
            }
            catch (Exception e)
            {
                Debug.WriteLine("_musicDirectories.Load: " + e.Message);

                //IsBusy = false;
                IsWorking = false;

                if (Application.Current != null)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        App app = App.Current as App;
                        app.AppendErrorLog("Exception@UpdateLibraryDirectories", e.Message);
                    });
                }
            }

            //IsBusy = false;
            IsWorking = false;
        }

        private async void GetPlaylistSongs(NodeMenuPlaylistItem playlistNode)
        {
            if (playlistNode == null) 
                return;

            IsWorking = true;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (playlistNode.PlaylistSongs.Count > 0)
                    playlistNode.PlaylistSongs.Clear();
            });

            CommandPlaylistResult result = await _mpc.MpdQueryPlaylistSongs(playlistNode.Name);
            if (result.IsSuccess)
            {
                IsWorking = true;

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    playlistNode.PlaylistSongs = result.PlaylistSongs;

                    if (SelectedNodeMenu == playlistNode)
                    {
                        UpdateProgress?.Invoke(this, "[UI] Playlist loading...");
                        //PlaylistSongs = new ObservableCollection<SongInfo>(playlistNode.PlaylistSongs);
                        PlaylistSongs = playlistNode.PlaylistSongs;
                        UpdateProgress?.Invoke(this, "");
                        SelectedPlaylistSong = null;
                    }

                    playlistNode.IsUpdateRequied = false;
                });
            }

            IsWorking = false;
        }

        private async void GetLibrary(NodeMenuLibrary librarytNode)
        {
            if (librarytNode == null)
                return;

            if (librarytNode.IsAcquired)
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MusicEntries.Count > 0)
                MusicEntries.Clear();

                if (MusicDirectories.Count > 0)
                    MusicDirectories.Clear();
            });

            CommandResult result = await _mpc.MpdQueryListAll();
            if (result.IsSuccess)
            {
                librarytNode.IsAcquired = true;
                UpdateLibrary();
            }
        }

        private static int CompareVersionString(string a, string b)
        {
            return (new System.Version(a)).CompareTo(new System.Version(b));
        }

        #endregion

        #region == Events ==

        private void OnMpdIdleConnected(MPC sender)
        {
            Debug.WriteLine("OK MPD " + _mpc.MpdVerText + " @OnMpdConnected");

            MpdVersion = _mpc.MpdVerText;

            MpdStatusButton = _pathMpdOkButton;

            // Need this to show CurrentSong.
            IsConnected = true;

            // Run in a background thread.
            Task.Run(() => LoadInitialData());
        }

        private async void LoadInitialData()
        {
            IsBusy = true;

            await Task.Delay(5);

            CommandResult result = await _mpc.MpdIdleSendPassword(_password);

            if (result.IsSuccess)
            {
                bool r = await _mpc.MpdCommandConnectionStart(_mpc.MpdHost, _mpc.MpdPort, _mpc.MpdPassword);

                if (r)
                {
                    if (IsUpdateOnStartup)
                    {
                        await _mpc.MpdSendUpdate();
                    }

                    result = await _mpc.MpdIdleQueryStatus();

                    if (result.IsSuccess)
                    {
                        await Task.Delay(5);
                        UpdateStatus();

                        await Task.Delay(5);
                        await _mpc.MpdIdleQueryCurrentSong();

                        await Task.Delay(5);
                        UpdateCurrentSong();

                        await Task.Delay(5);
                        await _mpc.MpdIdleQueryPlaylists();

                        await Task.Delay(5);
                        UpdatePlaylists();

                        await Task.Delay(5);
                        await _mpc.MpdIdleQueryCurrentQueue();

                        await Task.Delay(5);
                        UpdateCurrentQueue();

                        await Task.Delay(5);

                        // This no longer needed since it is aquired as needed basis.
                        //await _mpc.MpdIdleQueryListAll();
                        //await Task.Delay(5);
                        //UpdateLibrary();

                        //UpdateProgress?.Invoke(this, "");

                        _mpc.MpdIdleStart();
                    }

                }
            }

            IsBusy = false;

            await Task.Delay(500);

            // MPD protocol ver check.
            if (_mpc.MpdVerText != "")
            {
                if (CompareVersionString(_mpc.MpdVerText, "0.20.0") == -1)
                {
                    MpdStatusButton = _pathMpdAckErrorButton;
                    //StatusBarMessage = string.Format(MPDCtrl.Properties.Resources.StatusBarMsg_MPDVersionIsOld, _mpc.MpdVerText);
                    MpdStatusMessage = string.Format(MPDCtrl.Properties.Resources.StatusBarMsg_MPDVersionIsOld, _mpc.MpdVerText);
                }
            }
        }

        private void OnMpdPlayerStatusChanged(MPC sender)
        {
            if (_mpc.MpdStatus.MpdError != "")
            {
                MpdStatusMessage = MpdVersion + ": " + MPDCtrl.Properties.Resources.MPD_StatusError + " - " + _mpc.MpdStatus.MpdError;
                MpdStatusButton = _pathMpdAckErrorButton;
            }
            else
            {
                MpdStatusMessage = "";
                MpdStatusButton = _pathMpdOkButton;
            }

            UpdateStatus();
        }

        private void OnMpdCurrentQueueChanged(MPC sender)
        {
            UpdateCurrentQueue();
        }

        private void OnMpdPlaylistsChanged(MPC sender)
        {
            UpdatePlaylists();
        }

        private void OnAlbumArtChanged(MPC sender)
        {
            // AlbumArt
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                if ((!_mpc.AlbumCover.IsDownloading) && _mpc.AlbumCover.IsSuccess)
                {
                    if ((CurrentSong != null) && (_mpc.AlbumCover.AlbumImageSource != null))
                    {
                        if (!String.IsNullOrEmpty(CurrentSong.File))
                        {
                            if (CurrentSong.File == _mpc.AlbumCover.SongFilePath)
                            {
                                //AlbumArt = null;
                                AlbumArt = _mpc.AlbumCover.AlbumImageSource;
                                IsAlbumArtVisible = true;
                            }
                        }
                    }
                }
            });
        }

        private void OnDebugCommandOutput(MPC sender, string data)
        {
            if (IsShowDebugWindow)
            {
                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugCommandOutput?.Invoke(this, data);
                });
            }
        }

        private void OnDebugIdleOutput(MPC sender, string data)
        {
            if (IsShowDebugWindow)
            {
                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugIdleOutput?.Invoke(this, data);
                });
            }
        }

        private void OnConnectionError(MPC sender, string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            IsConnected = false;
            IsConnecting = false;
            IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + msg;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
        }

        private void OnConnectionStatusChanged(MPC sender, MPC.ConnectionStatus status)
        {

            if (status == MPC.ConnectionStatus.NeverConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                StatusButton = _pathDisconnectedButton;
            }
            else if (status == MPC.ConnectionStatus.Connected)
            {
                IsConnected = true;
                IsConnecting = false;
                IsNotConnectingNorConnected = false;
                IsConnectionSettingShow = false;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Connected;
                StatusButton = _pathConnectedButton;
            }
            else if (status == MPC.ConnectionStatus.Connecting)
            {
                IsConnected = false;
                IsConnecting = true;
                IsNotConnectingNorConnected = false;
                //IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Connecting;
                StatusButton = _pathConnectingButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.ConnectFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_ConnectFail_Timeout");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectFail_Timeout;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.SeeConnectionErrorEvent)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_SeeConnectionErrorEvent");
                StatusButton = _pathErrorInfoButton;
            }
            else if (status == MPC.ConnectionStatus.Disconnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_Disconnected");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Disconnected;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.DisconnectedByHost)
            {
                // TODO: not really usued now...

                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_DisconnectedByHost");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByHost;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.Disconnecting)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = false;
                //IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Disconnecting;
                StatusButton = _pathConnectingButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.DisconnectedByUser)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                //IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_DisconnectedByUser");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByUser;
                StatusButton = _pathDisconnectedButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.SendFail_NotConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_SendFail_NotConnected");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.SendFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_SendFail_Timeout");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_Timeout;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
        }

        private void OnMpdAckError(MPC sender, string ackMsg)
        {
            if (string.IsNullOrEmpty(ackMsg))
                return;
            
            string s = ackMsg;
            string patternStr = @"[\[].+?[\]]";//@"[{\[].+?[}\]]";
            s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
            s = s.Replace("ACK ", string.Empty);
            s = s.Replace("{} ", string.Empty);

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                AckWindowOutput?.Invoke(this, MpdVersion + ": " + MPDCtrl.Properties.Resources.MPD_CommandError + " - " + s + Environment.NewLine);
            });
            
            IsShowAckWindow = true;
        }

        private void OnMpcProgress(MPC sender, string msg)
        {
            StatusBarMessage = msg;
        }

        private void OnUpdateProgress(string msg)
        {
            StatusBarMessage = msg;
        }

        private void OnMpcIsBusy(MPC sender, bool on)
        {
            this.IsBusy = on;
        }

        #endregion

        #region == Timers ==

        private readonly System.Timers.Timer _elapsedTimer;
        private void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_elapsed < _time) && (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play))
            {
                _elapsed += 0.5;
                NotifyPropertyChanged(nameof(Elapsed));
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        #endregion

        #region == Commands ==

        #region == Playback play ==

        public ICommand PlayCommand { get; }
        public bool PlayCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlayCommand_ExecuteAsync()
        {
            switch (_mpc.MpdStatus.MpdState)
            {
                case Status.MpdPlayState.Play:
                    {
                        //State>>Play: So, send Pause command
                        await _mpc.MpdPlaybackPause();
                        break;
                    }
                case Status.MpdPlayState.Pause:
                    {
                        //State>>Pause: So, send Resume command
                        await _mpc.MpdPlaybackResume(Convert.ToInt32(_volume));
                        break;
                    }
                case Status.MpdPlayState.Stop:
                    {
                        //State>>Stop: So, send Play command
                        await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume));
                        break;
                    }
            }

        }

        public ICommand PlayNextCommand { get; }
        public bool PlayNextCommand_CanExecute()
        {
            if (IsBusy) return false;
            //if (Queue.Count < 1) { return false; }
            return true;
        }
        public async void PlayNextCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackNext(Convert.ToInt32(_volume));
        }

        public ICommand PlayPrevCommand { get; }
        public bool PlayPrevCommand_CanExecute()
        {
            if (IsBusy) return false;
            //if (Queue.Count < 1) { return false; }
            return true;
        }
        public async void PlayPrevCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackPrev(Convert.ToInt32(_volume));
        }

        public ICommand ChangeSongCommand { get; set; }
        public bool ChangeSongCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count < 1) { return false; }
            if (_selectedQueueSong == null) { return false; }
            return true;
        }
        public async void ChangeSongCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume), _selectedQueueSong.Id);
        }

        public ICommand PlayPauseCommand { get; }
        public bool PlayPauseCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlayPauseCommand_Execute()
        {
            await _mpc.MpdPlaybackPause();
        }

        public ICommand PlayStopCommand { get; }
        public bool PlayStopCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlayStopCommand_Execute()
        {
            await _mpc.MpdPlaybackStop();
        }

        #endregion

        #region == Playback opts ==

        public ICommand SetRandomCommand { get; }
        public bool SetRandomCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetRandomCommand_ExecuteAsync()
        {
            await _mpc.MpdSetRandom(_random);
        }

        public ICommand SetRpeatCommand { get; }
        public bool SetRpeatCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetRpeatCommand_ExecuteAsync()
        {
            await _mpc.MpdSetRepeat(_repeat);
        }

        public ICommand SetConsumeCommand { get; }
        public bool SetConsumeCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetConsumeCommand_ExecuteAsync()
        {
            await _mpc.MpdSetConsume(_consume);
        }

        public ICommand SetSingleCommand { get; }
        public bool SetSingleCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetSingleCommand_ExecuteAsync()
        {
            await _mpc.MpdSetSingle(_single);
        }

        #endregion

        #region == Playback seek and volume ==

        public ICommand SetVolumeCommand { get; }
        public bool SetVolumeCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetVolumeCommand_ExecuteAsync()
        {
            await _mpc.MpdSetVolume(Convert.ToInt32(_volume));
        }

        public ICommand SetSeekCommand { get; }
        public bool SetSeekCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetSeekCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackSeek(_mpc.MpdStatus.MpdSongID, Convert.ToInt32(_elapsed));
        }

        public ICommand VolumeMuteCommand { get; }
        public bool VolumeMuteCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void VolumeMuteCommand_Execute()
        {
            await _mpc.MpdSetVolume(0);
        }

        public ICommand VolumeDownCommand { get; }
        public bool VolumeDownCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void VolumeDownCommand_Execute()
        {
            if (_volume >= 10)
            {
                await _mpc.MpdSetVolume(Convert.ToInt32(_volume - 10));
            }
        }

        public ICommand VolumeUpCommand { get; }
        public bool VolumeUpCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void VolumeUpCommand_Execute()
        {
            if (_volume <= 90)
            {
                await _mpc.MpdSetVolume(Convert.ToInt32(_volume + 10));
            }
        }

        #endregion

        #region == Queue ==

        public ICommand QueueListviewSaveAsCommand { get; }
        public bool QueueListviewSaveAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public void QueueListviewSaveAsCommand_ExecuteAsync()
        {
            IsSaveAsPlaylistPopupVisible = true;
        }

        public ICommand QueueListviewSaveAsPopupCommand { get; }
        public bool QueueListviewSaveAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public async void QueueListviewSaveAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            await _mpc.MpdSave(playlistName);
            
            IsSaveAsPlaylistPopupVisible = false;
        }

        public ICommand QueueListviewEnterKeyCommand { get; set; }
        public bool QueueListviewEnterKeyCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count < 1) { return false; }
            if (_selectedQueueSong == null) { return false; }
            return true;
        }
        public async void QueueListviewEnterKeyCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume), _selectedQueueSong.Id);
        }

        public ICommand QueueListviewLeftDoubleClickCommand { get; set; }
        public bool QueueListviewLeftDoubleClickCommand_CanExecute()
        {
            if (IsBusy) return false;
            //if (IsWorking) return false;
            if (Queue.Count < 1) { return false; }
            if (_selectedQueueSong == null) { return false; }
            return true;
        }
        public async void QueueListviewLeftDoubleClickCommand_ExecuteAsync(SongInfoEx song)
        {
            await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume), song.Id);
        }

        public ICommand QueueListviewClearCommand { get; }
        public bool QueueListviewClearCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) { return false; }
            return true;
        }
        public void QueueListviewClearCommand_ExecuteAsync()
        {
            IsConfirmClearQueuePopupVisible = true;
        }

        public ICommand QueueListviewConfirmClearPopupCommand { get; }
        public bool QueueListviewConfirmClearPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public async void QueueListviewConfirmClearPopupCommand_Execute()
        {
            await _mpc.MpdClear();

            IsConfirmClearQueuePopupVisible = false;
        }

        public ICommand QueueListviewDeleteCommand { get; }
        public bool QueueListviewDeleteCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public void QueueListviewDeleteCommand_Execute(object obj)
        {
            if (obj == null) return;

            List<SongInfoEx> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            List<string> deleteIdList = new();

            foreach (var item in selectedList)
            {
                deleteIdList.Add(item.Id);
            }

            queueListviewSelectedQueueSongIdsForPopup = deleteIdList;

            IsConfirmDeleteQueuePopupVisible = true;
        }

        public ICommand QueueListviewConfirmDeletePopupCommand { get; }
        public bool QueueListviewConfirmDeletePopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (SelectedQueueSong == null) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public async void QueueListviewConfirmDeletePopupCommand_Execute()
        {
            if (queueListviewSelectedQueueSongIdsForPopup.Count < 1)
                return;

            if (queueListviewSelectedQueueSongIdsForPopup.Count == 1)
                await _mpc.MpdDeleteId(queueListviewSelectedQueueSongIdsForPopup[0]);
            else if (queueListviewSelectedQueueSongIdsForPopup.Count >= 1)
                await _mpc.MpdDeleteId(queueListviewSelectedQueueSongIdsForPopup);

            queueListviewSelectedQueueSongIdsForPopup.Clear();

            IsConfirmDeleteQueuePopupVisible = false;
        }

        public ICommand QueueListviewMoveUpCommand { get; }
        public bool QueueListviewMoveUpCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewMoveUpCommand_Execute(object obj)
        {
            if (obj == null) return;

            if (Queue.Count <= 1)
                return;

            List<SongInfoEx> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            Dictionary<string, string> IdToNewPos = new();

            foreach (var item in selectedList)
            {
                int i = 0;
                try
                {
                    i = Int32.Parse(item.Pos);

                    if (i == 0) return;

                    i -= 1;

                    IdToNewPos.Add(item.Id, i.ToString());
                }
                catch
                {
                    return;
                }
            }

            await _mpc.MpdMoveId(IdToNewPos);
        }

        public ICommand QueueListviewMoveDownCommand { get; }
        public bool QueueListviewMoveDownCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewMoveDownCommand_Execute(object obj)
        {
            if (obj == null) return;

            if (Queue.Count <= 1)
                return;

            List<SongInfoEx> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            Dictionary<string, string> IdToNewPos = new();

            foreach (var item in selectedList)
            {
                int i = 0;
                try
                {
                    i = Int32.Parse(item.Pos);

                    if (i >= Queue.Count-1) return;

                    i += 1;

                    IdToNewPos.Add(item.Id, i.ToString());
                }
                catch
                {
                    return;
                }
            }

            await _mpc.MpdMoveId(IdToNewPos);
        }

        public ICommand QueueListviewSaveSelectedAsCommand { get; }
        public bool QueueListviewSaveSelectedAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public void QueueListviewSaveSelectedAsCommand_Execute(object obj)
        {
            if (obj == null) return;

            List<SongInfoEx> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            List<string> fileUrisToAddList = new();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            queueListviewSelectedQueueSongIdsForPopup = fileUrisToAddList;

            IsSelectedSaveAsPopupVisible = true;

        }

        public ICommand QueueListviewSaveSelectedAsPopupCommand { get; }
        public bool QueueListviewSaveSelectedAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewSaveSelectedAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            if (queueListviewSelectedQueueSongIdsForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlistName, queueListviewSelectedQueueSongIdsForPopup);

            queueListviewSelectedQueueSongIdsForPopup.Clear();

            IsSelectedSaveAsPopupVisible = false;
        }

        public ICommand QueueListviewSaveSelectedToPopupCommand { get; }
        public bool QueueListviewSaveSelectedToPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewSaveSelectedToPopupCommand_Execute(Playlist playlist)
        {
            if (playlist == null)
                return;

            if (string.IsNullOrEmpty(playlist.Name))
                return;

            if (queueListviewSelectedQueueSongIdsForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlist.Name, queueListviewSelectedQueueSongIdsForPopup);

            queueListviewSelectedQueueSongIdsForPopup.Clear();

            IsSelectedSaveToPopupVisible = false;
        }

        public ICommand QueueListviewSaveSelectedToCommand { get; }
        public bool QueueListviewSaveSelectedToCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public void QueueListviewSaveSelectedToCommand_Execute(object obj)
        {
            if (obj == null) return;

            List<SongInfoEx> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            List<string> fileUrisToAddList = new();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            queueListviewSelectedQueueSongIdsForPopup = fileUrisToAddList;

            IsSelectedSaveToPopupVisible = true;

        }

        public ICommand ScrollIntoNowPlayingCommand { get; }
        public bool ScrollIntoNowPlayingCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (Queue.Count == 0) { return false; }
            if (CurrentSong == null) { return false; }
            return true;
        }
        public void ScrollIntoNowPlayingCommand_Execute()
        {
            if (Queue.Count == 0) return;
            if (CurrentSong == null) return;
            if (Queue.Count < CurrentSong.Index + 1) return;

            // should I?
            //SelectedQueueSong = CurrentSong;

            ScrollIntoView?.Invoke(this, CurrentSong.Index);
        }

        public ICommand FilterQueueClearCommand { get; }
        public bool FilterQueueClearCommand_CanExecute()
        {
            if (string.IsNullOrEmpty(FilterQueueQuery))
                return false;
            return true;
        }
        public void FilterQueueClearCommand_Execute()
        {
            FilterQueueQuery = "";
        }

        public ICommand QueueFindShowHideCommand { get; }
        public static bool QueueFindShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueFindShowHideCommand_Execute()
        {
            if (IsQueueFindVisible)
            {
                IsQueueFindVisible = false;
            }
            else
            {
                QueueForFilter.Clear();
                QueueForFilter =  new ObservableCollection<SongInfoEx>(Queue);
                
                var collectionView = CollectionViewSource.GetDefaultView(QueueForFilter);
                collectionView.Filter = x =>
                {
                    return false;
                };
                
                FilterQueueQuery = "";

                IsQueueFindVisible = true;
            }
        }

        #endregion

        #region == Search ==

        public ICommand SearchExecCommand { get; }
        public bool SearchExecCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (string.IsNullOrEmpty(SearchQuery))
                return false;
            return true;
        }
        public async void SearchExecCommand_Execute()
        {
            if (string.IsNullOrEmpty(SearchQuery)) return;

            // TODO: Make "==" an option in search.
            //"==";

            string queryShiki = "contains";

            await _mpc.MpdSearch(SelectedSearchTags.ToString(), queryShiki, SearchQuery);

            UpdateProgress?.Invoke(this, "");
        }

        public ICommand SearchResultListviewSaveSelectedAsCommand { get; }
        public bool SearchResultListviewSaveSelectedAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public void SearchResultListviewSaveSelectedAsCommand_Execute(object obj)
        {
            if (obj == null) return;

            List<SongInfo> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            List<string> fileUrisToAddList = new();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            searchResultListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSearchResultSelectedSaveAsPopupVisible = true;
        }

        public ICommand SearchResultListviewSaveSelectedAsPopupCommand { get; }
        public bool SearchResultListviewSaveSelectedAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public async void SearchResultListviewSaveSelectedAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            if (searchResultListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlistName, searchResultListviewSelectedQueueSongUriForPopup);

            searchResultListviewSelectedQueueSongUriForPopup.Clear();

            IsSearchResultSelectedSaveAsPopupVisible = false;
        }

        public ICommand SearchResultListviewSaveSelectedToCommand { get; }
        public bool SearchResultListviewSaveSelectedToCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public void SearchResultListviewSaveSelectedToCommand_Execute(object obj)
        {
            if (obj == null) return;

            List<SongInfo> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            List<string> fileUrisToAddList = new();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            searchResultListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSearchResultSelectedSaveToPopupVisible = true;

        }

        public ICommand SearchResultListviewSaveSelectedToPopupCommand { get; }
        public bool SearchResultListviewSaveSelectedToPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public async void SearchResultListviewSaveSelectedToPopupCommand_Execute(Playlist playlist)
        {
            if (playlist == null)
                return;

            if (string.IsNullOrEmpty(playlist.Name))
                return;

            if (searchResultListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlist.Name, searchResultListviewSelectedQueueSongUriForPopup);

            searchResultListviewSelectedQueueSongUriForPopup.Clear();

            IsSearchResultSelectedSaveToPopupVisible = false;
        }

        #endregion

        #region == Library ==

        public ICommand SongFilesListviewAddCommand { get; }
        public bool SongFilesListviewAddCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public async void SongFilesListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<NodeFile>();

                List<String> uriList = new();

                foreach (var item in collection)
                {
                    uriList.Add((item as NodeFile).OriginalFileUri);
                }

                await _mpc.MpdAdd(uriList);
            }
            else
            {
                if (items.Count == 1)
                    await _mpc.MpdAdd((items[0] as NodeFile).OriginalFileUri);
            }
        }

        public ICommand SongFilesListviewSaveSelectedAsCommand { get; }
        public bool SongFilesListviewSaveSelectedAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public void SongFilesListviewSaveSelectedAsCommand_Execute(object obj)
        {
            if (obj == null) return;

            List<NodeFile> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<NodeFile>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as NodeFile);
                }
            });

            List<string> fileUrisToAddList = new();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.OriginalFileUri))
                    fileUrisToAddList.Add(item.OriginalFileUri);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            songFilesListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSongFilesSelectedSaveAsPopupVisible = true;
        }

        public ICommand SongFilesListviewSaveSelectedAsPopupCommand { get; }
        public bool SongFilesListviewSaveSelectedAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public async void SongFilesListviewSaveSelectedAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            if (songFilesListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlistName, songFilesListviewSelectedQueueSongUriForPopup);

            songFilesListviewSelectedQueueSongUriForPopup.Clear();

            IsSongFilesSelectedSaveAsPopupVisible = false;
        }

        public ICommand SongFilesListviewSaveSelectedToCommand { get; }
        public bool SongFilesListviewSaveSelectedToCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public void SongFilesListviewSaveSelectedToCommand_Execute(object obj)
        {
            if (obj == null) return;

            List<NodeFile> selectedList = new();

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<NodeFile>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as NodeFile);
                }
            });

            List<string> fileUrisToAddList = new();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.OriginalFileUri))
                    fileUrisToAddList.Add(item.OriginalFileUri);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            songFilesListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSongFilesSelectedSaveToPopupVisible = true;

        }

        public ICommand SongFilesListviewSaveSelectedToPopupCommand { get; }
        public bool SongFilesListviewSaveSelectedToPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public async void SongFilesListviewSaveSelectedToPopupCommand_Execute(Playlist playlist)
        {
            if (playlist == null)
                return;

            if (string.IsNullOrEmpty(playlist.Name))
                return;

            if (songFilesListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlist.Name, songFilesListviewSelectedQueueSongUriForPopup);

            songFilesListviewSelectedQueueSongUriForPopup.Clear();

            IsSongFilesSelectedSaveToPopupVisible = false;
        }

        public ICommand FilterMusicEntriesClearCommand { get; }
        public bool FilterMusicEntriesClearCommand_CanExecute()
        {
            if (string.IsNullOrEmpty(FilterMusicEntriesQuery))
                return false;
            return true;
        }
        public void FilterMusicEntriesClearCommand_Execute()
        {
            FilterMusicEntriesQuery = "";
        }

        #endregion

        #region == Playlists ==

        public ICommand ChangePlaylistCommand { get; set; }
        public bool ChangePlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (_selectedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;
            return true;
        }
        public async void ChangePlaylistCommand_ExecuteAsync()
        {
            if (_selectedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            await _mpc.MpdChangePlaylist(_selectedPlaylist.Name);
        }

        public ICommand PlaylistListviewLeftDoubleClickCommand { get; set; }
        public bool PlaylistListviewLeftDoubleClickCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (_selectedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(Playlist playlist)
        {
            if (_selectedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            if (_selectedPlaylist != playlist)
                return;

            await _mpc.MpdLoadPlaylist(playlist.Name);
        }

        public ICommand PlaylistListviewEnterKeyCommand { get; set; }
        public bool PlaylistListviewEnterKeyCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (_selectedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewEnterKeyCommand_ExecuteAsync()
        {
            if (_selectedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            await _mpc.MpdLoadPlaylist(_selectedPlaylist.Name);
        }

        public ICommand PlaylistListviewLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewLoadPlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (_selectedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selectedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            await _mpc.MpdLoadPlaylist(_selectedPlaylist.Name);
        }

        public ICommand PlaylistListviewClearLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewClearLoadPlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            if (_selectedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selectedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            await _mpc.MpdChangePlaylist(_selectedPlaylist.Name);
        }

        // TODO: Rename playlists.
        public ICommand PlaylistListviewRenamePlaylistCommand { get; set; }
        public bool PlaylistListviewRenamePlaylistCommand_CanExecute()
        {
            if (IsBusy) 
                return false;
            if (IsWorking) return false;
            if (_selectedPlaylist == null) 
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;

            return true;
        }
        public void PlaylistListviewRenamePlaylistCommand_Execute(Playlist playlist)
        {
            if (_selectedPlaylist == null) 
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            if (_selectedPlaylist != playlist)
                return;
        }
        public async void DoRenamePlaylist(String oldPlaylistName, String newPlaylistName)
        {
            await _mpc.MpdRenamePlaylist(oldPlaylistName, newPlaylistName);
        }

        // TODO: CheckPlaylistNameExists when Rename playlists.
        /*
        private bool CheckPlaylistNameExists(string playlistName)
        {
            bool match = false;

            if (Playlists.Count > 0)
            {

                foreach (var hoge in Playlists)
                {
                    if (hoge.Name.ToLower() == playlistName.ToLower())
                    {
                        match = true;
                        break;
                    }
                }
            }

            return match;
        }
        */

        public ICommand PlaylistListviewRemovePlaylistCommand { get; set; }
        public bool PlaylistListviewRemovePlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selectedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;
            return true;
        }
        public void PlaylistListviewRemovePlaylistCommand_Execute(Playlist playlist)
        {
            if (_selectedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            if (_selectedPlaylist != playlist)
                return;

            IsConfirmDeletePlaylistPopupVisible = true;
        }

        public ICommand PlaylistListviewConfirmRemovePlaylistPopupCommand { get; set; }
        public bool PlaylistListviewConfirmRemovePlaylistPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selectedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewConfirmRemovePlaylistPopupCommand_Execute()
        {
            if (_selectedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selectedPlaylist.Name))
                return;

            await _mpc.MpdRemovePlaylist(_selectedPlaylist.Name);

            IsConfirmDeletePlaylistPopupVisible = false;
        }

        #endregion

        #region == PlaylistItems ==

        // Do reload after confirming to reload playlist.
        public ICommand PlaylistListviewConfirmUpdatePopupCommand { get; set; }
        public bool PlaylistListviewConfirmUpdatePopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public void PlaylistListviewConfirmUpdatePopupCommand_Execute()
        {
            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    GetPlaylistSongs((SelectedNodeMenu as NodeMenuPlaylistItem));
                }
            }

            IsConfirmUpdatePlaylistSongsPopupVisible = false;
        }

        // Deletes song in a playlist.
        public ICommand PlaylistListviewDeletePosCommand { get; set; }
        public bool PlaylistListviewDeletePosCommand_CanExecute()
        {
            if (SelectedPlaylistSong == null) return false;
            if (IsBusy) return false;
            return true;
        }
        public void PlaylistListviewDeletePosCommand_Execute(object obj)
        {
            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                // not supported by MPD protocol error.
                IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible = true;
            }
            else
            {
                if (items.Count == 1)
                    IsConfirmDeletePlaylistSongPopupVisible = true;
                    //await _mpc.MpdPlaylistDelete(playlistName, (items[0] as SongInfo).Index);
            }
        }

        // 
        public ICommand PlaylistListviewConfirmDeletePosNotSupportedPopupCommand { get; set; }
        public static bool PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_CanExecute()
        {
            return true;
        }
        public void PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_Execute()
        {
            IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible = false;
        }

        // 
        public ICommand PlaylistListviewDeletePosPopupCommand { get; set; }
        public bool PlaylistListviewDeletePosPopupCommand_CanExecute()
        {
            if (SelectedPlaylistSong == null) return false;
            if (IsBusy) return false;
            return true;
        }
        public async void PlaylistListviewDeletePosPopupCommand_Execute()
        {
            string playlistName;

            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    return;
                }
                else
                {
                    playlistName = (SelectedNodeMenu as NodeMenuPlaylistItem).Name;
                }
            }
            else
            {
                return;
            }

            if (SelectedPlaylistSong == null)
                return;

            await _mpc.MpdPlaylistDelete(playlistName, SelectedPlaylistSong.Index);

            IsConfirmDeletePlaylistSongPopupVisible = false;
        }

        // 
        public ICommand PlaylistListviewClearCommand { get; set; }
        public bool PlaylistListviewClearCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public void PlaylistListviewClearCommand_Execute()
        {
            IsConfirmPlaylistClearPopupVisible = true;
        }

        public ICommand PlaylistListviewClearPopupCommand { get; set; }
        public bool PlaylistListviewClearPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlaylistListviewClearPopupCommand_Execute()
        {
            string playlistName;

            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    return;
                }
                else
                {
                    playlistName = (SelectedNodeMenu as NodeMenuPlaylistItem).Name;
                }
            }
            else
            {
                return;
            }

            await _mpc.MpdPlaylistClear(playlistName);

            IsConfirmPlaylistClearPopupVisible = false;
        }

        #endregion

        #region == Search and PlaylistItems ==

        public ICommand SongsListviewAddCommand { get; }
        public bool SongsListviewAddCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (IsWorking) return false;
            return true;
        }
        public async void SongsListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<SongInfo>();

                List<String> uriList = new();

                foreach (var item in collection)
                {
                    uriList.Add((item as SongInfo).File);
                }

                await _mpc.MpdAdd(uriList);
            }
            else
            {
                if (items.Count == 1)
                    await _mpc.MpdAdd((items[0] as SongInfo).File);
            }
        }

        #endregion

        #region == Settings ==

        public ICommand ShowSettingsCommand { get; }
        public bool ShowSettingsCommand_CanExecute()
        {
            if (IsConnecting) return false;
            return true;
        }
        public void ShowSettingsCommand_Execute()
        {
            if (IsConnecting) return;

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
        public static bool SettingsOKCommand_CanExecute()
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
                SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileDeleted + " (" + tmpNama + ")";

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

            Profile pro = new();
            pro.Host = _host;
            pro.Port = _port;

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                Password = passwordBox.Password;
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
            NotifyPropertyChanged(nameof(IsCurrentProfileSet));

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
        public void UpdateProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (SelectedProfile == null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (_port == 0) return;

            SelectedProfile.Host = _host;
            SelectedProfile.Port = _port;

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                SelectedProfile.Password = passwordBox.Password;
                Password = passwordBox.Password;

                if (SelectedProfile == CurrentProfile)
                {
                    // No need since _mpc uses password when it connects.
                    //_mpc.MpdPassword = passwordBox.Password;
                }
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

        #endregion

        #region == Connection ==

        public ICommand ChangeConnectionProfileCommand { get; }
        public bool ChangeConnectionProfileCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(Host)) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (IsConnecting) return false;
            if ((SelectedProfile != null) && CurrentProfile == null) return false;
            return true;
        }
        public async void ChangeConnectionProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (string.IsNullOrWhiteSpace(Host)) return;
            if (_port == 0) return;
            if (IsConnecting) return;
            if (IsBusy) return;
            if (IsWorking) return;

            IsSwitchingProfile = true;

            if (IsConnected)
            {
                _mpc.MpdStop = true;
                _mpc.MpdDisconnect();
                _mpc.MpdStop = false;
            }

            // Save volume.
            if (CurrentProfile != null)
                CurrentProfile.Volume = Convert.ToInt32(Volume);

            // Validate Host input.
            if (Host == "")
            {
                SetError(nameof(Host), "Error: Host must be specified."); //TODO: translate
                NotifyPropertyChanged(nameof(Host));
                return;
            }
            else
            {
                /*
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
                        ClearError(nameof(Host));
                    }
                }
                catch
                {
                    //System.FormatException
                    SetError(nameof(Host), "Error: Invalid address format."); //TODO: translate

                    return;
                }
                */
            }

            HostIpAddress = null;
            try
            {
                var addresses = await Dns.GetHostAddressesAsync(Host);
                if (addresses.Count() > 0)
                {
                    HostIpAddress = addresses[0];
                }
                else
                {
                    //TODO: translate.
                    SetError(nameof(Host), "Error: Could not retrive IP Address from the hostname.");
                    NotifyPropertyChanged(nameof(Host));
                    // TODO::
                    ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
                    StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";
                    return;
                }
            }
            catch(Exception)
            {
                //TODO: translate.
                SetError(nameof(Host), "Error: Could not retrive IP Address from the hostname. (SocketException)");
                NotifyPropertyChanged(nameof(Host));
                // TODO::
                ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
                StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";
                return;
            }

            if (_port == 0)
            {
                //TODO: translate.
                SetError(nameof(Port), "Error: Port must be specified.");
                NotifyPropertyChanged(nameof(Host));
                return;
            }

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                Password = passwordBox.Password;
            }

            // Clear current...
            if (CurrentSong != null)
            {
                CurrentSong.IsPlaying = false;
                CurrentSong = null;
            }
            if (CurrentSong != null)
            {
                SelectedQueueSong = null;
            }

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedNodeMenu = null;

                Queue.Clear();
                _mpc.CurrentQueue.Clear();

                _mpc.MpdStatus.Reset();

                if (_mainMenuItems.PlaylistsDirectory != null)
                    _mainMenuItems.PlaylistsDirectory.Children.Clear();
                
                Playlists.Clear();
                _mpc.Playlists.Clear();
                SelectedPlaylist = null;

                SelectedPlaylistSong = null;

                if (_mainMenuItems.LibraryDirectory != null)
                    _mainMenuItems.LibraryDirectory.IsAcquired = false;

                MusicEntries.Clear();

                //MusicDirectories.Clear();// Don't
                //_mpc.LocalDirectories.Clear();// Don't
                //_mpc.LocalFiles.Clear();// Don't
                //SelectedNodeDirectory.Children.Clear();// Don't
                _musicDirectories.IsCanceled = true;
                if (_musicDirectories.Children.Count > 0)
                    _musicDirectories.Children[0].Children.Clear();
                MusicDirectories.Clear();

                FilterMusicEntriesQuery = "";

                SelectedQueueSong = null;
                CurrentSong = null;

                SearchResult.Clear();
                SearchQuery = "";

                IsAlbumArtVisible = false;
                AlbumArt = _albumArtDefault;

                // TODO: more
            });

            IsConnecting = true;

            if (HostIpAddress == null) return;
            //ConnectionResult r = await _mpc.MpdIdleConnect(_host, _port);
            ConnectionResult r = await _mpc.MpdIdleConnect(HostIpAddress.ToString(), _port);

            if (r.IsSuccess)
            {
                IsSettingsShow = false;

                if (CurrentProfile == null)
                {
                    // Create new profile
                    Profile prof = new()
                    {
                        Name = _host + ":" + _port.ToString(),
                        Host = _host,
                        //HostIpAddress = _hostIpAddress,
                        Port = _port,
                        Password = _password,
                        IsDefault = true
                    };

                    CurrentProfile = prof;
                    SelectedProfile = prof;

                    Profiles.Add(prof);
                    NotifyPropertyChanged(nameof(IsCurrentProfileSet));
                }
                else
                {
                    //SelectedProfile = new Profile();
                    SelectedProfile.Host = _host;
                    //SelectedProfile.HostIpAddress = _hostIpAddress;
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
                        SelectedProfile.IsDefault = false;
                    }

                    SelectedProfile.Name = Host + ":" + _port.ToString();

                    CurrentProfile = SelectedProfile;
                }
            }

            IsSwitchingProfile = false;
        }

        private async void ChangeConnection(Profile prof)
        {
            if (IsConnecting) return;
            if (IsBusy) return;
            if (IsWorking) return;

            IsBusy = true;
            IsSwitchingProfile = true;

            if (IsConnected)
            {
                _mpc.MpdStop = true;
                _mpc.MpdDisconnect();
                _mpc.MpdStop = false;
            }

            // Save volume.
            if (CurrentProfile != null)
                CurrentProfile.Volume = Volume;

            // Clear current...
            if (CurrentSong != null)
            {
                CurrentSong.IsPlaying = false;
                CurrentSong = null;
            }
            if (CurrentSong != null)
            {
                SelectedQueueSong = null;
            }

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedNodeMenu = null;

                SelectedQueueSong = null;
                CurrentSong = null;

                _mpc.MpdStatus.Reset();

                Queue.Clear();
                _mpc.CurrentQueue.Clear();

                if (_mainMenuItems.PlaylistsDirectory != null)
                    _mainMenuItems.PlaylistsDirectory.Children.Clear();

                Playlists.Clear();
                _mpc.Playlists.Clear();
                SelectedPlaylist = null;

                SelectedPlaylistSong = null;

                if (_mainMenuItems.LibraryDirectory != null)
                    _mainMenuItems.LibraryDirectory.IsAcquired = false;

                MusicEntries.Clear();

                // TODO: not good when directory is being built.
                //MusicDirectories.Clear(); // Don't
                //_mpc.LocalDirectories.Clear();// Don't
                //_mpc.LocalFiles.Clear();// Don't
                //SelectedNodeDirectory.Children.Clear();// Don't
                _musicDirectories.IsCanceled = true;
                if (_musicDirectories.Children.Count > 0)
                    _musicDirectories.Children[0].Children.Clear();
                //MusicDirectories.Clear();

                FilterMusicEntriesQuery = "";

                SearchResult.Clear();
                SearchQuery = "";

                IsAlbumArtVisible = false;
                AlbumArt = _albumArtDefault;

                HostIpAddress = null;

                // TODO: more?
            });

            _volume = prof.Volume;
            NotifyPropertyChanged(nameof(Volume));


            Host = prof.Host;

            HostIpAddress = null;

            try
            {
                var addresses = await Dns.GetHostAddressesAsync(Host);
                if (addresses.Count() > 0)
                {
                    HostIpAddress = addresses[0];
                }
                else
                {
                    SetError(nameof(Host), "Error: Could not retrive IP Address from the hostname."); //TODO: translate.

                    // TODO:::::::
                    ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
                    StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";
                    return;
                }
            }
            catch (Exception)
            {
                SetError(nameof(Host), "Error: Could not retrive IP Address from the hostname. (SocketException)"); //TODO: translate.

                // TODO:::::::
                ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
                StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";

                return;
            }

            _port = prof.Port;
            Password = prof.Password;

            IsConnecting = true;

            if (HostIpAddress == null) return;
            //ConnectionResult r = await _mpc.MpdIdleConnect(_host, _port);
            ConnectionResult r = await _mpc.MpdIdleConnect(HostIpAddress.ToString(), _port);

            if (r.IsSuccess)
            {
                CurrentProfile = prof;

                SelectedNodeMenu = MainMenuItems[0];
            }

            IsSwitchingProfile = false;
            IsBusy = false;
        }

        #endregion

        #region == Dialogs ==

        public ICommand ShowChangePasswordDialogCommand { get; }
        public bool ShowChangePasswordDialogCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void ShowChangePasswordDialogCommand_Execute(object obj)
        {
            if (IsChangePasswordDialogShow)
            {
                IsChangePasswordDialogShow = false;
            }
            else
            {
                if (obj == null) return;
                // for Unbindable PasswordBox.
                var passwordBox = obj as PasswordBox;
                passwordBox.Password = "";

                IsChangePasswordDialogShow = true;
            }
        }

        public ICommand ChangePasswordDialogOKCommand { get; }
        public bool ChangePasswordDialogOKCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void ChangePasswordDialogOKCommand_Execute(object obj)
        {
            if (obj == null) return;

            // MultipleCommandParameterConverter!
            var values = (object[])obj;

            if ((values[0] is PasswordBox) && (values[1] is PasswordBox))
            {
                if ((values[0] as PasswordBox).Password == _password)
                {
                    SelectedProfile.Password = (values[1] as PasswordBox).Password; //allow empty string.

                    Password = SelectedProfile.Password;
                    NotifyPropertyChanged(nameof(IsPasswordSet));
                    NotifyPropertyChanged(nameof(IsNotPasswordSet));

                    (values[0] as PasswordBox).Password = "";
                    (values[1] as PasswordBox).Password = "";

                    if (SelectedProfile == CurrentProfile)
                    {
                        //_mpc.MpdPassword = SelectedProfile.Password;
                    }

                    SettingProfileEditMessage = MPDCtrl.Properties.Resources.ChangePasswordDialog_PasswordUpdated;

                }
                else
                {
                    ChangePasswordDialogMessage = MPDCtrl.Properties.Resources.ChangePasswordDialog_CurrentPasswordMismatch;
                    return;
                }

                IsChangePasswordDialogShow = false;
            }
        }

        public ICommand ChangePasswordDialogCancelCommand { get; }
        public static bool ChangePasswordDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void ChangePasswordDialogCancelCommand_Execute()
        {
            IsChangePasswordDialogShow = false;
        }

        #endregion

        #region == QueueListview header colums Show/Hide ==

        public ICommand QueueColumnHeaderPositionShowHideCommand { get; }
        public static bool QueueColumnHeaderPositionShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderPositionShowHideCommand_Execute()
        {
            if (QueueColumnHeaderPositionVisibility)
            {
                QueueColumnHeaderPositionVisibility = false;
                QueueColumnHeaderPositionWidth = 0;
            }
            else
            {
                QueueColumnHeaderPositionVisibility = true;
                QueueColumnHeaderPositionWidth = QueueColumnHeaderPositionWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderNowPlayingShowHideCommand { get; }
        public static bool QueueColumnHeaderNowPlayingShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderNowPlayingShowHideCommand_Execute()
        {
            if (QueueColumnHeaderNowPlayingVisibility)
            {
                QueueColumnHeaderNowPlayingVisibility = false;
                QueueColumnHeaderNowPlayingWidth = 0;
            }
            else
            {
                QueueColumnHeaderNowPlayingVisibility = true;
                QueueColumnHeaderNowPlayingWidth = QueueColumnHeaderNowPlayingWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderTimeShowHideCommand { get; }
        public static bool QueueColumnHeaderTimeShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderTimeShowHideCommand_Execute()
        {
            if (QueueColumnHeaderTimeVisibility)
            {

                QueueColumnHeaderTimeVisibility = false;
                QueueColumnHeaderTimeWidth = 0;
            }
            else
            {
                QueueColumnHeaderTimeVisibility = true;
                QueueColumnHeaderTimeWidth = QueueColumnHeaderTimeWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderArtistShowHideCommand { get; }
        public static bool QueueColumnHeaderArtistShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderArtistShowHideCommand_Execute()
        {
            if (QueueColumnHeaderArtistVisibility)
            {
                QueueColumnHeaderArtistVisibility = false;
                QueueColumnHeaderArtistWidth = 0;
            }
            else
            {
                QueueColumnHeaderArtistVisibility = true;
                QueueColumnHeaderArtistWidth = QueueColumnHeaderArtistWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderAlbumShowHideCommand { get; }
        public static bool QueueColumnHeaderAlbumShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderAlbumShowHideCommand_Execute()
        {
            if (QueueColumnHeaderAlbumVisibility)
            {
                QueueColumnHeaderAlbumVisibility = false;
                QueueColumnHeaderAlbumWidth = 0;
            }
            else
            {
                QueueColumnHeaderAlbumVisibility = true;
                QueueColumnHeaderAlbumWidth = QueueColumnHeaderAlbumWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderGenreShowHideCommand { get; }
        public static bool QueueColumnHeaderGenreShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderGenreShowHideCommand_Execute()
        {
            if (QueueColumnHeaderGenreVisibility)
            {
                QueueColumnHeaderGenreVisibility = false;
                QueueColumnHeaderGenreWidth = 0;
            }
            else
            {
                QueueColumnHeaderGenreVisibility = true;
                QueueColumnHeaderGenreWidth = QueueColumnHeaderGenreWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderLastModifiedShowHideCommand { get; }
        public static bool QueueColumnHeaderLastModifiedShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderLastModifiedShowHideCommand_Execute()
        {
            if (QueueColumnHeaderLastModifiedVisibility)
            {
                QueueColumnHeaderLastModifiedVisibility = false;
                QueueColumnHeaderLastModifiedWidth = 0;
            }
            else
            {
                QueueColumnHeaderLastModifiedVisibility = true;
                QueueColumnHeaderLastModifiedWidth = QueueColumnHeaderLastModifiedWidthRestore;
            }
        }


        #endregion

        #region == DebugWindow and AckWindow ==

        public ICommand ClearDebugCommandTextCommand { get; }
        public static bool ClearDebugCommandTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearDebugCommandTextCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugCommandClear?.Invoke();
            });
        }

        public ICommand ClearDebugIdleTextCommand { get; }
        public static bool ClearDebugIdleTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearDebugIdleTextCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugIdleClear?.Invoke();
            });
        }

        public ICommand ShowDebugWindowCommand { get; }
        public static bool ShowDebugWindowCommand_CanExecute()
        {
            return true;
        }
        public void ShowDebugWindowCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugWindowShowHide?.Invoke();
            });
        }

        public ICommand ClearAckTextCommand { get; }
        public static bool ClearAckTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearAckTextCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                AckWindowClear?.Invoke();
            });
        }

        public ICommand ShowAckWindowCommand { get; }
        public static bool ShowAckWindowCommand_CanExecute()
        {
            return true;
        }
        public void ShowAckWindowCommand_Execute()
        {
            if (IsShowAckWindow)
                IsShowAckWindow = false;
            else
                IsShowAckWindow = true;
        }

        #endregion

        #region == Find ==

        public ICommand ShowFindCommand { get; }
        public static bool ShowFindCommand_CanExecute()
        {
            return true;
        }
        public void ShowFindCommand_Execute()
        {
            if (SelectedNodeMenu is NodeMenuQueue)
            {
                QueueFindShowHideCommand_Execute();
            }
            else if(SelectedNodeMenu is NodeMenuSearch)
            {

            }
            else
            {
                SelectedNodeMenu = _mainMenuItems.SearchDirectory;

                IsQueueFindVisible = false;
            }
        }

        public ICommand QueueFilterSelectCommand { get; set; }
        public static bool QueueFilterSelectCommand_CanExecute()
        {
            return true;
        }
        public void QueueFilterSelectCommand_Execute(object obj)
        {
            if (obj == null)
                return;

            if (obj != _selectedQueueFilterSong)
                return;

            IsQueueFindVisible = false;

            if (_selectedQueueFilterSong != null)
            {
                ScrollIntoViewAndSelect?.Invoke(this, _selectedQueueFilterSong.Index);
            }
        }

        #endregion

        #region == TreeViewMenu ContextMenu ==
        
        public ICommand TreeviewMenuItemLoadPlaylistCommand { get; }
        public bool TreeviewMenuItemLoadPlaylistCommand_CanExecute()
        {
            if (SelectedNodeMenu == null)
                return false;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return false;
            if (IsBusy) return false;
            if (IsWorking) return false;

            return true;
        }
        public async void TreeviewMenuItemLoadPlaylistCommand_Execute()
        {
            if (IsBusy) return;
            if (IsWorking) return;
            if (SelectedNodeMenu == null)
                return;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return;
            
            await _mpc.MpdLoadPlaylist(SelectedNodeMenu.Name);
        }

        public ICommand TreeviewMenuItemClearLoadPlaylistCommand { get; }
        public bool TreeviewMenuItemClearLoadPlaylistCommand_CanExecute()
        {
            if (SelectedNodeMenu == null)
                return false;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return false;
            if (IsBusy) return false;
            if (IsWorking) return false;

            return true;
        }
        public async void TreeviewMenuItemClearLoadPlaylistCommand_Execute()
        {
            if (IsBusy)
                return;
            if (IsWorking) return;
            if (SelectedNodeMenu == null)
                return;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            await _mpc.MpdChangePlaylist(SelectedNodeMenu.Name);
        }

        #endregion

        public ICommand EscapeCommand { get; }
        public static bool EscapeCommand_CanExecute()
        {
            return true;
        }
        public void EscapeCommand_ExecuteAsync()
        {
            IsChangePasswordDialogShow = false;

            //IsSettingsShow = false; //Don't.

            IsQueueFindVisible = false;
        }

        #endregion
    }
}
