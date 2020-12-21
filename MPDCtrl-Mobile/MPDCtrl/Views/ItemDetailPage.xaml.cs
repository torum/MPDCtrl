using System.ComponentModel;
using Xamarin.Forms;
using MPDCtrl.ViewModels;

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