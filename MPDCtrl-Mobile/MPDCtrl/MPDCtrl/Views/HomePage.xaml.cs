using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Collections.ObjectModel;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Views
{
    public partial class HomePage : ContentPage
    {
        public HomePage()
        {
            InitializeComponent();
            BindingContext = new HomeViewModel();

            (BindingContext as HomeViewModel).ScrollIntoView += (sender, arg) => { this.ScrollIntoViewTo(arg); };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            //(BindingContext as HomeViewModel).OnAppearing();

        }

        public void ScrollIntoViewTo(SongInfo arg)
        {

            //HomeQueueListview.ScrollTo(arg, Xamarin.Forms.ScrollToPosition.Start, false);

        }

        private void ToolbarItemTop_Clicked(object sender, EventArgs e)
        {
            MainScrolView.ScrollToAsync(0,0,true);
        }
    }
}