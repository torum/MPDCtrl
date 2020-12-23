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
        }
    }
}