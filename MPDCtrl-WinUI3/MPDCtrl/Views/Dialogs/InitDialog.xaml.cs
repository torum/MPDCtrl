using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views.Dialogs;

public sealed partial class InitDialog : Page
{
    private readonly Profile? _pro;

    //private readonly ResourceLoader _resourceLoader = new();

    public MainViewModel ViewModel
    {
        get;
    }

    public InitDialog(MainViewModel vm)
    {
        ViewModel = vm;

        InitializeComponent();

        _pro = new();
    }

    private void TextBoxHost_TextChanged(object sender, TextChangedEventArgs e)
    {
        // validate and show ok icon
    }

    private void TextBoxPort_TextChanged(object sender, TextChangedEventArgs e)
    {
        // validate and show ok icon
    }

    public Profile? GetProfile()
    {
        if (_pro is null)
        {
            return null;
        }

        /*
    // Validate Host input.
    if (Host == "")
    {
        //SetError(nameof(Host), "Error: Host must be specified."); //TODO: translate
        NotifyPropertyChanged(nameof(Host));
        return;
    }
    else
    {
        if (Host == "localhost")
        {
            Host = "127.0.0.1";
        }

        IPAddress? ipAddress = null;
        try
        {
            ipAddress = IPAddress.Parse(Host);
            if (ipAddress is not null)
            {
                //ClearError(nameof(Host));
            }
        }
        catch
        {
            //System.FormatException
            //SetError(nameof(Host), "Error: Invalid address format."); //TODO: translate

            return;
        }
    }
    */

        _pro.Host = this.TextBoxHost.Text ?? string.Empty;

        if (string.IsNullOrEmpty(this.TextBoxPort.Text))
        {
            _pro.Port = (int)6600;
        }
        else
        {
            try
            {
                _pro.Port = int.Parse(this.TextBoxPort.Text);
            }
            catch
            {
                _pro.Port = (int)6600;
            }
        }

        _pro.Password = this.PasswordBox.Password ?? string.Empty;

        _pro.Name = _pro.Host + ":" + _pro.Port.ToString();

        return _pro;
    }
}
