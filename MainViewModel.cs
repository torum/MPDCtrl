/// 
/// 
/// MPD Ctrl
/// https://github.com/torumyax/MPD-Ctrl
/// 
/// TODO:
///  Media keys.
///  Save settings
///  Settings page.
///  User friendly error message.
///
/// Known issue:
///
/// 

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;

namespace WpfMPD
{

    public class MainViewModel : INotifyPropertyChanged
    {

        #region PRIVATE FIELD DECLARATION
        
        private MPC _MPC;
        private MPC.Song _selectedSong;
        private bool _isChanged;
        private bool _isBusy;
        private string _playButton;
        private double _volume;
        private bool _repeat;
        private bool _random;
        private double _time;
        private double _elapsed;
        private string _selecctedPlaylist;
        private static string _pathPlayButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathPauseButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathStopButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private DispatcherTimer _elapsedTimer;
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

        #endregion END of PRIVATE FIELD declaration

        #region PUBLIC PROPERTY FIELD

        public ObservableCollection<MPC.Song> Songs
        {
            get { return _MPC.CurrentQueue; }
        }

        public MPC.Song SelectedSong
        {
            get
            {
                return _selectedSong;
            }
            set
            {
                if (value != null) {
                    _selectedSong = value;
                    this.NotifyPropertyChanged("SelectedSong");

                    System.Diagnostics.Debug.WriteLine("\n\nListView_SelectionChanged: " + value.Title);

                    if (_MPC.MpdCurrentSong.ID != value.ID)
                    {
                        if (ChangeSongCommand.CanExecute(null))
                        {
                            ChangeSongCommand.Execute(null);
                        }
                    }
                }
            }
        }

