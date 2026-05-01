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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
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

        ValidateHostInput();
        ValidatePortInput();
    }

    public void SetProfile(Profile pro)
    {
        if (pro is null)
        {
            return;
        }

        _pro = pro;

        this.TextBoxHost.Text = pro.Host;
        this.TextBoxPort.Text = pro.Port.ToString();
        this.PasswordBox.Password = pro.Password;
        this.IsDefaultCheckBox.IsChecked = pro.IsDefault;
    }

    public Profile? GetProfile()
    {
        if (_pro is null)
        {
            return null;
        }

        // TODO: Validate Host input.

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

        //Debug.WriteLine($"GetProfile() password: {this.PasswordBox.Password}");

        _pro.Password = this.PasswordBox.Password ?? string.Empty;

        _pro.IsDefault = this.IsDefaultCheckBox.IsChecked ?? false;

        _pro.Name = _pro.Host + ":" + _pro.Port.ToString();

        return _pro;
    }

    public Profile? GetProfileAsNew()
    {
        Profile pro = new()
        {
            Host = this.TextBoxHost.Text ?? string.Empty
        };

        if (string.IsNullOrEmpty(this.TextBoxPort.Text))
        {
            pro.Port = (int)6600;
        }
        else
        {
            try
            {
                pro.Port = int.Parse(this.TextBoxPort.Text);
            }
            catch
            {
                pro.Port = (int)6600;
            }
        }

        pro.Password = this.PasswordBox.Password ?? string.Empty;

        pro.IsDefault = this.IsDefaultCheckBox.IsChecked ?? false;

        pro.Name = pro.Host + ":" + pro.Port.ToString();

        return pro;
    }

    private async void ValidateHostInput()
    {
        // validate and show ok icon
        bool isError;
        string hostText = this.TextBoxHost.Text;
        hostText = hostText.Trim();

        if (string.IsNullOrWhiteSpace(hostText))
        {
            isError = true;
        }
        else
        {
            if (hostText.Equals("localhost") || hostText.Equals("127.0.0.1"))
            {
                isError = false;
            }
            else
            {
                try
                {
                    //ipAddress = IPAddress.Parse(hostText);
                    if (IPAddress.TryParse(hostText, out IPAddress? ipAddress))
                    {
                        if (ipAddress is not null)
                        {
                            isError = false;
                        }
                        else
                        {
                            isError = true;
                        }
                    }
                    else
                    {
                        isError = true;
                    }
                }
                catch
                {
                    isError = true;
                }

                if (isError)
                {
                    try
                    {
                        var addresses = await Dns.GetHostAddressesAsync(hostText, AddressFamily.InterNetwork);
                        if (addresses.Length > 0)
                        {
                            //ipAddress = addresses[0];
                            Debug.WriteLine($"IP addresses for {hostText}: {addresses[0]}");
                            foreach (var ip in addresses)
                            {
                                Debug.WriteLine(ip);
                            }
                            isError = false;
                        }
                        else
                        {
                            isError = true;
                        }
                    }
                    catch (Exception)
                    {
                        isError = true;
                    }
                }
            }
        }

        if (isError)
        {
            this.IconHostOK.Visibility = Visibility.Collapsed;
            this.IconHostError.Visibility = Visibility.Visible;
        }
        else
        {
            this.IconHostOK.Visibility = Visibility.Visible;
            this.IconHostError.Visibility = Visibility.Collapsed;
        }
    }

    private void ValidatePortInput()
    {
        // validate and show ok icon
        bool isError;
        string portText = this.TextBoxPort.Text;

        if (portText.Equals("6600"))
        {
            isError = false;
        }
        else if (string.IsNullOrWhiteSpace(portText))
        {
            isError = true;
        }
        else
        {
            if (Int32.TryParse(portText, out var i))
            {
                if (i >= 1024)
                {
                    isError = false;
                }
                else
                {
                    isError = true;
                }
            }
            else
            {
                isError = true;
            }
        }

        if (isError)
        {
            this.IconPortOK.Visibility = Visibility.Collapsed;
            this.IconPortError.Visibility = Visibility.Visible;
        }
        else
        {
            this.IconPortOK.Visibility = Visibility.Visible;
            this.IconPortError.Visibility = Visibility.Collapsed;
        }
    }

    private void TextBoxHost_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidateHostInput();
    }

    private void TextBoxPort_TextChanged(object sender, TextChangedEventArgs e)
    {
        ValidatePortInput();
    }
}
