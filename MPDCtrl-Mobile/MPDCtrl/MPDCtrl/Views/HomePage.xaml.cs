using System;
using System.ComponentModel;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
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
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as HomeViewModel).OnAppearing();
        }
    }
}