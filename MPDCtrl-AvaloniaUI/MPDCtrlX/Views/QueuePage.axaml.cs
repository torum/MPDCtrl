using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using FluentAvalonia.UI.Controls;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.ViewModels;
using System;
using System.Collections;
using System.Diagnostics;
using System.Linq;

namespace MPDCtrlX.Views;

public partial class QueuePage : UserControl
{
    private readonly MainViewModel? _viewModel;

    public QueuePage()
    {
        _viewModel = (App.Current as App)?.AppHost.Services.GetRequiredService<MainViewModel>();

        DataContext = _viewModel;

        if (_viewModel != null)
        {
            _viewModel.ScrollIntoView += (sender, arg) => { this.OnScrollIntoView(arg); };
            _viewModel.ScrollIntoViewAndSelect += (sender, arg) => { this.OnScrollIntoViewAndSelect(arg); };
        }

        InitializeComponent();
    }

    public void UpdateHeaderWidth()
    {
        if (_viewModel != null)
        {
            // This is a dirty work around for AvaloniaUI.
            _viewModel.QueueColumnHeaderPositionWidth = this.test1x.Width;
            _viewModel.QueueColumnHeaderNowPlayingWidth = this.test2x.Width;
            _viewModel.QueueColumnHeaderTitleWidth = this.test3x.Width;
            _viewModel.QueueColumnHeaderTimeWidth = this.test4x.Width;
            _viewModel.QueueColumnHeaderArtistWidth = this.test5x.Width;
            _viewModel.QueueColumnHeaderAlbumWidth = this.test6x.Width;
            _viewModel.QueueColumnHeaderDiscWidth = this.test7x.Width;
            _viewModel.QueueColumnHeaderTrackWidth = this.test8x.Width;
            _viewModel.QueueColumnHeaderGenreWidth = this.test9x.Width;
            _viewModel.QueueColumnHeaderLastModifiedWidth = this.test10x.Width;
        }
    }

    private void ListBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        // This is a dirty work around for AvaloniaUI.
        if (_viewModel.QueueColumnHeaderPositionWidth > 10)
        {
            this.test1x.Width = _viewModel.QueueColumnHeaderPositionWidth;
        }
        else
        {
            this.test1x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderNowPlayingWidth > 10)
        {
            //this.test2x.Width = _viewModel.QueueColumnHeaderNowPlayingWidth;
        }
        else
        {
            //this.test2x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderTitleWidth > 10)
        {
            this.test3x.Width = _viewModel.QueueColumnHeaderTitleWidth;
        }
        else
        {
            this.test3x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderTimeWidth > 10)
        {
            this.test4x.Width = _viewModel.QueueColumnHeaderTimeWidth;
        }
        else
        {
            this.test4x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderArtistWidth > 10)
        {
            this.test5x.Width = _viewModel.QueueColumnHeaderArtistWidth;
        }
        else
        {
            this.test5x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderAlbumWidth > 10)
        {
            this.test6x.Width = _viewModel.QueueColumnHeaderAlbumWidth;
        }
        else
        {
            this.test6x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderDiscWidth > 10)
        {
            this.test7x.Width = _viewModel.QueueColumnHeaderDiscWidth;
        }
        else
        {
            this.test7x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderTrackWidth > 10)
        {
            this.test8x.Width = _viewModel.QueueColumnHeaderTrackWidth;
        }
        else
        {
            this.test8x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderGenreWidth > 10)
        {
            this.test9x.Width = _viewModel.QueueColumnHeaderGenreWidth;
        }
        else
        {
            this.test9x.Width = 50; // Default width if not set
        }

        if (_viewModel.QueueColumnHeaderLastModifiedWidth > 10)
        {
            this.test10x.Width = _viewModel.QueueColumnHeaderLastModifiedWidth;
        }
        else
        {
            this.test10x.Width = 50; // Default width if not set
        }

    }

    private void OnScrollIntoView(int ind) 
    {
        if (this.QueueListBox is ListBox lb)
        {
            //lb.AutoScrollToSelectedItem = true;
            lb.ScrollIntoView(ind);
        }
    }
    private void OnScrollIntoViewAndSelect(int ind)
    {
        if (this.QueueListBox is ListBox lb)
        {
            lb.ScrollIntoView(ind);

            var test = _viewModel?.Queue.FirstOrDefault(x => x.IsPlaying == true);
            if (test != null)
            {
                //lb.ScrollIntoView(test.Index);
                test.IsSelected = true;
            }

            //lb.AutoScrollToSelectedItem = true;
        }
    }
}