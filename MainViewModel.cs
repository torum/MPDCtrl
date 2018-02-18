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
                _selecctedPlaylist = value;
                this.NotifyPropertyChanged("SelectedPlaylist");

                System.Diagnostics.Debug.WriteLine("\n\nPlaylist_SelectionChanged: " + value);

                if (ChangePlaylistCommand.CanExecute(null))
                {
                    ChangePlaylistCommand.Execute(null);
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

                    //TODO try using ValueChanged Event using <i:Interaction.Triggers>
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

            //Assign commands
            this._playCommand = new WpfMPD.Common.RelayCommand(this.PlayCommand_ExecuteAsync, this.PlayCommand_CanExecute);
            this._playNextCommand = new WpfMPD.Common.RelayCommand(this.PlayNextCommand_ExecuteAsync, this.PlayNextCommand_CanExecute);
            this._playPrevCommand = new WpfMPD.Common.RelayCommand(this.PlayPrevCommand_ExecuteAsync, this.PlayPrevCommand_CanExecute);

            this._setRepeatCommand = new WpfMPD.Common.RelayCommand(this.SetRpeatCommand_ExecuteAsync, this.SetRpeatCommand_CanExecute);
            this._setRandomCommand = new WpfMPD.Common.RelayCommand(this.SetRandomCommand_ExecuteAsync, this.SetRandomCommand_CanExecute);
            this._setVolumeCommand = new WpfMPD.Common.RelayCommand(this.SetVolumeCommand_ExecuteAsync, this.SetVolumeCommand_CanExecute);

            this._changeSongCommand = new WpfMPD.Common.RelayCommand(this.ChangeSongCommand_ExecuteAsync, this.ChangeSongCommand_CanExecute);
            this._changePlaylistCommand = new WpfMPD.Common.RelayCommand(this.ChangePlaylistCommand_ExecuteAsync, this.ChangePlaylistCommand_CanExecute);

            //Create MPC instance.
            this._MPC = new MPC();

            //Connect to MPD server and query status and info.
            QueryStatus();
            QueryCurrentPlayQueue();
            QueryPlaylists();
            
        }

        #region PRIVATE METHODS

        private async void QueryStatus()
        {

            bool isDone = await _MPC.MPDQueryStatus();
            if (isDone)
            {
                //System.Diagnostics.Debug.WriteLine("QueryStatus is done.");

                UpdateButtonStatus();

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

                    //let listview know it is changed. 
                    this.SelectedSong = _MPC.MPDCurrentSong;

                    //Listview selection changed event in the code behind takes care ScrollIntoView. 
                    //This is a VIEW matter.
                    IsBusy = false;
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

                //selected item should read "Current Play Queue"
                //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1

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

            this.Volume = Convert.ToDouble(_MPC.MPDStatus.MPDVolume);
            this.Random = _MPC.MPDStatus.MPDRandom;
            this.Repeat = _MPC.MPDStatus.MPDRepeat;

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
            }

            //if playlist changes, and random sets on, don't know which is current untill status is returned.
            //so,
            //let listview know it is changed. 
            if (this._selectedSong == null)
            {
                this.SelectedSong = _MPC.MPDCurrentSong;
            }
            else
            {
                if (_MPC.MPDCurrentSong.ID != this._selectedSong.ID)
                {
                    this.SelectedSong = _MPC.MPDCurrentSong;
                }
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
            bool isDone = false;
            isDone = await _MPC.MPDPlaybackNext();

            if (isDone)
            {
                UpdateButtonStatus();

                //let listview know it is changed. 
                this.SelectedSong = _MPC.MPDCurrentSong;
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
            bool isDone = false;
            isDone = await _MPC.MPDPlaybackPrev();

            if (isDone)
            {
                UpdateButtonStatus();

                //let listview know it is changed. 
                this.SelectedSong = _MPC.MPDCurrentSong;
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
            bool isDone = false;
            isDone = await _MPC.MPDSetRepeat(this._repeat);

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
            bool isDone = false;
            isDone = await _MPC.MPDSetRandom(this._random);

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
            bool isDone = false;
            isDone = await _MPC.MPDSetVolume(Convert.ToInt32(this._volume));

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
            bool isDone = false;
            isDone = await _MPC.MPDPlaybackPlay(_selectedSong.ID);

            if (isDone)
            {
                UpdateButtonStatus();
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
            //IsBusy = true;
            try {
                bool isDone = false;
                isDone = await _MPC.MPDChangePlaylist(this._selecctedPlaylist);

                if (isDone)
                {
                    QueryCurrentPlayQueue();

                }
            }
            finally { 
                //IsBusy = false;
            }

            //TODO make other controls disabled
            //https://stackoverflow.com/questions/7346663/how-to-show-a-waitcursor-when-the-wpf-application-is-busy-databinding

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
