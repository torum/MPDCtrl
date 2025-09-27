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
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

namespace MPDCtrl.Views.Dialogs;

public sealed partial class ProfileDialog : Page
{
    private Profile? _pro;

    public ProfileDialog()
    {
        InitializeComponent();
    }
    public void SetProfile(Profile pro)
    {
        if (pro is null)
        {
            return;
        }

        _pro = pro;
        /*
        this.HostTextBox.Text = pro.Host;
        this.PortTextBox.Text = pro.Port.ToString();
        this.PasswordBox.Text = pro.Password;
        this.IsDefaultCheckBox.IsChecked = pro.IsDefault;
        */
    }

    public Profile? GetProfile()
    {
        if (_pro is null)
        {
            return null;
        }

        /*
        // TODO: Validate Host input.

        /*
        _pro.Host = this.HostTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(this.PortTextBox.Text))
        {
            _pro.Port = (int)6600;
        }
        else
        {
            try
            {
                _pro.Port = int.Parse(this.PortTextBox.Text);
            }
            catch
            {
                _pro.Port = (int)6600;
            }
        }

        _pro.Password = this.PasswordBox.Text ?? string.Empty;

        _pro.IsDefault = this.IsDefaultCheckBox.IsChecked ?? false;

        _pro.Name = _pro.Host + ":" + _pro.Port.ToString();
        */
        return _pro;
    }

    public Profile? GetProfileAsNew()
    {
        Profile pro = new();
        /*
        pro.Host = this.HostTextBox.Text ?? string.Empty;

        if (string.IsNullOrEmpty(this.PortTextBox.Text))
        {
            pro.Port = (int)6600;
        }
        else
        {
            try
            {
                pro.Port = int.Parse(this.PortTextBox.Text);
            }
            catch
            {
                pro.Port = (int)6600;
            }
        }

        pro.Password = this.PasswordBox.Text ?? string.Empty;

        pro.IsDefault = this.IsDefaultCheckBox.IsChecked ?? false;

        pro.Name = pro.Host + ":" + pro.Port.ToString();
        */
        return pro;
    }
}
