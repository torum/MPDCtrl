using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using MPDCtrl.Services;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;
using System.Diagnostics;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace MPDCtrl.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {

        private MPC _mpc;
        private Connection _con;

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

        #region == Current queue ==

        private SongInfo _selectedItem;
        public SongInfo SelectedItem
        {
            get => _selectedItem;
            set
            {
                if (value == _selectedItem)
                    return;

                SetProperty(ref _selectedItem, value);

                OnItemSelected(value);
            }
        }

        public ObservableCollection<SongInfo> Queue
        {
            get
            {
                if (_con != null)
                {
                    return _con.Queue;
                }
                else
                {
                    return null;
                }
            }
        }

        #endregion

        #region == Player controls ==

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

                    if (_mpc != null)
                    {
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
            if (_mpc != null)
            {
                if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) != _volume)
                {
                    if (VolumeSliderCommand.CanExecute(null))
                    {
                        VolumeSliderCommand.Execute(null);
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
                if (_repeat != value)
                {
                    _repeat = value;
                    NotifyPropertyChanged("Repeat");

                    if (_mpc != null)
                    {
                        if (_mpc.MpdStatus.MpdRepeat != value)
                        {
                            if (RepeatButtonCommand.CanExecute(null))
                            {
                                RepeatButtonCommand.Execute(null);
                            }
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
                if (_random != value)
                {
                    _random = value;
                    NotifyPropertyChanged("Random");

                    if (_mpc != null)
                    {
                        if (_mpc.MpdStatus.MpdRandom != value)
                        {
                            if (RandomButtonCommand.CanExecute(null))
                            {
                                RandomButtonCommand.Execute(null);
                            }
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
                if (_consume != value)
                {
                    _consume = value;
                    NotifyPropertyChanged("Consume");

                    if (_mpc != null)
                    {
                        if (_mpc.MpdStatus.MpdConsume != value)
                        {
                            if (ConsumeButtonCommand.CanExecute(null))
                            {
                                ConsumeButtonCommand.Execute(null);
                            }
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
                if (_single != value)
                {
                    _single = value;
                    NotifyPropertyChanged("Single");

                    if (_mpc != null)
                    {
                        if (_mpc.MpdStatus.MpdSingle != value)
                        {
                            if (SingleButtonCommand.CanExecute(null))
                            {
                                SingleButtonCommand.Execute(null);
                            }
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
                NotifyPropertyChanged("TimeRemainFormated");
            }
        }

        public string TimeRemainFormated
        {
            get
            {
                string _timeRemainFormatted = "";
                try
                {
                    int sec, min, hour, s;

                    sec = Convert.ToInt32(Time - Elapsed);

                    //sec = Int32.Parse(_time);
                    min = sec / 60;
                    s = sec % 60;
                    hour = min / 60;
                    min = min % 60;

                    if ((hour == 0) && min == 0)
                    {
                        // 見た目がよくない
                        //_timeRemainFormatted = "-" + String.Format("{0}", s);

                        _timeRemainFormatted = "-" + String.Format("{0}:{1:00}", min, s);
                    }
                    else if ((hour == 0) && (min != 0))
                    {
                        _timeRemainFormatted = "-" + String.Format("{0}:{1:00}", min, s);
                    }
                    else if ((hour != 0) && (min != 0))
                    {
                        _timeRemainFormatted = "-" + String.Format("{0}:{1:00}:{2:00}", hour, min, s);
                    }
                    else if (hour != 0)
                    {
                        _timeRemainFormatted = "-" + String.Format("{0}:{1:00}:{2:00}", hour, min, s);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Oops@TimeFormated: " + Time + " : " + hour.ToString() + " " + min.ToString() + " " + s.ToString());
                    }
                }
                catch (FormatException e)
                {
                    // Ignore.
                    // System.Diagnostics.Debug.WriteLine(e.Message);
                    System.Diagnostics.Debug.WriteLine("Wrong Time format. " + Time + " " + e.Message);
                }

                return _timeRemainFormatted;
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
                    
                    // If we have a timer and we are in this event handler, a user is still interact with the slider
                    // we stop the timer
                    if (_elapsedDelayTimer != null)
                        _elapsedDelayTimer.Stop();

                    // we always create a new instance of Timer
                    _elapsedDelayTimer = new System.Timers.Timer();
                    _elapsedDelayTimer.AutoReset = false;

                    // if one second passes, that means our user has stopped interacting with the slider
                    // we do real event
                    _elapsedDelayTimer.Interval = (double)1000;
                    _elapsedDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeElapsed);

                    _elapsedDelayTimer.Start();
                }
                NotifyPropertyChanged("ElapsedFormated");
                NotifyPropertyChanged("TimeRemainFormated");
            }
        }

        public string ElapsedFormated
        {
            get
            {
                string _elapsedFormatted = "";
                try
                {
                    int sec, min, hour, s;

                    sec = Convert.ToInt32(Elapsed);

                    min = sec / 60;
                    s = sec % 60;
                    hour = min / 60;
                    min = min % 60;

                    if ((hour == 0) && min == 0)
                    {
                        // 見た目がよくない
                        //_elapsedFormatted = String.Format("{0}", s);

                        _elapsedFormatted = String.Format("{0}:{1:00}", min, s);
                    }
                    else if ((hour == 0) && (min != 0))
                    {
                        _elapsedFormatted = String.Format("{0}:{1:00}", min, s);
                    }
                    else if ((hour != 0) && (min != 0))
                    {
                        _elapsedFormatted = String.Format("{0}:{1:00}:{2:00}", hour, min, s);
                    }
                    else if (hour != 0)
                    {
                        _elapsedFormatted = String.Format("{0}:{1:00}:{2:00}", hour, min, s);
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("Oops@_elapsedFormatted: " + Time + " : " + hour.ToString() + " " + min.ToString() + " " + s.ToString());
                    }
                }
                catch (FormatException e)
                {
                    // Ignore.
                    // System.Diagnostics.Debug.WriteLine(e.Message);
                    System.Diagnostics.Debug.WriteLine("Wrong Time format. " + Elapsed + " " + e.Message);
                }

                return _elapsedFormatted;
            }
        }

        private System.Timers.Timer _elapsedDelayTimer = null;
        private void DoChangeElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_mpc != null)
            {
                if ((_elapsed < _time))
                {
                    if (SeekSliderCommand.CanExecute(null))
                    {
                        SeekSliderCommand.Execute(null);
                    }
                }
            }
        }

        #endregion

        #region == Button Images for Player State ==

        private ImageSource _playButtonImageSource = "Player-play-circle-86-black.png";
        public ImageSource PlayButtonImageSource
        {
            get => _playButtonImageSource;
            set
            {
                SetProperty(ref _playButtonImageSource, value);
            }
        }

        private ImageSource _repeatButtonImageSource = "Player-repeat-off.png";
        public ImageSource RepeatButtonImageSource
        {
            get => _repeatButtonImageSource;
            set
            {
                SetProperty(ref _repeatButtonImageSource, value);
            }
        }

        private ImageSource _shuffleButtonImageSource = "Player-shuffle-off.png";
        public ImageSource ShuffleButtonImageSource
        {
            get => _shuffleButtonImageSource;
            set
            {
                SetProperty(ref _shuffleButtonImageSource, value);
            }
        }

        private ImageSource _singleButtonImageSource = "Player-single-off.png";
        public ImageSource SingleButtonImageSource
        {
            get => _singleButtonImageSource;
            set
            {
                SetProperty(ref _singleButtonImageSource, value);
            }
        }

        private ImageSource _consumeButtonImageSource = "Player-consume-off.png";
        public ImageSource ConsumeButtonImageSource
        {
            get => _consumeButtonImageSource;
            set
            {
                SetProperty(ref _consumeButtonImageSource, value);
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

        //
        private System.Timers.Timer _elapsedTimer;

        public event EventHandler<SongInfo> ScrollIntoView;

        public HomeViewModel()
        {
            Title = "Now Playing";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);
            _mpc.StatusUpdate += new MPC.MpdStatusUpdate(OnMpdStatusUpdate);

            Elapsed = 0;
            Time = 0.1; // 0 causes xamarin to crash.

            _elapsedTimer = new System.Timers.Timer();
            _elapsedTimer.Interval = (double)1000;
            _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);

            PlayButtonCommand = new Command(() => Play());
            PlayBackButtonCommand = new Command(() => PlayBack());
            PlayNextButtonCommand = new Command(() => PlayNext());
            RandomButtonCommand = new Command(() => SetRandom());
            RepeatButtonCommand = new Command(() => SetRepeat());
            ConsumeButtonCommand = new Command(() => SetConsume());
            SingleButtonCommand = new Command(() => SetSingle());
            SingleButtonCommand = new Command(() => SetSingle());
            VolumeSliderCommand = new Command(() => SetVolume());
            SeekSliderCommand = new Command(() => SetSeek());

            ItemSelected = new Command<SongInfo>(OnItemSelected);
        }

        public void OnAppearing()
        {
            if (_con.IsConnecting)
            {
                IsBusy = true;
            }

            if (_con.IsConnected && _con.IsMpdConnected)
            {
                //_mpc.MpdQueryStatus();
            }
        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }

        private void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_elapsed <= _time) && (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play))
            {
                _elapsed += 1;
                NotifyPropertyChanged("Elapsed");
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        private void UpdateButtonStatus()
        {
            try
            {
                //Play button
                switch (_mpc.MpdStatus.MpdState)
                {
                    case Status.MpdPlayState.Play:
                        {
                            PlayButtonImageSource = "Player-pause-circle-86-black.png";
                            break;
                        }
                    case Status.MpdPlayState.Pause:
                        {
                            PlayButtonImageSource = "Player-play-circle-86-black.png";
                            break;
                        }
                    case Status.MpdPlayState.Stop:
                        {
                            PlayButtonImageSource = "Player-play-circle-86-black.png";
                            break;
                        }
                }

                Random = _mpc.MpdStatus.MpdRandom;
                if (Random)
                    ShuffleButtonImageSource = "Player-shuffle-on.png";
                else
                    ShuffleButtonImageSource = "Player-shuffle-off.png";

                Repeat = _mpc.MpdStatus.MpdRepeat;
                if (Repeat)
                    RepeatButtonImageSource = "Player-repeat-on.png";
                else
                    RepeatButtonImageSource = "Player-repeat-off.png";

                Consume = _mpc.MpdStatus.MpdConsume;
                if (Consume)
                    ConsumeButtonImageSource = "Player-consume-on.png";
                else
                    ConsumeButtonImageSource = "Player-consume-off.png";

                Single = _mpc.MpdStatus.MpdSingle;
                if (Single)
                    SingleButtonImageSource = "Player-single-on.png";
                else
                    SingleButtonImageSource = "Player-single-off.png";

                // 0 causes xamarin crash.
                if (_mpc.MpdStatus.MpdSongTime > 0)
                {
                    _time = _mpc.MpdStatus.MpdSongTime;
                    NotifyPropertyChanged("Time");
                }

                _elapsed = _mpc.MpdStatus.MpdSongElapsed;
                NotifyPropertyChanged("Elapsed");

                NotifyPropertyChanged("TimeRemainFormated");
                NotifyPropertyChanged("TimeRemainFormated");

                _volume = Convert.ToDouble(_mpc.MpdStatus.MpdVolume);
                NotifyPropertyChanged("Volume");

                // Start elapsed timer.
                if (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play)
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

        private async void OnMpdStatusUpdate(MPC sender, object data)
        {
            if ((data as string) == "isPlayer")
            {
                UpdateButtonStatus();

                bool isSongChanged = false;
                if (CurrentSong != null)
                {
                    if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                    {
                        isSongChanged = true;

                        // Clear IsPlaying icon
                        CurrentSong.IsPlaying = false;

                        //IsAlbumArtVisible = false;
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            AlbumArt = _albumArtDefault;
                        });
                    }
                }

                Device.BeginInvokeOnMainThread(() =>
                {
                    // Sets Current Song
                    var item = _con.Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (item != null)
                    {
                        CurrentSong = (item as SongInfo);
                        (item as SongInfo).IsPlaying = true;

                        _selectedItem = CurrentSong;
                        NotifyPropertyChanged("SelectedItem");

                        ScrollIntoView?.Invoke(this, CurrentSong);
                    }
                    else
                    {
                        CurrentSong = null;
                    }


                });

                await Task.Delay(500);

                if (isSongChanged && (CurrentSong != null))
                {
                    // AlbumArt
                    if (!String.IsNullOrEmpty(CurrentSong.file))
                    {
                        _mpc.MpdQueryAlbumArt(CurrentSong.file);
                    }
                }

            }
            else if ((data as string) == "isCurrentQueue")
            {
                Device.BeginInvokeOnMainThread(() =>
                {
                    // Set Current and NowPlaying.
                    var curitem = _con.Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem != null)
                    {
                        CurrentSong = (curitem as SongInfo);
                        (curitem as SongInfo).IsPlaying = true;
                        
                        // AlbumArt
                        if (_mpc.AlbumArt.SongFilePath != curitem.file)
                        {
                            //IsAlbumArtVisible = false;
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                AlbumArt = _albumArtDefault;

                                if (!String.IsNullOrEmpty((curitem as SongInfo).file))
                                {
                                    _mpc.MpdQueryAlbumArt((curitem as SongInfo).file);
                                }
                            });
                        }
                    }
                    else
                    {
                        CurrentSong = null;

                        //IsAlbumArtVisible = false;
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            AlbumArt = _albumArtDefault;
                        });
                        
                    }
                });
            }
            else if ((data as string) == "isAlbumart")
            {
                if (CurrentSong != null)
                {
                    if (CurrentSong.file == _mpc.AlbumArt.SongFilePath)
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                        });
                    }
                }

                /*
                Device.BeginInvokeOnMainThread(() =>
                {
                    Debug.WriteLine("isAlbumart0: " + CurrentSong.file + " : " + _mpc.AlbumArt.SongFilePath);
                    AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                });
                if ((!_mpc.AlbumArt.IsDownloading) && _mpc.AlbumArt.IsSuccess)
                {
                    Device.BeginInvokeOnMainThread(() =>
                    {
                        Debug.WriteLine("isAlbumart1: " + CurrentSong.file + " : " + _mpc.AlbumArt.SongFilePath);
                        AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                    });
                    if ((CurrentSong != null) && (_mpc.AlbumArt.AlbumImageSource != null))
                    {
                        Device.BeginInvokeOnMainThread(() =>
                        {
                            Debug.WriteLine("isAlbumart2: " + CurrentSong.file + " : " + _mpc.AlbumArt.SongFilePath);
                            AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                        });
                        // AlbumArt
                        if (!String.IsNullOrEmpty(CurrentSong.file))
                        {
                            Device.BeginInvokeOnMainThread(() =>
                            {
                                Debug.WriteLine("isAlbumart3: " + CurrentSong.file + " : "+ _mpc.AlbumArt.SongFilePath);
                                AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                            });
                            if (CurrentSong.file == _mpc.AlbumArt.SongFilePath)
                            {
                                Device.BeginInvokeOnMainThread(() =>
                                {
                                    Debug.WriteLine("isAlbumart4");
                                    AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                                });
                                //IsAlbumArtVisible = true;
                            }
                        }
                    }
                }
                */
            }
        }

        #region == Player commands ==

        public ICommand PlayButtonCommand { get; }
        void Play()
        {
            if (_con.IsConnected)
            {
                switch (_mpc.MpdStatus.MpdState)
                {
                    case Status.MpdPlayState.Play:
                        {
                            //State>>Play: So, send Pause command
                            _mpc.MpdPlaybackPause();
                            break;
                        }
                    case Status.MpdPlayState.Pause:
                        {
                            //State>>Pause: So, send Resume command
                            _mpc.MpdPlaybackResume();
                            break;
                        }
                    case Status.MpdPlayState.Stop:
                        {
                            //State>>Stop: So, send Play command
                            _mpc.MpdPlaybackPlay();
                            break;
                        }
                }
            }
        }

        public ICommand PlayBackButtonCommand { get; }
        void PlayBack()
        {
            if (_con.IsConnected)
            {
                _mpc.MpdPlaybackPrev();
            }
        }

        public ICommand PlayNextButtonCommand { get; }
        void PlayNext()
        {
            if (_con.IsConnected)
            {
                _mpc.MpdPlaybackNext();
            }
        }

        public ICommand VolumeSliderCommand { get; }
        void SetVolume()
        {
            if (_con.IsConnected)
            {
                //if (_volumeDelayTimer == null)
                    _mpc.MpdSetVolume(Convert.ToInt32(_volume));
            }
        }

        public ICommand SeekSliderCommand { get; }
        void SetSeek()
        {
            if (_con.IsConnected)
            {
                //if (_elapsedDelayTimer == null)
                    _mpc.MpdPlaybackSeek(_mpc.MpdStatus.MpdSongID, Convert.ToInt32(_elapsed));
            }
        }

        public ICommand RandomButtonCommand { get; }
        void SetRandom()
        {
            if (_con.IsConnected)
            {
                _mpc.MpdSetRandom(!Random);
            }
        }

        public ICommand RepeatButtonCommand { get; }
        void SetRepeat()
        {
            if (_con.IsConnected)
            {
                _mpc.MpdSetRepeat(!Repeat);
            }
        }

        public ICommand ConsumeButtonCommand { get; }
        void SetConsume()
        {
            if (_con.IsConnected)
            {
                _mpc.MpdSetConsume(!Consume);
            }
        }

        public ICommand SingleButtonCommand { get; }
        void SetSingle()
        {
            if (_con.IsConnected)
            {
                _mpc.MpdSetSingle(!Single);
            }
        }


        #endregion

        public Command<SongInfo> ItemSelected { get; }
        void OnItemSelected(SongInfo item)
        {
            if (item == null)
                return;

            _mpc.MpdPlaybackPlay(item.Id);
        }
    }
}