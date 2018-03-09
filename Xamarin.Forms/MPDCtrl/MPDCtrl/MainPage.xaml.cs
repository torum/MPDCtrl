using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace MPDCtrl
{
	public partial class MainPage : ContentPage
	{
		public MainPage()
		{
			InitializeComponent();
		}

        private void ListView_ItemSelected(object sender, SelectedItemChangedEventArgs e)
        {
            //(sender as ListView)?.ScrollToItem((sender as ListView).SelectedItem);
            (sender as ListView)?.ScrollTo((sender as ListView).SelectedItem, Xamarin.Forms.ScrollToPosition.MakeVisible, true);
            //System.Diagnostics.Debug.WriteLine("ListView_ItemSelected.");
            /*
        //
        // 概要:
        //     Scroll to make a specified list item visible.
        MakeVisible = 0,
        //
        // 概要:
        //     Scroll to the start of a list.
        Start = 1,
        //
        // 概要:
        //     Scroll to the center of a list.
        Center = 2,
        //
        // 概要:
        //     Scroll to the end of a list.
        End = 3
             */
             
        }
    }
}
