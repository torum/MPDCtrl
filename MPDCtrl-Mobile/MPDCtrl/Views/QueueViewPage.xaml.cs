using System;
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
    public partial class QueueViewPage : ContentPage
    {
        public QueueViewPage()
        {
            InitializeComponent();
            BindingContext = new QueueViewModel();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            (BindingContext as QueueViewModel).OnAppearing();
        }
    }
}
