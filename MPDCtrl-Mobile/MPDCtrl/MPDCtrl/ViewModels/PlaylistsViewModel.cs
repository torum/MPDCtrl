using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;
using MPDCtrl.Services;
using MPDCtrl.Models;
using System.Collections.ObjectModel;
using System.Linq;

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

        public event EventHandler<AskNewNameToRenameToEventArgs> AskNewNameToRenameTo;

        public event EventHandler<string> ConfirmPlaylistItemDelete;

        public PlaylistsViewModel()
        {
            Title = "Playlists";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            ItemSelected = new Command<String>(OnItemSelected);
            PlaylistItemRenameCommand = new Command<String>(PlaylistItemRename);
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
                ConfirmPlaylistItemDelete?.Invoke(this, item);
            }
        }
        
        public void DoPlaylistItemDelete(string playlistNameToDelete)
        {
            if (String.IsNullOrEmpty(playlistNameToDelete))
                return;

            if (_con.IsConnected)
            {
                _mpc.MpdRemovePlaylist(playlistNameToDelete);

                if (_con.Playlists.Count == 1)
                {
                    _con.Playlists.Clear();
                }
            }
        }

        public Command<String> PlaylistItemRenameCommand { get; }
        void PlaylistItemRename(String item)
        {
            if (String.IsNullOrEmpty(item))
                return;

            if (_con.IsConnected)
            {
                AskNewNameToRenameToEventArgs arg = new AskNewNameToRenameToEventArgs();
                arg.Playlists = _con.Playlists.ToList();
                arg.OriginalPlaylistName = item;

                AskNewNameToRenameTo?.Invoke(this, arg);
            }
        }

        public void DoPlaylistItemRename(String OldName, string NewName)
        {
            if (String.IsNullOrEmpty(OldName))
                return;

            if (String.IsNullOrEmpty(NewName))
                return;

            if (_con.IsConnected)
            {
                _mpc.MpdRenamePlaylist(OldName, NewName);
            }
        }
    }
}
