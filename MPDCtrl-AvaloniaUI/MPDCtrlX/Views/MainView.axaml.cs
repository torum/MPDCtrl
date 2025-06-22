using Avalonia.Controls;
using Avalonia.Threading;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.Models;
using MPDCtrlX.ViewModels;
using System;
using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Text;

namespace MPDCtrlX.Views;

public partial class MainView : UserControl
{
    private readonly MainViewModel? _viewModel;

    public MainView()//MainViewModel? vm
    {
        _viewModel = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();//new MainViewModel();//vm ?? throw new ArgumentNullException(nameof(vm), "MainViewModel cannot be null.");

        InitializeComponent();

        //DataContext = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();//new MainViewModel();
        DataContext = _viewModel;

        if (_viewModel != null)
        {
            // Event subscriptions
            //vm.OnWindowLoaded(this);

            // Subscribe to Window events.

            //Loaded += vm.OnWindowLoaded;
            //Closing += vm.OnWindowClosing;
            //ContentRendered += vm.OnContentRendered;

            // Subscribe to ViewModel events.

            //vm.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };
            //vm.ScrollIntoViewAndSelect += (sender, arg) => { this.OnScrollIntoViewAndSelect(arg); };

        }

        /*
        var os = Environment.OSVersion;
        if (os.Platform.ToString().StartsWith("Win"))
        {
            this.MainGrid.RowDefinitions[0].Height = new GridLength(32, GridUnitType.Pixel);
            this.ImageLogo.IsVisible = true;
        }
        else
        {
            this.MainGrid.RowDefinitions[0].Height = new GridLength(0, GridUnitType.Pixel);
            this.ImageLogo.IsVisible = false;
        }
        */
    }
    /*
#pragma warning disable CS8618
    public MainView()
#pragma warning restore CS8618
    {
        InitializeComponent();

    }
    */


    private void TreeView_SelectionChanged(object? sender, Avalonia.Controls.SelectionChangedEventArgs e)
    {
        /*
        NodeMenu? value = ((sender as TreeView)?.SelectedItem as NodeMenu);
        if (value == null)
        {
            return;
        }
        
        if (value is NodeMenuQueue)
        {
            this.ContentFrame.Content = (App.Current as App)?.AppHost.Services.GetRequiredService<QueuePage>();
        }
        else if (value is NodeMenuPlaylists)
        {
            this.ContentFrame.Content = (App.Current as App)?.AppHost.Services.GetRequiredService<PlaylistsPage>();
        }
        else if (value is NodeMenuPlaylistItem)
        {
            this.ContentFrame.Content = (App.Current as App)?.AppHost.Services.GetRequiredService<PlaylistItemPage>();
        }
        else if (value is NodeMenuLibrary)
        {
            this.ContentFrame.Content = (App.Current as App)?.AppHost.Services.GetRequiredService<LibraryPage>();
        }
        else if (value is NodeMenuSearch)
        {
            this.ContentFrame.Content = (App.Current as App)?.AppHost.Services.GetRequiredService<SearchPage>();
        }
        else if (value is NodeMenuAlbum)
        {
            this.ContentFrame.Content = (App.Current as App)?.AppHost.Services.GetRequiredService<AlbumPage>();
        }
        else if (value is NodeMenuArtist)
        {
            this.ContentFrame.Content = (App.Current as App)?.AppHost.Services.GetRequiredService<ArtistPage>();
        }
        */
    }
    
    public void WindowDeactivated()
    {
        //this.Header.Opacity = 0.3;
        this.AppTitleBar.Opacity = 0.3;
    }
    public void WindowActivated()
    {
        //this.Header.Opacity = 1;
        this.AppTitleBar.Opacity = 1;
    }
}
