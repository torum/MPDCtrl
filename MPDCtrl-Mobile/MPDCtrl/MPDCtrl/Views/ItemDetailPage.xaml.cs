using MPDCtrl.ViewModels;
using System.ComponentModel;
using Xamarin.Forms;

namespace MPDCtrl.Views
{
    public partial class ItemDetailPage : ContentPage
    {
        public ItemDetailPage()
        {
            InitializeComponent();
            BindingContext = new ItemDetailViewModel();
        }
    }
}