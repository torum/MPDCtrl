using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ReactiveUI;
using MPDCtrlX.ViewModels;

namespace MPDCtrlX.ViewModels.Classes
{
    /// <summary>
    /// Profile class for connection setting.
    /// </summary>
    public class Profile : ReactiveObject//ViewModelBase
    {
        private string _host;
        public string Host
        {
            get { return _host; }
            set
            {
                if (_host == value)
                    return;

                _host = value;
                this.RaisePropertyChanged(nameof(Host));
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

        private int _port;
        public int Port
        {
            get { return _port; }
            set
            {
                if (_port == value)
                    return;

                _port = value;
                this.RaisePropertyChanged(nameof(Port));
            }
        }

        private string _password;
        public string Password
        {
            get { return _password; }
            set
            {
                if (_password == value)
                    return;

                _password = value;
                this.RaisePropertyChanged(nameof(Password));
            }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set
            {
                if (_name == value)
                    return;

                _name = value;
                this.RaisePropertyChanged(nameof(Name));
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

                this.RaisePropertyChanged(nameof(IsDefault));
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

                this.RaisePropertyChanged(nameof(Volume));
            }
        }
    }
}
