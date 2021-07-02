using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MPDCtrl.Common;

namespace MPDCtrl.ViewModels.Classes
{
    /// <summary>
    /// Profile class for connection setting.
    /// </summary>
    public class Profile : ViewModelBase
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
                NotifyPropertyChanged(nameof(Host));
            }
        }

        private int _port;
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

        private string _password;
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

        private string _name;
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
    }
}
