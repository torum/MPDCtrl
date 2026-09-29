using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MPDCtrl.ViewModels;
using System;

namespace MPDCtrl.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        ViewModel = App.GetService<MainViewModel>();

        InitializeComponent();
    }

    public MainViewModel ViewModel
    {
        get;
    }

    private async void HyperlinkButton_AlbumCacheFolderPath_Click(object sender, RoutedEventArgs e)
    {
        var dir = App.AlbumCoverCacheFolder;

        await Windows.System.Launcher.LaunchFolderPathAsync(dir);

    }
}
