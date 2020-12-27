using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDCtrl.ViewModels;

namespace MPDCtrl.Views
{
    public partial class ConnectPage : ContentPage
    {
        public ConnectPage()
        {
            InitializeComponent();
            BindingContext = new ConnectViewModel();

            (BindingContext as ConnectViewModel).GoToPage += (sender, arg) => { this.GoToPage(arg); };
        }

        public async void GoToPage(String pageName)
        {
            if (string.IsNullOrEmpty(pageName))
                return;


            // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
            //await Shell.Current.GoToAsync($"//{nameof(HomePage)}");

            Application.Current.MainPage = new AppShell();


            await Shell.Current.GoToAsync($"//"+ pageName);

        }

    }
}