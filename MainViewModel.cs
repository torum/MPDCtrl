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
        private ICommand _playCommand;
        private ICommand _playNextCommand;
        private ICommand _playPrevCommand;
        private ICommand _setRepeatCommand;
        private ICommand _setRandomCommand;
        private ICommand _setVolumeCommand;
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

                    if (_MPC.MPDCurrentSong.ID != value.ID)
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

                if (Convert.ToDouble(_MPC.MPDStatus.MPDVolume) != value) {

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

                if (_MPC.MPDStatus.MPDRepeat != value) {
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

                if (_MPC.MPDStatus.MPDRandom != value)
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
            this._changeSongCommand = new WpfMPD.Common.RelayCommand(this.ChangeSongCommand_ExecuteAsync, this.ChangeSongCommand_CanExecute);
            this._changePlaylistCommand = new WpfMPD.Common.RelayCommand(this.ChangePlaylistCommand_ExecuteAsync, this.ChangePlaylistCommand_CanExecute);
            this._windowClosingCommand = new WpfMPD.Common.RelayCommand(this.WindowClosingCommand_Execute, this.WindowClosingCommand_CanExecute);

            //Create MPC instance.
            this._MPC = new MPC();

            //Assigned idle event.
            this._MPC.StatusChanged += new MPC.MPDStatusChanged(OnStatusChanged);

            //Connect to MPD server and query status and info.
            QueryStatus();


            //start idle.
            //ConnectIdle();
            
        }

        #region PRIVATE METHODS

        private async void ConnectIdle()
        {
            await _MPC.MPDIdleConnect();
        }

        private async void DisConnectIdle()
        {
            bool isDone = await _MPC.MPDIdleDisConnect();
        }

        private async void QueryStatus()
        {

            bool isDone = await _MPC.MPDQueryStatus();
            if (isDone)
            {
                //System.Diagnostics.Debug.WriteLine("QueryStatus is done.");

                UpdateButtonStatus();

                QueryCurrentPlayQueue();

            }
            else
            {
                System.Diagnostics.Debug.WriteLine("QueryStatus returned ACK." + "\n");
            }
        }

        private async void QueryCurrentPlayQueue()
        {
            IsBusy = true;
            try
            {
                bool isDone = await _MPC.MPDQueryCurrentPlaylist();
                if (isDone)
                {
                    //System.Diagnostics.Debug.WriteLine("QueryCurrentPlaylist is done.");

                    //change it quietly.
                    this._selectedSong = _MPC.MPDCurrentSong;
                    //let listview know it is changed.
                    this.NotifyPropertyChanged("SelectedSong");

                    //Listview selection changed event in the code behind takes care ScrollIntoView. 
                    //This is a VIEW matter.

                    QueryPlaylists();

                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("QueryCurrentPlayQueue returned ACK." + "\n");
                }
            }
            finally {
                IsBusy = false;
            }
        }

        private async void QueryPlaylists()
        {

            bool isDone = await _MPC.MPDQueryPlaylists();
            if (isDone)
            {
                //System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                //selected item should now read "Current Play Queue"
                //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

                //start idle.
                ConnectIdle();

            }
            else
            {
                System.Diagnostics.Debug.WriteLine("QueryPlaylists returned ACK." + "\n");
            }
        }

        private void UpdateButtonStatus()
        {
            //Play button
            switch (_MPC.MPDStatus.MPDState)
            {
                case MPC.Status.MPDPlayState.Play:
                    {
                        this.PlayButton = _pathPlayButton;
                        break;
                    }
                case MPC.Status.MPDPlayState.Pause:
                    {
                        this.PlayButton = _pathPauseButton;
                        break;
                    }
                case MPC.Status.MPDPlayState.Stop:
                    {
                        this.PlayButton = _pathStopButton;
                        break;
                    }
            }

            // "quietly" update view.
            this._volume = Convert.ToDouble(_MPC.MPDStatus.MPDVolume);
            this.NotifyPropertyChanged("Volume");

            this._random = _MPC.MPDStatus.MPDRandom;
            this.NotifyPropertyChanged("Random");

            this._repeat = _MPC.MPDStatus.MPDRepeat;
            this.NotifyPropertyChanged("Repeat");

            // no need to care about "double" updates.
            this.Time = Convert.ToDouble(_MPC.MPDStatus.MPDSongTime);
            this.Elapsed = Convert.ToDouble(_MPC.MPDStatus.MPDSongElapsed);
        }

        #endregion END of PRIVATE METHODS

        #region COMMANDS

        public ICommand PlayCommand { get { return this._playCommand; } }

        public bool PlayCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void PlayCommand_ExecuteAsync()
        {
            bool isDone = false;
            switch (_MPC.MPDStatus.MPDState)
            {
                case MPC.Status.MPDPlayState.Play:
                    {
                        //State>>Play: So, send Pause command
                        isDone = await _MPC.MPDPlaybackPause();
                        break;
                    }
                case MPC.Status.MPDPlayState.Pause:
                    {
                        //State>>Pause: So, send Resume command
                        isDone = await _MPC.MPDPlaybackResume();
                        break;
                    }
                case MPC.Status.MPDPlayState.Stop:
                    {
                        //State>>Stop: So, send Play command
                        isDone = await _MPC.MPDPlaybackPlay();
                        break;
                    }
            }

            if (isDone)
            {
                UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MPDCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayCommand returned ACK." + "\n");
            }

        }

        public ICommand PlayNextCommand { get { return this._playNextCommand; } }

        public bool PlayNextCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void PlayNextCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MPDPlaybackNext();

            if (isDone)
            {
                UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MPDCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayNextCommand returned ACK." + "\n");
            }
        }

        public ICommand PlayPrevCommand { get { return this._playPrevCommand; } }

        public bool PlayPrevCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void PlayPrevCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MPDPlaybackPrev();

            if (isDone)
            {
                UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MPDCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("PlayPrevCommand returned ACK. " + "\n");
            }
        }

        public ICommand SetRpeatCommand { get { return this._setRepeatCommand; } }

        public bool SetRpeatCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void SetRpeatCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MPDSetRepeat(this._repeat);

            if (isDone)
            {
                UpdateButtonStatus();
            }
        }

        public ICommand SetRandomCommand { get { return this._setRandomCommand; } }

        public bool SetRandomCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void SetRandomCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MPDSetRandom(this._random);

            if (isDone)
            {
                UpdateButtonStatus();
            }
        }

        public ICommand SetVolumeCommand { get { return this._setVolumeCommand; } }

        public bool SetVolumeCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void SetVolumeCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MPDSetVolume(Convert.ToInt32(this._volume));

            if (isDone)
            {
                UpdateButtonStatus();
            }
        }

        public ICommand ChangeSongCommand { get { return this._changeSongCommand; } }

        public bool ChangeSongCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void ChangeSongCommand_ExecuteAsync()
        {
            bool isDone = await _MPC.MPDPlaybackPlay(_selectedSong.ID);

            if (isDone)
            {
                UpdateButtonStatus();

                //change it quietly.
                this._selectedSong = _MPC.MPDCurrentSong;
                //let listview know it is changed.
                this.NotifyPropertyChanged("SelectedSong");
            }
        }

        public ICommand ChangePlaylistCommand { get { return this._changePlaylistCommand; } }

        public bool ChangePlaylistCommand_CanExecute()
        {
            //TODO if (this.IsBusy) { return false; } else { return true; }
            return true;
        }

        public async void ChangePlaylistCommand_ExecuteAsync()
        {
            if (this._selecctedPlaylist == "") return;

            this._selectedSong = null;

            //MPD >> clear load playlistinfo > returns and updates playlist.
            bool isDone = await _MPC.MPDChangePlaylist(this._selecctedPlaylist);
            if (isDone)
            {
                //Start play. MPD >> play status > returns and update status.
                isDone = await _MPC.MPDPlaybackPlay();
                if (isDone) { 

                    UpdateButtonStatus();

                    //this.SelectedSong = _MPC.MPDCurrentSong; //  <-don't

                    //change it quietly.
                    this._selectedSong = _MPC.MPDCurrentSong;
                    //let listview know it is changed.
                    this.NotifyPropertyChanged("SelectedSong");
                }
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

        private void OnStatusChanged(MPC sender, object data)
        {
            System.Diagnostics.Debug.WriteLine("OnStatusChanged: " + (data as string));
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion END of EVENTS


        private void NotifyPropertyChanged(string info)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }

}
