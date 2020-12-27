using MPDCtrl.Views;
using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using Xamarin.Forms;
using MPDCtrl.Services;
using MPDCtrl.Models;
using MPDCtrl.Models.Classes;
using System.Diagnostics;

namespace MPDCtrl.ViewModels
{
    public class ConnectViewModel : BaseViewModel
    {
        private MPC _mpc;
        private Connection _con;

        private string _host;
        public string Host
        {
            get { return _host; }
            set
            {
                ClearErrror("Host");
                _host = value;

                // Validate input.
                if (value == "")
                {
                    SetError("Host", "Settings_ErrorHostMustBeSpecified");

                }
                else if (value == "localhost")
                {
                    _host = "127.0.0.1";
                }
                else
                {
                    try
                    {
                        IPAddress ipAddress = IPAddress.Parse(value);
                        if (ipAddress != null)
                        {
                            _host = value;
                        }
                    }
                    catch
                    {
                        //System.FormatException
                        SetError("Host", "Settings_ErrorHostInvalidAddressFormat");
                    }
                }

                NotifyPropertyChanged("Host");
            }
        }

        private int _port = 6600;
        public string Port
        {
            get { return _port.ToString(); }
            set
            {
                ClearErrror("Port");

                if (value == "")
                {
                    SetError("Port", "Settings_ErrorPortMustBeSpecified");
                    _port = 0;
                }
                else
                {
                    // Validate input. Test with i;
                    if (Int32.TryParse(value, out int i))
                    {
                        //Int32.TryParse(value, out _defaultPort)
                        // Change the value only when test was successfull.
                        _port = i;
                        ClearErrror("Port");
                    }
                    else
                    {
                        SetError("Port", "Resources.Settings_ErrorInvalidPortNaN");
                        _port = 0;
                    }
                }

                NotifyPropertyChanged("Port");
            }
        }

        private string _password = "";
        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                if (_password == value)
                    return;

                _password = value;

                NotifyPropertyChanged("Password");
            }
        }

        private string _connectionStatus;
        public string ConnectionStatus
        {
            get
            {
                return _connectionStatus;
            }
            set
            {
                if (_connectionStatus == value)
                    return;

                _connectionStatus = value;

                NotifyPropertyChanged("ConnectionStatus");
            }
        }


        public event EventHandler<String> GoToPage;


        public Command ConnectCommand { get; }

        public ConnectViewModel()
        {

            Title = "Connect to MPD";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            ConnectCommand = new Command(OnConnectClicked, Connect_CanExecute);


            _mpc.Connected += new MPC.MpdConnected(OnConnected);
        }

        private bool Connect_CanExecute()
        {
            /*
            if (string.IsNullOrEmpty(Host))
                return false;
            if (_port == 0)
                return false;
            */
            return true;
        }

        private async void OnConnectClicked()
        {
            _mpc.MpdHost = Host;
            _mpc.MpdPort = _port;
            _mpc.MpdPassword = Password;

            IsBusy = true;

            ConnectionStatus = "Connecting to " + Host + ":" + _port.ToString() + "...";

            ConnectionResult ret = await _mpc.MpdConnect();
            if (ret.isSuccess)
            {
                ConnectionStatus = "Success!" + Environment.NewLine + "Waiting for OK from the MPD...";

            }
            else
            {
                ConnectionStatus = "Error connecting: " + ret.errorMessage;
            }

        }

        private void OnConnected(MPC sender)
        {
            Debug.WriteLine("OnConnected@ConnectViewModel");

            IsBusy = false;

            ConnectionStatus = "Connected! Saving connection info...";

            Profile pro = new Profile();
            pro.Name = Host + ":" + _port.ToString();
            pro.Host = Host;
            pro.Port = _port;
            pro.Password = Password;
            pro.IsDefault = true;

            _con.Profiles.Add(pro);
            _con.SaveProfile();

            ConnectionStatus = "...";

            Device.BeginInvokeOnMainThread(
            () =>
            {
                GoToPage?.Invoke(this, "HomePage");
            });

        }
    }
}
