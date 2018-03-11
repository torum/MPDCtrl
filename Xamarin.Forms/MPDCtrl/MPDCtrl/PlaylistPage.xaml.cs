using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MPDCtrl
{
	[XamlCompilation(XamlCompilationOptions.Compile)]
	public partial class PlaylistPage : ContentPage
	{
		public PlaylistPage ()
		{
            NavigationPage.SetHasNavigationBar(this, false);

            InitializeComponent ();
		}

        private async void PlaylistsListview_ItemTapped(object sender, ItemTappedEventArgs e)
        {
            await Navigation.PopAsync();
            //await Navigation.PushAsync(p);
        }
    }
}