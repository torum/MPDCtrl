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

        public Command LoadItemsCommand { get; }

        public Command<SongInfo> ItemTapped { get; }

        public QueueViewModel()
        {
            Title = "Queue";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            Queue = _con.Queue;

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);



            ItemTapped = new Command<SongInfo>(OnItemSelected);

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());


        }


        public void OnAppearing()
        {
            //IsBusy = true;
            SelectedItem = null;

            if (_con.IsConnected)
            {
                if (Queue.Count == 0)
                {
                    //_mpc.MpdQueryCurrentQueue();
                }
            }
        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            this.IsBusy = on;
        }

        async Task ExecuteLoadItemsCommand()
        {
            //IsBusy = true;

            try
            {
                //Queue.Clear();
                if (Queue.Count == 0)
                {
                    _mpc.MpdQueryCurrentQueue();
                }

                /*
                var items = await DataStore.GetItemsAsync(true);
                foreach (var item in items)
                {
                    Items.Add(item);
                }
                */
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {
                //IsBusy = false;
            }
        }


        void OnItemSelected(SongInfo item)
        {
            if (item == null)
                return;

            _mpc.MpdPlaybackPlay(item.Id);

            // This will push the ItemDetailPage onto the navigation stack
            //await Shell.Current.GoToAsync($"{nameof(ItemDetailPage)}?{nameof(ItemDetailViewModel.ItemId)}={item.Id}");
        }
    }
}
