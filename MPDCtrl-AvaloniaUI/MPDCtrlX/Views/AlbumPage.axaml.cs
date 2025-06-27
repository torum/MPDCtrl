using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MPDCtrlX.ViewModels;

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

    }
}