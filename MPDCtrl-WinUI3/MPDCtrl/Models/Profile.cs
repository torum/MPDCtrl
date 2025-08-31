
using CommunityToolkit.Mvvm.ComponentModel;
using MPDCtrl.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace MPDCtrl.Models;

/// <summary>
/// Profile class for connection setting.
/// </summary>
public partial class Profile : ObservableObject
{
    private string _host = "";
    public string Host
    {
        get { return _host; }
        set
        {
            if (_host == value)
                return;

            _host = value;
            OnPropertyChanged(nameof(Host));
        }
    }

    /*
    private IPAddress _hostIpAddress;
    public IPAddress HostIpAddress
    {
        get { return _hostIpAddress; }
        set
        {
            if (_hostIpAddress == value)
                return;

            _hostIpAddress = value;
            OnPropertyChanged(nameof(HostIpAddress));
        }
    }
    */

    private int _port = 6600;
    public int Port
    {
        get { return _port; }
        set
        {
            if (_port == value)
                return;

            _port = value;
            OnPropertyChanged(nameof(Port));
        }
    }

    private string _password = "";
    public string Password
    {
        get { return _password; }
        set
        {
            if (_password == value)
                return;

            _password = value;
            OnPropertyChanged(nameof(Password));
        }
    }

    private string _name = "";
    public string Name
    {
        get { return _name; }
        set
        {
            if (_name == value)
                return;

            _name = value;
            OnPropertyChanged(nameof(Name));
        }
    }

    private bool _isDefault;
    public bool IsDefault
    {
        get { return _isDefault; }
        set
        {
            if (_isDefault == value)
                return;

            _isDefault = value;

            OnPropertyChanged(nameof(IsDefault));
        }
    }

    private double _volume = 50;
    public double Volume
    {
        get { return _volume; }
        set
        {
            if (_volume == value)
                return;

            _volume = value;

            OnPropertyChanged(nameof(Volume));
        }
    }
}
