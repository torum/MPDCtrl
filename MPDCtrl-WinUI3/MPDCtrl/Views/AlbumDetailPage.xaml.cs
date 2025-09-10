using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views;

public class Breadcrumb
{
    public string? Name
    {
        get; set;
    }
    /*
    public string? Page
    {
        get; set;
    }
    */
}

public sealed partial class AlbumDetailPage : Page
{
    public MainViewModel ViewModel
    {
        get;
    }

    public ObservableCollection<Breadcrumb> BreadcrumbItems = [];

    private Frame? _frame;

    private readonly ResourceLoader _resourceLoader = new();

    public AlbumDetailPage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();

        var selectedAlbumName = ViewModel.SelectedAlbum?.Name ?? string.Empty;
        var basePageTitle = _resourceLoader.GetString("MenuTreeItem_Albums");

        BreadcrumbItems = new ObservableCollection<Breadcrumb>{
        new() { Name = basePageTitle},
        new() { Name = selectedAlbumName },
        };
        /*
        BreadcrumbBar1.ItemsSource = new ObservableCollection<Breadcrumb>{
        new() { Name = basePageTitle},
        new() { Name = selectedAlbumName },
        };
        */
        //BreadcrumbBar1.ItemClicked += BreadcrumbBar_ItemClicked;
    }

    private void BreadcrumbBar_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args)
    {
        if (_frame is null)
        {
            return;
        }

        if (args.Index == 0)
        {
            if (_frame.Navigate(typeof(Views.AlbumsPage), _frame, new SlideNavigationTransitionInfo() { Effect = SlideNavigationTransitionEffect.FromLeft }))
            {
                // Needed to invoke the same album in AlbumListView
                //ViewModel.SelectedAlbum = null; <- do it in OnNavigatedFrom.
            }
        }
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is not Frame frame)
        {
            return;
        }

        _frame = frame;

        ViewModel.IsGoBackButtonVisible = true;

        BreadcrumbItems[1].Name = ViewModel.SelectedAlbum?.Name ?? string.Empty;

        base.OnNavigatedTo(e);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        // Needed to invoke the same album in AlbumListView
        ViewModel.SelectedAlbum = null;

        ViewModel.IsGoBackButtonVisible = false;

        base.OnNavigatedFrom(e); // Always call the base implementation
    }
}
