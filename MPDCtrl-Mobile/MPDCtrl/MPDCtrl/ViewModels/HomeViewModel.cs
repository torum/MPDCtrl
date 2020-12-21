using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using MPDCtrl.Services;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;
using System.Diagnostics;
using System.Linq;

namespace MPDCtrl.ViewModels
{
    public class HomeViewModel : BaseViewModel
    {

        private MPC _mpc;
        private Connection _con;

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

        public HomeViewModel()
        {
            Title = "Now Playing";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);
            _mpc.StatusUpdate += new MPC.MpdStatusUpdate(OnMpdStatusUpdate);

            PlayButtonCommand = new Command(() => Play());
            PlayBackButtonCommand = new Command(() => PlayBack());
            PlayNextButtonCommand = new Command(() => PlayNext());

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
            this.IsBusy = on;
        }

        private void OnMpdStatusUpdate(MPC sender, object data)
        {
            if ((data as string) == "isPlayer")
            {
                //UpdateButtonStatus();

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
                            //AlbumArt = _albumArtDefault;
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
                    }
                    else
                    {
                        CurrentSong = null;
                    }
                });

                if (isSongChanged && (CurrentSong != null))
                {
                    // AlbumArt
                    if (!String.IsNullOrEmpty(CurrentSong.file))
                    {
                        //_mpc.MpdQueryAlbumArt(CurrentSong.file);
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
                        /*
                        // AlbumArt
                        if (_MPC.AlbumArt.SongFilePath != curitem.file)
                        {
                            IsAlbumArtVisible = false;
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                AlbumArt = _albumArtDefault;
                            });

                            if (!String.IsNullOrEmpty((curitem as MPC.SongInfo).file))
                            {
                                _MPC.MpdQueryAlbumArt((curitem as MPC.SongInfo).file);
                            }
                        }
                        */
                    }
                    else
                    {
                        CurrentSong = null;
                        /*
                        IsAlbumArtVisible = false;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            AlbumArt = _albumArtDefault;
                        });
                        */
                    }
                });
            }
            else if ((data as string) == "isAlbumart")
            {
                /*
                if ((!_mpc.AlbumArt.IsDownloading) && _mpc.AlbumArt.IsSuccess)
                {
                    if ((CurrentSong != null) && (_mpc.AlbumArt.AlbumImageSource != null))
                    {
                        // AlbumArt
                        if (!String.IsNullOrEmpty(CurrentSong.file))
                        {
                            if (CurrentSong.file == _mpc.AlbumArt.SongFilePath)
                            {
                                AlbumArt = _mpc.AlbumArt.AlbumImageSource;
                                IsAlbumArtVisible = true;
                            }
                        }
                    }
                }
                */
            }
        }





        public ICommand PlayButtonCommand { get; }
        void Play()
        {
            if (_con.IsConnected)
            {
                switch (_mpc.MpdStatus.MpdState)
                {
                    case MPC.Status.MpdPlayState.Play:
                        {
                            //State>>Play: So, send Pause command
                            _mpc.MpdPlaybackPause();
                            break;
                        }
                    case MPC.Status.MpdPlayState.Pause:
                        {
                            //State>>Pause: So, send Resume command
                            _mpc.MpdPlaybackResume();
                            break;
                        }
                    case MPC.Status.MpdPlayState.Stop:
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
    }
}