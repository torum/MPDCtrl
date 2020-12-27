using MPDCtrl.Services;
using MPDCtrl.Views;
using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;
using System.Diagnostics;

namespace MPDCtrl
{
    public partial class App : Application
    {
        private Connection _con = new Connection();

        public Connection MpdConection
        {
            get => _con;
        }

        public App()
        {
            InitializeComponent();

            DependencyService.Register<MockDataStore>();

            //MainPage = new AppShell();
            if (_con.IsProfileSet)
            {
                Debug.WriteLine("IsProfileSet");

                MainPage = new AppShell();

                _con.Start();
            }
            else
            {
                Debug.WriteLine("NOT IsProfileSet");

                MainPage = new ConnectPage();
            }


        }

        protected override void OnStart()
        {
            //
        }

        protected override void OnSleep()
        {
        }

        protected override void OnResume()
        {
        }

        public static string AppTheme
        {
            get; set;
        }
    }
}
