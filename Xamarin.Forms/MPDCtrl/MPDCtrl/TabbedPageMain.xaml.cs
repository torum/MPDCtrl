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


            MainPage mp = new MainPage
            {
                BindingContext = new MainViewModel()
            };
            SettingsPage sp = new SettingsPage
            {
                BindingContext = mp.BindingContext
            };

            Children.Add(new NavigationPage(mp)
            {
                Title = "Home",
                //Icon = Images.Tab_Navigate
            });
            Children.Add(new NavigationPage(sp)
            {
                Title = "Settings",
                //Icon = Images.Tab_Navigate
            });
        }
    }
}