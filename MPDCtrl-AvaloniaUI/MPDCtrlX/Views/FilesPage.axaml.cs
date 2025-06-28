using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using MPDCtrlX.ViewModels;

namespace MPDCtrlX.Views;

public partial class FilesPage : UserControl
{
    private readonly MainViewModel? _viewModel;

    public FilesPage()
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

        this.Library1x.Width = _viewModel.LibraryColumnHeaderTitleWidth;
        this.Library2x.Width = _viewModel.LibraryColumnHeaderFilePathWidth;

    }

}