        public ObservableCollection<string> Playlists
        {
            get { return _MPC.Playlists; }
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

                    if (_selecctedPlaylist != "")
                    {
                        System.Diagnostics.Debug.WriteLine("\n\nPlaylist_SelectionChanged: " + _selecctedPlaylist);

                        if (ChangePlaylistCommand.CanExecute(null))
                        {
                            ChangePlaylistCommand.Execute(null);
                        }
                    }
                }
            }
        }

        public string PlayButton {
            get
            {
                return this._playButton;
            }
            set
            {
                this._playButton = value;
                this.NotifyPropertyChanged("PlayButton");
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
                this._volume = value;
                this.NotifyPropertyChanged("Volume");

                if (Convert.ToDouble(_MPC.MpdStatus.MpdVolume) != value) {

                    //TODO try using ValueChanged Event using <i:Interaction.Triggers>  ?
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
                this._repeat = value;
                this.NotifyPropertyChanged("Repeat");

                if (_MPC.MpdStatus.MpdRepeat != value) {
                    if (SetRpeatCommand.CanExecute(null))
                    {
                        SetRpeatCommand.Execute(null);
                    }
                }
            }
        }

        public bool Random
        {
            get { return _random; }
            set
            {
                this._random = value;
                this.NotifyPropertyChanged("Random");

                if (_MPC.MpdStatus.MpdRandom != value)
                {
                    if (SetRandomCommand.CanExecute(null))
                    {
                        SetRandomCommand.Execute(null);
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
                this._elapsed = value;
                this.NotifyPropertyChanged("Elapsed");

                System.Diagnostics.Debug.WriteLine("\n\nSlider_Elapsed Changed: " + value.ToString());

                if (SetSeekCommand.CanExecute(null))
                {
                    SetSeekCommand.Execute(null);
                }
            }
        }

        public bool IsChanged
        {
            get
            {
                return this._isChanged;
            }
            set
            {
                this._isChanged = value;
                this.NotifyPropertyChanged("IsChanged");
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

        #endregion END of PUBLIC PROPERTY FIELD

        //Constructor
        public MainViewModel()
        {
            this._isChanged = false;

            //Initialize play button with "play" state.
            this.PlayButton = _pathPlayButton;

            this._selecctedPlaylist = "";

            //Assign commands
            this._playCommand = new WpfMPD.Common.RelayCommand(this.PlayCommand_ExecuteAsync, this.PlayCommand_CanExecute);
            this._playNextCommand = new WpfMPD.Common.RelayCommand(this.PlayNextCommand_ExecuteAsync, this.PlayNextCommand_CanExecute);
            this._playPrevCommand = new WpfMPD.Common.RelayCommand(this.PlayPrevCommand_ExecuteAsync, this.PlayPrevCommand_CanExecute);
            this._setRepeatCommand = new WpfMPD.Common.RelayCommand(this.SetRpeatCommand_ExecuteAsync, this.SetRpeatCommand_CanExecute);
            this._setRandomCommand = new WpfMPD.Common.RelayCommand(this.SetRandomCommand_ExecuteAsync, this.SetRandomCommand_CanExecute);
            this._setVolumeCommand = new WpfMPD.Common.RelayCommand(this.SetVolumeCommand_ExecuteAsync, this.SetVolumeCommand_CanExecute);
            this._setSeekCommand = new WpfMPD.Common.RelayCommand(this.SetSeekCommand_ExecuteAsync, this.SetSeekCommand_CanExecute);
            this._changeSongCommand = new WpfMPD.Common.RelayCommand(this.ChangeSongCommand_ExecuteAsync, this.ChangeSongCommand_CanExecute);
            this._changePlaylistCommand = new WpfMPD.Common.RelayCommand(this.ChangePlaylistCommand_ExecuteAsync, this.ChangePlaylistCommand_CanExecute);
            this._windowClosingCommand = new WpfMPD.Common.RelayCommand(this.WindowClosingCommand_Execute, this.WindowClosingCommand_CanExecute);

            //Create MPC instance.
            this._MPC = new MPC();

            //Assigned idle event.
            this._MPC.StatusChanged += new MPC.MpdStatusChanged(OnStatusChanged);

            //Song time elapsed timer.
            _elapsedTimer = new DispatcherTimer();
            _elapsedTimer.Interval = TimeSpan.FromMilliseconds(1000);//new TimeSpan(0, 0, 1);
            _elapsedTimer.Tick += new EventHandler(ElapsedTimer);

            //Connect to MPD server and query status and info.
            QueryStatus();

            //start idle connection, but don't start idle mode yet.
            ConnectIdle();

        }

        #region PRIVATE METHODS

        private async void ConnectIdle()
        {
            await _MPC.MpdIdleConnect();
        }

        private async void DisConnectIdle()
        {
            bool isDone = await _MPC.MpdIdleDisConnect();
        }

        private async void OnStatusChanged(MPC sender, object data)
        {
            if (data == null) { return; }

            // list of SubSystems we are subscribing.
            // player mixer options playlist stored_playlist

            bool isPlayer = false;
            bool isPlaylist = false;
            bool isStoredPlaylist = false;
            foreach (var subsystem in (data as List<string>))
            {
                System.Diagnostics.Debug.WriteLine("OnStatusChanged: " + subsystem);

                if (subsystem == "player")
                {
                    isPlayer = true;
                }
                else if (subsystem == "mixer")
                {
                    isPlayer = true;
                }
                else if (subsystem == "options")
                {
                    isPlayer = true;
                }
                else if (subsystem == "playlist")
                {
                    isPlaylist = true;
                }
                else if (subsystem == "stored_playlist")
                {
                    isStoredPlaylist = true;
                }
            }


            if ((isPlayer && isPlaylist))
            {
                IsBusy = true;

                // Reset view.
                _MPC.CurrentQueue.Clear();
                this._selectedSong = null;
                this.NotifyPropertyChanged("SelectedSong");
                this._selecctedPlaylist = "";
                this.NotifyPropertyChanged("SelectedPlaylist");
                UpdateButtonStatus();

                // Get updated information.
                bool isDone = await sender.MpdQueryCurrentPlaylist();
                if (isDone)
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryCurrentPlaylist> is done.");

                    // Update status.
                    isDone = await sender.MpdQueryStatus();
                    if (isDone)
                    {
                        System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");

                        UpdateButtonStatus();

                        // Change it quietly.
                        if (sender.MpdCurrentSong != null)
                        {
                            this._selectedSong = sender.MpdCurrentSong;
                            // Let listview know it is changed.
                            this.NotifyPropertyChanged("SelectedSong");
                        }

                        // Listview selection changed event in the code behind takes care ScrollIntoView. 
                        // This is a VIEW matter.

                        //Testing
                        await _MPC.MpdIdleStart();
                    }
                    else
                    {
                        IsBusy = false;
                        // Let user know.
                        System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                    }

                    if (isStoredPlaylist)
                    {
                        // Retrieve playlists
                        sender.Playlists.Clear();
                        isDone = await sender.MpdQueryPlaylists();
                        if (isDone)
                        {
                            //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                            // Selected item should now read "Current Play Queue"
                            //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                            IsBusy = false;
                        }
                        else
                        {
                            IsBusy = false;
                            //TODO: Let user know.
                            System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                        }
                    }
                    else
                    {
                        IsBusy = false;
                    }
                }
                else
                {
                    IsBusy = false;
                    //TODO: Let user know.
                    System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                }
            }
            else if (isPlaylist)
            {
                IsBusy = true;

                // Reset view.
                sender.CurrentQueue.Clear();
                this._selectedSong = null;
                this.NotifyPropertyChanged("SelectedSong");
                this._selecctedPlaylist = "";
                this.NotifyPropertyChanged("SelectedPlaylist");
                UpdateButtonStatus();

                // Get updated information.
                bool isDone = await sender.MpdQueryCurrentPlaylist();
                if (isDone)
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryCurrentPlaylist> is done.");

                    // Update status.
                    isDone = await sender.MpdQueryStatus();
                    if (isDone)
                    {
                        System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");

                        UpdateButtonStatus();

                        // Change it quietly.
                        if (sender.MpdCurrentSong != null)
                        {
                            this._selectedSong = sender.MpdCurrentSong;
                            // Let listview know it is changed.
                            this.NotifyPropertyChanged("SelectedSong");
                        }

                        // Listview selection changed event in the code behind takes care ScrollIntoView. 
                        // This is a VIEW matter.

                        //Testing
                        await _MPC.MpdIdleStart();

                        if (isStoredPlaylist)
                        {
                            // Retrieve playlists
                            sender.Playlists.Clear();
                            isDone = await sender.MpdQueryPlaylists();
                            if (isDone)
                            {
                                //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                                // Selected item should now read "Current Play Queue"
                                //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                                IsBusy = false;
                            }
                            else
                            {
                                IsBusy = false;
                                //TODO: Let user know.
                                System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                            }
                        }
                        else
                        {
                            IsBusy = false;
                        }
                    }
                    else
                    {
                        IsBusy = false;
                        // Let user know.
                        System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                    }
                }
                else
                {
                    IsBusy = false;
                    //TODO: Let user know.
                    System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
                }
            }
            else if (isPlayer)
            {

                IsBusy = true;

                // Reset view.
                this._selectedSong = null;
                this.NotifyPropertyChanged("SelectedSong");
                UpdateButtonStatus();

                // Update status.
                bool isDone = await sender.MpdQueryStatus();
                if (isDone)
                {
                    System.Diagnostics.Debug.WriteLine("OnStatusChanged <MpdQueryStatus> is done.");

                    UpdateButtonStatus();

                    // Change it quietly.
                    if (sender.MpdCurrentSong != null)
                    {
                        this._selectedSong = sender.MpdCurrentSong;
                        // Let listview know it is changed.
                        this.NotifyPropertyChanged("SelectedSong");
                    }
                    // Listview selection changed event in the code behind takes care ScrollIntoView. 
                    // This is a VIEW matter.

                    //Testing
                    await _MPC.MpdIdleStart();

                    if (isStoredPlaylist)
                    {
                        // Retrieve playlists
                        sender.Playlists.Clear();
                        isDone = await sender.MpdQueryPlaylists();
                        if (isDone)
                        {
                            //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                            // Selected item should now read "Current Play Queue"
                            //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                            IsBusy = false;
                        }
                        else
                        {
                            IsBusy = false;
                            //TODO: Let user know.
                            System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                        }
                    }
                    else
                    {
                        IsBusy = false;
                    }
                }
                else
                {
                    IsBusy = false;
                    // Let user know.
                    System.Diagnostics.Debug.WriteLine("MpdQueryStatus returned with false." + "\n");
                }
            }
            else if (isStoredPlaylist)
            {
                this._selecctedPlaylist = "";
                this.NotifyPropertyChanged("SelectedPlaylist");

                if (isStoredPlaylist)
                {
                    // Retrieve playlists
                    sender.Playlists.Clear();
                    bool isDone = await sender.MpdQueryPlaylists();
                    if (isDone)
                    {
                        //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                        // Selected item should now read "Current Play Queue"
                        //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                        IsBusy = false;
                    }
                    else
                    {
                        IsBusy = false;
                        //TODO: Let user know.
                        System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
                    }

                    //Testing
                    await _MPC.MpdIdleStart();
                }
                else
                {
                    IsBusy = false;
                }
            }

            // Don't.It's already done.
            //bool isDone = await _MPC.MpdIdleStart();
        }

        private async void QueryStatus()
        {
            IsBusy = true;
            bool isDone = await _MPC.MpdQueryStatus();
            if (isDone)
            {
                //System.Diagnostics.Debug.WriteLine("QueryStatus is done.");

                UpdateButtonStatus();
                IsBusy = false;

                //retrieve play queue
                QueryCurrentPlayQueue();
            }
            else
            {
                IsBusy = false;
                //TODO: connection fail to establish. 
                // Let user know.
                System.Diagnostics.Debug.WriteLine("QueryStatus returned with false." + "\n");
            }
            
        }

        private async void QueryCurrentPlayQueue()
        {
            IsBusy = true;
            bool isDone = await _MPC.MpdQueryCurrentPlaylist();
            if (isDone)
            {
                //System.Diagnostics.Debug.WriteLine("QueryCurrentPlaylist is done.");

                if (_MPC.CurrentQueue.Count > 0) {
                    //change it quietly.
                    this._selectedSong = _MPC.MpdCurrentSong;
                    //let listview know it is changed.
                    this.NotifyPropertyChanged("SelectedSong");

                    //Listview selection changed event in the code behind takes care ScrollIntoView. 
                    //This is a VIEW matter.
                }

                IsBusy = false;

                //retrieve playlists
                QueryPlaylists();
            }
            else
            {
                IsBusy = false;
                //TODO: connection fail to establish. 
                // Let user know.
                System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned false." + "\n");
            }

        }

        private async void QueryPlaylists()
        {
            IsBusy = true;
            bool isDone = await _MPC.MpdQueryPlaylists();
            if (isDone)
            {
                //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                //selected item should now read "Current Play Queue"
                //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                IsBusy = false;

                //start idle mode. //TODO is this the right place?
                isDone = await _MPC.MpdIdleStart();
            }
            else
            {
                IsBusy = false;
                //TODO: connection fail to establish. 
                // Let user know.
                System.Diagnostics.Debug.WriteLine("QueryPlaylists returned false." + "\n");
            }
        }

        private void UpdateButtonStatus()
        {
            //Play button
            switch (_MPC.MpdStatus.MpdState)
            {
                case MPC.Status.MpdPlayState.Play:
                    {
                        this.PlayButton = _pathPlayButton;
                        break;
                    }
                case MPC.Status.MpdPlayState.Pause:
                    {
                        this.PlayButton = _pathPauseButton;
                        break;
                    }
                case MPC.Status.MpdPlayState.Stop:
                    {
                        this.PlayButton = _pathStopButton;
                        break;
                    }
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

        #endregion END of PRIVATE METHODS

        #region COMMANDS

        public ICommand PlayCommand { get { return this._playCommand; } }

        public bool PlayCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; } 
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayCommand_ExecuteAsync()
        {
            bool isDone = false;
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

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MpdCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayCommand returned false." + "\n");
            }

        }

        public ICommand PlayNextCommand { get { return this._playNextCommand; } }

        public bool PlayNextCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayNextCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdPlaybackNext();

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MpdCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayNextCommand returned false." + "\n");
            }
        }

        public ICommand PlayPrevCommand { get { return this._playPrevCommand; } }

        public bool PlayPrevCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void PlayPrevCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdPlaybackPrev();

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MpdCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayPrevCommand returned false. " + "\n");
            }
        }

        public ICommand SetRpeatCommand { get { return this._setRepeatCommand; } }

        public bool SetRpeatCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            return true;
        }

        public async void SetRpeatCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdSetRepeat(this._repeat);

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetRepeat returned false");
            }
        }

        public ICommand SetRandomCommand { get { return this._setRandomCommand; } }

        public bool SetRandomCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            return true;
        }

        public async void SetRandomCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdSetRandom(this._random);

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetRandom returned false");
            }
        }

        public ICommand SetVolumeCommand { get { return this._setVolumeCommand; } }

        public bool SetVolumeCommand_CanExecute()
        {
            //if (this.IsBusy) { return false; }
            return true;
        }

        public async void SetVolumeCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdSetVolume(Convert.ToInt32(this._volume));

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdSetVolume returned false");
            }
        }

        public ICommand SetSeekCommand { get { return this._setSeekCommand; } }

        public bool SetSeekCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }
            return true;
        }

        public async void SetSeekCommand_ExecuteAsync()
        {
            
            bool isDone = await _MPC.MpdPlaybackSeek(_MPC.MpdStatus.MpdSongID, Convert.ToInt32(this._elapsed));

            if (isDone)
            {
                //Don't need to. Timer takes care of updating slider elapsed value.
                //UpdateButtonStatus();
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackSeek returned false");
            }
            
        }

        public ICommand ChangeSongCommand { get { return this._changeSongCommand; } }

        public bool ChangeSongCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }
            if (_MPC.CurrentQueue.Count < 1) { return false; }
            return true;
        }

        public async void ChangeSongCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MpdPlaybackPlay(_selectedSong.ID);

            if (isDone)
            {
                // Don't. Let idle connection do the job.
                //UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MpdCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackPlay returned false");
            }
        }

        public ICommand ChangePlaylistCommand { get { return this._changePlaylistCommand; } }

        public bool ChangePlaylistCommand_CanExecute()
        {
            if (this.IsBusy) { return false; }
            return true;
        }

        public async void ChangePlaylistCommand_ExecuteAsync()
        {
            if (this._selecctedPlaylist == "") return;

            this._selectedSong = null;
            _MPC.CurrentQueue.Clear();

            //MPD >> clear load playlistinfo > returns and updates playlist.
            bool isDone = await _MPC.MpdChangePlaylist(this._selecctedPlaylist);
            if (isDone)
            {
                if (_MPC.CurrentQueue.Count > 0) {
                    //Start play. MPD >> play status > returns and update status.
                    isDone = await _MPC.MpdPlaybackPlay();
                    if (isDone)
                    {

                        // Don't. Let idle connection do the job.
                        //UpdateButtonStatus();

                        // Don't. Let idle connection do the job.
                        if (_MPC.MpdCurrentSong != null)
                        {
                            //change it quietly.
                            //this._selectedSong = _MPC.MpdCurrentSong;
                            //let listview know it is changed.
                            //this.NotifyPropertyChanged("SelectedSong");
                        }
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("\n\nMpdPlaybackPlay returned false");
                    }
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("\n\nMpdChangePlaylist returned false");
            }

            //TODO make other controls disabled
            //https://stackoverflow.com/questions/7346663/how-to-show-a-waitcursor-when-the-wpf-application-is-busy-databinding

        }

        public ICommand WindowClosingCommand { get { return this._windowClosingCommand; } }

        public bool WindowClosingCommand_CanExecute()
        {
            return true;
        }

        public void WindowClosingCommand_Execute()
        {
            //https://stackoverflow.com/questions/3683450/handling-the-window-closing-event-with-wpf-mvvm-light-toolkit
            //TODO close connection, save ini setting etc.
            
            //System.Diagnostics.Debug.WriteLine("WindowClosingCommand");

            //disconnect idle connection.
            DisConnectIdle();
        }


        #endregion END of COMMANDS

        #region EVENTS


        public event PropertyChangedEventHandler PropertyChanged;

        #endregion END of EVENTS


        private void NotifyPropertyChanged(string info)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }

}
