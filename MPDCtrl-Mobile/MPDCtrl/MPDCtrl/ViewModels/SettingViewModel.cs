using System;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;
using System.Net;
using Xamarin.Essentials;
using MPDCtrl.Services;
using MPDCtrl.Models;
using MPDCtrl.ViewModels;
using System.Diagnostics;
using MPDCtrl.Models.Classes;

namespace MPDCtrl.ViewModels
{


    class SettingViewModel : BaseViewModel
    {

        private MPC _mpc;
        private Connection _con;

        public ObservableCollection<Profile> Profiles
        {
            get { return _con.Profiles; }
        }

        private Profile _selectedProfile;
        public Profile SelectedProfile
        {
            get
            {
                return _selectedProfile;
            }
            set
            {
                if (_selectedProfile == value)
                    return;

                _selectedProfile = value;

                if (_selectedProfile != null)
                {
                    ClearErrror("Host");
                    ClearErrror("Port");
                    Host = SelectedProfile.Host;
                    Port = SelectedProfile.Port.ToString();
                    Password = SelectedProfile.Password;
                    SetIsDefault = SelectedProfile.IsDefault;
                }
                else
                {
                    ClearErrror("Host");
                    ClearErrror("Port");
                    Host = "";
                    Port = "6600";
                    Password = "";
                }

                SettingProfileEditMessage = "";

                NotifyPropertyChanged("SelectedProfile");
            }
        }

        private string _host = "";
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
                    IPAddress ipAddress = null;
                    try
                    {
                        ipAddress = IPAddress.Parse(value);
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

        private string _password;
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

        private bool _setIsDefault = true;
        public bool SetIsDefault
        {
            get { return _setIsDefault; }
            set
            {
                if (_setIsDefault == value)
                    return;

                if (value == false)
                {
                    if (Profiles.Count <= 1)
                    {
                        return;
                    }
                }

                _setIsDefault = value;

                NotifyPropertyChanged("SetIsDefault");
            }
        }

        private string _settingProfileEditMessage;
        public string SettingProfileEditMessage
        {
            get
            {
                return _settingProfileEditMessage;
            }
            set
            {
                _settingProfileEditMessage = value;
                NotifyPropertyChanged("SettingProfileEditMessage");
            }
        }


        public event EventHandler<String> GoToPage;

        public SettingViewModel()
        {
            Title = "Profile";

            App me = App.Current as App;
            _con = me.MpdConection;
            _mpc = _con.Mpc;

            _mpc.IsBusy += new MPC.MpdIsBusy(OnClientIsBusy);

            SaveOrUpdateProfileCommand = new Command(SaveOrUpdateProfile);
            AddNewProfileCommand = new Command(AddNewProfile);
            DeleteProfileCommand = new Command(DeleteProfile);

            if (_con.CurrentProfile != null)
            {
                SelectedProfile = _con.CurrentProfile;

            }

            _mpc.Connected += new MPC.MpdConnected(OnConnected);
        }

        private void OnClientIsBusy(MPC sender, bool on)
        {
            IsBusy = on;
        }

        public Command AddNewProfileCommand { get; }
        void AddNewProfile()    
        {
            Profile pro = new Profile();
            SelectedProfile = pro;
        }

        public Command DeleteProfileCommand { get; }
        void DeleteProfile()
        {
            if (Profiles.Count <= 1)
            {
                SettingProfileEditMessage = "You must have at least one profile.";
                return;
            }

            Profiles.Remove(SelectedProfile);

            SelectedProfile = Profiles[0];

            _con.SaveProfile();
        }


        public Command SaveOrUpdateProfileCommand { get; }
        async void SaveOrUpdateProfile()
        {
            if (string.IsNullOrEmpty(Host))
            {
                SettingProfileEditMessage = "Please enter a valid host address.";
                return;
            }
            if (_port == 0)
            {
                SettingProfileEditMessage = "Please enter a valid port number.";
                return;
            }

            if (SelectedProfile != _con.CurrentProfile)
            {
                if (SelectedProfile == null)
                {
                    Profile pro = new Profile();
                    _selectedProfile = pro;
                }

                SelectedProfile.Name = Host + ":" + Port;
                _selectedProfile.Host = Host;
                _selectedProfile.Port = _port;
                _selectedProfile.Password = Password;

                Profiles.Add(SelectedProfile);

                _con.CurrentProfile = SelectedProfile;

            }
            else
            {
                _con.CurrentProfile.Name = Host + ":" + Port;
                _con.CurrentProfile.Host = Host;
                _con.CurrentProfile.Port = _port;
                _con.CurrentProfile.Password = Password;

            }

            _con.SaveProfile();

            _mpc.MpdHost = _con.CurrentProfile.Host;
            _mpc.MpdPort = _con.CurrentProfile.Port;
            _mpc.MpdPassword = _con.CurrentProfile.Password;

            //IsBusy = true;

            SettingProfileEditMessage = "Connecting to " + Host + ":" + _port.ToString() + "...";

            ConnectionResult ret = await _mpc.MpdConnect();
            if (ret.isSuccess)
            {
                SettingProfileEditMessage = "Success!" + Environment.NewLine + "Waiting for OK from the MPD...";
            }
            else
            {
                SettingProfileEditMessage = "Error connecting: " + ret.errorMessage;
            }

        }

        private void OnConnected(MPC sender)
        {
            Debug.WriteLine("OnConnected@SettingViewModel");

            IsBusy = false;

            SettingProfileEditMessage = "Connected!";

            Device.BeginInvokeOnMainThread(
            () =>
            {
                GoToPage?.Invoke(this, "HomePage");
            });

            SettingProfileEditMessage = "";
        }
    }
}
