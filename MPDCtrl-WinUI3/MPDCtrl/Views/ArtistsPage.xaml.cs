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
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views;

public sealed partial class ArtistsPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public ArtistsPage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();
    }

    private void ArtistsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SelectedArtistAlbumsScrollViewer is null)
        {
            return;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            this.SelectedArtistAlbumsScrollViewer.ChangeView(0, 0, null);
        });
    }
}
