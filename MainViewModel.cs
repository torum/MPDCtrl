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

        #region PRIVATE FIELD declaration

        private static string pathPlayButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string pathPauseButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string pathStopButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private MPC _MPC;
        private MPC.Song _selectedSong;
        private bool _isChanged;
        private string _playButton;
        private double _volume;
        private bool _repeat;
        private bool _random;
        private ICommand playCommand;

        #endregion PRIVATE FIELD declaration

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
               // System.Diagnostics.Debug.WriteLine("\n\n☆ListView_SelectionChanged: " + value.Title);
                //TODO
                _selectedSong = value;
                this.NotifyPropertyChanged("SelectedSong");
            }
        }

        public ObservableCollection<string> Playlists
        {
            get { return _MPC.Playlists; }
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
                //TODO if != then command
                this._volume = value;
                this.NotifyPropertyChanged("Volume");
            }
        }

        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                //TODO MPC.setOption
                this._repeat = value;
                this.NotifyPropertyChanged("Repeat");
            }
        }

        public bool Random
        {
            get { return _random; }
            set
            {
                //TODO
                this._random = value;
                this.NotifyPropertyChanged("Random");
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

        #endregion END PUBLIC PROPERTY FIELD



        public MainViewModel()
        {
            this._isChanged = false;
            this.PlayButton = pathPlayButton;

            this.playCommand = new WpfMPD.Common.RelayCommand(this.PlayCommand_ExecuteAsync, this.PlayCommand_CanExecute);


            this._MPC = new MPC();
            QueryStatus();
            QueryCurrentPlayQueue();

            QueryPlaylists();


        }

        #region METHODS
        private async void QueryStatus()
        {

            bool isDone = await _MPC.MPDQueryStatus();
            if (isDone)
            {
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            //Play button
            switch (_MPC.MPDStatus.MPDState)
            {
                case MPC.Status.MPDPlayState.Play:
                    {
                        this.PlayButton = pathPlayButton;
                        break;
                    }
                case MPC.Status.MPDPlayState.Pause:
                    {
                        this.PlayButton = pathPauseButton;
                        break;
                    }
                case MPC.Status.MPDPlayState.Stop:
                    {
                        this.PlayButton = pathStopButton;
                        break;
                    }
            }


            this.Volume = _MPC.MPDStatus.MPDVolume;
            this.Random = _MPC.MPDStatus.MPDRandom;
            this.Repeat = _MPC.MPDStatus.MPDRepeat;

        }

        private async void QueryCurrentPlayQueue()
        {

            bool isDone = await _MPC.MPDQueryCurrentPlaylist();
            if (isDone)
            {
                //System.Diagnostics.Debug.WriteLine("QueryCurrentPlaylist is done.");

                //let listview know it is changed. 
                this.SelectedSong = _MPC.MPDCurrentSong;

                //Listview selection changed event in the code behind takes care ScrollIntoView. 
                //This is a VIEW matter.
            }
        }

        private async void QueryPlaylists()
        {

            bool isDone = await _MPC.MPDQueryPlaylists();
            if (isDone)
            {
                System.Diagnostics.Debug.WriteLine("QueryPlaylists is done.");

                //selected item should read "Current Play Queue"
                //https://stackoverflow.com/questions/2343446/default-text-for-templated-combo-box?rq=1


            }
        }

        #endregion

        #region COMMANDS

        public ICommand PlayCommand { get { return this.playCommand; } }
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
                UpdateUI();
            }
        }

        #endregion END COMMANDS

        #region EVENTS
        public event PropertyChangedEventHandler PropertyChanged;
        #endregion


        private void NotifyPropertyChanged(string info)
        {
            this.PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }

    }


}
