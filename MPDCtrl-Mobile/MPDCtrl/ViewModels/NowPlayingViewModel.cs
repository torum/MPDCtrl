using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;

namespace MPDCtrl.ViewModels
{
    public class NowPlayingViewModel : BaseViewModel
    {
        private MPC _mpc;

        private SongInfo _currentSong;
        public SongInfo CurrentSong
        {
            get => _currentSong;
            set
            {
                SetProperty(ref _currentSong, value);
                
            }
        }

        public NowPlayingViewModel()
        {
            Title = "Now Playing";

            App me = App.Current as App;

            //_mpc = me.Mpc;


        }
    }
}
