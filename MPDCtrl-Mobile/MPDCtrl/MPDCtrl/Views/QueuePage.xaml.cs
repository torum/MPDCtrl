using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Views
{
    public partial class QueuePage : ContentPage
    {
        public QueuePage()
        {
            InitializeComponent();
            BindingContext = new QueueViewModel();

            (BindingContext as QueueViewModel).AskNewPlaylistNameToGueueSaveAs += (sender, arg) => { this.AskNewPlaylistNameToGueueSaveAs(arg); };
            (BindingContext as QueueViewModel).AskWhichPlaylistToSaveTo += (sender, arg) => { this.AskWhichPlaylistToSaveTo(arg); };
            
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as QueueViewModel).OnAppearing();
        }

        public async void AskNewPlaylistNameToGueueSaveAs(List<String> arg)
        {
            if (arg == null)
                return;

            string newPlaylistName = await DisplayPromptAsync("Save current queue as", "New playlist name:");

            if (string.IsNullOrEmpty(newPlaylistName))
                return;

            //if (arg.IndexOf(res, StringComparison.OrdinalIgnoreCase) >= 0)
            if (CheckPlaylistNameExists(newPlaylistName, arg))
            {
                await DisplayAlert("Alert", newPlaylistName + " already exists", "OK");
                return;
            }
            else
            {
                (BindingContext as QueueViewModel).DoQueueSaveAs(newPlaylistName);
            }
        }

        public async void AskWhichPlaylistToSaveTo(AskWhichPlaylistToSaveQueueItemToEventArgs arg)
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

            (BindingContext as QueueViewModel).DoQueueItemSaveTo(playlistName, arg.File);
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
    }
}
