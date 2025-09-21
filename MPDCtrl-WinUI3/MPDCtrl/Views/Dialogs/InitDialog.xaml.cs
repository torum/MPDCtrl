using Microsoft.Extensions.Hosting;
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
using System.Net;
using System.Net.Sockets;
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

        ValidateHostInput();
        ValidatePortInput();
    }

    private async void ValidateHostInput()
    {
        // validate and show ok icon
        bool isError = true;
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
                IPAddress? ipAddress = null;
                try
                {
                    //ipAddress = IPAddress.Parse(hostText);
                    if (IPAddress.TryParse(hostText, out ipAddress))
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
                    ipAddress = null;
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
        bool isError = true;
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

    public Profile? GetProfile()
    {
        if (_pro is null)
        {
            return null;
        }

        _pro.Host = this.TextBoxHost.Text ?? string.Empty;
        _pro.Host = _pro.Host.Trim();

        if (string.IsNullOrEmpty(this.TextBoxPort.Text))
        {
            _pro.Port = (int)6600;
        }
        else
        {
            try
            {
                _pro.Port = int.Parse(this.TextBoxPort.Text.Trim());
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
