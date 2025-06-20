using Avalonia.Threading;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;

namespace MPDCtrlX.Models;

/// <summary>
/// Profile class for connection setting.
/// </summary>
public class Profile : INotifyPropertyChanged
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
            NotifyPropertyChanged(nameof(Host));
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
            NotifyPropertyChanged(nameof(HostIpAddress));
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
            NotifyPropertyChanged(nameof(Port));
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
            NotifyPropertyChanged(nameof(Password));
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
            NotifyPropertyChanged(nameof(Name));
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

            NotifyPropertyChanged(nameof(IsDefault));
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

            NotifyPropertyChanged(nameof(Volume));
        }
    }

    #region == INotifyPropertyChanged ==

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void NotifyPropertyChanged(string propertyName)
    {
        //Application.Current.Dispatcher.Invoke(() =>
        Dispatcher.UIThread.Post(() =>
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        });
    }

    #endregion
}
