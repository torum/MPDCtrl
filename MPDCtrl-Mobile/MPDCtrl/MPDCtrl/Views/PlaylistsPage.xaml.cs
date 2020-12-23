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
    public partial class PlaylistsPage : ContentPage
    {
        public PlaylistsPage()
        {
            InitializeComponent();
            BindingContext = new PlaylistsViewModel();

            (BindingContext as PlaylistsViewModel).AskNewNameToRenameTo += (sender, arg) => { this.AskNewNameToRenameTo(arg); };

            (BindingContext as PlaylistsViewModel).ConfirmPlaylistItemDelete += (sender, arg) => { this.ConfirmPlaylistItemDelete(arg); };
            
        }

        public async void AskNewNameToRenameTo(AskNewNameToRenameToEventArgs arg)
        {
            if (arg == null)
                return;

            if (string.IsNullOrEmpty(arg.OriginalPlaylistName))
                return;

            string newPlaylistName = await DisplayPromptAsync("Rename " + arg.OriginalPlaylistName + " as", "New playlist name:");

            if (string.IsNullOrEmpty(newPlaylistName))
                return;

            //if (arg.IndexOf(res, StringComparison.OrdinalIgnoreCase) >= 0)
            if (CheckPlaylistNameExists(newPlaylistName, arg.Playlists))
            {
                await DisplayAlert("Alert", newPlaylistName + " already exists", "OK");
                return;
            }
            else
            {
                (BindingContext as PlaylistsViewModel).DoPlaylistItemRename(arg.OriginalPlaylistName, newPlaylistName);
            }
        }

        private bool CheckPlaylistNameExists(string playlistName, List<string> playlists)
        {
            bool match = false;

            if (playlists.Count > 0)
            {

                foreach (var hoge in playlists)
                {
                    if (hoge.ToLower() == playlistName.ToLower())
                    {
                        match = true;
                        break;
                    }
                }
            }

            return match;
        }

        public async void ConfirmPlaylistItemDelete(string arg)
        {
            if (arg == null)
                return;

            if (string.IsNullOrEmpty(arg))
                return;

            bool answer = await DisplayAlert("Comfirmation", "Are you sure you'd like to delete: " + arg, "Yes", "No");

            if (answer)
                (BindingContext as PlaylistsViewModel).DoPlaylistItemDelete(arg);
           
        }
    }
}