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
    public class QueueViewModel : BaseViewModel
    {

        //private MPC _mpc = new MPC("192.168.3.2",6600,"asdf");

        private MPC _mpc;

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
            Title = "Current Queue";

            App me = App.Current as App;

            _mpc = me.MpdConection.Mpc;
            Queue = me.MpdConection.Queue;



            ItemTapped = new Command<SongInfo>(OnItemSelected);

            LoadItemsCommand = new Command(async () => await ExecuteLoadItemsCommand());



        }


        public void OnAppearing()
        {
            IsBusy = true;
            SelectedItem = null;
        }

        async Task ExecuteLoadItemsCommand()
        {
            IsBusy = true;

            try
            {
                //Queue.Clear();


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
                IsBusy = false;
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
