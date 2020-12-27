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
    public partial class SettingPage : ContentPage
    {
        public SettingPage()
        {
            InitializeComponent();
            BindingContext = new SettingViewModel();

            (BindingContext as SettingViewModel).GoToPage += (sender, arg) => { this.GoToPage(arg); };
        }

        public async void GoToPage(String pageName)
        {
            if (string.IsNullOrEmpty(pageName))
                return;

            Application.Current.MainPage = new AppShell();
            await Shell.Current.GoToAsync($"//" + pageName);
        }
    }
}