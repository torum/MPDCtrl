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


    private void ListBox_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_viewModel == null)
        {
            return;
        }

        this.test1x.Width = _viewModel.QueueColumnHeaderPositionWidth;
        this.test2x.Width = _viewModel.QueueColumnHeaderNowPlayingWidth;
        this.test3x.Width = _viewModel.QueueColumnHeaderTitleWidth;
        this.test4x.Width = _viewModel.QueueColumnHeaderTimeWidth;
        this.test5x.Width = _viewModel.QueueColumnHeaderArtistWidth;
        this.test6x.Width = _viewModel.QueueColumnHeaderAlbumWidth;
        this.test7x.Width = _viewModel.QueueColumnHeaderDiscWidth;
        this.test8x.Width = _viewModel.QueueColumnHeaderTrackWidth;
        this.test9x.Width = _viewModel.QueueColumnHeaderGenreWidth;
        this.test10x.Width = _viewModel.QueueColumnHeaderLastModifiedWidth;
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