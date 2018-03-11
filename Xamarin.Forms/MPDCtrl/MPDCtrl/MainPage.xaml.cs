using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MPDCtrl
{
	public partial class MainPage : ContentPage
    {
		public MainPage()
		{
            NavigationPage.SetHasNavigationBar(this, false);

            InitializeComponent();
		}

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            (sender as ListView)?.ScrollTo((sender as ListView).SelectedItem, Xamarin.Forms.ScrollToPosition.MakeVisible, true);
            // MakeVisible = 0, Scroll to make a specified list item visible.
            // Start = 1,Scroll to the start of a list.
            // Center = 2,Scroll to the center of a list.
            // End = 3, Scroll to the end of a list.
        }

        private async void PickPlaylistButton_Clicked(object sender, EventArgs e)
        {
            Page p = new PlaylistPage
            {
                BindingContext = this.BindingContext
            };
            await Navigation.PushAsync(p);
        }

        private void VolumeSlider_ValueChanged(object sender, ValueChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("VolumeSlider_ValueChanged");
        }
    }
}
