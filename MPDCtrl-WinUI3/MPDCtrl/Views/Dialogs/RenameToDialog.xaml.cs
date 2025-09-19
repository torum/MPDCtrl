using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views.Dialogs;

public sealed partial class RenameToDialog : Page
{
    public string TextBoxPlaylistNameText = string.Empty;

    public RenameToDialog()
    {
        InitializeComponent();
    }

    private void TextBoxPlaylistName_TextChanged(object sender, TextChangedEventArgs e)
    {
        TextBoxPlaylistNameText = TextBoxPlaylistName.Text;
    }



    //Dialog_Title_NewPlaylistName
}
