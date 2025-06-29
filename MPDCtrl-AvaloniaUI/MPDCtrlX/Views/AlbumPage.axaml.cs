using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MPDCtrlX.Models;
using MPDCtrlX.ViewModels;
using System;
using System.Diagnostics;

namespace MPDCtrlX.Views;

public partial class AlbumPage : UserControl
{
    private readonly MainViewModel? _viewModel;

    public AlbumPage()
    {
        _viewModel = App.GetService<MainViewModel>();
        DataContext = _viewModel;

        InitializeComponent();
    }

    private void ListBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        //this.Artist1x.Width = _viewModel.LibraryColumnHeaderTitleWidth;
        //this.Artist2x.Width = _viewModel.LibraryColumnHeaderFilePathWidth;

        /*
        var realizedContainers = icAlbums.GetRealizedContainers();
        if (realizedContainers != null)
        {
            Debug.WriteLine($"Realized containers not null.");
            foreach (var container in realizedContainers)
            {
                var dataContext = container.DataContext;

                if (dataContext is Album album)
                {
                    Debug.WriteLine($"Album Name: {album.Name}");
                }
                else
                {
                    Debug.WriteLine($"DataContext is not of type Album: {dataContext?.GetType().Name}");
                }
            }
        }
        else
        {
             Debug.WriteLine("No realized containers found.");
        }
        */
    }

}