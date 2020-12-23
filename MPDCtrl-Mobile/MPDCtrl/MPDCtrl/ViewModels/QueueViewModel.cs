using System;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using MPDCtrl.Services;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;

namespace MPDCtrl.ViewModels
{
    public class QueueViewModel : BaseViewModel
    {
        private MPC _mpc;
        private Connection _con;

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

        public event EventHandler<List<String>> AskNewPlaylistNameToGueueSaveAs;

        public event EventHandler<AskWhichPlaylistToSaveQueueItemToEventArgs> AskWhichPlaylistToSaveTo;

        public QueueViewModel()
        {
            Title = "Queue";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);

            QueueSaveAsCommand = new Command(QueueSaveAs);
            QueueClearCommand = new Command(QueueClear);

            //ItemTapped = new Command<SongInfo>(OnItemTapped);
            ItemSelected = new Command<SongInfo>(OnItemSelected);
            QueueItemDeleteCommand = new Command<SongInfo>(QueueItemDelete);
            QueueItemSaveToCommand = new Command<SongInfo>(QueueItemSaveTo);

        }

        public void OnAppearing()
        {
            SelectedItem = null;
        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }

        public Command QueueClearCommand { get; }
        void QueueClear()
        {
            if (_con.IsConnected)
            {
                _con.Queue.Clear();
                _mpc.MpdClear();
            }
        }

        public Command QueueSaveAsCommand { get; }
        void QueueSaveAs()
        {
            if (_con.IsConnected)
            {
                if (_con.Queue.Count > 0)
                {
                    List<String> plts = _con.Playlists.ToList();

                    AskNewPlaylistNameToGueueSaveAs?.Invoke(this, plts);
                }
            }
        }

        public void DoQueueSaveAs(string newName)
        {
            if (string.IsNullOrEmpty(newName))
                return;

            if (_con.IsConnected)
            {
                _mpc.MpdSave(newName);
            }
        }

        public Command<SongInfo> QueueItemDeleteCommand { get; }
        void QueueItemDelete(SongInfo item)
        {
            if (item == null)
                return;

            if (_con.IsConnected)
            {
                _mpc.MpdDeleteId(item.Id);

                if (_con.Queue.Count == 1)
                {
                    _con.Queue.Clear();
                }
            }
        }

        public Command<SongInfo> QueueItemSaveToCommand { get; }
        void QueueItemSaveTo(SongInfo item)
        {
            if (item == null)
                return;

            if (_con.IsConnected)
            {
                AskWhichPlaylistToSaveQueueItemToEventArgs arg = new AskWhichPlaylistToSaveQueueItemToEventArgs();
                arg.File = item.file;
                arg.Playlists = _con.Playlists.ToArray();

                AskWhichPlaylistToSaveTo?.Invoke(this, arg); 
            }
        }
        public void DoQueueItemSaveTo(string playlistName, string file)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            if (string.IsNullOrEmpty(file))
                return;

            if (_con.IsConnected)
            {
                _mpc.MpdPlaylistAdd(playlistName, file);
            }
        }

        /*
        public Command<SongInfo> ItemTapped { get; }
        void OnItemTapped(SongInfo item)
        {
            if (item == null)
                return;

            _mpc.MpdPlaybackPlay(item.Id);
        }
        */

        public Command<SongInfo> ItemSelected { get; }
        void OnItemSelected(SongInfo item)
        {
            if (item == null)
                return;

            _mpc.MpdPlaybackPlay(item.Id);
        }
    }
}
