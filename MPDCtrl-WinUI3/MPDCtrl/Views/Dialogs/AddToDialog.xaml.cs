using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using MPDCtrl.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views.Dialogs;

public sealed partial class AddToDialog : Page
{
    public bool CreateNewCheckBoxIsChecked = false;
    public string TextBoxPlaylistNameText = string.Empty;
    public Playlist? PlaylistComboBoxSelectedItem;

    public AddToDialog()
    {
        InitializeComponent();
    }

    public void SetPlaylists(ObservableCollection<Playlist> playlists)
    {
        PlaylistComboBox.ItemsSource = playlists;
    }

    private void CreateNewCheckBox_Click(object sender, RoutedEventArgs e)
    {
        CreateNewCheckBoxIsChecked = CreateNewCheckBox.IsChecked ?? false;

        if (CreateNewCheckBox.IsChecked is true)
        {
            TextBoxPlaylistName.Visibility = Visibility.Visible;
            PlaylistComboBox.IsEnabled = false;
        }
        else
        {
            TextBoxPlaylistName.Visibility = Visibility.Collapsed;
            PlaylistComboBox.IsEnabled = true;
        }
    }

    private void TextBoxPlaylistName_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBoxPlaylistNameText = TextBoxPlaylistName.Text;
    }

    private void PlaylistComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PlaylistComboBox.SelectedItem is Playlist playlist)
        {
            PlaylistComboBoxSelectedItem = playlist;
        }
    }
}
