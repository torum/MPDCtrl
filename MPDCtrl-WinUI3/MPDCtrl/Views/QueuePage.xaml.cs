using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Diagnostics;
using MPDCtrl.Models;

namespace MPDCtrl.Views;

public sealed partial class QueuePage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public QueuePage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();

        ViewModel.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };
        ViewModel.ScrollIntoViewAndSelect += (sender, arg) => { this.OnScrollIntoViewAndSelect(arg); };
    }

    private void OnScrollIntoView(object obj)
    {
        if (obj == null) { return; }
        /*
        await Task.Yield();
        await Task.Delay(100); // Wait for UI to update
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            if (this.QueueListview is ListView lb)
            {
                //lb.AutoScrollToSelectedItem = true;
                lb.ScrollIntoView(ind);
            }
        });
        */
        if (this.QueueListview is ListView lb)
        {
            lb.ScrollIntoView(obj, ScrollIntoViewAlignment.Leading);
        }
    }

    private async void OnScrollIntoViewAndSelect(object obj)
    {
        await Task.Yield();
        await Task.Delay(800); // Need to wait for UI to update
        /*
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {

        });
        */
        if (this.QueueListview is ListView lb)
        {
            lb.ScrollIntoView(obj, ScrollIntoViewAlignment.Leading);

            if (obj is SongInfoEx song)
            {
                //song.IsSelected = true; This won't work in Winui3?
                lb.SelectedItem = song;
            }
        }
    }
}
