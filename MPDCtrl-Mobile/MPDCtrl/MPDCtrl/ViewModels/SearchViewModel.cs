using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using MPDCtrl.Services;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;

namespace MPDCtrl.ViewModels
{
 
    class SearchViewModel : BaseViewModel
    {
        private MPC _mpc;
        private Connection _con;

        private Song _selectedItem;
        public Song SelectedItem
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

        public ObservableCollection<Song> SearchResult
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.SearchResult;
                }
                else
                {
                    return null;
                }
            }
        }

        private String _selectedSearchTag = "Title";
        public String SelectedSearchTag
        {
            get
            {
                return _selectedSearchTag;
            }
            set
            {
                if (_selectedSearchTag == value)
                    return;

                _selectedSearchTag = value;
                NotifyPropertyChanged("SelectedSearchTags");
            }
        }

        private string _searchQuery;
        public string SearchQuery
        {
            get
            {
                return _searchQuery;
            }
            set
            {
                if (_searchQuery == value)
                    return;

                _searchQuery = value;
                NotifyPropertyChanged("SearchQuery");
            }
        }

        public event EventHandler<AskWhichPlaylistToSaveSearchResultToEventArgs> AskWhichPlaylistToSaveSearchResultTo;
        public event EventHandler<AskWhichPlaylistToSaveSearchResultItemToEventArgs> AskWhichPlaylistToSaveSearchResultItemTo;

        public SearchViewModel()
        {
            Title = "Search";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);

            //ItemTapped = new Command<Song>(OnItemTapped);
            ItemSelected = new Command<Song>(OnItemSelected);

            SearchExecCommand = new Command(SearchExe);

            SearchResultSaveAsCommand = new Command(SearchResultSaveAs);
            SearchResultItemSaveToCommand = new Command<Song>(SearchResultItemSaveTo);
            SearchResultItemAddCommand = new Command<Song>(SearchResultItemAdd);


        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }

        /*
        public Command<Song> ItemTapped { get; }
        void OnItemTapped(Song item)
        {
            if (item == null)
                return;

            //
        }
        */

        public Command<Song> ItemSelected { get; }
        void OnItemSelected(Song item)
        {
            if (item == null)
                return;

            //
        }

        public Command SearchExecCommand { get; }
        void SearchExe()
        {
            if (SearchQuery == "")
                return;

            if (_mpc != null)
            {
                string queryShiki = "contains";//"==";

                _mpc.SearchResult.Clear();

                if (SelectedSearchTag == "")
                    SelectedSearchTag = "Title";

                _mpc.MpdSearch(SelectedSearchTag, queryShiki, SearchQuery);
            }
        }

        public Command SearchResultSaveAsCommand { get; }
        void SearchResultSaveAs()
        {
            if (_con.IsConnected)
            {
                if (_mpc.SearchResult.Count > 0)
                {
                    AskWhichPlaylistToSaveSearchResultToEventArgs arg = new AskWhichPlaylistToSaveSearchResultToEventArgs();
                    arg.Playlists = _con.Playlists.ToArray();

                    AskWhichPlaylistToSaveSearchResultTo?.Invoke(this, arg);
                }
            }
        }

        public void DoSearchResultSaveAs(string playlistName)
        {
            if (_con.IsConnected)
            {
                if (_mpc.SearchResult.Count > 0)
                {

                    List<String> sr = new List<string>();

                    foreach (var item in _mpc.SearchResult)
                    {
                        sr.Add(item.file);
                    }

                    _mpc.MpdPlaylistAdd(playlistName, sr);
                }
            }
        }

        public Command SearchResultItemSaveToCommand { get; }
        void SearchResultItemSaveTo(Song item)
        {
            if (_con.IsConnected)
            {
                if (_mpc.SearchResult.Count > 0)
                {
                    AskWhichPlaylistToSaveSearchResultItemToEventArgs arg = new AskWhichPlaylistToSaveSearchResultItemToEventArgs();
                    arg.File = item.file;
                    arg.Playlists = _con.Playlists.ToArray();

                    AskWhichPlaylistToSaveSearchResultItemTo?.Invoke(this, arg);
                }
            }
        }
        
        public void DoSearchResultItemSaveTo(string playlistName, string file)
        {
            if (_con.IsConnected)
            {
                if (_mpc.SearchResult.Count > 0)
                {
                    _mpc.MpdPlaylistAdd(playlistName, file);
                }
            }
        }

        public Command SearchResultItemAddCommand { get; }
        void SearchResultItemAdd(Song item)
        {
            if (_con.IsConnected)
            {
                if (_mpc.SearchResult.Count > 0)
                {
                    _mpc.MpdAdd(item.file);
                }
            }
        }
    }
}
