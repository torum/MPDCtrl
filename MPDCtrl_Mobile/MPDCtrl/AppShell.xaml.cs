using System;
using System.Collections.Generic;
using MPDCtrl.ViewModels;
using MPDCtrl.Views;
using Xamarin.Forms;

namespace MPDCtrl
{
    public partial class AppShell : Xamarin.Forms.Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute(nameof(ItemDetailPage), typeof(ItemDetailPage));
            Routing.RegisterRoute(nameof(NewItemPage), typeof(NewItemPage));
        }

    }
}
