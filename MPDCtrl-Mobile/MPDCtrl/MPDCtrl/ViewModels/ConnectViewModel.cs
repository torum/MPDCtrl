using MPDCtrl.Views;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MPDCtrl.ViewModels
{
    public class ConnectViewModel : BaseViewModel
    {
        public Command LoginCommand { get; }

        public ConnectViewModel()
        {
            LoginCommand = new Command(OnLoginClicked);
        }

        private async void OnLoginClicked(object obj)
        {
            // Prefixing with `//` switches to a different navigation stack instead of pushing to the active one
            await Shell.Current.GoToAsync($"//{nameof(HomePage)}");
        }
    }
}
