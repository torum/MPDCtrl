using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using MPDCtrl.Services;
using MPDCtrl.Views;
using MPDCtrl.ViewModels;
using MPDCtrl.Models;

namespace MPDCtrl
{
    public partial class App : Application
    {
        Connection _con = new Connection();

        public Connection MpdConection
        {
            get => _con;
        }

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();
            MainPage = new AppShell();
        }

        protected override void OnStart()
        {
            _con.Start();

        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }
    }
}
