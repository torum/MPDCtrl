using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xamarin.Forms;
using Xamarin.Forms.Xaml;

namespace MPDCtrl
{
    [XamlCompilation(XamlCompilationOptions.Compile)]
    public partial class TabbedPageMain : TabbedPage
    {
        public TabbedPageMain ()
        {
            //InitializeComponent();

            Children.Add(new NavigationPage(new MainPage())
            {
                Title = "Home",
                //Icon = Images.Tab_Navigate
            });
            Children.Add(new NavigationPage(new SettingsPage())
            {
                Title = "Settings",
                //Icon = Images.Tab_Navigate
            });
        }
    }
}