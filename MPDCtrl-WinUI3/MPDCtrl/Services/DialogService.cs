using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.ApplicationModel.Resources;
using MPDCtrl.Models;
using MPDCtrl.Services.Contracts;
using MPDCtrl.Views;
using MPDCtrl.Views.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MPDCtrl.Services;

public class DialogService : IDialogService
{
    private readonly ResourceLoader _resourceLoader = new();

    public record AddToDialogResult(string PlaylistName, bool AsNew);
    public record RenameDialogResult(string PlaylistName);

    public DialogService()
    {
       
    }

    public async Task<Profile?> ShowInitDialog(ViewModels.MainViewModel vm)
    {
        if (App.MainWnd is null)
        {
            Debug.WriteLine("App.MainWnd is null");
            return null;
        }

        if (App.MainWnd.Content is not ShellPage)
        {
            Debug.WriteLine("App.MainWnd?.Content is not ShellPage");
            return null;
        }

        Debug.WriteLine("ShowInitDialog");
        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWnd.Content.XamlRoot,
            Title = "MPDCtrl",//_resourceLoader.GetString("Dialog_Title_SelectPlaylist")
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = _resourceLoader.GetString("Dialog_Connect"),
            DefaultButton = ContentDialogButton.Primary,
            IsSecondaryButtonEnabled = false,
            CloseButtonText = _resourceLoader.GetString("Dialog_Cancel"),//
            Content = new Views.Dialogs.InitDialog(vm)
            {
                //DataContext = vm
            }
        };

        if (dialog.Content is not InitDialog dialogContent)
        {
            return null;
        }

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        return dialogContent.GetProfile();

    }

    public async Task<AddToDialogResult?> ShowSongsAddToDialog(ViewModels.MainViewModel vm)
    {
        if (App.MainWnd is null)
        {
            return null;
        }

        if (App.MainWnd.Content is not ShellPage)
        {
            return null;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWnd.Content.XamlRoot,
            Title = _resourceLoader.GetString("Dialog_Title_SelectPlaylist"),
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = _resourceLoader.GetString("Dialog_Ok"),
            DefaultButton = ContentDialogButton.Primary,
            IsSecondaryButtonEnabled = false,
            CloseButtonText = _resourceLoader.GetString("Dialog_Cancel"),
            Content = new Views.Dialogs.SongsAddToDialog()
            {
                //DataContext = new DialogViewModel()
            }
        };

        if (dialog.Content is not SongsAddToDialog dialogContent)
        {
            return null;
        }

        // Sort
        CultureInfo ci = CultureInfo.CurrentCulture;
        StringComparer comp = StringComparer.Create(ci, true);

        //dialogContent.PlaylistComboBox.ItemsSource = new ObservableCollection<Playlist>(vm.Playlists.OrderBy(x => x.Name, comp));
        dialogContent.SetPlaylists(new ObservableCollection<Playlist>(vm.Playlists.OrderBy(x => x.Name, comp)));

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            if (dialogContent.CreateNewCheckBoxIsChecked)
            {
                var str = dialogContent.TextBoxPlaylistNameText ?? string.Empty;

                if (!string.IsNullOrEmpty(str.Trim()))
                {
                    return new AddToDialogResult(str.Trim(), true);
                }
            }
            else
            {
                var plselitem = dialogContent.PlaylistComboBoxSelectedItem;

                if (plselitem is Models.Playlist pl)
                {
                    if (!string.IsNullOrWhiteSpace(pl.Name))
                    {
                        return new AddToDialogResult(pl.Name, false);
                    }
                }
            }
        }

        return null;
    }

    public async Task<RenameDialogResult?> ShowPlaylistRenameToDialog(ViewModels.MainViewModel vm)
    {
        if (App.MainWnd is null)
        {
            return null;
        }

        if (App.MainWnd.Content is not ShellPage)
        {
            return null;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWnd.Content.XamlRoot,
            Title = _resourceLoader.GetString("Dialog_Title_NewPlaylistName"),
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = _resourceLoader.GetString("Dialog_Ok"),
            DefaultButton = ContentDialogButton.Primary,
            IsSecondaryButtonEnabled = false,
            CloseButtonText = _resourceLoader.GetString("Dialog_Cancel"),
            Content = new Views.Dialogs.PlaylistRenameToDialog()
            {
                //DataContext = new DialogViewModel()
            }
        };

        if (dialog.Content is not PlaylistRenameToDialog dialogContent)
        {
            return null;
        }

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            var str = dialogContent.TextBoxPlaylistNameText ?? string.Empty;

            if (!string.IsNullOrEmpty(str.Trim()))
            {
                return new RenameDialogResult(str.Trim());
            }
        }

        return null;
    }

    public async Task<Profile?> ShowProfileAddDialog()
    {
        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWnd?.Content.XamlRoot,
            Title = "ADD",
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = "Dialog_Ok",
            DefaultButton = ContentDialogButton.Primary,
            IsSecondaryButtonEnabled = false,
            CloseButtonText = "Dialog_CancelClose",
            Content = new Views.Dialogs.ProfileDialog()
            {
                //DataContext = new DialogViewModel()
            }
        };

        if (dialog.Content is not Views.Dialogs.ProfileDialog dlg)
        {
            return null;
        }

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        var ret = dlg.GetProfileAsNew();
        if (ret is not null)
        {
            return ret;
        }

        return null;
    }

    public async Task<Profile?> ShowProfileEditDialog(Profile selectedProfile)
    {
        if (selectedProfile is null)
        {
            return null;
        }

        var dialog = new ContentDialog
        {
            XamlRoot = App.MainWnd?.Content.XamlRoot,
            Title = "EDIT",
            IsPrimaryButtonEnabled = true,
            PrimaryButtonText = "Dialog_Ok",
            DefaultButton = ContentDialogButton.Primary,
            IsSecondaryButtonEnabled = false,
            CloseButtonText = "Dialog_CancelClose",
            Content = new Views.Dialogs.ProfileDialog()
            {
                //DataContext = new DialogViewModel()
            }
        };

        if (dialog.Content is not Views.Dialogs.ProfileDialog dlg)
        {
            return null;
        }

        dlg.SetProfile(selectedProfile);

        var result = await dialog.ShowAsync();

        if (result != ContentDialogResult.Primary)
        {
            return null;
        }

        return dlg.GetProfile();
        /*
        if (ret is not null)
        {
            selectedProfile = ret;
        }
        */
    }

}
