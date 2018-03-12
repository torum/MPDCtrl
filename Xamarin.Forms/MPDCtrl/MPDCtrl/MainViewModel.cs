/// 
/// 
/// MPD Ctrl for iOS
/// https://github.com/torumyax/MPD-Ctrl
/// 
/// TODO:
/// -- Priority 1 --
///  Settings page.
///   xam plugins:
///   https://github.com/jamesmontemagno/SettingsPlugin
///  Icons...
///  Detect resize
///   https://forums.xamarin.com/discussion/87615/detecting-device-orientation
/// -- Priority 2 --
///  i18n
///  Better error messages for users.
///  
/// -- Goal --
/// AppX and Microsoft Store. 
///
/// Known issues:


// Xamarin issues.
//
// Xamarin NavigationPage.SetHasNavigationBar false doesn't work 
// https://github.com/xamarin/Xamarin.Forms/issues/1437
// https://github.com/xamarin/Xamarin.Forms/issues/1627

// Xamarin ListView.ScrollTo doesn't animate on UWP #2048
// https://github.com/xamarin/Xamarin.Forms/issues/2048
// 
// Slider crashes when Minimum value greater than zero and set before Max value in XAML #1943
// https://github.com/xamarin/Xamarin.Forms/issues/1943
//
// 
// PropertyChangedEventArgs updates binding value #2073
// https://github.com/xamarin/Xamarin.Forms/issues/2073



