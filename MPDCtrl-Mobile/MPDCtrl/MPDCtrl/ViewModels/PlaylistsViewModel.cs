using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using MPDCtrl.Services;
using MPDCtrl.Models;
using System.Collections.ObjectModel;

namespace MPDCtrl.ViewModels
{
    class PlaylistsViewModel : BaseViewModel
    {
        private MPC _mpc;
        private Connection _con;

        private String _selectedItem;
        public String SelectedItem
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

        public ObservableCollection<String> Playlists
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.Playlists;
                }
                else
                {
                    return null;
                }
            }
        }

        public PlaylistsViewModel()
        {
            Title = "Playlists";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            ItemSelected = new Command<String>(OnItemSelected);
            PlaylistItemDeleteCommand = new Command<String>(PlaylistItemDelete);

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);

        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }

        public Command<String> ItemSelected { get; }
        void OnItemSelected(String item)
        {
            if (item == null)
                return;

            if (String.IsNullOrEmpty(item))
                return;

            if (_con.IsConnected)
            {
                // TODO: Ask if clear queue and load / add to queue.

                _con.Queue.Clear();
                _mpc.MpdChangePlaylist(item);

                // TODO: 
                _mpc.MpdPlaybackPlay();
            }
        }

        public Command<String> PlaylistItemDeleteCommand { get; }
        void PlaylistItemDelete(String item)
        {
            if (item == null)
                return;

            if (String.IsNullOrEmpty(item))
                return;

            if (_con.IsConnected)
            {
                // TODO: Comfirm dialog
                _mpc.MpdRemovePlaylist(item);
            }
        }
    }
}
