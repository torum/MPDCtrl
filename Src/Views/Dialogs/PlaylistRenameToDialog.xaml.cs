using Microsoft.UI.Xaml.Controls;

namespace MPDCtrl.Views.Dialogs;

public sealed partial class PlaylistRenameToDialog : Page
{
    public string TextBoxPlaylistNameText = string.Empty;

    public PlaylistRenameToDialog()
    {
        InitializeComponent();
    }

    private void TextBoxPlaylistName_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBoxPlaylistNameText = TextBoxPlaylistName.Text;
    }



    //Dialog_Title_NewPlaylistName
}
