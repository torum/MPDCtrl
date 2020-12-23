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
                SetProperty(ref _selectedItem, value);
                OnItemSelected(value);
            }
        }

        public ObservableCollection<SongInfo> Queue { get; set; } = new ObservableCollection<SongInfo>();

        public QueueViewModel()
        {
            Title = "Queue";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            Queue = _con.Queue;

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);

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
                _mpc.MpdClear();
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
            }
        }

        public Command<SongInfo> QueueItemSaveToCommand { get; }
        void QueueItemSaveTo(SongInfo item)
        {
            if (item == null)
                return;

            if (_con.IsConnected)
            {
                // TODO:
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
