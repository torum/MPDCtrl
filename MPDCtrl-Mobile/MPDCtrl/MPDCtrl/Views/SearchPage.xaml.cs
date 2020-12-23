using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPDCtrl.ViewModels;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MPDCtrl.Views
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class SearchPage : ContentPage
    {
        public SearchPage()
        {
            InitializeComponent();
            BindingContext = new SearchViewModel();

            (BindingContext as SearchViewModel).AskWhichPlaylistToSaveSearchResultTo += (sender, arg) => { this.AskWhichPlaylistToSaveSearchResultTo(arg); };
            (BindingContext as SearchViewModel).AskWhichPlaylistToSaveSearchResultItemTo += (sender, arg) => { this.AskWhichPlaylistToSaveSearchResultItemTo(arg); };

            
        }

        public async void AskWhichPlaylistToSaveSearchResultTo(AskWhichPlaylistToSaveSearchResultToEventArgs arg)
        {
            if (arg == null)
                return;

            if (arg.Playlists == null)
                return;

            string playlistName = "";
            if (arg.Playlists.Length > 0)
            {
                bool answer = await DisplayAlert("Save to playlist", "Would you like to create a new playlist?", "Yes", "No");

                if (answer)
                {

                    playlistName = await DisplayPromptAsync("Create a new playlist", "New playlist name:");

                    if (string.IsNullOrEmpty(playlistName))
                        return;
                }
                else
                {
                    playlistName = await DisplayActionSheet("Save search result to?", "Cancel", null, arg.Playlists);

                    if (string.IsNullOrEmpty(playlistName))
                        return;

                    if (playlistName == "Cancel")
                        return;
                }
            }
            else
            {
                playlistName = await DisplayPromptAsync("Create new playlist", "New playlist name:");

                if (string.IsNullOrEmpty(playlistName))
                    return;

            }

            if (string.IsNullOrEmpty(playlistName))
                return;

            (BindingContext as SearchViewModel).DoSearchResultSaveAs(playlistName);
        }

        public async void AskWhichPlaylistToSaveSearchResultItemTo(AskWhichPlaylistToSaveSearchResultItemToEventArgs arg)
        {
            if (arg == null)
                return;

            if (string.IsNullOrEmpty(arg.File))
                return;

            if (arg.Playlists == null)
                return;

            string playlistName = "";
            if (arg.Playlists.Length > 0)
            {
                bool answer = await DisplayAlert("Save to playlist", "Would you like to create a new playlist?", "Yes", "No");

                if (answer)
                {

                    playlistName = await DisplayPromptAsync("Create a new playlist", "New playlist name:");

                    if (string.IsNullOrEmpty(playlistName))
                        return;
                }
                else
                {
                    playlistName = await DisplayActionSheet("Save selected to?", "Cancel", null, arg.Playlists);

                    if (string.IsNullOrEmpty(playlistName))
                        return;

                    if (playlistName == "Cancel")
                        return;
                }
            }
            else
            {
                playlistName = await DisplayPromptAsync("Create new playlist", "New playlist name:");

                if (string.IsNullOrEmpty(playlistName))
                    return;

            }

            if (string.IsNullOrEmpty(playlistName))
                return;

            (BindingContext as SearchViewModel).DoSearchResultItemSaveTo(playlistName, arg.File);
        }

        //
    }
}