using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using System.Threading;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace MPDCtrl
{
    class MainViewModel : INotifyPropertyChanged
    {
        #region PRIVATE FIELD declaration

        const string CurrentPlayQueue = "Current Play Queue";

        private MPC _MPC;
        private string _defaultHost;
        private int _defaultPort;
        private string _defaultPassword;
        private MPC.Song _selectedSong;
        private bool _isPlaying;
        private string _selecctedPlaylist;
        private bool _isBusy;
        private bool _isWorking;
        //private string _playButton;
        private double _volume;
        private bool _repeat;
        private bool _random;
        private double _time;
        private double _elapsed;
        private System.Timers.Timer _elapsedTimer;
        private bool _showSettings;
        private string _errorMessage;
        //private Profile _profile;
        private ICommand _playCommand;
        private ICommand _playNextCommand;
        private ICommand _playPrevCommand;
        private ICommand _setRepeatCommand;
        private ICommand _setRandomCommand;
        private ICommand _setVolumeCommand;
        private ICommand _setSeekCommand;
        private ICommand _changeSongCommand;
        private ICommand _changePlaylistCommand;
        private ICommand _windowClosingCommand;
        private ICommand _playPauseCommand;
        private ICommand _playStopCommand;
        private ICommand _volumeMuteCommand;
        private ICommand _volumeDownCommand;
        private ICommand _volumeUpCommand;
        private ICommand _showSettingsCommand;
        private ICommand _newConnectinSettingCommand;
        private ICommand _addConnectinSettingCommand;
        private ICommand _deleteConnectinSettingCommand;

        #endregion END of PRIVATE FIELD declaration

        #region PUBLIC PROPERTY FIELD

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

        public MPC.Song SelectedSong
        {
            get
            {
                return _selectedSong;
            }
            set
            {
                if (_selectedSong != value) {
                    _selectedSong = value;
                    this.NotifyPropertyChanged("SelectedSong");
                    if (_MPC != null)
                    {
                        if (value != null)
                        {
                            if (_MPC.MpdStatus.MpdSongID != value.ID)
                            {
                                if (ChangeSongCommand.CanExecute(null))
                                {
                                    ChangeSongCommand.Execute(null);
                                }
                            }
                        }
                    }
                }
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

                    if (!string.IsNullOrEmpty(_selecctedPlaylist))
                    {
                        //System.Diagnostics.Debug.WriteLine("\n\nPlaylist_SelectionChanged: " + _selecctedPlaylist);

                        if (ChangePlaylistCommand.CanExecute(null))
                        {
                            ChangePlaylistCommand.Execute(null);
                        }
                    }
                }
            }
        }

        public bool IsPlaying
        {
            get
            {
                return _isPlaying;
            }
            set {
                _isPlaying = value;
                this.NotifyPropertyChanged("IsPlaying");
            }
        }

        public double Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                if (_volume != value) {
                    this._volume = value;
                    this.NotifyPropertyChanged("Volume");

                    if (_MPC != null)
                    {
                        /*
                        if (Convert.ToDouble(_MPC.MpdStatus.MpdVolume) != value)
                        {
                            if (SetVolumeCommand.CanExecute(null))
                            {
                                SetVolumeCommand.Execute(null);
                            }
                        }
                        */

                        // If we have a timer and we are in this event handler, a user is still interact with the slider
                        // we stop the timer
                        if (_volumeDelayTimer != null)
                            _volumeDelayTimer.Stop();

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

        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                if (this._repeat != value) {
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
        }

        public bool Random
        {
            get { return _random; }
            set
            {
                if (this._random != value) {
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
        }

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

        public double Elapsed
        {
            get
            {
                return this._elapsed;
            }
            set
            {
                if ((value < this._time) && _elapsed != value) {
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
            }
        }

        public bool IsBusy
        {
            get
            {
                return this._isBusy;
            }
            set
            {
                this._isBusy = value;
                this.NotifyPropertyChanged("IsBusy");
            }
        }

        public bool IsWorking
        {
            get
            {
                return this._isWorking;
            }
            set
            {
                this._isWorking = value;
                this.NotifyPropertyChanged("IsWorking");
            }
        }

        public bool ShowSettings
        {
            get { return this._showSettings; }
            set
            {
                this._showSettings = value;
                this.NotifyPropertyChanged("IsVisible");
                this.NotifyPropertyChanged("ShowSettings");
            }
        }

        public bool IsVisible
        {
            get
            {
                if (this._showSettings)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }
        }

        public string ErrorMessage
        {
            get
            {
                return this._errorMessage;
            }
            set
            {
                this._errorMessage = value;
                this.NotifyPropertyChanged("ErrorMessage");
            }
        }

        #endregion END of PUBLIC PROPERTY FIELD


        // Constructor
        public MainViewModel()
        {
            // Initialize connection setting.
            this._defaultHost = "192.168.3.123"; //127.0.0.1
            this._defaultPort = 6600;
            this._defaultPassword = "";

            Elapsed = 1.000;
            Time = 100.000;

            // Assign commands
            this._playCommand = new MPDCtrl.RelayCommand(this.PlayCommand_ExecuteAsync, this.PlayCommand_CanExecute);
            this._playNextCommand = new MPDCtrl.RelayCommand(this.PlayNextCommand_ExecuteAsync, this.PlayNextCommand_CanExecute);
            this._playPrevCommand = new MPDCtrl.RelayCommand(this.PlayPrevCommand_ExecuteAsync, this.PlayPrevCommand_CanExecute);
            this._setRepeatCommand = new MPDCtrl.RelayCommand(this.SetRpeatCommand_ExecuteAsync, this.SetRpeatCommand_CanExecute);
            this._setRandomCommand = new MPDCtrl.RelayCommand(this.SetRandomCommand_ExecuteAsync, this.SetRandomCommand_CanExecute);
            this._setVolumeCommand = new MPDCtrl.RelayCommand(this.SetVolumeCommand_ExecuteAsync, this.SetVolumeCommand_CanExecute);
            this._setSeekCommand = new MPDCtrl.RelayCommand(this.SetSeekCommand_ExecuteAsync, this.SetSeekCommand_CanExecute);
            this._changeSongCommand = new MPDCtrl.RelayCommand(this.ChangeSongCommand_ExecuteAsync, this.ChangeSongCommand_CanExecute);
            this._changePlaylistCommand = new MPDCtrl.RelayCommand(this.ChangePlaylistCommand_ExecuteAsync, this.ChangePlaylistCommand_CanExecute);
            this._windowClosingCommand = new MPDCtrl.RelayCommand(this.WindowClosingCommand_Execute, this.WindowClosingCommand_CanExecute);
            this._playPauseCommand = new MPDCtrl.RelayCommand(this.PlayPauseCommand_Execute, this.PlayPauseCommand_CanExecute);
            this._playStopCommand = new MPDCtrl.RelayCommand(this.PlayStopCommand_Execute, this.PlayStopCommand_CanExecute);
            this._volumeMuteCommand = new MPDCtrl.RelayCommand(this.VolumeMuteCommand_Execute, this.VolumeMuteCommand_CanExecute);
            this._volumeDownCommand = new MPDCtrl.RelayCommand(this.VolumeDownCommand_Execute, this.VolumeDownCommand_CanExecute);
            this._volumeUpCommand = new MPDCtrl.RelayCommand(this.VolumeUpCommand_Execute, this.VolumeUpCommand_CanExecute);
            this._showSettingsCommand = new MPDCtrl.RelayCommand(this.ShowSettingsCommand_Execute, this.ShowSettingsCommand_CanExecute);
            ////this._newConnectinSettingCommand = new WpfMPD.Common.RelayCommand(, this.NewConnectinSettingCommand_CanExecute);
            //this._newConnectinSettingCommand = new MPDCtrl.GenericRelayCommand<object>(param => this.NewConnectinSettingCommand_Execute(param), param => this.NewConnectinSettingCommand_CanExecute());
            this._addConnectinSettingCommand = new MPDCtrl.RelayCommand(this.AddConnectinSettingCommand_Execute, this.AddConnectinSettingCommand_CanExecute);
            this._deleteConnectinSettingCommand = new MPDCtrl.RelayCommand(this.DeleteConnectinSettingCommand_Execute, this.DeleteConnectinSettingCommand_CanExecute);

            // Init Song's time elapsed timer.
            this._elapsedTimer = new System.Timers.Timer();
            this._elapsedTimer.Interval = (double)1000;
            this._elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);

            Start();
        }

        private async void Start()
        {
            await StartConnection();
        }

        private async Task<bool> StartConnection()
        {
            // Create MPC instance.
            this._MPC = new MPC(this._defaultHost, this._defaultPort, this._defaultPassword);

            // Assign idle event.
            this._MPC.StatusChanged += new MPC.MpdStatusChanged(OnStatusChanged);
            this._MPC.ErrorReturned += new MPC.MpdError(OnError);

            await Task.Delay(100);

            //TODO: i18n
            //ErrorMessage = MPDCtrl.Properties.Resources.Connecting; //"Connecting...";
            ErrorMessage = "Connecting...";

            IsWorking = true;
            if (await _MPC.MpdCmdConnect())
            {
                //TODO: i18n
                ErrorMessage = "Updating...";

                // Connect to MPD server and query status and info.
                if (await QueryStatus())
                {
                    // Connect and start "idle" connection.
                    if (_MPC.MpdIdleConnect())
                    {
                        ErrorMessage = CurrentPlayQueue;
                        IsWorking = false;
                        return true;
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("MpdCmdConnect@StartConnection() failed.");
            }

            IsWorking = false;

            return false;
        }

        private async void OnStatusChanged(MPC sender, object data)
        {
            if (data == null) { return; }

            // The list of SubSystems we are subscribing.
            // player mixer options playlist stored_playlist

            bool isPlayer = false;
            bool isPlaylist = false;
            bool isStoredPlaylist = false;
            foreach (var subsystem in (data as List<string>))
            {
                //System.Diagnostics.Debug.WriteLine("OnStatusChanged: " + subsystem);

                if (subsystem == "player")
                {
                    //System.Diagnostics.Debug.WriteLine("OnStatusChanged: player");
                    isPlayer = true;
                }
                else if (subsystem == "mixer")
                {
                    //System.Diagnostics.Debug.WriteLine("OnStatusChanged: mixer");
                    isPlayer = true;
                }
                else if (subsystem == "options")
                {
                    //System.Diagnostics.Debug.WriteLine("OnStatusChanged: options");
                    isPlayer = true;
                }
                else if (subsystem == "playlist")
                {
                    //System.Diagnostics.Debug.WriteLine("OnStatusChanged: playlist");
                    isPlaylist = true;
                }
                else if (subsystem == "stored_playlist")
                {
                    //System.Diagnostics.Debug.WriteLine("OnStatusChanged: stored_playlist");
                    isStoredPlaylist = true;
                }
            }

            int c = 0;
            while (_isBusy)
            {
                c++;
                await Task.Delay(10);
                if (c > (100 * 100))
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusChanged: TIME OUT");
                    IsBusy = false;
                }
            }

            IsBusy = true;
            IsWorking = true;

            if ((isPlayer && isPlaylist))
            {
                System.Diagnostics.Debug.WriteLine("OnStatusChanged: isPlayer & isPlaylist");

                // Reset view.
                Device.BeginInvokeOnMainThread(
                    () =>
                    {
                        sender.CurrentQueue.Clear();
                    });
                this._selectedSong = null;

                ErrorMessage = "Updating...";

                // Get updated information.
                bool isDone = await sender.MpdQueryStatus();
                if (isDone)
                {
                    ErrorMessage = "Loading..."; 

                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");

                    isDone = await sender.MpdQueryCurrentPlaylist();
                    if (isDone)
                    {
                        ErrorMessage = CurrentPlayQueue;

                        System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryCurrentPlaylist> is done.");

                        _selecctedPlaylist = "";

                        if (sender.MpdCurrentSong != null)
                        {
                            this._selectedSong = sender.MpdCurrentSong;
                            this.NotifyPropertyChanged("SelectedSong");
                            System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer & isPlaylist SelectedSong is : " + this._selectedSong.Title);
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer & isPlaylist MpdCurrentSong is : null");
                        }

                        if (isStoredPlaylist)
                        {
                            // Retrieve playlists
                            sender.Playlists.Clear();
                            isDone = await sender.MpdQueryPlaylists();
                            if (isDone)
                            {
                                System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                                IsBusy = false;
                                IsWorking = false;
                            }
                            else
                            {
                                IsBusy = false;
                                IsWorking = false;
                                System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                            }
                        }
                        else
                        {
                            IsBusy = false;
                            IsWorking = false;
                        }

                        Device.BeginInvokeOnMainThread(() => UpdateButtonStatus());
                        //TODO: Oops.
                        //Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
                    }
                    else
                    {
                        IsBusy = false;
                        IsWorking = false;
                        System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                    }
                }
                else
                {
                    IsBusy = false;
                    IsWorking = false;
                    System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                }

            }
            else if (isPlaylist)
            {

                ErrorMessage = "Loading...";

                System.Diagnostics.Debug.WriteLine("OnStatusChanged: isPlaylist");

                // Reset view.
                //sender.CurrentQueue.Clear();
                Device.BeginInvokeOnMainThread(
                    () =>
                    {
                        sender.CurrentQueue.Clear();
                    });
                this._selectedSong = null;

                // Get updated information.
                bool isDone = await sender.MpdQueryCurrentPlaylist();
                if (isDone)
                {
                    ErrorMessage = CurrentPlayQueue;

                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryCurrentPlaylist> is done.");

                    _selecctedPlaylist = "";

                    if (isStoredPlaylist)
                    {
                        //sender.Playlists.Clear();
                        Device.BeginInvokeOnMainThread(
                            () =>
                            {
                                sender.Playlists.Clear();
                            });
                        // Retrieve playlists
                        isDone = await sender.MpdQueryPlaylists();
                        if (isDone)
                        {
                            IsBusy = false;
                            IsWorking = false;
                        }
                        else
                        {
                            IsBusy = false;
                            IsWorking = false;
                            System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                        }
                    }
                    else
                    {
                        IsBusy = false;
                        IsWorking = false;
                    }

                    Device.BeginInvokeOnMainThread(() => UpdateButtonStatus());
                    //TODO: Oops.
                    //Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());

                }
                else
                {
                    IsBusy = false;
                    IsWorking = false;
                    System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                }
            }
            else if (isPlayer)
            {
                //ErrorMessage = "Updating...";

                System.Diagnostics.Debug.WriteLine("OnStatusChanged: isPlayer");

                // Update status.
                bool isDone = await sender.MpdQueryStatus();
                if (isDone)
                {
                    ErrorMessage = CurrentPlayQueue;

                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");

                    bool needReselect = false;
                    if (sender.MpdCurrentSong != null)
                    {
                        if (sender.MpdCurrentSong.ID != _MPC.MpdStatus.MpdSongID)
                        {
                            needReselect = true;
                        }
                    }
                    else
                    {
                        needReselect = true;
                    }
                    if (this._selectedSong != null) { 
                        if (this._selectedSong.ID != _MPC.MpdStatus.MpdSongID)
                        {
                            needReselect = true;
                        }
                    }else
                    {
                        needReselect = true;
                    }

                    if (needReselect)
                    {
                        // Need to re select here in case of ... subsystem == "player" alone called. 
                        // "status" command result alone does not set "MpdCurrentSong"
                        try
                        {
                            
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                var item = _MPC.CurrentQueue.FirstOrDefault(i => i.ID == _MPC.MpdStatus.MpdSongID);
                                if (item != null)
                                {
                                    sender.MpdCurrentSong = (item as MPC.Song); // just in case.
                                    this._selectedSong = (item as MPC.Song);
                                    System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer SelectedSong is : " + this._selectedSong.Title);
                                }
                                else
                                {
                                    System.Diagnostics.Debug.WriteLine("OnStatusChanged isPlayer SelectedSong is NULL");
                                }
                            });
                            // Update UI
                            this.NotifyPropertyChanged("SelectedSong");
                            
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine("_MPC.CurrentQueue.FirstOrDefault@(isPlayer) failed: " + ex.Message);
                        }
                    }

                    if (isStoredPlaylist)
                    {

                        // Retrieve playlists
                        sender.Playlists.Clear();
                        isDone = await sender.MpdQueryPlaylists();
                        if (isDone)
                        {
                            IsBusy = false;
                            IsWorking = false;
                        }
                        else
                        {
                            IsBusy = false;
                            IsWorking = false;
                            System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                        }
                    }
                    else
                    {
                        IsBusy = false;
                        IsWorking = false;
                    }

                    // Testing
                    Device.BeginInvokeOnMainThread(() => UpdateButtonStatus());
                    //TODO: Opps.
                    //Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());

                }
                else
                {
                    IsBusy = false;
                    IsWorking = false;
                    System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                }
            }
            else if (isStoredPlaylist)
            {
                this._selecctedPlaylist = "";

                if (isStoredPlaylist)
                {
                    // Retrieve playlists
                    //sender.Playlists.Clear();
                    Device.BeginInvokeOnMainThread(
                        () =>
                        {
                            sender.Playlists.Clear();
                        });
                    bool isDone = await sender.MpdQueryPlaylists();
                    if (isDone)
                    {
                        IsBusy = false;
                        IsWorking = false;
                    }
                    else
                    {
                        IsBusy = false;
                        IsWorking = false;
                        System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                    }
                }
                else
                {
                    IsBusy = false;
                    IsWorking = false;
                }
            }

        }

        private async Task<bool> QueryStatus()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdQueryStatus();
            IsWorking = false;
            if (isDone)
            {
                System.Diagnostics.Debug.WriteLine("QueryStatus is done.");

                UpdateButtonStatus();

                ErrorMessage = "Loading...";

                // Retrieve play queue.
                return await QueryCurrentPlayQueue();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("QueryStatus returned with false." + "\n");
                return false;
            }
        }

        private async Task<bool> QueryCurrentPlayQueue()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdQueryCurrentPlaylist();
            IsWorking = false;
            if (isDone)
            {
                System.Diagnostics.Debug.WriteLine("QueryCurrentPlaylist is done.");

                // Not good.
                //if (_MPC.CurrentQueue.Count > 0)
                //{

                    if (_MPC.MpdCurrentSong != null)
                    {
                        this._selectedSong = _MPC.MpdCurrentSong;
                        this.NotifyPropertyChanged("SelectedSong");
                        System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue SelectedSong is : " + this._selectedSong.Title);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue MpdCurrentSong is null");
                    }

                //}
                //else
                //{
                //    System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue NOT CurrentQueue.Count > 0");
                //}

                ErrorMessage = "Retrieving...";

                // Retrieve playlists
                return await QueryPlaylists();
            }
            else
            {
                //IsBusy = false;
                System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                return false;
            }

        }

        private async Task<bool> QueryPlaylists()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdQueryPlaylists();
            IsWorking = false;
            if (isDone)
            {
                System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                //IsBusy = false;

                return true;
            }
            else
            {
                //IsBusy = false;
                System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                return false;
            }
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
            ErrorMessage = s;
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
                            //this.PlayButton = _pathPlayButton;
                            IsPlaying = true;
                            break;
                        }
                    case MPC.Status.MpdPlayState.Pause:
                        {
                            //this.PlayButton = _pathPauseButton;
                            IsPlaying = false;
                            break;
                        }
                    case MPC.Status.MpdPlayState.Stop:
                        {
                            //this.PlayButton = _pathStopButton;
                            IsPlaying = false;
                            break;
                        }
                }

                // Not in Xamarin.Forms -> "quietly" update view.
                ////// https://github.com/xamarin/Xamarin.Forms/issues/2073

                this._volume = Convert.ToDouble(_MPC.MpdStatus.MpdVolume);
                if (!_isBusy) { 
                    this.NotifyPropertyChanged("Volume");
                }

                this._random = _MPC.MpdStatus.MpdRandom;
                this.NotifyPropertyChanged("Random");

                this._repeat = _MPC.MpdStatus.MpdRepeat;
                this.NotifyPropertyChanged("Repeat");

                // Not apply in Xamarin.Forms > No need to care about "double" updates for time.
                // https://github.com/xamarin/Xamarin.Forms/issues/2073
                this.Time = _MPC.MpdStatus.MpdSongTime;

                this._elapsed = _MPC.MpdStatus.MpdSongElapsed;
                this.NotifyPropertyChanged("Elapsed");

                // Start elapsed timer.
                if (_MPC.MpdStatus.MpdState == MPC.Status.MpdPlayState.Play)
                {
                    this._elapsedTimer.Start();
                }
                else
                {
                    this._elapsedTimer.Stop();
                }
            }
            catch
            {
                System.Diagnostics.Debug.WriteLine("Error@UpdateButtonStatus");
            }
        }

        private void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_elapsed <= _time) && (_MPC.MpdStatus.MpdState == MPC.Status.MpdPlayState.Play))
            {
                // In Xamarin.Forms, it's same as Elapsed +=1;
                this._elapsed += 1;
                this.NotifyPropertyChanged("Elapsed");
            }
            else
            {
                this._elapsedTimer.Stop();
            }
        }

        #region COMMANDS

        public ICommand PlayCommand { get { return this._playCommand; } }

        public bool PlayCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            //TODO:
            //if (Songs.Count < 1) { return false; }
            return true;
        }

        public async void PlayCommand_ExecuteAsync()
        {
            bool isDone = false;
            IsWorking = true;

            switch (_MPC.MpdStatus.MpdState)
            {
                case MPC.Status.MpdPlayState.Play:
                    {
                        //State>>Play: So, send Pause command
                        isDone = await _MPC.MpdPlaybackPause();
                        break;
                    }
                case MPC.Status.MpdPlayState.Pause:
                    {
                        //State>>Pause: So, send Resume command
                        isDone = await _MPC.MpdPlaybackResume();
                        break;
                    }
                case MPC.Status.MpdPlayState.Stop:
                    {
                        //State>>Stop: So, send Play command
                        isDone = await _MPC.MpdPlaybackPlay();
                        break;
                    }
            }

            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("PlayCommand returned false." + "\n");
            }
        }

        public ICommand PlayNextCommand { get { return this._playNextCommand; } }

        public bool PlayNextCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            //TODO:
            //if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayNextCommand_ExecuteAsync()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdPlaybackNext();
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("PlayNextCommand returned false." + "\n");
            }
        }

        public ICommand PlayPrevCommand { get { return this._playPrevCommand; } }

        public bool PlayPrevCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            //TODO:
            //if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayPrevCommand_ExecuteAsync()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdPlaybackPrev();
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("PlayPrevCommand returned false. " + "\n");
            }
        }

        public ICommand SetRpeatCommand { get { return this._setRepeatCommand; } }

        public bool SetRpeatCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetRpeatCommand_ExecuteAsync()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdSetRepeat(this._repeat);
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetRepeat returned false");
            }
        }

        public ICommand SetRandomCommand { get { return this._setRandomCommand; } }

        public bool SetRandomCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetRandomCommand_ExecuteAsync()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdSetRandom(this._random);
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetRandom returned false");
            }
        }

        public ICommand SetVolumeCommand { get { return this._setVolumeCommand; } }

        public bool SetVolumeCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetVolumeCommand_ExecuteAsync()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdSetVolume(Convert.ToInt32(this._volume));
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetVolume returned false");
            }
        }

        public ICommand SetSeekCommand { get { return this._setSeekCommand; } }

        public bool SetSeekCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }  //!
            if (_MPC == null) { return false; }
            return true;
        }

        public async void SetSeekCommand_ExecuteAsync()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdPlaybackSeek(_MPC.MpdStatus.MpdSongID, Convert.ToInt32(this._elapsed));
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackSeek returned false");
            }

        }

        public ICommand ChangeSongCommand { get { return this._changeSongCommand; } }

        public bool ChangeSongCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            //TODO:
            //if (_MPC.CurrentQueue.Count < 1) { return false; }
            if (_selectedSong == null) { return false; }
            return true;
        }

        public async void ChangeSongCommand_ExecuteAsync()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdPlaybackPlay(_selectedSong.ID);
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackPlay returned false");
            }
        }

        public ICommand ChangePlaylistCommand { get { return this._changePlaylistCommand; } }

        public bool ChangePlaylistCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            if (_MPC == null) { return false; }
            if (this._selecctedPlaylist == "") { return false; }
            return true;
        }

        public async void ChangePlaylistCommand_ExecuteAsync()
        {
            // Little dirty, but since our ObservableCollection isn't thread safe...
            if (_isBusy)
            {
                await Task.Delay(1000);
                if (_isBusy)
                {
                    await Task.Delay(1000);
                    if (_isBusy)
                    {
                        //ErrorMessage = "Change playlist time out.";
                        System.Diagnostics.Debug.WriteLine("ChangePlaylistCommand_ExecuteAsync: TIME OUT");
                        return;
                    }
                }
            }

            if (String.IsNullOrEmpty(this._selecctedPlaylist)) { return; }

            IsWorking = true;
            // No longer load lists. Just tell it to change.
            bool isDone = await _MPC.MpdChangePlaylist(this._selecctedPlaylist);
            IsWorking = false;

            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdChangePlaylist returned false");
            }
        }

        public ICommand WindowClosingCommand { get { return this._windowClosingCommand; } }

        public bool WindowClosingCommand_CanExecute()
        {
            return true;
        }

        public void WindowClosingCommand_Execute()
        {
            //System.Diagnostics.Debug.WriteLine("WindowClosingCommand");

            // Disconnect connections.
            if (_MPC != null)
            {
                _MPC.MpdIdleDisConnect();
                _MPC.MpdStop = true;
                _MPC.MpdCmdDisConnect();
            }
        }

        public ICommand PlayPauseCommand { get { return this._playPauseCommand; } }

        public bool PlayPauseCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void PlayPauseCommand_Execute()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdPlaybackPause();
            IsWorking = false;
            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("PlayPauseCommand returned false.");
            }
        }

        public ICommand PlayStopCommand { get { return this._playStopCommand; } }

        public bool PlayStopCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void PlayStopCommand_Execute()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdPlaybackStop();
            IsWorking = false;
            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("PlayStopCommand returned false.");
            }
        }

        public ICommand VolumeMuteCommand { get { return this._volumeMuteCommand; } }

        public bool VolumeMuteCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void VolumeMuteCommand_Execute()
        {
            IsWorking = true;
            bool isDone = await _MPC.MpdSetVolume(0);
            IsWorking = false;
            if (!isDone)
            {
                System.Diagnostics.Debug.WriteLine("\nVolumeMuteCommand returned false.");
            }
        }

        public ICommand VolumeDownCommand { get { return this._volumeDownCommand; } }

        public bool VolumeDownCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void VolumeDownCommand_Execute()
        {
            if (this._volume >= 10)
            {
                IsWorking = true;
                bool isDone = await _MPC.MpdSetVolume(Convert.ToInt32(this._volume - 10));
                IsWorking = false;
                if (!isDone)
                {
                    System.Diagnostics.Debug.WriteLine("\nVolumeDownCommand returned false.");
                }
            }
        }

        public ICommand VolumeUpCommand { get { return this._volumeUpCommand; } }

        public bool VolumeUpCommand_CanExecute()
        {
            if (_MPC == null) { return false; }
            return true;
        }

        public async void VolumeUpCommand_Execute()
        {
            if (this._volume <= 90)
            {
                IsWorking = true;
                bool isDone = await _MPC.MpdSetVolume(Convert.ToInt32(this._volume + 10));
                IsWorking = false;
                if (!isDone)
                {
                    System.Diagnostics.Debug.WriteLine("\nVolumeUpCommand returned false.");
                }
            }
        }

        public ICommand ShowSettingsCommand { get { return this._showSettingsCommand; } }

        public bool ShowSettingsCommand_CanExecute()
        {
            return true;
        }

        public void ShowSettingsCommand_Execute()
        {
            if (ShowSettings)
            {
                ShowSettings = false;
            }
            else
            {
                //IsChanged = false;

                ShowSettings = true;
            }
        }

        public ICommand NewConnectinSettingCommand { get { return this._newConnectinSettingCommand; } }

        public bool NewConnectinSettingCommand_CanExecute()
        {
            if (IsBusy) { return false; }
            return true;
        }

        //public async void NewConnectinSettingCommand_Execute()
        public async void NewConnectinSettingCommand_Execute(object param)
        {
            // New connection from Setting.

            /*
            // Validate Host input.
            if (this._defaultHost == "")
            {
                //SetError("Host", "Error: Host must be epecified."); //TODO: translate
                this.NotifyPropertyChanged("Host");
                return;
            }
            else
            {
                IPAddress ipAddress = null;
                try
                {
                    ipAddress = IPAddress.Parse(this._defaultHost);
                    if (ipAddress != null)
                    {
                        // Good.
                    }
                }
                catch
                {
                    //System.FormatException
                    //SetError("Host", "Error: Invalid address format."); //TODO: translate
                    this.NotifyPropertyChanged("Host");
                    return;
                }
            }

            // for Unbindable PasswordBox.
            var passwordBox = param as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                this._defaultPassword = passwordBox.Password;
            }

            */

            if (_MPC != null)
            {
                await this._MPC.MpdIdleStop();
                this._MPC.MpdIdleDisConnect();

                _MPC.CurrentQueue.Clear();
                _MPC.Playlists.Clear();
                _MPC.MpdCurrentSong = null;
                _MPC.MpdStop = true;
                _MPC = null;
            }

            IsBusy = true;
            if (await StartConnection())
            {
                IsBusy = false;
                ShowSettings = false;
                
                /*
                if (this._profile == null)
                {
                    Profile profile = new Profile
                    {
                        Host = this._defaultHost,
                        Port = this._defaultPort,
                        Password = Encrypt(this._defaultPassword),
                        Name = this._defaultHost + ":" + this._defaultPort.ToString(),
                        ID = Guid.NewGuid().ToString(),
                    };

                    MPDCtrl.Properties.Settings.Default.Profiles.Profiles.Add(profile);

                    this._profile = profile;
                }
                else
                {
                    this._profile.Host = this._defaultHost;
                    this._profile.Port = this._defaultPort;
                    this._profile.Password = Encrypt(this._defaultPassword);
                }

                // Make it default;
                MPDCtrl.Properties.Settings.Default.DefaultProfileID = this._profile.ID;

                this.NotifyPropertyChanged("SelectedProfile");
                this.NotifyPropertyChanged("Playlists");

                // Save settings.
                MPDCtrl.Properties.Settings.Default.Save();
                */
            }
            else
            {
                IsBusy = false;
                //TODO: show error.
                System.Diagnostics.Debug.WriteLine("Failed@NewConnectinSettingCommand_Execute");
            }

            this.NotifyPropertyChanged("Songs");
        }

        public ICommand AddConnectinSettingCommand { get { return this._addConnectinSettingCommand; } }

        public bool AddConnectinSettingCommand_CanExecute()
        {
            return true;
        }

        public void AddConnectinSettingCommand_Execute()
        {
            // Add a new profile.
            //MPDCtrl.Properties.Settings.Default.DefaultProfileID = "";
            //SelectedProfile = null;

        }

        public ICommand DeleteConnectinSettingCommand { get { return this._deleteConnectinSettingCommand; } }

        public bool DeleteConnectinSettingCommand_CanExecute()
        {
            //if (SelectedProfile == null) { return false; }
            return true;
        }

        public void DeleteConnectinSettingCommand_Execute()
        {
            // Delete the selected profile entry. 
            /*
            if (this._profile != null)
            {
                try
                {
                    MPDCtrl.Properties.Settings.Default.Profiles.Profiles.Remove(this._profile);
                }
                catch { }
            }
            SelectedProfile = null;
            */
        }


        #endregion END of COMMANDS

        #region == INotifyPropertyChanged ==
        public event PropertyChangedEventHandler PropertyChanged;

        /*
        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        */
        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            Device.BeginInvokeOnMainThread(
                          () =>
                          {
                              PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
                          });
        }
        

        #endregion

        #region == IDataErrorInfo ==
        /*
        private Dictionary<string, string> _ErrorMessages = new Dictionary<string, string>();

        string IDataErrorInfo.Error
        {
            get { return (_ErrorMessages.Count > 0) ? "Has Error" : null; }
        }

        string IDataErrorInfo.this[string columnName]
        {
            get
            {
                if (_ErrorMessages.ContainsKey(columnName))
                    return _ErrorMessages[columnName];
                else
                    return "";
            }
        }

        protected void SetError(string propertyName, string errorMessage)
        {
            _ErrorMessages[propertyName] = errorMessage;
        }

        protected void ClearErrror(string propertyName)
        {
            if (_ErrorMessages.ContainsKey(propertyName))
                //_ErrorMessages.Remove(propertyName);
                _ErrorMessages[propertyName] = "";
        }
        */
        #endregion

    }




    /// <summary>
    /// Class RelayCommand.
    /// </summary>
    public class RelayCommand : ICommand
    {
        /// <summary>
        /// The _execute
        /// </summary>
        private readonly Action execute;

        /// <summary>
        /// The _can execute
        /// </summary>
        private readonly Func<bool> canExecute;

        /// <summary>
        /// Initializes a new instance of the <see cref="RelayCommand"/> class.
        /// </summary>
        /// <param name="execute">The execute.</param>
        /// <param name="canExecute">The can execute.</param>
        /// <exception cref="System.ArgumentNullException">execute</exception>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            if (execute == null)
            {
                throw new ArgumentNullException("execute");
            }

            this.execute = execute;

            if (canExecute != null)
            {
                this.canExecute = canExecute;
            }
        }

        /// <summary>
        /// Initializes a new instance of the RelayCommand class that
        /// can always execute.
        /// </summary>
        /// <param name="execute">The execution logic.</param>
        /// <exception cref="ArgumentNullException">If the execute argument is null.</exception>
        public RelayCommand(Action execute)
            : this(execute, null)
        {
        }

        /// <summary>
        /// Occurs when changes occur that affect whether the command should execute.
        /// </summary>
        public event EventHandler CanExecuteChanged;


        /// <summary>
        /// Raises the can execute changed.
        /// </summary>
        public void RaiseCanExecuteChanged()
        {

            var handler = this.CanExecuteChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Defines the method that determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        /// <returns>true if this command can be executed; otherwise, false.</returns>
        public bool CanExecute(object parameter)
        {
            return this.canExecute == null || this.canExecute.Invoke();
        }

        /// <summary>
        /// Defines the method to be called when the command is invoked.
        /// </summary>
        /// <param name="parameter">Data used by the command.  If the command does not require data to be passed, this object can be set to null.</param>
        public virtual void Execute(object parameter)
        {
            if (CanExecute(parameter))
            {
                this.execute.Invoke();
            }
        }
    }
}