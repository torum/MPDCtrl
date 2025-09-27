using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.ApplicationModel.Resources;
using MPDCtrl.Helpers;
using MPDCtrl.Models;
using MPDCtrl.Services;
using MPDCtrl.Services.Contracts;
using MPDCtrl.Views;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Input;
using System.Xml;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.ApplicationModel.DataTransfer;
using Windows.Graphics.Imaging;
using Windows.Media.Core;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using WinRT.Interop;

namespace MPDCtrl.ViewModels;

public partial class MainViewModel : ObservableObject
{
    #region == Flags ==

    private bool _isBusy;
    public bool IsBusy
    {
        get
        {
            return _isBusy;
        }
        set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged(nameof(IsBusy));
        }
    }

    private bool _isWorking;
    public bool IsWorking
    {
        get
        {
            return _isWorking;
        }
        set
        {
            if (_isWorking == value)
                return;

            _isWorking = value;
            OnPropertyChanged(nameof(IsWorking));
        }
    }

    #endregion

    #region == Profile ==

    private readonly ObservableCollection<Profile> _profiles = [];
    public ObservableCollection<Profile> Profiles
    {
        get { return _profiles; }
    }

    private Profile? _currentProfile;
    public Profile? CurrentProfile
    {
        get { return _currentProfile; }
        set
        {
            if (_currentProfile == value)
                return;

            _currentProfile = value;
            OnPropertyChanged(nameof(CurrentProfile));

            SelectedProfile = _currentProfile;

            if (_currentProfile is not null)
            {
                _volume = _currentProfile.Volume;
                OnPropertyChanged(nameof(Volume));

                Host = _currentProfile.Host;
                Port = _currentProfile.Port.ToString();
                _password = _currentProfile.Password;
                OnPropertyChanged(nameof(Password));
            }
            else
            {
                Debug.WriteLine("(_currentProfile is not null)");
            }
        }
    }

    private Profile? _selectedProfile;
    public Profile? SelectedProfile
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

            OnPropertyChanged(nameof(SelectedProfile));
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

            _setIsDefault = value;

            OnPropertyChanged(nameof(SetIsDefault));
        }
    }

    /*
    private bool _isRememberAsProfile = true;
    public bool IsRememberAsProfile
    {
        get
        {
            return _isRememberAsProfile;
        }
        set
        {
            if (_isRememberAsProfile == value)
                return;

            _isRememberAsProfile = value;
            OnPropertyChanged(nameof(IsRememberAsProfile));
        }
    }
    */

    private string _host = "";
    public string Host
    {
        get { return _host; }
        set
        {
            //ClearError(nameof(Host));
            _host = value;

            // Validate input.
            if (value == "")
            {
                //SetError(nameof(Host), MPDCtrlX.Properties.Resources.Settings_ErrorHostMustBeSpecified);

            }
            else if (value == "localhost")
            {
                _host = "127.0.0.1";
            }
            else
            {
                IPAddress ipAddress;
                try
                {
                    ipAddress = IPAddress.Parse(value);
                    if (ipAddress is not null)
                    {
                        _host = value;
                    }
                }
                catch
                {
                    //System.FormatException
                    //SetError(nameof(Host), MPDCtrlX.Properties.Resources.Settings_ErrorHostInvalidAddressFormat);
                }
            }

            OnPropertyChanged(nameof(Host));
        }
    }

    private IPAddress? _hostIpAddress;
    public IPAddress? HostIpAddress
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

    private int _port = 6600;
    public string Port
    {
        get { return _port.ToString(); }
        set
        {
            //ClearError(nameof(Port));

            if (value == "")
            {
                //SetError(nameof(Port), MPDCtrlX.Properties.Resources.Settings_ErrorPortMustBeSpecified);
                _port = 6600;
            }
            else
            {
                // Validate input. Test with i;
                if (Int32.TryParse(value, out int i))
                {
                    //Int32.TryParse(value, out _defaultPort)
                    // Change the value only when test was successfull.
                    _port = i;
                    //ClearError(nameof(Port));
                }
                else
                {
                    //SetError(nameof(Port), MPDCtrlX.Properties.Resources.Settings_ErrorInvalidPortNaN);
                    _port = 6600;
                }
            }

            OnPropertyChanged(nameof(Port));
        }
    }

    private string _password = "";
    public string Password
    {
        get
        {
            return DummyPassword(_password);
        }
        set
        {
            // Don't. if (_password == value) ...

            _password = value;

            OnPropertyChanged(nameof(Password));
        }
    }

    public static string Encrypt(string s)
    {
        if (string.IsNullOrEmpty(s)) { return ""; }

        byte[] entropy = [0x72, 0xa2, 0x12, 0x04];

        // Uses System.Security.Cryptography.ProtectedData
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                byte[] userData = System.Text.Encoding.UTF8.GetBytes(s);
                byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(userData, entropy, System.Security.Cryptography.DataProtectionScope.CurrentUser);

                return System.Convert.ToBase64String(encryptedData);
            }
            catch
            {
                Debug.WriteLine($"Encrypt fail.");
                return s;
            }
        }
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        else
        {
            string encryptionKey = "withas";
            byte[] sBytes = Encoding.Unicode.GetBytes(s);
            using (System.Security.Cryptography.Aes encryptor = System.Security.Cryptography.Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using MemoryStream ms = new();
                using (CryptoStream cs = new(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(sBytes, 0, sBytes.Length);
                    cs.Close();
                }
                s = Convert.ToBase64String(ms.ToArray());
            }
            return s;
        }
    }

    public static string Decrypt(string s)
    {
        if (string.IsNullOrEmpty(s)) { return ""; }

        byte[] entropy = [0x72, 0xa2, 0x12, 0x04];

        // Uses System.Security.Cryptography.ProtectedData
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            try
            {
                byte[] encryptedData = System.Convert.FromBase64String(s);
                byte[] userData = System.Security.Cryptography.ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);

                return System.Text.Encoding.UTF8.GetString(userData);
            }
            catch
            {
                Debug.WriteLine($"Decrypt fail.");
                return "";
            }
        }
        //else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        else
        {
            string encryptionKey = "withas";
            s = s.Replace(" ", "+");
            byte[] sBytes = Convert.FromBase64String(s);
            using (System.Security.Cryptography.Aes encryptor = System.Security.Cryptography.Aes.Create())
            {
                Rfc2898DeriveBytes pdb = new(encryptionKey, entropy, 10000, HashAlgorithmName.SHA1);
                encryptor.Key = pdb.GetBytes(32);
                encryptor.IV = pdb.GetBytes(16);
                using MemoryStream ms = new();
                using (CryptoStream cs = new(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                {
                    cs.Write(sBytes, 0, sBytes.Length);
                    cs.Close();
                }
                s = Encoding.Unicode.GetString(ms.ToArray());
            }

            return s;
        }
    }

    private static string DummyPassword(string s)
    {
        if (string.IsNullOrEmpty(s)) { return ""; }
        string e = "";
        for (int i = 1; i <= s.Length; i++)
        {
            e += "*";
        }
        return e;
    }

    #endregion

    #region == CurrentSong ==

    private SongInfoEx? _currentSong;
    public SongInfoEx? CurrentSong
    {
        get
        {
            return _currentSong;
        }
        set
        {
            if (_currentSong == value)
                return;

            _currentSong = value;
            OnPropertyChanged(nameof(CurrentSong));
            OnPropertyChanged(nameof(CurrentSongTitle));
            OnPropertyChanged(nameof(CurrentSongArtist));
            OnPropertyChanged(nameof(CurrentSongAlbum));
            OnPropertyChanged(nameof(IsCurrentSongArtistNotNull));
            OnPropertyChanged(nameof(IsCurrentSongAlbumNotNull));

            //CurrentSongChanged?.Invoke(this, CurrentSongStringForWindowTitle);

            if (value is null)
            {
                //_elapsedTimer.Stop();
                IsCurrentSongNotNull = false;
            }
            else
            {
                IsCurrentSongNotNull = true;
            }
        }
    }

    public string CurrentSongTitle
    {
        get
        {
            if (_currentSong is not null)
            {
                return _currentSong.Title;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public string CurrentSongArtist
    {
        get
        {
            if (_currentSong is not null)
            {
                if (!string.IsNullOrEmpty(_currentSong.Artist))
                    return _currentSong.Artist.Trim();
                else
                    return "";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public string CurrentSongAlbum
    {
        get
        {
            if (_currentSong is not null)
            {
                if (!string.IsNullOrEmpty(_currentSong.Album))
                    return _currentSong.Album.Trim();
                else
                    return "";
            }
            else
            {
                return string.Empty;
            }
        }
    }

    public bool IsCurrentSongArtistNotNull
    {
        get
        {
            if (_currentSong is not null)
            {
                if (!string.IsNullOrEmpty(_currentSong.Artist))
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
    }

    public bool IsCurrentSongAlbumNotNull
    {
        get
        {
            if (_currentSong is not null)
            {
                if (!string.IsNullOrEmpty(_currentSong.Album))
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
        }
    }

    private bool _isCurrentSongNotNull;
    public bool IsCurrentSongNotNull
    {
        get
        {
            return _isCurrentSongNotNull;
        }
        set
        {
            if (_isCurrentSongNotNull == value)
                return;

            _isCurrentSongNotNull = value;
            OnPropertyChanged(nameof(IsCurrentSongNotNull));
        }
    }

    public string CurrentSongStringForWindowTitle
    {
        get
        {
            if (_currentSong is not null)
            {
                string s = string.Empty;

                if (!string.IsNullOrEmpty(_currentSong.Title))
                {
                    s = _currentSong.Title.Trim();
                }

                if (!string.IsNullOrEmpty(_currentSong.Artist))
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        s += " by ";
                    }
                    s += $"{_currentSong.Artist.Trim()}";
                }

                if (!string.IsNullOrEmpty(_currentSong.Album))
                {
                    if (!string.IsNullOrEmpty(s))
                    {
                        s += " from ";
                    }
                    s += $"{_currentSong.Album.Trim()}";
                }

                return s;
            }
            else
            {
                return string.Empty;
            }
        }
    }

    #endregion

    #region == AlbumArt ==

    private AlbumImage? _albumCover;
    public AlbumImage? AlbumCover
    {
        get
        {
            return _albumCover;
        }
        set
        {
            if (_albumCover == value)
                return;

            _albumCover = value;

            OnPropertyChanged(nameof(AlbumCover));
        }
    }

    private readonly ImageSource? _albumArtBitmapSourceDefault = null;
    private ImageSource? _albumArtBitmapSource;
    public ImageSource? AlbumArtBitmapSource
    {
        get
        {
            return _albumArtBitmapSource;
        }
        set
        {
            if (_albumArtBitmapSource == value)
                return;

            _albumArtBitmapSource = value;
            OnPropertyChanged(nameof(AlbumArtBitmapSource));
        }
    }

    #endregion

    #region == Playback ==

    private static readonly string _pathPlayButton = "M10.856 8.155A1.25 1.25 0 0 0 9 9.248v5.504a1.25 1.25 0 0 0 1.856 1.093l5.757-3.189a.75.75 0 0 0 0-1.312l-5.757-3.189ZM12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2ZM3.5 12a8.5 8.5 0 1 1 17 0 8.5 8.5 0 0 1-17 0Z";//"M2 12C2 6.477 6.477 2 12 2s10 4.477 10 10-4.477 10-10 10S2 17.523 2 12Zm8.856-3.845A1.25 1.25 0 0 0 9 9.248v5.504a1.25 1.25 0 0 0 1.856 1.093l5.757-3.189a.75.75 0 0 0 0-1.312l-5.757-3.189Z";//"M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
    private static readonly string _pathPauseButton = "M12 2C6.477 2 2 6.477 2 12s4.477 10 10 10 10-4.477 10-10S17.523 2 12 2Zm-1.5 6.25v7.5a.75.75 0 0 1-1.5 0v-7.5a.75.75 0 0 1 1.5 0Zm4.5 0v7.5a.75.75 0 0 1-1.5 0v-7.5a.75.75 0 0 1 1.5 0Z";
    //private static string _pathStopButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
    private string _playButton = _pathPlayButton;
    public string PlayButton
    {
        get
        {
            return _playButton;
        }
        set
        {
            if (_playButton == value)
                return;

            _playButton = value;
            OnPropertyChanged(nameof(PlayButton));
        }
    }

    private double _volume = 20;
    public double Volume
    {
        get
        {
            return _volume;
        }
        set
        {
            if (_volume == value)
            {
                return;
            }
            _volume = value;
            OnPropertyChanged(nameof(Volume));

            if (_mpc is null)
            {
                return;
            }

            if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) == _volume)
            {
                //return;
            }

            // If we have a timer and we are in this event handler, a user is still interact with the slider
            // we stop the timer
            _volumeDelayTimer?.Stop();

            //System.Diagnostics.Debug.WriteLine("Volume value is still changing. Skipping.");

            // we always create a new instance of DispatcherTimer
            _volumeDelayTimer = new System.Timers.Timer
            {
                AutoReset = false,

                // if one second passes, that means our user has stopped interacting with the slider
                // we do real event
                Interval = (double)1000
            };
            _volumeDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeVolume);

            _volumeDelayTimer.Start();
        }
    }

    private System.Timers.Timer? _volumeDelayTimer = null;
    private async void DoChangeVolume(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_mpc is null)
        {
            return;
        }
        await _mpc.MpdSetVolume(Convert.ToInt32(_volume));
    }

    private bool _repeat;
    public bool Repeat
    {
        get { return _repeat; }
        set
        {
            _repeat = value;
            OnPropertyChanged(nameof(Repeat));

            if (_mpc is null)
            {
                return;
            }
            if (_mpc.MpdStatus.MpdRepeat == value)
            {
                return;
            }

            Task.Run(SetRpeat);
        }
    }

    private bool _random;
    public bool Random
    {
        get { return _random; }
        set
        {
            _random = value;
            OnPropertyChanged(nameof(Random));

            if (_mpc is null)
            {
                return;
            }

            if (_mpc.MpdStatus.MpdRandom == value)
            {
                return;
            }

            Task.Run(SetRandom);
        }
    }

    private bool _consume;
    public bool Consume
    {
        get { return _consume; }
        set
        {
            _consume = value;
            OnPropertyChanged(nameof(Consume));

            if (_mpc is null)
            {
                return;
            }
            if (_mpc.MpdStatus.MpdConsume == value)
            {
                return;
            }

            Task.Run(SetConsume);
        }
    }

    private bool _single;
    public bool Single
    {
        get { return _single; }
        set
        {
            _single = value;
            OnPropertyChanged(nameof(Single));

            if (_mpc is null || _mpc.MpdStatus.MpdSingle == value)
            {
                return;
            }

            Task.Run(SetSingle);
        }
    }

    private int _time = 0;
    public int Time
    {
        get
        {
            return _time;
        }
        set
        {
            if (_time == value)
                return;

            _time = value;
            OnPropertyChanged(nameof(Time));
            OnPropertyChanged(nameof(TimeFormatted));
        }
    }

    public string TimeFormatted
    {
        get
        {
            int sec, min, hour, s;

            sec = Time / _elapsedTimeMultiplier;

            min = sec / 60;
            s = sec % 60;
            hour = min / 60;
            min %= 60;
            return string.Format("{0}:{1:00}:{2:00}", hour, min, s);
        }
    }

    private readonly int _elapsedTimeMultiplier = 1;// or 10
    private int _elapsed = 0;
    public int Elapsed
    {
        get
        {
            return _elapsed;
        }
        set
        {
            if ((value < _time) && _elapsed != value)
            {
                _elapsed = value;

                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    OnPropertyChanged(nameof(Elapsed));
                    OnPropertyChanged(nameof(ElapsedFormatted));
                });

                // If we have a timer and we are in this event handler, a user is still interact with the slider
                // we stop the timer
                _elapsedDelayTimer?.Stop();

                //System.Diagnostics.Debug.WriteLine("Elapsed value is still changing. Skipping.");

                // we always create a new instance of DispatcherTimer
                _elapsedDelayTimer = new System.Timers.Timer
                {
                    AutoReset = false,

                    // if one second passes, that means our user has stopped interacting with the slider
                    // we do real event
                    Interval = (double)1000
                };
                _elapsedDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeElapsed);

                _elapsedDelayTimer.Start();
            }
        }
    }

    public string ElapsedFormatted
    {
        get
        {
            int sec, min, hour, s;

            sec = _elapsed / _elapsedTimeMultiplier;

            min = sec / 60;
            s = sec % 60;
            hour = min / 60;
            min %= 60;

            return string.Format("{0}:{1:00}:{2:00}", hour, min, s);
        }
    }

    private System.Timers.Timer? _elapsedDelayTimer = null;
    private void DoChangeElapsed(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if (_mpc is not null && (_elapsed < _time) && SetSeekCommand.CanExecute(null))
        {
            //SetSeekCommand.Execute(null);
            Task.Run(SetSeek);
        }
    }

    private readonly System.Timers.Timer _elapsedTimer = new(1000); // when using _elapsedTimeMultiplier(other than 1), change this accordingly.
    private void ElapsedTimer(object? sender, System.Timers.ElapsedEventArgs e)
    {
        if ((_elapsed < _time) && (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play))
        {
            _elapsed += 1;

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(Elapsed));
                OnPropertyChanged(nameof(ElapsedFormatted));
            });
            //Debug.WriteLine($"ElapsedTimer: {_elapsed}/{_time}");
        }
        else
        {
            _elapsedTimer.Stop();
        }
    }

    #endregion

    #region == MenuTree ==

    private readonly MenuTreeBuilder _mainMenuItems = new("");
    public ObservableCollection<NodeTree> MainMenuItems
    {
        get { return _mainMenuItems.Children; }
        set
        {
            _mainMenuItems.Children = value;
            OnPropertyChanged(nameof(MainMenuItems));
        }
    }

    private NodeTree? _selectedNodeMenu = new NodeMenu("root");
    public NodeTree? SelectedNodeMenu
    {
        get { return _selectedNodeMenu; }
        set
        {
            if (_selectedNodeMenu == value)
                return;

            _selectedNodeMenu = value;
            OnPropertyChanged(nameof(SelectedNodeMenu));

            // Testing
            var onUIThread = App.MainWnd?.CurrentDispatcherQueue?.HasThreadAccess;
            if ((onUIThread is not null) && (onUIThread == false))
            {
                Debug.WriteLine("SelectedNodeMenu set HasNoThreadAccess");
            }

            if (value is null)
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    SelectedPlaylistName = string.Empty;
                    RenamedSelectPendingPlaylistName = string.Empty;
                    PlaylistSongs.Clear();
                });

                return;
            }

            if (value is NodeMenuQueue)
            {
                //
            }
            else if (value is NodeMenuSearch)
            {
                //
            }
            else if (value is NodeMenuArtist)
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    if ((Artists.Count > 0) && (SelectedAlbumArtist is null))
                    {
                        SelectedAlbumArtist = Artists[0];
                    }
                });
            }
            else if (value is NodeMenuAlbum)
            {
                //
            }
            else if (value is NodeMenuFiles nml)
            {
                if (!nml.IsAcquired || (MusicDirectories.Count <= 1) && (MusicEntries.Count == 0))
                {
                    GetFiles(nml);
                }
            }
            else if (value is NodeMenuPlaylists)
            {
                // Do nothing
            }
            else if (value is NodeMenuPlaylistItem nmpli)
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    PlaylistSongs = nmpli.PlaylistSongs;
                    SelectedPlaylistName = nmpli.Name;

                    if ((nmpli.PlaylistSongs.Count == 0) || nmpli.IsUpdateRequied)
                    {
                        GetPlaylistSongs(nmpli);
                    }
                });

            }
            else if (value is NodeMenu)
            {
                if (value.Name != "root")
                    throw new NotImplementedException();
            }

        }
    }

    #endregion

    #region == Playlists ==  

    private ObservableCollection<Playlist> _playlists = [];
    public ObservableCollection<Playlist> Playlists
    {
        get
        {
            return _playlists;
        }
        set
        {
            if (_playlists == value)
                return;

            _playlists = value;
            OnPropertyChanged(nameof(Playlists));
        }
    }

    #endregion

    #region == Queue ==  

    private ObservableCollection<SongInfoEx> _queue = [];
    public ObservableCollection<SongInfoEx> Queue
    {
        get
        {
            if (_mpc is not null)
            {
                return _queue;
                //return _mpc.CurrentQueue;
            }
            else
            {
                return _queue;
            }
        }
        set
        {
            if (_queue == value)
                return;

            _queue = value;
            OnPropertyChanged(nameof(Queue));
            OnPropertyChanged(nameof(QueuePageSubTitleSongCount));
        }
    }

    private SongInfoEx? _selectedQueueSong;
    public SongInfoEx? SelectedQueueSong
    {
        get
        {
            return _selectedQueueSong;
        }
        set
        {
            if (_selectedQueueSong == value)
                return;

            _selectedQueueSong = value;
            OnPropertyChanged(nameof(SelectedQueueSong));
        }
    }

    private bool _isQueueFindVisible;
    public bool IsQueueFindVisible
    {
        get
        {
            return _isQueueFindVisible;
        }
        set
        {
            if (_isQueueFindVisible == value)
                return;

            _isQueueFindVisible = value;

            QueueForFilter.Clear();

            FilterQueueQuery = "";

            OnPropertyChanged(nameof(IsQueueFindVisible));
        }
    }

    private bool FilterSongInfoEx(SongInfoEx song)
    {
        return song.Title.Contains(FilterQueueQuery, StringComparison.CurrentCultureIgnoreCase);// InvariantCultureIgnoreCase
    }

    private ObservableCollection<SongInfoEx> _queueForFilter = [];
    public ObservableCollection<SongInfoEx> QueueForFilter
    {
        get
        {
            return _queueForFilter;
        }
        set
        {
            if (_queueForFilter == value)
                return;

            _queueForFilter = value;
            OnPropertyChanged(nameof(QueueForFilter));
        }
    }

    private SearchTags _selectedQueueFilterTags = SearchTags.Title;
    public SearchTags SelectedQueueFilterTags
    {
        get
        {
            return _selectedQueueFilterTags;
        }
        set
        {
            if (_selectedQueueFilterTags == value)
                return;

            _selectedQueueFilterTags = value;
            OnPropertyChanged(nameof(SelectedQueueFilterTags));

            if (_filterQueueQuery == "")
                return;
        }
    }

    private string _filterQueueQuery = "";
    public string FilterQueueQuery
    {
        get
        {
            return _filterQueueQuery;
        }
        set
        {
            if (_filterQueueQuery == value)
                return;

            _filterQueueQuery = value;
            OnPropertyChanged(nameof(FilterQueueQuery));

            if (_filterQueueQuery == "")
            {
                return;
            }

            var filtered = Queue.Where(song => FilterSongInfoEx(song));
            QueueForFilter = new ObservableCollection<SongInfoEx>(filtered);
        }
    }

    private SongInfoEx? _selectedQueueFilterSong;
    public SongInfoEx? SelectedQueueFilterSong
    {
        get
        {
            return _selectedQueueFilterSong;
        }
        set
        {
            if (_selectedQueueFilterSong == value)
                return;

            _selectedQueueFilterSong = value;
            OnPropertyChanged(nameof(SelectedQueueFilterSong));
        }
    }

    private string _queuePageSubTitleSongCount = "";
    public string QueuePageSubTitleSongCount
    {
        get
        {
            string str = _resourceLoader.GetString("QueuePage_SubTitle_SongCount");

            _queuePageSubTitleSongCount = string.Format(str, Queue.Count);
            return _queuePageSubTitleSongCount;
        }
    }

    #endregion

    #region == Search ==

    private ObservableCollection<SongInfo>? _searchResult = [];
    public ObservableCollection<SongInfo>? SearchResult
    {
        get
        {
            return _searchResult;
        }
        set
        {
            if (_searchResult == value)
                return;

            _searchResult = value;
            OnPropertyChanged(nameof(SearchResult));
            OnPropertyChanged(nameof(SearchPageSubTitleResultCount));
            OnPropertyChanged(nameof(IsSearchControlEnabled));
        }
    }

    private bool _isSearchControlEnabled;
    public bool IsSearchControlEnabled
    {
        get
        {
            return _isSearchControlEnabled;
        }
        set
        {
            if (_isSearchControlEnabled == value)
                return;

            _isSearchControlEnabled = value;
            OnPropertyChanged(nameof(IsSearchControlEnabled));
        }
    }

    // Search Tags, re-init in construtor in order to translate with resource string.
    private ObservableCollection<Models.SearchOption> _searchTagList =
    [
        new Models.SearchOption(SearchTags.Title, "Title"),
        new Models.SearchOption(SearchTags.Artist, "Artist"),
        new Models.SearchOption(SearchTags.Album, "Album"),
        new Models.SearchOption(SearchTags.Genre, "Genre"),
        new Models.SearchOption(SearchTags.Any, "Any")
    ];

    public ObservableCollection<Models.SearchOption> SearchTagList
    {
        get
        {
            return _searchTagList;
        }
    }

    private Models.SearchOption _selectedSearchTag = new(SearchTags.Title, "Title");
    public Models.SearchOption SelectedSearchTag
    {
        get
        {
            return _selectedSearchTag;
        }
        set
        {
            if (_selectedSearchTag == value)
                return;

            _selectedSearchTag = value;
            OnPropertyChanged(nameof(SelectedSearchTag));
        }
    }

    // Search Shiki (contain/==), re-init in construtor in order to translate with resource string.
    private ObservableCollection<Models.SearchWith> _searchShikiList =
    [
        new Models.SearchWith(SearchShiki.Contains, "Contains"),
        new Models.SearchWith(SearchShiki.Equals, "Equals")
    ];

    public ObservableCollection<Models.SearchWith> SearchShikiList
    {
        get
        {
            return _searchShikiList;
        }
    }

    private Models.SearchWith _selectedSearchShiki = new(SearchShiki.Contains, "Contains");
    public Models.SearchWith SelectedSearchShiki
    {
        get
        {
            return _selectedSearchShiki;
        }
        set
        {
            if (_selectedSearchShiki == value)
                return;

            _selectedSearchShiki = value;
            OnPropertyChanged(nameof(SelectedSearchShiki));
        }
    }

    // 
    private string _searchQuery = "";
    public string SearchQuery
    {
        get
        {
            return _searchQuery;
        }
        set
        {
            if (_searchQuery == value)
                return;

            _searchQuery = value;
            OnPropertyChanged(nameof(SearchQuery));
            //SearchExecCommand.NotifyCanExecuteChanged();
        }
    }

    private string _searchPageSubTitleResultCount = "";
    public string SearchPageSubTitleResultCount
    {
        get
        {
            string str = _resourceLoader.GetString("SearchPage_SubTitle_ResultCount");
            _searchPageSubTitleResultCount = string.Format(str, SearchResult?.Count);
            return _searchPageSubTitleResultCount;
        }
    }

    #endregion

    #region == Artists ==

    private ObservableCollection<AlbumArtist> _artists = [];
    public ObservableCollection<AlbumArtist> Artists
    {
        get { return _artists; }
        set
        {
            if (_artists == value)
                return;

            _artists = value;
            OnPropertyChanged(nameof(Artists));
            OnPropertyChanged(nameof(ArtistPageSubTitleArtistCount));
            OnPropertyChanged(nameof(ArtistPageSubTitleArtistAlbumCount));
        }
    }

    private string _artistPageSubTitleArtistCount = "";
    public string ArtistPageSubTitleArtistCount
    {
        get
        {
            string str = _resourceLoader.GetString("ArtistPage_SubTitle_ArtistCount");
            _artistPageSubTitleArtistCount = string.Format(str, Artists.Count);
            return _artistPageSubTitleArtistCount;
        }
    }

    private string _artistPageSubTitleArtistAlbumCount = "";
    public string ArtistPageSubTitleArtistAlbumCount
    {
        get
        {
            if (_selectedAlbumArtist is null)
            {
                return string.Empty;
            }

            string str = _resourceLoader.GetString("ArtistPage_SubTitle_ArtistAlbumCount");
            _artistPageSubTitleArtistAlbumCount = string.Format(str, _selectedAlbumArtist.Albums.Count);
            return _artistPageSubTitleArtistAlbumCount;
        }
    }

    private AlbumArtist? _selectedAlbumArtist;
    public AlbumArtist? SelectedAlbumArtist
    {
        get { return _selectedAlbumArtist; }
        set
        {
            if (_selectedAlbumArtist == value)
            {
                return;
            }

            _selectedAlbumArtist = value;

            OnPropertyChanged(nameof(SelectedAlbumArtist));

            SelectedArtistAlbums = _selectedAlbumArtist?.Albums;
            OnPropertyChanged(nameof(ArtistPageSubTitleArtistAlbumCount));

            //_ = Task.Run(() => GetArtistSongs(_selectedAlbumArtist));
            ////_ = Task.Run(() => GetAlbumPictures(SelectedArtistAlbums));
            //GetAlbumPictures(SelectedArtistAlbums);

            Task.Run(() =>
            {
                GetArtistSongs(_selectedAlbumArtist);
                GetAlbumPictures(SelectedArtistAlbums);
            });
        }
    }

    private ObservableCollection<AlbumEx>? _selectedArtistAlbums = [];
    public ObservableCollection<AlbumEx>? SelectedArtistAlbums
    {
        get
        {
            return _selectedArtistAlbums;
        }
        set
        {
            if (_selectedArtistAlbums == value)
                return;

            _selectedArtistAlbums = value;

            if (_selectedArtistAlbums is not null)
            {
                // Workaround for WinUI3's limitation or lack of features. 
                foreach (var album in _selectedArtistAlbums)
                {
                    album.ParentViewModel = this;
                }
            }

            OnPropertyChanged(nameof(SelectedArtistAlbums));
        }
    }

    #endregion

    #region == Albums ==

    private ObservableCollection<AlbumEx> _albums = [];
    public ObservableCollection<AlbumEx> Albums
    {
        get { return _albums; }
        set
        {
            if (_albums == value)
                return;

            _albums = value;
            OnPropertyChanged(nameof(Albums));
            OnPropertyChanged(nameof(AlbumPageSubTitleAlbumCount));
        }
    }

    private string _albumPageSubTitleAlbumCount = "";
    public string AlbumPageSubTitleAlbumCount
    {
        get
        {
            string str = _resourceLoader.GetString("AlbumPage_SubTitle_AlbumCount");
            _albumPageSubTitleAlbumCount = string.Format(str, Albums.Count);
            return _albumPageSubTitleAlbumCount;
        }
    }

    private AlbumEx? _selectedAlbum = new();
    public AlbumEx? SelectedAlbum
    {
        get { return _selectedAlbum; }
        set
        {
            if (_selectedAlbum == value)
            {
                return;
            }

            _selectedAlbum = value;
            OnPropertyChanged(nameof(SelectedAlbum));
            //OnPropertyChanged(nameof(SelectedAlbumSongs));
            //OnPropertyChanged(nameof(AlbumPageSubTitleSelectedAlbumSongsCount));

            if (_selectedAlbum is null)
            {
                //SelectedAlbumSongs = [];
                return;
            }

            /*
             *  do this at GetAlbumSongs(_selectedAlbum);
            SelectedAlbumSongs = _selectedAlbum.Songs;
            */
            //_ = Task.Run(async () => { await GetAlbumSongs(_selectedAlbum); });

            Task.Run(() => 
            {
                GetAlbumSongs(_selectedAlbum);
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    AlbumSelectedNavigateToDetailsPage?.Invoke(this, EventArgs.Empty);
                });
            });
        }
    }

    private readonly ObservableCollection<SongInfo>? _selectedAlbumSongs = [];
    public ObservableCollection<SongInfo>? SelectedAlbumSongs
    {
        get
        {
            if (_selectedAlbum is not null)
            {
                return _selectedAlbum.Songs;
            }

            return _selectedAlbumSongs;
        }
        /*
        set
        {
            if (_selectedAlbumSongs == value)
                return;

            _selectedAlbumSongs = value;
            OnPropertyChanged(nameof(SelectedAlbumSongs));
            OnPropertyChanged(nameof(AlbumPageSubTitleSelectedAlbumSongsCount));
        }
        */
    }

    public string AlbumPageSubTitleSelectedAlbumSongsCount
    {
        get
        {
            if (_selectedAlbumSongs is null)
            {
                return string.Empty;
            }

            string str = _resourceLoader.GetString("AlbumPage_SubTitle_SelectedAlbumSongsCount");
            if (_selectedAlbum is not null)
            {
                return string.Format(str, _selectedAlbum.Songs.Count); ;
            }
            else
            {
                return string.Format(str, _selectedAlbumSongs.Count); ;
            }
        }
    }

    private ObservableCollection<AlbumEx>? _visibleItemsAlbumsEx = [];
    public ObservableCollection<AlbumEx>? VisibleItemsAlbumsEx
    {
        get => _visibleItemsAlbumsEx;
        set
        {
            if (_visibleItemsAlbumsEx == value)
                return;

            _visibleItemsAlbumsEx = value;
            //OnPropertyChanged(nameof(VisibleItemsAlbumsEx));

            if (_visibleItemsAlbumsEx is null)
            {
                return;
            }

            Task.Run(() => GetAlbumPictures(VisibleItemsAlbumsEx));
            //GetAlbumPictures(VisibleItemsAlbumsEx); 
        }
    }

    #endregion

    #region == Files ==

    private readonly DirectoryTreeBuilder _musicDirectories = new("");
    public ObservableCollection<NodeTree> MusicDirectories
    {
        get { return _musicDirectories.Children; }
        set
        {
            _musicDirectories.Children = value;
            OnPropertyChanged(nameof(MusicDirectories));
        }
    }

    private NodeDirectory _selectedNodeDirectory = new(".", new Uri(@"file:///./"));
    public NodeDirectory SelectedNodeDirectory
    {
        get { return _selectedNodeDirectory; }
        set
        {
            if (_selectedNodeDirectory == value)
                return;

            _selectedNodeDirectory = value;
            OnPropertyChanged(nameof(SelectedNodeDirectory));

            if (_selectedNodeDirectory is null)
                return;

            if (MusicEntries is null)
                return;
            if (MusicEntries.Count == 0)
                return;

            if (_selectedNodeDirectory.DireUri.LocalPath == "/")
            {
                if (FilterMusicEntriesQuery != "")
                {
                    var filtered = _musicEntries.Where(song => song.Name.Contains(FilterMusicEntriesQuery, StringComparison.InvariantCultureIgnoreCase));
                    _musicEntriesFiltered = new ObservableCollection<NodeFile>(filtered);
                }
                else
                {
                    _musicEntriesFiltered = new ObservableCollection<NodeFile>(_musicEntries);
                }
            }
            else
            {
                FilterFiles();
            }

            OnPropertyChanged(nameof(MusicEntriesFiltered));
        }
    }

    private ObservableCollection<NodeFile> _musicEntries = [];
    public ObservableCollection<NodeFile> MusicEntries
    {
        get
        {
            return _musicEntries;
        }
        set
        {
            if (value == _musicEntries)
                return;

            _musicEntries = value;
            OnPropertyChanged(nameof(MusicEntries));
            OnPropertyChanged(nameof(FilesPageSubTitleFileCount));
        }
    }

    private ObservableCollection<NodeFile> _musicEntriesFiltered = [];
    public ObservableCollection<NodeFile> MusicEntriesFiltered
    {
        get
        {
            return _musicEntriesFiltered;
        }
        set
        {
            if (_musicEntriesFiltered == value)
                return;

            _musicEntriesFiltered = value;
            OnPropertyChanged(nameof(MusicEntriesFiltered));
        }
    }

    private void FilterFiles()
    {
        _musicEntriesFiltered.Clear();

        foreach (var entry in _musicEntries)
        {
            string path = entry.FileUri.LocalPath; //person.FileUri.AbsoluteUri;
            if (string.IsNullOrEmpty(path))
                continue;
            string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrEmpty(filename))
                continue;

            path = path.Replace(("/" + filename), "");

            if (path.StartsWith(_selectedNodeDirectory.DireUri.LocalPath))
            {
                if (FilterMusicEntriesQuery != "")
                {
                    if (entry.Name.Contains(FilterMusicEntriesQuery, StringComparison.InvariantCultureIgnoreCase))
                    {
                        _musicEntriesFiltered.Add(entry);
                    }
                }
                else
                {
                    _musicEntriesFiltered.Add(entry);
                }
            }
        }
    }

    private string _filterMusicEntriesQuery = "";
    public string FilterMusicEntriesQuery
    {
        get
        {
            return _filterMusicEntriesQuery;
        }
        set
        {
            if (_filterMusicEntriesQuery == value)
                return;

            _filterMusicEntriesQuery = value;
            OnPropertyChanged(nameof(FilterMusicEntriesQuery));

            if (_selectedNodeDirectory is null)
                return;

            if ((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath == "/")
            {
                if (FilterMusicEntriesQuery != "")
                {
                    var filtered = _musicEntries.Where(song => song.Name.Contains(FilterMusicEntriesQuery, StringComparison.InvariantCultureIgnoreCase));
                    MusicEntriesFiltered = new ObservableCollection<NodeFile>(filtered);
                }
                else
                {
                    MusicEntriesFiltered = new ObservableCollection<NodeFile>(_musicEntries);
                }
            }
            else
            {
                FilterFiles();
                OnPropertyChanged(nameof(MusicEntriesFiltered));
            }
        }
    }

    private string _filesPageSubTitleFileCount = "";
    public string FilesPageSubTitleFileCount
    {
        get
        {
            string str = _resourceLoader.GetString("FilesPage_SubTitle_FileCount");
            _filesPageSubTitleFileCount = string.Format(str, MusicEntries.Count);
            return _filesPageSubTitleFileCount;
        }
    }

    #endregion

    #region == PlaylistItems ==

    private ObservableCollection<SongInfo> _playlistSongs = [];
    public ObservableCollection<SongInfo> PlaylistSongs
    {
        get
        {
            return _playlistSongs;
        }
        set
        {
            if (_playlistSongs != value)
            {
                _playlistSongs = value;
                OnPropertyChanged(nameof(PlaylistSongs));
                OnPropertyChanged(nameof(PlaylistPageSubTitleSongCount));
            }
        }
    }

    private string _playlistPageSubTitleSongCount = "";
    public string PlaylistPageSubTitleSongCount
    {
        get
        {
            string str = _resourceLoader.GetString("PlaylistPage_SubTitle_SongCount");
            _playlistPageSubTitleSongCount = string.Format(str, PlaylistSongs.Count);
            return _playlistPageSubTitleSongCount;
        }
    }


    private string _selectedPlaylistName = "";
    public string SelectedPlaylistName
    {
        get
        {
            return _selectedPlaylistName;
        }
        set
        {
            if (_selectedPlaylistName == value)
                return;

            _selectedPlaylistName = value;
            OnPropertyChanged(nameof(SelectedPlaylistName));
        }
    }

    private string _renamedSelectPendingPlaylistName = "";
    public string RenamedSelectPendingPlaylistName
    {
        get
        {
            return _renamedSelectPendingPlaylistName;
        }
        set
        {
            if (_renamedSelectPendingPlaylistName == value)
                return;

            _renamedSelectPendingPlaylistName = value;
            OnPropertyChanged(nameof(RenamedSelectPendingPlaylistName));
        }
    }

    #endregion

    #region == Status Messages == 

    private string _statusBarMessage = "";
    public string StatusBarMessage
    {
        get
        {
            return _statusBarMessage;
        }
        set
        {
            _statusBarMessage = value;
            OnPropertyChanged(nameof(StatusBarMessage));
        }
    }

    private string _connectionStatusMessage = "";
    public string ConnectionStatusMessage
    {
        get
        {
            return _connectionStatusMessage;
        }
        set
        {
            _connectionStatusMessage = value;
            OnPropertyChanged(nameof(ConnectionStatusMessage));
        }
    }

    private string _infoBarInfoTitle = "";
    public string InfoBarInfoTitle
    {
        get
        {
            return _infoBarInfoTitle;
        }
        set
        {
            _infoBarInfoTitle = value;
            OnPropertyChanged(nameof(InfoBarInfoTitle));
        }
    }

    private string _infoBarInfoMessage = "";
    public string InfoBarInfoMessage
    {
        get
        {
            return _infoBarInfoMessage;
        }
        set
        {
            _infoBarInfoMessage = value;
            OnPropertyChanged(nameof(InfoBarInfoMessage));
        }
    }

    private bool _isShowInfoWindow;
    public bool IsShowInfoWindow

    {
        get { return _isShowInfoWindow; }
        set
        {
            if (_isShowInfoWindow == value)
                return;

            _isShowInfoWindow = value;

            if (!_isShowInfoWindow)
            {
                InfoBarInfoTitle = string.Empty;
                InfoBarInfoMessage = string.Empty;
            }

            OnPropertyChanged(nameof(IsShowInfoWindow));
        }
    }

    private string _infoBarAckTitle = "";
    public string InfoBarAckTitle
    {
        get
        {
            return _infoBarAckTitle;
        }
        set
        {
            _infoBarAckTitle = value;
            OnPropertyChanged(nameof(InfoBarAckTitle));
        }
    }

    private string _infoBarAckMessage = "";
    public string InfoBarAckMessage
    {
        get
        {
            return _infoBarAckMessage;
        }
        set
        {
            _infoBarAckMessage = value;
            OnPropertyChanged(nameof(InfoBarAckMessage));
        }
    }

    private bool _isShowAckWindow;
    public bool IsShowAckWindow

    {
        get { return _isShowAckWindow; }
        set
        {
            if (_isShowAckWindow == value)
                return;

            _isShowAckWindow = value;

            if (!_isShowAckWindow)
            {
                InfoBarAckTitle = string.Empty;
                InfoBarAckMessage = string.Empty;
            }

            OnPropertyChanged(nameof(IsShowAckWindow));
        }
    }

    private string _infoBarErrTitle = "";
    public string InfoBarErrTitle
    {
        get
        {
            return _infoBarErrTitle;
        }
        set
        {
            _infoBarErrTitle = value;
            OnPropertyChanged(nameof(InfoBarErrTitle));
        }
    }

    private string _infoBarErrMessage = "";
    public string InfoBarErrMessage
    {
        get
        {
            return _infoBarErrMessage;
        }
        set
        {
            _infoBarErrMessage = value;
            OnPropertyChanged(nameof(InfoBarErrMessage));
        }
    }

    private bool _isShowErrWindow;
    public bool IsShowErrWindow

    {
        get { return _isShowErrWindow; }
        set
        {
            if (_isShowErrWindow == value)
                return;

            _isShowErrWindow = value;

            if (!_isShowErrWindow)
            {
                InfoBarErrTitle = string.Empty;
                InfoBarErrMessage = string.Empty;
            }

            OnPropertyChanged(nameof(IsShowErrWindow));
        }
    }

    private string _mpdVersion = "";
    public string MpdVersion
    {
        get
        {
            if (_mpdVersion != "")
                return "MPD Protocol v" + _mpdVersion;
            else
                return _mpdVersion;

        }
        set
        {
            if (value == _mpdVersion)
                return;

            _mpdVersion = value;
            OnPropertyChanged(nameof(MpdVersion));
        }
    }

    public string ShortStatusWithMpdVersion
    {
        // TODO: localize.
        get
        {
            if (IsConnected)
            {
                if (!string.IsNullOrEmpty(_mpdVersion))
                {
                    if (CurrentProfile is not null)
                    {
                        return $"Connected to {CurrentProfile.Name} with MPD Protocol v{_mpdVersion}";
                    }
                    else
                    {
                        return $"Connected with MPD Protocol v{_mpdVersion}";
                    }
                }
                else
                {
                    if (CurrentProfile is not null)
                    {
                        return $"Connected to {CurrentProfile.Name}";
                    }
                    else
                    {
                        return "Connected";
                    }
                }
            }
            else if (IsConnecting)
            {
                if (CurrentProfile is not null)
                {
                    return $"Connecting to {CurrentProfile.Name}...";
                }
                else
                {
                    return "Connecting...";
                }
            }
            else
            {
                if (IsNotConnectingNorConnected)
                {
                    return "Not connected";
                }


                return "Not connected";
            }


        }
    }

#pragma warning disable IDE0079
#pragma warning disable CA1822
    public string VersionText
#pragma warning restore CA1822
#pragma warning restore IDE0079
    {
        get
        {
            Version version;

            if (RuntimeHelper.IsMSIX)
            {
                var packageVersion = Package.Current.Id.Version;

                version = new(packageVersion.Major, packageVersion.Minor, packageVersion.Build, packageVersion.Revision);
            }
            else
            {
                version = Assembly.GetExecutingAssembly().GetName().Version!;
            }

            return $"{"Version".GetLocalized()} - {version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
        }
    }

    #endregion

    #region == Status flags ==

    private bool _isConnected;
    public bool IsConnected
    {
        get => _isConnected;
        set
        {
            if (_isConnected == value)
                return;

            _isConnected = value;
            OnPropertyChanged(nameof(IsConnected));
            OnPropertyChanged(nameof(ShortStatusWithMpdVersion));
            OnPropertyChanged(nameof(IsNotConnecting));

            IsConnecting = !_isConnected;

            if (!_isConnected)
            {
                IsNotConnectingNorConnected = true;
            }
            if (_isConnected)
            {
                IsConnectButtonEnabled = true;
            }
        }
    }

    private bool _isConnecting;
    public bool IsConnecting
    {
        get
        {
            return _isConnecting;
        }
        set
        {
            if (_isConnecting == value)
                return;

            _isConnecting = value;
            OnPropertyChanged(nameof(IsConnecting));
            OnPropertyChanged(nameof(IsNotConnecting));
            OnPropertyChanged(nameof(ShortStatusWithMpdVersion));

            if (_isConnecting)
            {
                IsConnectButtonEnabled = false;
            }
        }
    }

    private bool _isNotConnectingNorConnected = true;
    public bool IsNotConnectingNorConnected
    {
        get
        {
            return _isNotConnectingNorConnected;
        }
        set
        {
            if (_isNotConnectingNorConnected == value)
                return;

            _isNotConnectingNorConnected = value;
            OnPropertyChanged(nameof(IsNotConnectingNorConnected));
            OnPropertyChanged(nameof(ShortStatusWithMpdVersion));

            if (_isNotConnectingNorConnected)
            {
                IsConnectButtonEnabled = true;
            }
        }
    }

    private bool _isConnectButtonEnabled = true;
    public bool IsConnectButtonEnabled
    {
        get { return _isConnectButtonEnabled; }
        set
        {
            if (_isConnectButtonEnabled == value)
                return;

            _isConnectButtonEnabled = value;

            OnPropertyChanged(nameof(IsConnectButtonEnabled));
        }
    }

    public bool IsNotConnecting
    {
        get
        {
            return !_isConnecting;
        }
    }

    #endregion

    #region == Options ==

    private bool _isUpdateOnStartup = true;
    public bool IsUpdateOnStartup
    {
        get { return _isUpdateOnStartup; }
        set
        {
            if (_isUpdateOnStartup == value)
                return;

            _isUpdateOnStartup = value;

            OnPropertyChanged(nameof(IsUpdateOnStartup));
        }
    }

    private bool _isDownloadAlbumArt = true;
    public bool IsDownloadAlbumArt
    {
        get { return _isDownloadAlbumArt; }
        set
        {
            if (_isDownloadAlbumArt == value)
                return;

            _isDownloadAlbumArt = value;

            OnPropertyChanged(nameof(IsDownloadAlbumArt));
        }
    }

    private bool _isDownloadAlbumArtEmbeddedUsingReadPicture = true;
    public bool IsDownloadAlbumArtEmbeddedUsingReadPicture
    {
        get { return _isDownloadAlbumArtEmbeddedUsingReadPicture; }
        set
        {
            if (_isDownloadAlbumArtEmbeddedUsingReadPicture == value)
                return;

            _isDownloadAlbumArtEmbeddedUsingReadPicture = value;

            OnPropertyChanged(nameof(IsDownloadAlbumArtEmbeddedUsingReadPicture));
        }
    }

    private bool _isAutoScrollToNowPlaying = true;
    public bool IsAutoScrollToNowPlaying
    {
        get { return _isAutoScrollToNowPlaying; }
        set
        {
            if (_isAutoScrollToNowPlaying == value)
                return;

            _isAutoScrollToNowPlaying = value;

            OnPropertyChanged(nameof(IsAutoScrollToNowPlaying));
        }
    }

    private bool _isSaveLog;
    public bool IsSaveLog
    {
        get { return _isSaveLog; }
        set
        {
            if (_isSaveLog == value)
                return;

            _isSaveLog = value;

            OnPropertyChanged(nameof(IsSaveLog));
        }
    }

    //
    private bool _isDebugWindowEnabled;
    public bool IsDebugWindowEnabled

    {
        get { return _isDebugWindowEnabled; }
        set
        {
            if (_isDebugWindowEnabled == value)
                return;

            _isDebugWindowEnabled = value;

            OnPropertyChanged(nameof(IsDebugWindowEnabled));
        }
    }

    private bool _isShowDebugWindow;
    public bool IsShowDebugWindow

    {
        get { return _isShowDebugWindow; }
        set
        {
            if (_isShowDebugWindow == value)
                return;

            _isShowDebugWindow = value;

            OnPropertyChanged(nameof(IsShowDebugWindow));

            if (_isShowDebugWindow)
            {
                /*
                //Application.Current.Dispatcher.Invoke(() =>
                Dispatcher.UIThread.Post(() =>
                {
                    //DebugWindowShowHide?.Invoke
                    DebugWindowShowHide2?.Invoke(this, true);
                });
                */
            }
            else
            {
                /*
                //Application.Current.Dispatcher.Invoke(() =>
                Dispatcher.UIThread.Post(() =>
                {
                    //DebugWindowShowHide?.Invoke();
                    DebugWindowShowHide2?.Invoke(this, false);
                });
                */
            }
        }

    }
    #endregion

    #region == Layout ==

    private bool _isNavigationViewMenuOpen = true;
    public bool IsNavigationViewMenuOpen
    {
        get { return _isNavigationViewMenuOpen; }
        set
        {
            if (_isNavigationViewMenuOpen == value)
                return;

            _isNavigationViewMenuOpen = value;
            OnPropertyChanged(nameof(IsNavigationViewMenuOpen));

            /* What's this for?
            foreach (var hoge in MainMenuItems)
            {
                switch (hoge)
                {
                    case NodeMenuLibrary lib:
                        lib.Expanded = _isNavigationViewMenuOpen;
                        break;
                    case NodeMenuPlaylists plt:
                        plt.Expanded = _isNavigationViewMenuOpen;
                        break;
                }
            }
            */
        }
    }

    private bool _isGoBackButtonVisible = false;
    public bool IsGoBackButtonVisible
    {
        get { return _isGoBackButtonVisible; }
        set
        {
            if (_isGoBackButtonVisible == value)
                return;

            _isGoBackButtonVisible = value;
            OnPropertyChanged(nameof(IsGoBackButtonVisible));

            // Update RegionsForCustomTitleBar.
            GoBackButtonVisibilityChanged?.Invoke(this, EventArgs.Empty);
        }
    }


    #endregion

    #region == Theme ==

    private ElementTheme _theme = ElementTheme.Default;
    public ElementTheme Theme
    {
        get => _theme;
        set => SetProperty(ref _theme, value);
    }

    private SystemBackdropOption _material = SystemBackdropOption.Mica;
    public SystemBackdropOption Material
    {
        get => _material;
        set => SetProperty(ref _material, value);
    }

    private bool _isAcrylicSupported = false;
    public bool IsAcrylicSupported
    {
        get => _isAcrylicSupported;
        set => SetProperty(ref _isAcrylicSupported, value);
    }

    private bool _isBackdropEnabled = false;
    public bool IsBackdropEnabled
    {
        get => _isBackdropEnabled;
        set => SetProperty(ref _isBackdropEnabled, value);
    }

    private bool _isMicaSupported = false;
    public bool IsMicaSupported
    {
        get => _isMicaSupported;
        set => SetProperty(ref _isMicaSupported, value);
    }

    #endregion

    #region == Events ==

    // Queue listview ScrollIntoView.
    public event EventHandler<object>? ScrollIntoView;

    // Queue listview ScrollIntoView and select (for filter and first time loading the queue).
    public event EventHandler<object>? ScrollIntoViewAndSelect;

    // For status bar message.
    public event EventHandler<string>? UpdateProgress;

    public event EventHandler? AlbumSelectedNavigateToDetailsPage;
    public event EventHandler<AlbumEx>? AlbumScrollIntoView;
    public event EventHandler? AlbumsCollectionHasBeenReset;
    public event EventHandler? GoBackButtonVisibilityChanged;

    public event EventHandler<string>? DebugCommandOutput;
    public event EventHandler<string>? DebugIdleOutput;
    public event EventHandler? DebugCommandClear;
    public event EventHandler? DebugIdleClear;

    #endregion

    private readonly ResourceLoader _resourceLoader = new();

    private readonly IMpcService _mpc;
    private readonly IDialogService _dialogs;

    public MainViewModel(IMpcService mpcService, IDialogService dialogService)
    {
        _mpc = mpcService;
        _dialogs = dialogService;

        InitializeAndSubscribe();


#if DEBUG
        IsDebugWindowEnabled = true;
#else
        IsDebugWindowEnabled = false;
        IsShowDebugWindow = false;
#endif
    }

    public async Task StartMPC()
    {
        if (CurrentProfile is null)
        {
            //ConnectionStatusMessage = MPDCtrlX.Properties.Resources.Init_NewConnectionSetting; // no need. 

            //StatusButton = _pathNewConnectionButton;

            var pro = await _dialogs.ShowInitDialog(this);
            if (pro is null)
            {
                return;
            }

            _password = pro.Password;
            _host = pro.Host;
            _port = pro.Port;

            CurrentProfile = pro;

            _ = Task.Run(() => Start(_host, _port));

            return;
        }

        _ = Task.Run(() => Start(_host, _port));
    }

    public void CleanUp()
    {
        try
        {
            if (IsConnected)
            {
                _mpc.MpdStop = true;

                _mpc.MpdDisconnect(false);
            }
        }
        catch { }
    }

    public void SetError(string error)
    {
        InfoBarErrMessage = error;

        IsShowErrWindow = true;
    }

    #region == Private Methods ==

    private void InitializeAndSubscribe()
    {
        #region == Subscribe to events ==

        _mpc.MpdIdleConnected += new MpcService.IsMpdIdleConnectedEvent(OnMpdIdleConnected);
        _mpc.MpdPlayerStatusChanged += new MpcService.MpdPlayerStatusChangedEvent(OnMpdPlayerStatusChanged);
        _mpc.MpdCurrentQueueChanged += new MpcService.MpdCurrentQueueChangedEvent(OnMpdCurrentQueueChanged);
        _mpc.MpdPlaylistsChanged += new MpcService.MpdPlaylistsChangedEvent(OnMpdPlaylistsChanged);

        _mpc.DebugCommandOutput += new MpcService.DebugCommandOutputEvent(OnDebugCommandOutput);
        _mpc.DebugIdleOutput += new MpcService.DebugIdleOutputEvent(OnDebugIdleOutput);

        _mpc.ConnectionStatusChanged += new MpcService.ConnectionStatusChangedEvent(OnConnectionStatusChanged);
        _mpc.ConnectionError += new MpcService.ConnectionErrorEvent(OnConnectionError);

        _mpc.MpdAckError += new MpcService.MpdAckErrorEvent(OnMpdAckError);
        _mpc.MpdFatalError += new MpcService.MpdFatalErrorEvent(OnMpdFatalError);

        _mpc.MpdAlbumArtChanged += new MpcService.MpdAlbumArtChangedEvent(OnAlbumArtChanged);

        _mpc.MpcProgress += new MpcService.MpcProgressEvent(OnMpcProgress);
        _mpc.IsBusy += new MpcService.IsBusyEvent(OnMpcIsBusy);

        this.UpdateProgress += (sender, arg) => { this.OnUpdateProgress(arg); };

        #endregion

        #region == Init Song's time elapsed timer. ==  

        _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);

        #endregion

        // needs to be here for _resourceLoader.
        _searchTagList =
            [
                new Models.SearchOption(SearchTags.Title, _resourceLoader.GetString("SearchTag_Title")),
                new Models.SearchOption(SearchTags.Artist, _resourceLoader.GetString("SearchTag_Artist")),
                new Models.SearchOption(SearchTags.Album, _resourceLoader.GetString("SearchTag_Album")),
                new Models.SearchOption(SearchTags.Genre, _resourceLoader.GetString("SearchTag_Genre")),
                new Models.SearchOption(SearchTags.Any, _resourceLoader.GetString("SearchTag_Any"))
            ];

        // needs to be here for _resourceLoader.
        _searchShikiList =
            [
                new Models.SearchWith(SearchShiki.Contains, _resourceLoader.GetString("Search_Shiki_Contains")),
                new Models.SearchWith(SearchShiki.Equals, _resourceLoader.GetString("Search_Shiki_Equals"))
            ];
    }

    private async Task Start(string host, int port)
    {
        HostIpAddress = null;
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host, AddressFamily.InterNetwork);
            if (addresses.Length > 0)
            {
                HostIpAddress = addresses[0];
            }
            else
            {
                // TODO::
                ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
                //StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";

                InfoBarAckTitle = "Error";
                InfoBarAckMessage = "Could not retrive IP Address from the hostname.";
                IsShowAckWindow = true;

                return;
            }
        }
        catch (Exception)
        {
            // TODO::
            ConnectionStatusMessage = "Error: Could not retrive IP Address from the hostname.";
            //StatusBarMessage = "Error: Could not retrive IP Address from the hostname.";

            InfoBarAckTitle = "Error";
            InfoBarAckMessage = "Could not retrive IP Address from the hostname.";
            IsShowAckWindow = true;

            return;
        }

        // Start MPD connection.
        _ = Task.Run(() => _mpc.MpdIdleConnect(HostIpAddress.ToString(), port));
    }

    private async Task LoadInitialData()
    {
        IsBusy = true;

        await Task.Delay(5);
        //await Task.Yield();

        CommandResult result = await _mpc.MpdIdleSendPassword(_password);

        if (result.IsSuccess)
        {
            bool r = await _mpc.MpdCommandConnectionStart(_mpc.MpdHost, _mpc.MpdPort, _mpc.MpdPassword);

            if (r)
            {
                // Testing:
                //await _mpc.MpdIdleQueryProtocol();

                if (IsUpdateOnStartup)
                {
                    await _mpc.MpdSendUpdate();
                }

                result = await _mpc.MpdIdleQueryStatus();

                if (result.IsSuccess)
                {
                    await Task.Delay(5);
                    UpdateStatus();

                    await Task.Delay(50);
                    await _mpc.MpdIdleQueryCurrentSong();
                    UpdateCurrentSong();

                    await Task.Delay(50);
                    await _mpc.MpdIdleQueryPlaylists();
                    UpdatePlaylists();

                    await Task.Delay(50);
                    await _mpc.MpdIdleQueryCurrentQueue();
                    UpdateCurrentQueue();

                    await Task.Delay(300);
                    await _mpc.MpdQueryListAlbumArtists();
                    UpdateAlbumsAndArtists();

                    await Task.Delay(20);

                    // Idle start.
                    _mpc.MpdIdleStart();
                }
            }
        }

        IsBusy = false;

        await Task.Delay(300);

        // MPD protocol ver check.
        if (_mpc.MpdVerText != "")
        {
            if (CompareVersionString(_mpc.MpdVerText, "0.20.0") == -1)
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    InfoBarErrTitle = MpdVersion;

                    InfoBarErrMessage = string.Format(_resourceLoader.GetString("StatusBarMsg_MPDVersionIsOld"), _mpc.MpdVerText);

                    IsShowErrWindow = true;
                });
            }
        }
    }

    private void UpdateStatus()
    {
        UpdateButtonStatus();

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            UpdateProgress?.Invoke(this, "[UI] Status updating...");

            bool isSongChanged = false;
            bool isCurrentSongWasNull = false;

            if (CurrentSong is not null)
            {
                if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                {
                    isSongChanged = true;

                    // Clear IsPlaying icon
                    CurrentSong.IsPlaying = false;

                    //
                    if (_mpc.MpdCurrentSong is not null)
                    {
                        _mpc.MpdCurrentSong.IsPlaying = false;
                    }
                    AlbumCover = null;
                    AlbumArtBitmapSource = _albumArtBitmapSourceDefault;
                }
            }
            else
            {
                // just in case
                if (_mpc.MpdCurrentSong is not null)
                {
                    _mpc.MpdCurrentSong.IsPlaying = false;
                }

                isCurrentSongWasNull = true;
            }

            if (Queue.Count > 0)
            {
                if (isSongChanged || isCurrentSongWasNull)
                {
                    // Sets Current Song
                    var item = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (item is not null)
                    {
                        //Debug.WriteLine("Currentsong is set. @UpdateStatus()");
                        CurrentSong = (item as SongInfoEx);
                        CurrentSong.IsPlaying = true;
                        CurrentSong.IsAlbumCoverNeedsUpdate = true;

                        //CurrentSong.IsSelected = true;

                        if (IsAutoScrollToNowPlaying)
                        {
                            ScrollIntoView?.Invoke(this, CurrentSong);
                        }

                        //IsAlbumArtVisible = false;
                        AlbumCover = null;
                        AlbumArtBitmapSource = _albumArtBitmapSourceDefault;

                        // AlbumArt
                        if (!string.IsNullOrEmpty(CurrentSong.File))
                        {
                            if (IsDownloadAlbumArt && CurrentSong.IsAlbumCoverNeedsUpdate)
                            {
                                //Debug.WriteLine("getting album cover. @UpdateStatus()");
                                var res = await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);

                                if (res != null)
                                {
                                    if (res.IsSuccess && (res.AlbumCover?.SongFilePath != null) && (CurrentSong.File != null))
                                    {
                                        if (res.AlbumCover?.SongFilePath == CurrentSong.File)
                                        {
                                            if ((res.AlbumCover.IsSuccess) && (!res.AlbumCover.IsDownloading))
                                            {
                                                AlbumCover = res.AlbumCover;
                                                AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                                //IsAlbumArtVisible = true;
                                                SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                                CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                            }
                                            else
                                            {
                                                Debug.WriteLine("if ((res.AlbumCover.IsSuccess) && (!res.AlbumCover.IsDownloading))");
                                            }
                                        }
                                        else
                                        {
                                            // Late comer.
                                            //Debug.WriteLine("if (res.AlbumCover?.SongFilePath == CurrentSong.File)");
                                        }
                                    }
                                    else
                                    {
                                        //Debug.WriteLine("if (res.IsSuccess && (res.AlbumCover?.SongFilePath != null) && (CurrentSong.File != null))");
                                    }
                                }
                                else
                                {
                                    Debug.WriteLine("if (res != null)");
                                }
                            }
                            else
                            {
                                Debug.WriteLine("if (IsDownloadAlbumArt && CurrentSong.IsAlbumCoverNeedsUpdate)");
                            }
                        }
                        else
                        {
                            Debug.WriteLine(" if (!string.IsNullOrEmpty(CurrentSong.File))");
                        }
                    }
                    else
                    {
                        //Debug.WriteLine("item is null. @UpdateStatus()");
                        // TODO:
                        CurrentSong = null;
                        AlbumCover = null;

                        AlbumArtBitmapSource = _albumArtBitmapSourceDefault;
                    }
                }
            }
            else
            {
                //Debug.WriteLine("Queue.Count == 0. @UpdateStatus()");
                // TODO:
                AlbumCover = null;

                AlbumArtBitmapSource = _albumArtBitmapSourceDefault;
            }

            UpdateProgress?.Invoke(this, "");
        });
    }

    private void UpdateButtonStatus()
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            try
            {
                //Play button
                switch (_mpc.MpdStatus.MpdState)
                {
                    case Status.MpdPlayState.Play:
                        {
                            PlayButton = _pathPauseButton;
                            break;
                        }
                    case Status.MpdPlayState.Pause:
                        {
                            PlayButton = _pathPlayButton;
                            break;
                        }
                    case Status.MpdPlayState.Stop:
                        {
                            PlayButton = _pathPlayButton;
                            break;
                        }

                        //_pathStopButton
                }

                if (_mpc.MpdStatus.MpdVolumeIsReturned)
                {
                    //Debug.WriteLine($"Volume is set to {_mpc.MpdStatus.MpdVolume} @UpdateButtonStatus()");

                    double tmpVol = Convert.ToDouble(_mpc.MpdStatus.MpdVolume);
                    if (_volume != tmpVol)
                    {
                        // "quietly" update.
                        _volume = tmpVol;
                        OnPropertyChanged(nameof(Volume));

                        //// save
                        //if (_currentProfile is not null)
                        //{
                        //    _currentProfile.Volume = _volume;
                        //}
                    }
                }

                _random = _mpc.MpdStatus.MpdRandom;
                OnPropertyChanged(nameof(Random));

                _repeat = _mpc.MpdStatus.MpdRepeat;
                OnPropertyChanged(nameof(Repeat));

                _consume = _mpc.MpdStatus.MpdConsume;
                OnPropertyChanged(nameof(Consume));

                _single = _mpc.MpdStatus.MpdSingle;
                OnPropertyChanged(nameof(Single));

                //start elapsed timer.
                if (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play)
                {
                    // no need to care about "double" updates for time.
                    Time = Convert.ToInt32(_mpc.MpdStatus.MpdSongTime);
                    Time *= _elapsedTimeMultiplier;
                    _elapsed = Convert.ToInt32(_mpc.MpdStatus.MpdSongElapsed);
                    _elapsed *= _elapsedTimeMultiplier;
                    if (!_elapsedTimer.Enabled)
                        _elapsedTimer.Start();
                }
                else
                {
                    _elapsedTimer.Stop();

                    // no need to care about "double" updates for time.
                    Time = Convert.ToInt32(_mpc.MpdStatus.MpdSongTime);
                    Time *= _elapsedTimeMultiplier;
                    _elapsed = Convert.ToInt32(_mpc.MpdStatus.MpdSongElapsed);
                    _elapsed *= _elapsedTimeMultiplier;
                    OnPropertyChanged(nameof(Elapsed));
                    OnPropertyChanged(nameof(ElapsedFormatted));
                }

                //
                //Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
            }
            catch
            {
                Debug.WriteLine("Error@UpdateButtonStatus");
            }
        });
    }

    private void UpdateCurrentSong()
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue( () =>
        {
            bool isSongChanged = false;
            bool isCurrentSongWasNull = false;

            if (CurrentSong != null)
            {
                if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                {
                    isSongChanged = true;

                    // Clear IsPlaying icon
                    CurrentSong.IsPlaying = false;

                    //
                    if (_mpc.MpdCurrentSong is not null)
                    {
                        _mpc.MpdCurrentSong.IsPlaying = false;
                    }

                }

                if (CurrentSong.IsAlbumCoverNeedsUpdate)
                {
                    isSongChanged = true;
                }
            }
            else
            {
                isCurrentSongWasNull = true;

                CurrentSong = _mpc.MpdCurrentSong;
            }

            if (_mpc.MpdCurrentSong != null)
            {
                if (_mpc.MpdCurrentSong.Id == _mpc.MpdStatus.MpdSongID)
                {
                    if (isSongChanged || isCurrentSongWasNull)
                    {
                        /*
                         * looks like no need to.
                        // AlbumArt
                        if (!string.IsNullOrEmpty(_mpc.MpdCurrentSong.File))
                        {
                            if (IsDownloadAlbumArt)
                            {
                                var res = await _mpc.MpdQueryAlbumArt(_mpc.MpdCurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
                                if ((res.AlbumCover.IsSuccess) && (!res.AlbumCover.IsDownloading) && (res.AlbumCover?.SongFilePath != null))
                                {
                                    if (res.AlbumCover?.SongFilePath == _mpc.MpdCurrentSong.File)
                                    {
                                        AlbumCover = res.AlbumCover;
                                        //AlbumArtBitmapSource = AlbumCover.AlbumImageSource;
                                        AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                        if (CurrentSong is not null)
                                        {
                                            SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                            CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                        }
                                    }
                                }
                            }
                        }
                        */
                    }
                    else
                    {
                        Debug.WriteLine("if (isSongChanged || isCurrentSongWasNull). @UpdateCurrentSong()");
                    }
                }
                else
                {
                    Debug.WriteLine("{_mpc.MpdCurrentSong.Id} != {_mpc.MpdStatus.MpdSongID}. @UpdateCurrentSong()");
                }
            }
            else
            {
                Debug.WriteLine("_mpc.MpdCurrentSong is null. @UpdateCurrentSong()");
            }
        });
    }

    private void UpdateCurrentQueue()
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            IsQueueFindVisible = false;

            if (Queue.Count > 0)
            {
                UpdateProgress?.Invoke(this, "[UI] Updating the queue...");
                await Task.Delay(20);

                IsWorking = true;

                try
                {
                    // The simplest way, but all the selections and listview position will be cleared. Kind of annoying when editing items list.
                    #region == simple & fast == 
                    /*
                    UpdateProgress?.Invoke(this, "[UI] Loading the queue...");

                    App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
                    {
                        Queue = new ObservableCollection<SongInfoEx>(_mpc.CurrentQueue);
                        foreach (var sng in Queue)
                        {
                            //UpdateProgress?.Invoke(this, "[UI] Queue list updating...(fixing)");

                            // Workaround for WinUI3's limitation or lack of features.
                            sng.ParentViewModel = this;
                        }

                        UpdateProgress?.Invoke(this, "[UI] Checking current song after Queue update.");

                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem is not null)
                        {
                            if (CurrentSong is not null)
                            {
                                bool asdf = false;
                                if (CurrentSong.Id == curitem.Id)
                                {
                                    asdf = curitem.IsAlbumCoverNeedsUpdate;
                                }
                                CurrentSong = curitem;
                                CurrentSong.IsPlaying = true;
                                CurrentSong.IsAlbumCoverNeedsUpdate = asdf;
                            }
                            else
                            {
                                CurrentSong = curitem;
                                CurrentSong.IsPlaying = true;
                                CurrentSong.IsAlbumCoverNeedsUpdate = true;
                            }

                            if (IsAutoScrollToNowPlaying)
                            {
                                // ScrollIntoView while don't change the selection 
                                ScrollIntoView?.Invoke(this, CurrentSong);
                            }

                            // AlbumArt
                            if (IsDownloadAlbumArt && CurrentSong.IsAlbumCoverNeedsUpdate)
                            {
                                var res = await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
                                if ((res.AlbumCover.IsSuccess) && (!res.AlbumCover.IsDownloading) && (res.AlbumCover?.SongFilePath != null))
                                {
                                    if (res.AlbumCover?.SongFilePath == CurrentSong.File)
                                    {
                                        AlbumCover = res.AlbumCover;
                                        //AlbumArtBitmapSource = AlbumCover.AlbumImageSource;
                                        AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                        SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                        CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            CurrentSong = null;

                            //IsAlbumArtVisible = false;
                            AlbumArtBitmapSource = _albumArtBitmapSourceDefault;
                        }

                        IsWorking = false;

                        UpdateProgress?.Invoke(this, "");
                    });
                    */
                    #endregion

                    #region == better way (only for wpf and avaloniaui) ==

                    IsWorking = true;

                    UpdateProgress?.Invoke(this, "[UI] Updating the queue...");

                    if (_mpc.CurrentQueue.Count == 0)
                    {
                        Queue.Clear();

                        CurrentSong = null;

                        //IsAlbumArtVisible = false;
                        AlbumCover = null;
                        AlbumArtBitmapSource = _albumArtBitmapSourceDefault;

                        UpdateProgress?.Invoke(this, "");

                        IsWorking = false;

                        return;
                    }


                    // tmp list of deletion
                    List<SongInfoEx> tmpQueue = [];

                    // deletes items that does not exists in the new queue. 
                    foreach (var sng in Queue)
                    {
                        UpdateProgress?.Invoke(this, "[UI] Queue list updating...(checking deleted items)");

                        IsWorking = true;

                        var queitem = _mpc.CurrentQueue.FirstOrDefault(i => i.Id == sng.Id);
                        if (queitem is null)
                        {
                            // add to tmp deletion list.
                            tmpQueue.Add(sng);
                        }
                    }

                    // loop the tmp deletion list and remove.
                    foreach (var hoge in tmpQueue)
                    {
                        UpdateProgress?.Invoke(this, "[UI] Queue list updating...(deletion)");

                        IsWorking = true;

                        Queue.Remove(hoge);
                    }

                    // update or add item from the new queue list.
                    foreach (var sng in _mpc.CurrentQueue)
                    {
                        UpdateProgress?.Invoke(this, $"[UI] Queue list updating...(checking and adding new items {sng.Id})");

                        IsWorking = true;

                        var fuga = Queue.FirstOrDefault(i => i.Id == sng.Id);
                        if (fuga is not null)
                        {
                            // this cuase strange selection problem.
                            //sng.IsSelected = fuga.IsSelected;

                            // In WinUI3, this won't work.
                            //fuga.IsSelected = false; // so clear it for now.
                            fuga.IsPlaying = false;

                            // Just update.
                            //fuga = sng; // < sort won't work. why...

                            fuga.Pos = sng.Pos;
                            //fuga.Id = sng.Id;
                            fuga.LastModified = sng.LastModified;
                            //fuga.Time = sng.Time; // format exception
                            fuga.Title = sng.Title;
                            fuga.Artist = sng.Artist;
                            fuga.Album = sng.Album;
                            fuga.AlbumArtist = sng.AlbumArtist;
                            fuga.Composer = sng.Composer;
                            fuga.Date = sng.Date;
                            fuga.Duration = sng.Duration;
                            fuga.File = sng.File;
                            fuga.Genre = sng.Genre;
                            fuga.Track = sng.Track;

                            fuga.Index = sng.Index;

                        }
                        else
                        {
                            Queue.Add(sng);
                        }

                        // Workaround for WinUI3's limitation or lack of features.
                        sng.ParentViewModel = this;
                    }

                    // Sorting.
                    // Sort here because Queue list may have been re-ordered.
                    UpdateProgress?.Invoke(this, "[UI] Queue list sorting...");

                    // WinUI3/AvaloniaUI doesn't have this. Only for WPF.
                    //var collectionView = CollectionViewSource.GetDefaultView(Queue);
                    // no need to add because It's been added when "loading".
                    //collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                    //collectionView.Refresh();

                    // This is not good, all the selections will be cleared and position will be reset, but ...
                    //Queue = new ObservableCollection<SongInfoEx>(Queue.OrderBy(n => n.Index));

                    Debug.WriteLine("Queue sort.");
                    Queue.Sort((a, b) => { return a.Index.CompareTo(b.Index); });

                    UpdateProgress?.Invoke(this, "[UI] Checking current song after Queue update.");

                    // Set Current and NowPlaying.
                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem is not null)
                    {
                        bool asdf = false;
                        if (CurrentSong is not null)
                        {
                            if (CurrentSong.Id == curitem.Id)
                            {
                                asdf = curitem.IsAlbumCoverNeedsUpdate;
                            }
                        }
                        else
                        {
                            asdf = true;
                        }

                        CurrentSong = curitem;
                        CurrentSong.IsPlaying = true;
                        CurrentSong.IsAlbumCoverNeedsUpdate = asdf;

                        //if (IsAutoScrollToNowPlaying)
                        //{
                        //    // ScrollIntoView while don't change the selection 
                        //    //ScrollIntoView?.Invoke(this, CurrentSong);
                        //}
                        // AlbumArt
                        if (IsDownloadAlbumArt && CurrentSong.IsAlbumCoverNeedsUpdate)
                        {
                            var res = await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
                            if ((res.AlbumCover.IsSuccess) && (!res.AlbumCover.IsDownloading) && (res.AlbumCover?.SongFilePath != null))
                            {
                                if (res.AlbumCover?.SongFilePath == CurrentSong.File)
                                {
                                    AlbumCover = res.AlbumCover;
                                    //AlbumArtBitmapSource = AlbumCover.AlbumImageSource;
                                    AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                    SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                    CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                }
                            }
                        }
                    }
                    else
                    {
                        CurrentSong = null;

                        //IsAlbumArtVisible = false;
                        AlbumCover = null;
                        AlbumArtBitmapSource = _albumArtBitmapSourceDefault;
                    }

                    UpdateProgress?.Invoke(this, "");

                    IsWorking = false;

                    #endregion
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception@UpdateCurrentQueue: " + e.Message);

                    UpdateProgress?.Invoke(this, "Exception@UpdateCurrentQueue: " + e.Message);

                    IsWorking = false;
                    App.AppendErrorLog("Exception@UpdateCurrentQueue", e.Message);

                    return;
                }
                finally
                {
                    IsWorking = false;
                    UpdateProgress?.Invoke(this, "");
                }

                IsWorking = false;
            }
            else
            {
                UpdateProgress?.Invoke(this, "[UI] Loading the queue...");
                await Task.Delay(20);
                /*
                if (IsSwitchingProfile)
                    return;
                */

                IsWorking = true;

                try
                {
                    IsWorking = true;

                    UpdateProgress?.Invoke(this, "[UI] Loading the queue...");

                    Queue = new ObservableCollection<SongInfoEx>(_mpc.CurrentQueue);

                    // Workaround for WinUI3's limitation or lack of features.
                    foreach (var hoge in Queue)
                    {
                        hoge.ParentViewModel = this;
                    }

                    UpdateProgress?.Invoke(this, "[UI] Queue checking current song...");

                    bool isNeedToFindCurrentSong = false;

                    if (CurrentSong is not null)
                    {
                        if (CurrentSong.Id != _mpc.MpdStatus.MpdSongID)
                        {
                            isNeedToFindCurrentSong = true;

                            CurrentSong.IsPlaying = false;
                        }
                        else
                        {
                            if (_mpc.MpdCurrentSong is not null)
                            {
                                // This means CurrentSong is already aquired by "currentsong" command.
                                if (_mpc.MpdCurrentSong.Id == _mpc.MpdStatus.MpdSongID)
                                {
                                    // Set Current(again) and NowPlaying.

                                    // the reason not to use CurrentSong is that it points different instance (set by "currentsong" command and currentqueue). 
                                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                                    if (curitem is not null)
                                    {
                                        CurrentSong = curitem;
                                        CurrentSong.IsPlaying = true;
                                        //CurrentSong.IsSelected = true;

                                        if (IsAutoScrollToNowPlaying)
                                        {
                                            // use ScrollIntoViewAndSelect instead of ScrollIntoView
                                            ScrollIntoViewAndSelect?.Invoke(this, CurrentSong);
                                        }

                                        // AlbumArt
                                        if (IsDownloadAlbumArt && CurrentSong.IsAlbumCoverNeedsUpdate)
                                        {
                                            var res = await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
                                            if ((res.AlbumCover.IsSuccess) && (!res.AlbumCover.IsDownloading) && (res.AlbumCover?.SongFilePath != null))
                                            {
                                                if (res.AlbumCover?.SongFilePath == CurrentSong.File)
                                                {
                                                    AlbumCover = res.AlbumCover;
                                                    //AlbumArtBitmapSource = AlbumCover.AlbumImageSource;
                                                    AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                                    SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                                    CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                                }
                                            }
                                        }
                                    }
                                    /*
                                    // the reason not to use CurrentSong is that it points different instance (set by "currentsong" command and currentqueue). 
                                    _mpc.MpdCurrentSong.IsPlaying = true;

                                    // just in case. < no. don't override.
                                    //CurrentSong.IsPlaying = true;

                                    // currentsong command does not return pos, so it's needed to be set.
                                    CurrentSong.Index = _mpc.MpdCurrentSong.Index;

                                    _mpc.MpdCurrentSong.IsSelected = true;

                                    if (IsAutoScrollToNowPlaying)
                                        // use ScrollIntoViewAndSelect instead of ScrollIntoView
                                        ScrollIntoViewAndSelect?.Invoke(this, CurrentSong.Index);
                                    */
                                }
                                else
                                {
                                    Debug.WriteLine("_mpc.MpdCurrentSong.Id != _mpc.MpdStatus.MpdSongID. @UpdateCurrentQueue()");
                                    isNeedToFindCurrentSong = true;
                                }
                            }
                            else
                            {
                                //Debug.WriteLine("_mpc.MpdCurrentSong is null. @UpdateCurrentQueue()");
                                isNeedToFindCurrentSong = true;
                            }
                        }
                    }
                    else
                    {
                        //Debug.WriteLine("CurrentSong is null. @UpdateCurrentQueue()");
                        isNeedToFindCurrentSong = true;
                    }

                    if (isNeedToFindCurrentSong)
                    {
                        // Set Current and NowPlaying.
                        var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                        if (curitem is not null)
                        {
                            //Debug.WriteLine($"Currentsong is set. {curitem.Title}. @UpdateCurrentQueue()");

                            CurrentSong = curitem;
                            CurrentSong.IsPlaying = true;
                            //CurrentSong.IsSelected = true;

                            if (IsAutoScrollToNowPlaying)
                            {
                                // use ScrollIntoViewAndSelect instead of ScrollIntoView
                                ScrollIntoViewAndSelect?.Invoke(this, CurrentSong);
                            }

                            // AlbumArt
                            if (IsDownloadAlbumArt && CurrentSong.IsAlbumCoverNeedsUpdate)
                            {
                                var res = await _mpc.MpdQueryAlbumArt(CurrentSong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
                                if ((res.AlbumCover.IsSuccess) && (!res.AlbumCover.IsDownloading) && (res.AlbumCover?.SongFilePath != null))
                                {
                                    if (res.AlbumCover?.SongFilePath == CurrentSong.File)
                                    {
                                        AlbumCover = res.AlbumCover;
                                        //AlbumArtBitmapSource = AlbumCover.AlbumImageSource;
                                        AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                        SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                        CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                    }
                                }
                            }
                        }
                        else
                        {
                            Debug.WriteLine("Looks like starting up with playback status Stop. @UpdateCurrentQueue()");
                            // just in case.
                            CurrentSong = null;
                            AlbumCover = null;

                            AlbumArtBitmapSource = _albumArtBitmapSourceDefault;
                        }
                    }

                    UpdateProgress?.Invoke(this, "");
                }
                catch (Exception e)
                {
                    Debug.WriteLine("Exception@UpdateCurrentQueue: " + e.Message);

                    StatusBarMessage = "Exception@UpdateCurrentQueue: " + e.Message;

                    IsWorking = false;

                    App.AppendErrorLog("Exception@UpdateCurrentQueue", e.Message);

                    return;
                }
                finally
                {
                    IsWorking = false;
                    UpdateProgress?.Invoke(this, "");
                }
            }

            IsWorking = false;

            OnPropertyChanged(nameof(QueuePageSubTitleSongCount));
        });
    }

    private void UpdatePlaylists()
    {
        //await Task.Delay(10);

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            UpdateProgress?.Invoke(this, "[UI] Playlists loading...");

            bool isListChanged = false;
            //IsBusy = true;
            IsWorking = true;

            if (Playlists.Count == 0)
            {
                // this is the initial load, so use this flag to sort it later.
                isListChanged = true;
            }

            UpdateProgress?.Invoke(this, "[UI] Playlists loading...");
            Playlists = new ObservableCollection<Playlist>(_mpc.Playlists);
            UpdateProgress?.Invoke(this, "");

            NodeMenuPlaylists playlistDir = _mainMenuItems.PlaylistsDirectory;

            if (playlistDir is not null)
            {
                foreach (var hoge in Playlists)
                {
                    var fuga = playlistDir.Children.FirstOrDefault(i => i.Name == hoge.Name);
                    if (fuga is null)
                    {
                        NodeMenuPlaylistItem playlistNode = new(hoge.Name)
                        {
                            IsUpdateRequied = true
                        };
                        playlistDir.Children.Add(playlistNode);
                        isListChanged = true;
                    }
                }

                List<NodeTree> tobedeleted = [];
                foreach (var hoge in playlistDir.Children)
                {
                    var fuga = Playlists.FirstOrDefault(i => i.Name == hoge.Name);
                    if (fuga is null)
                    {
                        tobedeleted.Add(hoge);
                        isListChanged = true;
                    }
                    else
                    {
                        if (hoge is NodeMenuPlaylistItem nmpi)
                        {
                            // in case
                            nmpi.IsUpdateRequied = true;
                        }
                    }
                }

                foreach (var hoge in tobedeleted)
                {
                    playlistDir.Children.Remove(hoge);
                    isListChanged = true;
                }

                // Sort > this was causing NavigationViewItem selection to reset..so >> only isChanged then sort it.
                if (isListChanged && playlistDir.Children.Count > 1)
                {
                    CultureInfo ci = CultureInfo.CurrentCulture;
                    StringComparer comp = StringComparer.Create(ci, true);
                    // 
                    playlistDir.Children = new ObservableCollection<NodeTree>(playlistDir.Children.OrderBy(x => x.Name, comp));  //<<This freaking resets selection of NavigationViewItem!
                }

                // Update playlist if selected
                if (SelectedNodeMenu is NodeMenuPlaylistItem nmpli)
                {
                    if (nmpli.IsUpdateRequied && nmpli.Selected)
                    {
                        GetPlaylistSongs(nmpli);

                        if (isListChanged)
                        {
                            // TODO: need to check if this is needed.
                            //GoToPlaylistPage(nmpli);
                        }
                    }
                }

                if (_isNavigationViewMenuOpen)
                {
                    playlistDir.Expanded = true;
                }
            }

            //IsBusy = false;
            IsWorking = false;

            // apply open/close after this menu is loaded.
            OnPropertyChanged(nameof(IsNavigationViewMenuOpen));

            if (!string.IsNullOrEmpty(RenamedSelectPendingPlaylistName))
            {
                GoToRenamedPlaylistPage(RenamedSelectPendingPlaylistName);
                RenamedSelectPendingPlaylistName = string.Empty;
            }
        });
    }

    private void GoToPlaylistPage(NodeMenuPlaylistItem playlist)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            foreach (var hoge in MainMenuItems)
            {
                if (hoge is not NodeMenuPlaylists) continue;
                foreach (var fuga in hoge.Children)
                {
                    if (fuga is not NodeMenuPlaylistItem) continue;
                    if (fuga != playlist) continue;
                    if (!IsNavigationViewMenuOpen) continue;
                    fuga.Selected = true;
                    SelectedNodeMenu = fuga;
                    SelectedPlaylistName = fuga.Name;

                    break;
                }
            }
        });
    }

    private void GoToRenamedPlaylistPage(string playlist)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            RenamedSelectPendingPlaylistName = string.Empty;
            _playlistPageSubTitleSongCount = "";
            OnPropertyChanged(nameof(PlaylistPageSubTitleSongCount));

            foreach (var hoge in MainMenuItems)
            {
                if (hoge is not NodeMenuPlaylists) continue;
                foreach (var fuga in hoge.Children)
                {
                    if (fuga is not NodeMenuPlaylistItem) continue;

                    if (string.Equals(playlist, fuga.Name, StringComparison.CurrentCulture))
                    {
                        await Task.Yield();
                        await Task.Delay(1000); // needed this for WinUI3.

                        Debug.WriteLine($"{playlist} is now selected....");
                        IsNavigationViewMenuOpen = true;
                        fuga.Selected = true;
                        SelectedNodeMenu = fuga;
                        SelectedPlaylistName = fuga.Name;
 
                        break;
                    }
                }
            }
        });
    }

    private Task UpdateLibraryMusicAsync()
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            UpdateProgress?.Invoke(this, "[UI] Library songs loading...");

            //IsBusy = true;
            IsWorking = true;
            await Task.Yield();

            var tmpMusicEntries = new ObservableCollection<NodeFile>();

            foreach (var songfile in _mpc.LocalFiles)
            {
                //IsBusy = true;
                //IsWorking = true;

                if (string.IsNullOrEmpty(songfile.File)) continue;

                try
                {
                    Uri uri = new(@"file:///" + songfile.File);
                    if (uri.IsFile)
                    {
                        string filename = System.IO.Path.GetFileName(songfile.File);//System.IO.Path.GetFileName(uri.LocalPath);
                        NodeFile hoge = new(filename, uri, songfile.File)
                        {
                            // Workaround for WinUI3's limitation or lack of features.
                            ParentViewModel = this
                        };

                        tmpMusicEntries.Add(hoge);
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(songfile + e.Message);

                    //IsBusy = false;
                    IsWorking = false;
                    await Task.Yield();
                    App.AppendErrorLog("Exception@UpdateLibraryMusic", e.Message);
                    //return Task.FromResult(false);
                    return;
                }
            }

            //IsBusy = true;
            IsWorking = true;

            UpdateProgress?.Invoke(this, "[UI] Library songs loading...");

            MusicEntries = new ObservableCollection<NodeFile>(tmpMusicEntries);// COPY

            _musicEntriesFiltered = _musicEntriesFiltered = new ObservableCollection<NodeFile>(tmpMusicEntries);
            OnPropertyChanged(nameof(MusicEntriesFiltered));

            UpdateProgress?.Invoke(this, "");
            //IsBusy = false;
            IsWorking = false;
            await Task.Yield();
        });

        return Task.CompletedTask;
    }

    private Task UpdateLibraryDirectoriesAsync()
    {
        // Directories
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            UpdateProgress?.Invoke(this, "[UI] Library directories loading...");
            IsWorking = true;
            await Task.Yield();

            try
            {
                var tmpMusicDirectories = new DirectoryTreeBuilder("");
                await tmpMusicDirectories.Load(_mpc.LocalDirectories);

                UpdateProgress?.Invoke(this, "[UI] Library directories loading...");

                IsWorking = true;
                await Task.Yield();

                MusicDirectories = new ObservableCollection<NodeTree>(tmpMusicDirectories.Children);// COPY
                if (MusicDirectories.Count > 0)
                {
                    if (MusicDirectories[0] is NodeDirectory nd)
                    {
                        _selectedNodeDirectory = nd;
                        OnPropertyChanged(nameof(SelectedNodeDirectory));
                    }
                }

                //IsBusy = false;
                IsWorking = false;
                await Task.Yield();
                UpdateProgress?.Invoke(this, "");
            }
            catch (Exception e)
            {
                Debug.WriteLine("_musicDirectories.Load: " + e.Message);

                //IsBusy = false;
                IsWorking = false;
                await Task.Yield();
                App.AppendErrorLog("Exception@UpdateLibraryDirectories", e.Message);
            }
            finally
            {
                //IsBusy = false;
                IsWorking = false;
                await Task.Yield();
                UpdateProgress?.Invoke(this, "");
            }
        });

        return Task.CompletedTask;
    }

    private void GetFiles(NodeMenuFiles filestNode)
    {
        // TODO: fix this

        if (filestNode is null)
            return;

        if (filestNode.IsAcquired)
        {
            return;
        }

        if (MusicEntries.Count > 0)
            MusicEntries.Clear();

        if (MusicDirectories.Count > 0)
            MusicDirectories.Clear();

        filestNode.IsAcquired = true;

        Task.Run(async () =>
        {
            //await Task.Delay(10);
            await Task.Yield();

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsWorking = true;
            });

            CommandResult result = await _mpc.MpdQueryListAll().ConfigureAwait(false);
            if (result.IsSuccess)
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    filestNode.IsAcquired = true;
                });

                //await UpdateLibraryMusicAsync().ConfigureAwait(false);
                //await UpdateLibraryDirectoriesAsync().ConfigureAwait(false);
                var dirTask = UpdateLibraryDirectoriesAsync();
                var fileTask = UpdateLibraryMusicAsync();
                await Task.WhenAll(dirTask, fileTask);
            }
            else
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    filestNode.IsAcquired = false;
                });
                Debug.WriteLine("fail to get MpdQueryListAll: " + result.ErrorMessage);
            }

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsWorking = false;
            });
        });
    }

    private void GetPlaylistSongs(NodeMenuPlaylistItem playlistNode)
    {
        if (playlistNode is null)
            return;

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () => {
            IsWorking = true;
            await Task.Yield();

            if (playlistNode.PlaylistSongs.Count > 0)
            {
                playlistNode.PlaylistSongs.Clear();
            }

            CommandPlaylistResult result = await _mpc.MpdQueryPlaylistSongs(playlistNode.Name);
            if (result.IsSuccess)
            {
                if (result.PlaylistSongs is not null)
                {
                    playlistNode.PlaylistSongs = new ObservableCollection<SongInfo>(result.PlaylistSongs);//result.PlaylistSongs

                    // Workaround for WinUI3's limitation or lack of features.
                    foreach (var hoge in playlistNode.PlaylistSongs)
                    {
                        hoge.ParentViewModel = this;
                    }

                    if (SelectedNodeMenu == playlistNode)
                    {
                        UpdateProgress?.Invoke(this, "[UI] Playlist loading...");
                        PlaylistSongs = playlistNode.PlaylistSongs; // just use this.
                        UpdateProgress?.Invoke(this, "");
                    }

                    playlistNode.IsUpdateRequied = false;
                }
            }

            IsWorking = false;
            await Task.Yield();
        });
    }

    private void UpdateAlbumsAndArtists()
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            IsWorking = true;
            await Task.Yield();


            UpdateProgress?.Invoke(this, "[UI] Updating the AlbumArtists...");
            Artists = new ObservableCollection<AlbumArtist>(_mpc.AlbumArtists);// COPY. //.OrderBy(x => x.Name, comp)

            UpdateProgress?.Invoke(this, "[UI] Updating the Albums...");
            Albums = new ObservableCollection<AlbumEx>(_mpc.Albums); // COPY. // Sort .OrderBy(x => x.Name, comp)

            UpdateProgress?.Invoke(this, "");

            IsWorking = false;
            await Task.Yield();
        });
    }

    private void GetAlbumSongs(AlbumEx album)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            IsWorking = true;
            await Task.Yield();

            if (!album.IsSongsAcquired)
            {
                if (!string.IsNullOrEmpty(album.AlbumArtist.Trim()))
                {
                    //Debug.WriteLine($"GetAlbumSongs: Album artist is not empty, searching by album artist. ({album.AlbumArtist})");
                    var r = await SearchArtistSongs(album.AlbumArtist);//.ConfigureAwait(ConfigureAwaitOptions.None);// no trim() here.

                    if (r.IsSuccess)
                    {
                        if (r.SearchResult is null)
                        {
                            Debug.WriteLine("GetAlbumSongs: SearchResult is null, returning.");
                            return;
                        }

                        foreach (var song in r.SearchResult)
                        {
                            if ((song.AlbumArtist.Equals(album.AlbumArtist, StringComparison.CurrentCulture)) || (song.Artist.Equals(album.AlbumArtist, StringComparison.CurrentCulture)))
                            {
                                //if (song.Album.Trim() == album.Name.Trim())
                                if (song.Album.Equals(album.Name, StringComparison.CurrentCulture))
                                {
                                    // WInUI3's walkaround.
                                    song.ParentViewModel = this;

                                    //Debug.WriteLine($"{song.Album}=={album.Name}?...{song.Title}");
                                    album.Songs.Add(song);
                                }
                            }
                        }
                        album.IsSongsAcquired = true;

                        //SelectedAlbum = album;
                        IsWorking = true;
                        await Task.Yield();

                    }
                    else
                    {
                        Debug.WriteLine("GetAlbumSongs: SearchArtistSongs returned false, returning.");
                        return;
                    }

                    ////
                    ///
                    /*
                    SelectedAlbumSongs = album.Songs;
                    */
                    OnPropertyChanged(nameof(SelectedAlbumSongs));
                    OnPropertyChanged(nameof(AlbumPageSubTitleSelectedAlbumSongsCount));
                }
                else
                {
                    Debug.WriteLine($"GetAlbumSongs: No album artist, trying to search by album name. ({album.Name})");

                    if (!string.IsNullOrEmpty(album.Name.Trim()))
                    {
                        var r = await SearchAlbumSongs(album.Name); // no trim() here.
                        if (r.IsSuccess)
                        {
                            if (r.SearchResult is null)
                            {
                                Debug.WriteLine("GetAlbumSongs: SearchResult is null, returning.");
                                return;
                            }

                            foreach (var song in r.SearchResult)
                            {
                                // WInUI3's walkaround.
                                song.ParentViewModel = this;

                                album.Songs.Add(song);
                            }
                            album.IsSongsAcquired = true;

                            //SelectedAlbum = album;
                            IsWorking = true;
                            await Task.Yield();
                        }
                        else
                        {
                            Debug.WriteLine("GetAlbumSongs: SearchArtistSongs returned false, returning.");
                            return;
                        }

                        ////
                        /*
                        SelectedAlbumSongs = album.Songs;
                        */
                        OnPropertyChanged(nameof(SelectedAlbumSongs));
                        OnPropertyChanged(nameof(AlbumPageSubTitleSelectedAlbumSongsCount));
                    }
                    else
                    {
                        Debug.WriteLine("GetAlbumSongs: No album name, no artist name.");
                        // This should not happen.
                    }
                }
            }
            else
            {
                //SelectedAlbum = album;
                await Task.Delay(50);
            }

            IsWorking = false;
            await Task.Yield();
        });
    }

    private void GetArtistSongs(AlbumArtist? artist)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            if (artist is null)
            {
                Debug.WriteLine("GetArtistSongs: artist is null, returning.");
                return;
            }

            IsWorking = true;
            //UpdateProgress?.Invoke(this, "[UI] Library songs loading...");

            var r = await SearchArtistSongs(artist.Name);

            IsWorking = true;
            //UpdateProgress?.Invoke(this, "[UI] Library songs loading...");


            if (!r.IsSuccess)
            {
                Debug.WriteLine("GetArtistSongs: SearchArtistSongs returned false, returning.");
                return;
            }
            if (artist is null)
            {
                Debug.WriteLine("GetArtistSongs: SelectedAlbumArtist is null, returning.");
                return;
            }


            if (r.SearchResult is null)
            {
                Debug.WriteLine("GetArtistSongs: SearchResult is null, returning.");
                return;
            }

            foreach (var slbm in artist.Albums)
            {
                if (artist is null)
                {
                    Debug.WriteLine("Artist is null, cannot add song to album.");
                    break;
                }

                if (SelectedAlbumArtist != artist)
                {
                    Debug.WriteLine("GetArtistSongs: SelectedAlbumArtist is not the same as artist, returning.");
                    break;
                    //return;
                }

                if (slbm.IsSongsAcquired)
                {
                    //Debug.WriteLine("GetArtistSongs: Album's song is already acquired, skipping.");
                    continue;
                }

                foreach (var song in r.SearchResult)
                {
                    if (song.Album.Equals(slbm.Name, StringComparison.CurrentCulture))
                    {
                        // WInUI3's walkaround.
                        song.ParentViewModel = this;

                        slbm.Songs?.Add(song);
                    }
                }

                slbm.IsSongsAcquired = true;
            }

            IsWorking = false;
        });
    }

    private void GetAlbumPictures(IEnumerable<object>? AlbumExItems)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            if (AlbumExItems is null)
            {
                Debug.WriteLine("GetAlbumPictures: (AlbumExItems is null)");
                return;
            }

            if (Albums.Count < 1)
            {
                Debug.WriteLine("GetAlbumPictures: (Albums.Count < 1)");
                return;
            }

            UpdateProgress?.Invoke(this, "[UI] Loading album covers ...");
            //IsBusy = true;

            IsWorking = true;
            await Task.Yield();

            foreach (var item in AlbumExItems)
            {
                if (item is not AlbumEx album)
                {
                    Debug.WriteLine("GetAlbumPictures: item is not AlbumEx, skipping...." + item.ToString());
                    continue;
                }

                if (album is null)
                {
                    Debug.WriteLine("GetAlbumPictures: album is null, skipping.");
                    continue;
                }

                if (album.IsImageAcquired)
                {
                    //Debug.WriteLine($"GetAlbumPictures: {album.Name} IsImageAcquired is true, skipping.");
                    continue;
                }

                if (album.IsImageLoading)
                {
                    //Debug.WriteLine($"GetAlbumPictures: {album.Name} IsImageLoading is true, skipping.");
                    continue;
                }
                album.IsImageLoading = true;

                if (string.IsNullOrEmpty(album.Name.Trim()))
                {
                    Debug.WriteLine($"GetAlbumPictures: album.Name is null or empty, skipping. {album.AlbumArtist}");
                    continue;
                }

                var strArtist = album.AlbumArtist.Trim(); // album.AlbumArtist already fallback to Artist if none.
                if (string.IsNullOrEmpty(strArtist))
                {
                    strArtist = "Unknown Artist";
                }
                else
                {
                    strArtist = SanitizeFilename(strArtist);
                }

                var strAlbum = album.Name.Trim();
                if (string.IsNullOrEmpty(strAlbum))
                {
                    strAlbum = "Unknown Album";
                }
                else
                {
                    strAlbum = SanitizeFilename(strAlbum);
                }

                string filePath = System.IO.Path.Combine(App.AppDataCacheFolder, System.IO.Path.Combine(strArtist, strAlbum)) + ".bmp";

                if (File.Exists(filePath))
                {
                    // Load cached album cover.

                    album.IsImageLoading = true;

                    try
                    {
                        BitmapImage? bitmap = new(new Uri(filePath));
                        album.AlbumImage = bitmap;
                        album.IsImageAcquired = true;
                        album.IsImageLoading = false;
                    }
                    catch (Exception e)
                    {
                        album.IsImageLoading = false;
                        Debug.WriteLine("GetAlbumPictures: Exception while loading: " + filePath + Environment.NewLine + e.Message);
                        continue;
                    }
                    
                    await Task.Delay(5);

                    album.IsImageLoading = false;
                    //Debug.WriteLine($"GetAlbumPictures: Successfully loaded album art from cache {filePath}");
                }
                else
                {
                    album.IsImageLoading = true;

                    string fileTempPath = System.IO.Path.Combine(App.AppDataCacheFolder, System.IO.Path.Combine(strArtist, strAlbum)) + ".tmp";
                    string strDirPath = System.IO.Path.Combine(App.AppDataCacheFolder, strArtist);

                    if (File.Exists(fileTempPath))
                    {
                        album.IsImageLoading = false;
                        continue; // Skip if temp file exists, it means the album art has found to have no image.
                    }

                    var ret = await SearchAlbumSongs(album.Name);
                    if (!ret.IsSuccess)
                    {
                        Debug.WriteLine("GetAlbumPictures: SearchAlbumSongs failed: " + ret.ErrorMessage);

                        album.IsImageLoading = false;
                        continue;
                    }

                    if (ret.SearchResult is null)
                    {
                        album.IsImageLoading = false;
                        Debug.WriteLine("GetAlbumPictures: ret.SearchResult is null, skipping.");
                        continue;
                    }

                    var sresult = new ObservableCollection<SongInfo>(ret.SearchResult);
                    if (sresult.Count < 1)
                    {
                        album.IsImageLoading = false;
                        Debug.WriteLine("GetAlbumPictures: ret.SearchResult.Count < 1, skipping. -> " + album.Name);
                        continue;
                    }

                    bool isWaitFailed = false;
                    bool isCoverExists = false;
                    bool isNoAlbumCover = false;

                    foreach (var albumsong in sresult)
                    {
                        if (albumsong is null)
                        {
                            continue;
                        }

                        var aat = albumsong.AlbumArtist.Trim();
                        if (string.IsNullOrEmpty(aat))
                        {
                            aat = albumsong.Artist.Trim();
                        }
                        
                        if (string.Equals(aat, album.AlbumArtist, StringComparison.CurrentCulture))
                        {
                            //Debug.WriteLine($"GetAlbumPictures: Processing song {albumsong.File} from album {album.Name}");
                            var r = await _mpc.MpdQueryAlbumArtForAlbumView(albumsong.File, IsDownloadAlbumArtEmbeddedUsingReadPicture);
                            if (!r.IsSuccess)
                            {
                                isNoAlbumCover = r.IsNoBinaryFound;
                                isWaitFailed = r.IsWaitFailed;
                                album.IsImageLoading = false;
                                //Debug.WriteLine($"MpdQueryAlbumArtForAlbumView failed: {r.ErrorMessage}");
                                continue;
                            }
                            if (r.AlbumCover is null) continue;
                            if (!r.AlbumCover.IsSuccess) continue;

                            album.IsImageAcquired = true;
                            album.IsImageLoading = false;

                            //Dispatcher.UIThread.Post(() =>
                            //{
                            //album.AlbumImage = r.AlbumCover.AlbumImageSource;
                            album.AlbumImage = await BitmapSourceFromByteArray(r.AlbumCover.BinaryData);
                            //
                            //});


                            if (r.AlbumCover.BinaryData is not null)
                            {
                                Directory.CreateDirectory(strDirPath);
                                File.WriteAllBytes(filePath, r.AlbumCover.BinaryData);
                                //Debug.WriteLine($"GetAlbumPictures: Successfully saved album art for {filePath}");
                            }
                            else
                            {
                                //Debug.WriteLine($"GetAlbumPictures: BinaryData is null: {filePath}");
                            }

                            //album.AlbumImage?.Save(filePath, 100);
                            //await SaveBitmapToFile(r.AlbumCover.AlbumImageSource, filePath);

                            //Debug.WriteLine($"GetAlbumPictures: Successfully retrieved album art for {albumsong.File}");
                            //Debug.WriteLine($"GetAlbumPictures: Successfully retrieved album art for {album.Name} by {album.AlbumArtist}");

                            //Debug.WriteLine(System.IO.Path.Combine(strArtist, strAlbum) + ".bmp");

                            isCoverExists = true;

                            break; // Break after first successful album art retrieval.
                        }
                        else
                        {
                            //Debug.WriteLine($" {album.Name} > {album.AlbumArtist} : {albumsong.AlbumArtist},  {albumsong.Artist}");
                        }
                    }

                    // File saved. Don't save temp file.
                    if (isCoverExists) continue;

                    // WaitFiled. Don't save temp file.
                    if (isWaitFailed) continue;

                    if (isNoAlbumCover)
                    {
                        // Writes tmp file indicating there was no album cover.
                        try
                        {
                            Directory.CreateDirectory(strDirPath);
                            DateTimeOffset dto = new(DateTime.UtcNow);
                            // Get the unix timestamp in seconds
                            var unixTime = dto.ToUnixTimeSeconds().ToString();

                            await using StreamWriter file = new(fileTempPath);
                            await file.WriteLineAsync(unixTime);
                            file.Close();
                            // Testing
                            await Task.Delay(10);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine("GetAlbumPictures: Exception while saving album art DUMMY file: " + e.Message);
                        }
                    }

                    album.IsImageLoading = false;

                    await Task.Yield();
                }
            }

            //IsBusy = false;
            IsWorking = false;
            await Task.Yield();
        });
    }

    private static async Task<BitmapImage?> BitmapSourceFromByteArray(byte[]? buffer)
    {
        if (buffer == null)
        {
            Debug.WriteLine("buffer == null) @BitmapSourceFromByteArray");
            return null;
        }

        // Bug in MPD 0.23.5 
        if (buffer?.Length <= 0)
        {
            Debug.WriteLine("if (buffer?.Length > 0) @BitmapSourceFromByteArray");

            return null;
        }

        try
        {
            /*
            SoftwareBitmap? softwareBitmap = null;

            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                // Write the byte array to the stream
                await stream.WriteAsync(buffer.AsBuffer());
                stream.Seek(0); // Reset stream position to the beginning

                // Create a BitmapDecoder from the stream
                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                // Get the SoftwareBitmap
                softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            }

            return softwareBitmap;
            */

            var bitmapImage = new BitmapImage();
            using (var stream = new InMemoryRandomAccessStream())
            {
                // Write the byte array to the in-memory stream.
                await stream.WriteAsync(buffer.AsBuffer());
                stream.Seek(0); // Reset the stream position to the beginning.

                // Set the BitmapImage source from the stream.
                await bitmapImage.SetSourceAsync(stream);
            }

            return bitmapImage;
        }
        catch (Exception e)
        {
            Debug.WriteLine($"Exception {e} @BitmapSourceFromByteArray");

        }

        return null;
    }

    private static void SaveAlbumCoverImage(SongInfoEx? current, AlbumImage? album)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            if ((current?.File) != (album?.SongFilePath))
            {
                Debug.WriteLine($"NOT ({current?.File} == {album?.SongFilePath})");
                return;
            }
            /*
            var strArtist = current?.AlbumArtist.Trim();
            if (string.IsNullOrEmpty(strArtist))
            {
                strArtist = "Unknown Artist";
            }
            else
            {
                strArtist = SanitizeFilename(strArtist);
            }
            */
            var strArtist = current?.AlbumArtist.Trim();
            if (string.IsNullOrEmpty(strArtist))
            {
                // Manually fallback to Artist. The same way Album class does.
                strArtist = current?.Artist.Trim();
                if (string.IsNullOrEmpty(strArtist))
                {
                    strArtist = "Unknown Artist";
                }
            }
            strArtist = SanitizeFilename(strArtist);

            var strAlbum = current?.Album ?? string.Empty;
            if (string.IsNullOrEmpty(strAlbum))
            {
                strAlbum = "Unknown Album";
            }
            else
            {
                strAlbum = SanitizeFilename(strAlbum);
            }

            string strDirPath = System.IO.Path.Combine(App.AppDataCacheFolder, strArtist);
            string filePath = System.IO.Path.Combine(App.AppDataCacheFolder, System.IO.Path.Combine(strArtist, strAlbum)) + ".bmp";
            try
            {
                //album?.AlbumImageSource?.Save(filePath, 100);
                if (album?.BinaryData is not null)
                {
                    Directory.CreateDirectory(strDirPath);
                    File.WriteAllBytes(filePath, album.BinaryData);
                    //Debug.WriteLine($"SaveAlbumCoverImage: Successfully saved album art for {filePath}");
                }
                //Debug.WriteLine($"SaveAlbumCoverImage: saved album art {strArtist}, {strAlbum}");
            }
            catch (Exception e)
            {
                Debug.WriteLine("SaveAlbumCoverImage: Exception while saving album art: " + e.Message);
            }
        });
    }

    public static string SanitizeFilename(string name)
    {
        // 1. Get the list of invalid characters for the current system
        // and add additional common invalid path characters.
        char[] invalidChars = Path.GetInvalidFileNameChars();

        // 2. Create a regex pattern to match invalid characters.
        // We escape the characters to ensure they are interpreted literally.
        string invalidCharsPattern = "[" + Regex.Escape(new string(invalidChars)) + "]";

        // 3. Replace all invalid characters with the replacement character.
        string sanitizedName = Regex.Replace(name, invalidCharsPattern, "_");

        // 4. Handle reserved Windows filenames (e.g., CON, PRN, NUL).
        string[] reservedNames = ["CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"];
        if (Array.Exists(reservedNames, s => s.Equals(sanitizedName, StringComparison.OrdinalIgnoreCase)))
        {
            sanitizedName = $"_{sanitizedName}_";
        }

        // 5. Trim trailing periods and spaces, which are invalid on Windows.
        sanitizedName = sanitizedName.TrimEnd('.', ' ');

        // 6. Ensure the filename isn't empty after sanitizing.
        if (string.IsNullOrWhiteSpace(sanitizedName))
        {
            return "Untitled";
        }

        return sanitizedName;
    }

    private async Task<CommandSearchResult> SearchAlbumSongs(string name)
    {
        if (name is null)
        {
            CommandSearchResult result = new()
            {
                IsSuccess = false,
                ErrorMessage = "SearchAlbumSongs: name is null"
            };
            return result;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            IsWorking = true;
            //UpdateProgress?.Invoke(this, "");
        });

        string queryShiki = "==";
        var res = await _mpc.MpdSearch("Album", queryShiki, name); // No name.Trim() because of "=="

        if (!res.IsSuccess)
        {
            Debug.WriteLine("SearchAlbumSongs failed: " + res.ErrorMessage);
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            IsWorking = false;
            UpdateProgress?.Invoke(this, "");
        });

        return res;
    }

    private async Task<CommandSearchResult> SearchArtistSongs(string name)
    {
        if (name is null)
        {
            CommandSearchResult result = new()
            {
                IsSuccess = false,
                ErrorMessage = "SearchArtistSongs: name is null"
            };
            return result;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            IsWorking = true;
            //UpdateProgress?.Invoke(this, "");
        });

        string queryShiki = "==";
        var res = await _mpc.MpdSearch("AlbumArtist", queryShiki, name); // AlbumArtist looks for VALUE in AlbumArtist and falls back to Artist tags if AlbumArtist does not exist. 

        if (!res.IsSuccess)
        {
            Debug.WriteLine("SearchArtistSongs failed: " + res.ErrorMessage);
        }
        else
        {
            //Debug.WriteLine(res.ResultText);
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            IsWorking = false;
            UpdateProgress?.Invoke(this, "");
        });

        return res;
    }

    private static int CompareVersionString(string a, string b)
    {
        return (new System.Version(a)).CompareTo(new System.Version(b));
    }

    #endregion

    #region == MPD events == 

    private void OnMpdIdleConnected(MpcService sender)
    {
        Debug.WriteLine("OK MPD " + _mpc.MpdVerText + " @OnMpdConnected");

        // ATTN: this won't be called if we etablishe the connection before MainWnd is initialized.
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue( () =>
        {
            MpdVersion = _mpc.MpdVerText;

            ////MpdStatusMessage = MpdVersion;// + ": " + MPDCtrlX.Properties.Resources.MPD_StatusConnected;

            //MpdStatusButton = _pathMpdOkButton;

            IsConnected = true;
            IsConnecting = false;
            IsNotConnectingNorConnected = false;

            // Just in case.
            IsShowAckWindow = false;
            IsShowErrWindow = false;

            // Add newly created Profile in the InitDialog to Profiles because connection was successfully established.
            if ((Profiles.Count <= 0) && (CurrentProfile is not null))
            {
                Profiles.Add(CurrentProfile);
            }
        });

        // 
        _ = Task.Run(LoadInitialData);
    }

    private void OnMpdPlayerStatusChanged(MpcService sender)
    {
        /*
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {

        });
        */
        /*
        if (_mpc.MpdStatus.MpdError != "")
        {
            MpdStatusMessage = MpdVersion + ": " + MPDCtrlX.Properties.Resources.MPD_StatusError + " - " + _mpc.MpdStatus.MpdError;
            MpdStatusButton = _pathMpdAckErrorButton;
        }
        else
        {
            MpdStatusMessage = "";
            MpdStatusButton = _pathMpdOkButton;
        }

        */
        UpdateStatus();
    }

    private void OnMpdCurrentQueueChanged(MpcService sender)
    {
        UpdateCurrentQueue();
    }

    private void OnMpdPlaylistsChanged(MpcService sender)
    {
        UpdatePlaylists();
    }

    private void OnAlbumArtChanged(MpcService sender)
    {
        //
    }

    private void OnDebugCommandOutput(MpcService sender, string data)
    {
        if (IsDebugWindowEnabled && IsShowDebugWindow)
        {
            DebugCommandOutput?.Invoke(this, data);
            /*
             *  this won't work.
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
            });
            */
        }
    }

    private void OnDebugIdleOutput(MpcService sender, string data)
    {
        if (IsDebugWindowEnabled && IsShowDebugWindow)
        {
            DebugIdleOutput?.Invoke(this, data);
            /*
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
            });
            */
        }
    }

    private void OnConnectionError(MpcService sender, string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return;

        /*
         *  ?
        IsConnected = false;
        IsConnecting = false;

        StatusButton = _pathErrorInfoButton;
        */

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            ConnectionStatusMessage = _resourceLoader.GetString("ConnectionStatus_ConnectionError") + ": " + msg;
            StatusBarMessage = ConnectionStatusMessage;

            InfoBarAckTitle = _resourceLoader.GetString("ConnectionStatus_ConnectionError");
            InfoBarAckMessage = msg;
            IsShowAckWindow = true;
        });
    }

    private void OnConnectionStatusChanged(MpcService sender, MpcService.ConnectionStatus status)
    {
        if (status == MpcService.ConnectionStatus.NeverConnected)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;

                /*
                IsConnectionSettingShow = true;
                StatusButton = _pathDisconnectedButton;
                */

            });

            //ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_NeverConnected;
            Debug.WriteLine("ConnectionStatus_NeverConnected");
        }
        else if (status == MpcService.ConnectionStatus.Connected)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = true;
                IsConnecting = false;
                IsNotConnectingNorConnected = false;
            });

            /*
            IsConnectionSettingShow = false;
            StatusButton = _pathConnectedButton;
            */

            //ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Connected;
            //Debug.WriteLine("ConnectionStatus_Connected");
        }
        else if (status == MpcService.ConnectionStatus.Connecting)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = true;
                IsNotConnectingNorConnected = false;
            });
            /*
            //IsConnectionSettingShow = true;
            StatusButton = _pathConnectingButton;
            */

            //ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Connecting;

            //StatusBarMessage = ConnectionStatusMessage;
            //Debug.WriteLine("ConnectionStatus_Connecting");
        }
        else if (status == MpcService.ConnectionStatus.ConnectFailTimeout)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
            });
            /*
            IsConnectionSettingShow = true;

            Debug.WriteLine("ConnectionStatus_ConnectFail_Timeout");
            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_ConnectFail_Timeout;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
            */

            Debug.WriteLine("ConnectionStatus_ConnectFail_Timeout");
        }
        else if (status == MpcService.ConnectionStatus.SeeConnectionErrorEvent)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
            });
            /*
            IsConnectionSettingShow = true;
            StatusButton = _pathErrorInfoButton;
            */

            _elapsedTimer.Stop();
            Debug.WriteLine("ConnectionStatus_SeeConnectionErrorEvent");
        }
        else if (status == MpcService.ConnectionStatus.Disconnected)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
            });

            /*
            IsConnectionSettingShow = true;
            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Disconnected;
            StatusButton = _pathErrorInfoButton;
            StatusBarMessage = ConnectionStatusMessage;
            */

            Debug.WriteLine("ConnectionStatus_Disconnected");

        }
        else if (status == MpcService.ConnectionStatus.DisconnectedByHost)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
            });

            // TODO: not really usued now...
            /*
            IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_DisconnectedByHost;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
            Debug.WriteLine("ConnectionStatus_DisconnectedByHost");
        }
        else if (status == MpcService.ConnectionStatus.Disconnecting)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = false;
            });
            /*
            //IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Disconnecting;
            StatusButton = _pathConnectingButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
            //Debug.WriteLine("ConnectionStatus_Disconnecting");
        }
        else if (status == MpcService.ConnectionStatus.DisconnectedByUser)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
            });
            /*
            //IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_DisconnectedByUser;
            StatusButton = _pathDisconnectedButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
            //Debug.WriteLine("ConnectionStatus_DisconnectedByUser");
        }
        else if (status == MpcService.ConnectionStatus.SendFailNotConnected)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
            });
            /*
            IsConnectionSettingShow = true;
            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
            */

            Debug.WriteLine("ConnectionStatus_SendFail_NotConnected");
        }
        else if (status == MpcService.ConnectionStatus.SendFailTimeout)
        {
            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
            });
            /*
            IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_SendFail_Timeout;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
            Debug.WriteLine("ConnectionStatus_SendFail_Timeout");
        }
    }

    private void OnMpdAckError(MpcService sender, string ackMsg, string origin)
    {
        if (string.IsNullOrEmpty(ackMsg))
            return;

        Debug.WriteLine($"MpdAckError: {ackMsg}");

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            string s = ackMsg;
            string patternStr = @"[\[].+?[\]]";//@"[{\[].+?[}\]]";
            s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
            s = s.Replace("ACK ", string.Empty);
            s = s.Replace("{} ", string.Empty);

            if (origin.Equals("Command", StringComparison.OrdinalIgnoreCase))
            {
                InfoBarAckTitle = MpdVersion + " " + _resourceLoader.GetString("MPD_CommandResponse");
            }
            else if (origin.Equals("Idle", StringComparison.OrdinalIgnoreCase))
            {
                InfoBarAckTitle = MpdVersion + " " + _resourceLoader.GetString("MPD_IdleResponse");
            }
            else
            {
                InfoBarAckTitle = MpdVersion;
            }

            InfoBarAckMessage = s;

            IsShowAckWindow = true;
        });
    }

    private void OnMpdFatalError(MpcService sender, string errMsg, string origin)
    {
        if (string.IsNullOrEmpty(errMsg))
            return;

        Debug.WriteLine($"MpdFatalError: {errMsg}");

        string s = errMsg;
        string patternStr = @"[\[].+?[\]]";//@"[{\[].+?[}\]]";
        s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
        s = s.Replace("ACK ", string.Empty);
        s = s.Replace("{} ", string.Empty);

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            if (origin.Equals("Command", StringComparison.OrdinalIgnoreCase))
            {
                InfoBarErrTitle = MpdVersion + " " + _resourceLoader.GetString("MPD_CommandResponse");
            }
            else if (origin.Equals("Idle", StringComparison.OrdinalIgnoreCase))
            {
                InfoBarErrTitle = MpdVersion + " " + _resourceLoader.GetString("MPD_IdleResponse");
            }
            else
            {
                InfoBarErrTitle = MpdVersion;
            }

            InfoBarErrMessage = s;

            IsShowErrWindow = true;
        });
    }

    private void OnMpcProgress(MpcService sender, string msg)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            StatusBarMessage = msg;
        });
    }

    private void OnMpcIsBusy(MpcService sender, bool on)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            IsBusy = on;
        });
    }

    private void OnUpdateProgress(string msg)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            StatusBarMessage = msg;
        });
    }

    #endregion

    #region == Commands ==

    [RelayCommand]
    private void SwitchTheme(ElementTheme? param)
    {
        if (param is null)
        {
            return;
        }

        if (Theme == param)
        {
            return;
        }

        if (App.MainWnd == null)
        {
            return;
        }
        //var mainWin = App.GetService<MainWindow>();

        Theme = (ElementTheme)param;

        if (App.MainWnd?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            //TitleBarHelper.UpdateTitleBar(Theme, App.MainWnd);

            App.MainWnd.SetCapitionButtonColor();
        }
    }

    [RelayCommand]
    private static void SwitchSystemBackdrop(string? backdrop)
    {
        if (backdrop == null)
        {
            return;
        }

        if (Enum.TryParse(backdrop, out SystemBackdropOption cacheBackdrop))
        {
            //var mainWin = App.GetService<MainWindow>();
            App.MainWnd?.SwitchBackdrop(cacheBackdrop);
        }
    }

    [RelayCommand]
    public async Task Play()
    {
        if (IsBusy) return;

        if (Queue.Count < 1) { return; }

        switch (_mpc.MpdStatus.MpdState)
        {
            case Status.MpdPlayState.Play:
                {
                    //State>>Play: So, send Pause command
                    await _mpc.MpdPlaybackPause();
                    break;
                }
            case Status.MpdPlayState.Pause:
                {
                    //State>>Pause: So, send Resume command
                    await _mpc.MpdPlaybackResume(Convert.ToInt32(_volume));
                    break;
                }
            case Status.MpdPlayState.Stop:
                {
                    //State>>Stop: So, send Play command
                    await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume));
                    break;
                }
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    [RelayCommand]
    public async Task PlayNext()
    {
        if (IsBusy) return;
        if (Queue.Count < 1) { return; }

        await _mpc.MpdPlaybackNext(Convert.ToInt32(_volume));
    }

    [RelayCommand]
    public async Task PlayPrev()
    {
        if (IsBusy) return;
        if (Queue.Count < 1) { return; }

        await _mpc.MpdPlaybackPrev(Convert.ToInt32(_volume));
    }

    [RelayCommand]
    public async Task Stop()
    {
        if (IsBusy) return;
        if (Queue.Count < 1) { return; }

        await _mpc.MpdPlaybackStop();
    }

    [RelayCommand]
    public async Task Pause()
    {
        if (IsBusy) return;
        if (Queue.Count < 1) { return; }

        await _mpc.MpdPlaybackPause();
    }

    [RelayCommand]
    public async Task SetSeek()
    {
        if (IsBusy) return;
        double elapsed = _elapsed / _elapsedTimeMultiplier;
        await _mpc.MpdPlaybackSeek(_mpc.MpdStatus.MpdSongID, elapsed);
    }

    [RelayCommand]
    public async Task SetRandom()
    {
        if (IsBusy) return;
        await _mpc.MpdSetRandom(_random);
    }

    [RelayCommand]
    public async Task SetRpeat()
    {
        if (IsBusy) return;
        await _mpc.MpdSetRepeat(_repeat);
    }

    [RelayCommand]
    public async Task SetConsume()
    {
        if (IsBusy) return;
        await _mpc.MpdSetConsume(_consume);
    }

    [RelayCommand]
    public async Task SetSingle()
    {
        if (IsBusy) return;
        await _mpc.MpdSetSingle(_single);
    }

    [RelayCommand]
    public void VolumeDown()
    {
        if (_volume >= 10)
        {
            Volume -= 10;
            //await _mpc.MpdSetVolume(Convert.ToInt32(_volume - 10));
        }
        else
        {
            Volume = 0;
        }
    }

    [RelayCommand]
    public void VolumeUp()
    {
        if (_volume <= 90)
        {
            Volume += 10;
            //await _mpc.MpdSetVolume(Convert.ToInt32(_volume + 10));
        }
        else
        {
            Volume = 100;
        }
    }

    [RelayCommand]
    public async Task VolumeMute()
    {
        await _mpc.MpdSetVolume(0);
    }


    [RelayCommand]
    public async Task SearchExec()
    {
        // Allow empty string.
        //if (string.IsNullOrEmpty(SearchQuery)) return; 

        string queryShiki = "contains";
        if (SelectedSearchShiki.Shiki == SearchShiki.Equals)
        {
            queryShiki = "==";
        }

        var res = await _mpc.MpdSearch(SelectedSearchTag.Key.ToString(), queryShiki, SearchQuery);
        if (res.IsSuccess)
        {
            if (res.SearchResult is not null)
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    // WinUI3's workaround.
                    foreach (var item in res.SearchResult)
                    {
                        item.ParentViewModel = this;
                    }

                    SearchResult = new ObservableCollection<SongInfo>(res.SearchResult); // COPY ON PURPOSE

                    if (SearchResult.Count > 0)
                    {
                        IsSearchControlEnabled = true;
                    }
                    else
                    {
                        IsSearchControlEnabled = false;
                    }
                });
            }
            else
            {
                Debug.WriteLine("Search result is null.");
                SearchResult?.Clear();
                IsSearchControlEnabled = false;
            }
        }
        else
        {
            Debug.WriteLine("Search failed: " + res.ErrorMessage);
            SearchResult?.Clear();
            IsSearchControlEnabled = false;
        }

        UpdateProgress?.Invoke(this, "");
    }

    [RelayCommand]
    public async Task QueueSelectedPlay(SongInfoEx? song)
    {
        if (song is null)
        {
            Debug.WriteLine($"Playback PlayPause is null");
            return;
        }

        if (Queue.Count < 1)
        {
            return;
        }
        if (_selectedQueueSong is null)
        {
            return;
        }
        await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume), song.Id);
    }

    [RelayCommand]
    public void SongsListviewPlayThis(object obj)
    {
        if (obj is null) return;

        if (obj is not SongInfo song)
        {
            return;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            Queue.Clear();
            CurrentSong = null;

            await _mpc.MpdSinglePlay(song.File, Convert.ToInt32(_volume));
        });
    }

    [RelayCommand]
    public async Task SongsListviewAddThis(object obj)
    {
        if (obj is null) return;

        if (obj is SongInfo song)
        {
            await _mpc.MpdAdd(song.File);
        }
    }

    [RelayCommand]
    public async Task QueueClearWithoutPrompt()
    {
        if (Queue.Count == 0) { return; }

        await _mpc.MpdPlaybackStop();
        await _mpc.MpdClear();
    }

    [RelayCommand]
    public void QueueFilterSelect(object obj)
    {
        if (Queue.Count <= 1)
            return;
        /*
        if (SelectedQueueFilterSong is null)
        {
            return;
        }
        ScrollIntoViewAndSelect?.Invoke(this, SelectedQueueFilterSong.Index);
        */

        if (obj is null) return;

        if (obj is SongInfoEx song)
        {
            ScrollIntoViewAndSelect?.Invoke(this, song);
        }
    }

    [RelayCommand]
    public async Task QueueListviewMoveUp(object obj)
    {
        if (obj is null) return;

        if (Queue.Count <= 1) return;

        if (obj is not SongInfoEx song) return;

        Dictionary<string, string> idToNewPos = [];

        try
        {
            int i = Int32.Parse(song.Pos);

            if (i == 0) return;

            i -= 1;

            idToNewPos.Add(song.Id, i.ToString());
        }
        catch
        {
            return;
        }

        await _mpc.MpdMoveId(idToNewPos);
    }

    [RelayCommand]
    public async Task QueueListviewMoveDown(object obj)
    {
        if (obj is null) return;

        if (Queue.Count <= 1)
            return;

        if (obj is not SongInfoEx song) return;

        Dictionary<string, string> idToNewPos = [];

        try
        {
            var i = Int32.Parse(song.Pos);

            if (i >= Queue.Count - 1) return;

            i += 1;

            idToNewPos.Add(song.Id, i.ToString());
        }
        catch
        {
            return;
        }

        await _mpc.MpdMoveId(idToNewPos);
    }

    [RelayCommand]
    public async Task QueueListviewSortBy(object obj)
    {
        if (obj is null)
        {
            return;
        }
        if (obj is not string key)
        {
            return;
        }

        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (Queue.Count <= 1) return;

        var ci = CultureInfo.CurrentCulture;
        var comp = StringComparer.Create(ci, true);

        ObservableCollection<SongInfoEx>? sorted;
        switch (key)
        {
            case "title":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.Title, comp));
                break;
            case "time":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.TimeSort));
                break;
            case "artist":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.Artist, comp));
                break;
            case "album":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.Album, comp));
                break;
            case "disc":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.DiscSort));
                break;
            case "track":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.TrackSort));
                break;
            case "genre":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.Genre, comp));
                break;
            case "lastmodified":
                sorted = new ObservableCollection<SongInfoEx>(Queue.OrderBy(x => x.LastModified));
                break;
            default:
                return;
        }

        Dictionary<string, string> idToNewPos = [];
        int i = 0;
        foreach (var item in sorted)
        {
            idToNewPos.Add(item.Id, i.ToString());
            i++;
        }

        await _mpc.MpdMoveId(idToNewPos);
    }

    [RelayCommand]
    public async Task QueueListviewSortReverse()
    {
        if (Queue.Count <= 1) return;

        var sorted = new ObservableCollection<SongInfoEx>(Queue.OrderByDescending(x => x.Index));

        Dictionary<string, string> idToNewPos = [];
        int i = 0;
        foreach (var item in sorted)
        {
            idToNewPos.Add(item.Id, i.ToString());
            i++;
        }

        await _mpc.MpdMoveId(idToNewPos);
    }

    [RelayCommand]
    public void ScrollIntoNowPlaying()
    {
        if (Queue.Count == 0) return;
        if (CurrentSong is null) return;
        if (Queue.Count < CurrentSong.Index + 1) return;

        //
        _mainMenuItems.QueueDirectory.Selected = true;

        ScrollIntoView?.Invoke(this, CurrentSong);
    }

    [RelayCommand]
    public async Task QueueAddToPlaylist()
    {
        if (Queue.Count == 0) return;

        var result = await _dialogs.ShowAddToDialog(this);

        if (result is null)
        {
            return;
        }

#pragma warning disable IDE0305
        _ = AddToPlaylist(result.PlaylistName, Queue.Select(s => s.File).ToList());
#pragma warning restore IDE0305
    }

    public async Task AddToPlaylist(string playlistName, List<string> uris)
    {
        if (string.IsNullOrEmpty(playlistName))
            return;
        if (uris.Count == 0)
            return;

        await _mpc.MpdPlaylistAdd(playlistName, uris);
    }

    [RelayCommand]
    public async Task QueueListviewAddSelectedItemsToPlaylist(System.Collections.Generic.IList<object> obj)
    {
        if (Queue.Count == 0) return;

        if (obj is null)
        {
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @QueueListviewAddSelectedItemsToPlaylist");
            return;
        }

        var collection = items.Cast<SongInfoEx>();

        List<string> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.File);
        }

        if (selectedList.Count == 0)
        {
            return;
        }

        var result = await _dialogs.ShowAddToDialog(this);

        if (result is null)
        {
            return;
        }

        _ = AddToPlaylist(result.PlaylistName, selectedList);
    }

    [RelayCommand]
    public async Task QueueListviewRemoveSelectedItems(System.Collections.Generic.IList<object> obj)
    {
        if (obj is null) return;

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @QueueListviewRemoveSelectedItems");
            return;
        }

        var collection = items.Cast<SongInfoEx>();

        List<string> deleteIdList = [];

        foreach (var item in collection)
        {
            deleteIdList.Add((item as SongInfoEx).Id);
        }
        // or
        //deleteIdList.AddRange(collection.Select(item => (item as SongInfoEx).Id));

        switch (deleteIdList.Count)
        {
            case 1:
                await _mpc.MpdDeleteId(deleteIdList[0]);
                break;
            case >= 1:
                await _mpc.MpdDeleteId(deleteIdList);
                break;
        }
    }

    [RelayCommand]
    public void SongsPlayAll(object obj)
    {
        if (obj is null) return;

        if (obj is not ObservableCollection<SongInfo> list) return;
        if (list.Count <= 0) return;

        List<string> uriList = [];

        foreach (var song in list)
        {
            uriList.Add(song.File);
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            Queue.Clear();
            CurrentSong = null; 
            await _mpc.MpdMultiplePlay(uriList, Convert.ToInt32(_volume));
        });

        // get album cover.
        //await Task.Yield();
        //await Task.Delay(200);
        ///UpdateCurrentSong();
    }

    [RelayCommand]
    public async Task SongsAddToQueue(object obj)
    {
        if (obj is null) return;

        if (obj is not ObservableCollection<SongInfo> list) return;
        switch (list.Count)
        {
            case > 1:
                {
                    List<string> uriList = [];

                    foreach (var song in list)
                    {
                        uriList.Add(song.File);
                    }
                    //uriList.AddRange(list.Select(song => song.File));

                    await _mpc.MpdAdd(uriList);
                    break;
                }
            case 1 when (list[0] is SongInfo si):
                await _mpc.MpdAdd(si.File);
                break;
        }
    }

    [RelayCommand]
    public async Task SongsAddToPlaylist(object obj)
    {
        if (obj is null) return;

        if (obj is ObservableCollection<SongInfo> list)
        {
            List<string> uriList = [];

            foreach (var item in list)
            {
                uriList.Add(item.File);
            }

            if (uriList.Count > 0)
            {
                //SearchPageAddToPlaylistDialogShow?.Invoke(this, uriList);
                var result = await _dialogs.ShowAddToDialog(this);

                if (result is null)
                {
                    return;
                }

                _ = AddToPlaylist(result.PlaylistName, uriList);
            }
        }
    }

    [RelayCommand]
    public void SongsSelectedItemsPlay(System.Collections.Generic.IList<object> obj)
    {
        if (obj is null)
        {
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @SongsSelectedItemsPlay");
            return;
        }

        var collection = items.Cast<SongInfo>();

        List<string> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.File);
        }

        if (selectedList.Count == 0)
        {
            return;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            Queue.Clear();
            CurrentSong = null;
            await _mpc.MpdMultiplePlay(selectedList, Convert.ToInt32(_volume));
        });
    }

    [RelayCommand]
    public async Task SongsAddSelectedItemsToPlaylist(System.Collections.Generic.IList<object> obj)
    {
        if (obj is null)
        {
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @SongsAddSelectedItemsToPlaylist");
            return;
        }

        var collection = items.Cast<SongInfo>();

        List<string> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.File);
        }

        if (selectedList.Count == 0)
        {
            return;
        }

        var result = await _dialogs.ShowAddToDialog(this);

        if (result is null)
        {
            return;
        }

        _ = AddToPlaylist(result.PlaylistName, selectedList);
    }

    [RelayCommand]
    public async Task SongsAddSelectedItemsToQueue(System.Collections.Generic.IList<object> obj)
    {
        if (obj is null)
        {
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @SongsAddSelectedItemsToQueue");
            return;
        }

        var collection = items.Cast<SongInfo>();

        List<string> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.File);
        }

        if (selectedList.Count == 1)
        {
            await _mpc.MpdAdd(selectedList[0]);
        }
        else if (selectedList.Count > 1)
        {
            await _mpc.MpdAdd(selectedList);
        }
    }

    public static ObservableCollection<SongInfo> SongsSortBy(ObservableCollection<SongInfo> target, string key)
    {
        ObservableCollection<SongInfo> sorted = [];

        if (target is null)
        {
            return sorted;
        }

        if (target.Count == 0)
        {
            return sorted;
        }

        if (string.IsNullOrEmpty(key))
        {
            return sorted;
        }

        var ci = CultureInfo.CurrentCulture;
        var comp = StringComparer.Create(ci, true);

        switch (key)
        {
            case "title":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.Title, comp));
                break;
            case "time":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.TimeSort));
                break;
            case "artist":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.Artist, comp));
                break;
            case "album":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.Album, comp));
                break;
            case "disc":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.DiscSort));
                break;
            case "track":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.TrackSort));
                break;
            case "genre":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.Genre, comp));
                break;
            case "lastmodified":
                sorted = new ObservableCollection<SongInfo>(target.OrderBy(x => x.LastModified));
                break;
            case "reverse":
                sorted = new ObservableCollection<SongInfo>(target.Reverse<SongInfo>());
                break;
            default:
                return sorted;
        }

        return sorted;
        /*
        Dictionary<string, string> idToNewPos = [];
        int i = 0;
        foreach (var item in sorted)
        {
            idToNewPos.Add(item.Id, i.ToString());
            i++;
        }
        */
        //await _mpc.MpdMoveId(idToNewPos);
    }

    [RelayCommand]
    public void SearchSongsSortBy(object obj)
    {
        if (obj is null)
        {
            return;
        }
        if (obj is not string key)
        {
            return;
        }

        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (SearchResult is null) return;

        if (SearchResult.Count <= 1) return;

        SearchResult = SongsSortBy(SearchResult, key);
    }

    [RelayCommand]
    public void AlbumsSortBy(object obj)
    {
        if (obj is null)
        {
            return;
        }
        if (obj is not string key)
        {
            return;
        }

        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        var ci = CultureInfo.CurrentCulture;
        var comp = StringComparer.Create(ci, true);

        switch (key)
        {
            case "artist":
                Albums = new ObservableCollection<AlbumEx>(Albums.OrderBy(x => x.AlbumArtist, comp));
                break;
            case "album":
                Albums = new ObservableCollection<AlbumEx>(Albums.OrderBy(x => x.Name, comp));
                break;

            case "reverse":
                Albums = new ObservableCollection<AlbumEx>(Albums.Reverse<AlbumEx>());
                break;
        }

        // Need this to load image.
        // Albums sort resets ObservableCollection which is not recognized by ListViewBehavior and does not UpdateVisibleItems,
        // so forcibly fire scroll event in AlbumsPage's code behind.
        AlbumsCollectionHasBeenReset?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void ListviewGoToAlbumPage(SongInfo song)
    {
        if (song is null)
        {
            return;
        }

        if (string.IsNullOrEmpty(song.Album.Trim()))
        {
            return;
        }

        var items = Albums.Where(i => i.Name == song.Album);
        if (items is null) return;
        var asdf = song.AlbumArtist;
        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            asdf = song.Artist;
        }

        // no artist name
        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            var hoge = items.FirstOrDefault(x => x.Name == song.Album);
            // found it
            if (hoge is not null)
            {
                GoToAlbumDetailsPage(hoge);
            }
        }
        else
        {
            foreach (var item in items)
            {
                if (item.AlbumArtist != asdf) continue;
                // found it
                if (item is not null)
                {
                    GoToAlbumDetailsPage(item);
                }
                break;
            }
        }
    }

    private void GoToAlbumPage()
    {
        IsNavigationViewMenuOpen = true;
        _mainMenuItems.AlbumsDirectory.Selected = true;
    }

    private void GoToAlbumDetailsPage(AlbumEx album)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            // First, go to album page so that "Back" mean back to AlbumPage.
            GoToAlbumPage();

            // ensure scroll to the item.
            await Task.Yield();
            //await Task.Delay(500);

            // Do this below when navigateFrom details page (by calling GoBackFromAlbumDetailsPage()).
            // scroll to the item (required a little trick to actually show images)
            //AlbumScrollIntoView?.Invoke(this, album);

            // go to album detail page.
            SelectedAlbum = album;

            // Fetch album cover if not.
            // Use a tmp collection to use existing method.
            var tmpCollection = new ObservableCollection<AlbumEx> { album };
            GetAlbumPictures(tmpCollection);
        });
    }

    public void GoBackFromAlbumDetailsPage(AlbumEx album)
    {
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            //
            await Task.Yield();
            //await Task.Delay(500);

            // scroll to the item (required a little trick to actually show images)
            AlbumScrollIntoView?.Invoke(this, album);
        });
    }

    [RelayCommand]
    public void ListviewGoToArtistPage(SongInfo song)
    {
        if (song is null)
        {
            return;
        }

        var asdf = song.AlbumArtist;
        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            asdf = song.Artist;
        }

        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            return;
        }

        var item = Artists.FirstOrDefault(i => i.Name == asdf);
        if (item is null) return;
        GoToArtistPage();
        SelectedAlbumArtist = item;
    }

    private void GoToArtistPage()
    {
        IsNavigationViewMenuOpen = true;
        _mainMenuItems.ArtistsDirectory.Selected = true;
        //GoToSelectedPage?.Invoke(this, _mainMenuItems.ArtistsDirectory);
        /*
        foreach (var hoge in MainMenuItems)
        {
            if (hoge is not NodeMenuLibrary) continue;
            foreach (var fuga in hoge.Children)
            {
                if (fuga is not NodeMenuArtist) continue;
                IsNavigationViewMenuOpen = true;
                fuga.Selected = true;
                break;
            }
        }
        */
    }

    [RelayCommand]
    public void CurrentSongToAlbumPage()
    {
        if (CurrentSong is null)
        {
            return;
        }
        if (string.IsNullOrEmpty(CurrentSong.Album.Trim()))
        {
            return;
        }
        var items = Albums.Where(i => i.Name == CurrentSong.Album);
        if (items is null) return;
        var asdf = CurrentSong.AlbumArtist;
        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            asdf = CurrentSong.Artist;
        }

        // no artist name
        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            var hoge = items.FirstOrDefault(x => x.Name == CurrentSong.Album);
            // found it
            if (hoge is not null)
            {
                //GoToAlbumPage();
                //SelectedAlbum = hoge;
                GoToAlbumDetailsPage(hoge);
            }
        }
        else
        {
            foreach (var item in items)
            {
                if (item.AlbumArtist != asdf) continue;
                // found it

                if (item is not null)
                {
                    //GoToAlbumPage();
                    //SelectedAlbum = item;
                    GoToAlbumDetailsPage(item);
                }
                break;
            }
        }
    }

    [RelayCommand]
    public void CurrentSongToArtistPage()
    {
        if (CurrentSong is null)
        {
            return;
        }

        var asdf = CurrentSong.AlbumArtist;
        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            asdf = CurrentSong.Artist;
        }

        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            return;
        }

        var item = Artists.FirstOrDefault(i => i.Name == asdf);
        if (item is null) return;
        GoToArtistPage();
        SelectedAlbumArtist = item;
    }

    [RelayCommand]
    public void SelectedAlbumGoToArtistPage(AlbumEx album)
    {
        if (album is null)
        {
            return;
        }

        var asdf = album.AlbumArtist;
        if (string.IsNullOrEmpty(asdf.Trim()))
        {
            return;
        }

        var item = Artists.FirstOrDefault(i => i.Name == asdf);
        if (item is null) return;
        GoToArtistPage();
        SelectedAlbumArtist = item;
    }

    [RelayCommand]
    public void SelectedAlbumArtistPlayAll(object obj)
    {
        if (obj is null) return;
        if (SelectedAlbumArtist is null) return;

        if (obj is not AlbumArtist selectedArtist)
        {
            return;
        }

        if (SelectedAlbumArtist != selectedArtist)
        {
            return;
        }

        var uriList = new List<string>();

        foreach (var hoge in selectedArtist.Albums)
        {
            if (hoge is null) continue;

            foreach (var song in hoge.Songs)
            {
                uriList.Add(song.File);
            }
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            Queue.Clear();
            CurrentSong = null;
            await _mpc.MpdMultiplePlay(uriList, Convert.ToInt32(_volume));
        });
    }


    [RelayCommand]
    public async Task SelectedAlbumArtistAddToQueue(object obj) 
    {
        if (obj is null) return;
        if (SelectedAlbumArtist is null) return;

        if (obj is not AlbumArtist selectedArtist)
        {
            return;
        }

        if (SelectedAlbumArtist != selectedArtist)
        {
            return;
        }

        var uriList = new List<string>();

        foreach (var hoge in selectedArtist.Albums)
        {
            if (hoge is null) continue;

            foreach (var song in hoge.Songs)
            {
                uriList.Add(song.File);
            }
        }

        if (uriList.Count > 1)
        {
            await _mpc.MpdAdd(uriList);
        }
        else if (uriList.Count == 1)
        {
            await _mpc.MpdAdd(uriList[0]);
        }
    }

    [RelayCommand]
    public async Task SelectedAlbumArtistAddToPlaylist(object obj)
    {
        if (obj is null) return;
        if (SelectedAlbumArtist is null) return;

        if (obj is not AlbumArtist selectedArtist)
        {
            return;
        }

        if (SelectedAlbumArtist != selectedArtist)
        {
            return;
        }

        var uriList = new List<string>();

        foreach (var hoge in selectedArtist.Albums)
        {
            if (hoge is null) continue;

            foreach (var song in hoge.Songs)
            {
                uriList.Add(song.File);
            }
        }

        if (uriList.Count > 0)
        {
            var result = await _dialogs.ShowAddToDialog(this);

            if (result is null)
            {
                return;
            }

            _ = AddToPlaylist(result.PlaylistName, uriList);
        }
    }

    [RelayCommand]
    public void FireDebugCommandClear()
    {
        DebugCommandClear?.Invoke(this,EventArgs.Empty);
    }

    [RelayCommand]
    public void FireDebugIdleClear()
    {
        DebugIdleClear?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    public void FireDebugWindowShowHide()
    {
        //DebugWindowShowHide?.Invoke(this, EventArgs.Empty);
        IsShowDebugWindow = !IsShowDebugWindow;
    }

    [RelayCommand]
    public void SelectedDirectoryPlayAll(object obj)
    {
        if (obj is null) return;

        if (obj is not ObservableCollection<NodeFile> list) return;
        if (list.Count <= 0) return;

        List<string> uriList = [];

        foreach (var song in list)
        {
            uriList.Add(song.OriginalFileUri);
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            Queue.Clear();
            CurrentSong = null;
            await _mpc.MpdMultiplePlay(uriList, Convert.ToInt32(_volume));
        });
    }

    [RelayCommand]
    public async Task SelectedDirectoryAddToQueue(object obj)
    {
        if (obj is null) return;

        if (obj is not ObservableCollection<NodeFile> list) return;
        switch (list.Count)
        {
            case > 1:
                {
                    List<string> uriList = [];

                    foreach (var song in list)
                    {
                        uriList.Add(song.OriginalFileUri);
                    }
                    //uriList.AddRange(list.Select(song => song.File));

                    await _mpc.MpdAdd(uriList);
                    break;
                }
            case 1 when (list[0] is NodeFile si):
                await _mpc.MpdAdd(si.OriginalFileUri);
                break;
        }
    }

    [RelayCommand]
    public async Task SelectedDirectoryAddToPlaylist(object obj)
    {
        if (obj is null) return;

        if (obj is ObservableCollection<NodeFile> list)
        {
            List<string> uriList = [];

            foreach (var item in list)
            {
                uriList.Add(item.OriginalFileUri);
            }

            if (uriList.Count > 0)
            {
                //SearchPageAddToPlaylistDialogShow?.Invoke(this, uriList);
                var result = await _dialogs.ShowAddToDialog(this);

                if (result is null)
                {
                    return;
                }

                _ = AddToPlaylist(result.PlaylistName, uriList);
            }
        }
    }

    [RelayCommand]
    public void FilesListviewPlayThis(object obj)
    {
        if (obj is null) return;

        if (obj is not NodeFile song)
        {
            return;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            Queue.Clear();
            CurrentSong = null;

            await _mpc.MpdSinglePlay(song.OriginalFileUri, Convert.ToInt32(_volume));
        });
    }

    [RelayCommand]
    public async Task FilesListviewAddThis(object obj)
    {
        if (obj is null) return;

        if (obj is NodeFile song)
        {
            await _mpc.MpdAdd(song.OriginalFileUri);
        }
    }

    [RelayCommand]
    public void FilesSelectedItemsPlay(System.Collections.Generic.IList<object> obj)
    {
        if (obj is null)
        {
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @FilesSelectedItemsPlay");
            return;
        }

        var collection = items.Cast<NodeFile>();

        List<string> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.OriginalFileUri);
        }

        if (selectedList.Count == 0)
        {
            return;
        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
            Queue.Clear();
            CurrentSong = null;
            await _mpc.MpdMultiplePlay(selectedList, Convert.ToInt32(_volume));
        });
    }

    [RelayCommand]
    public async Task FilesAddSelectedItemsToPlaylist(System.Collections.Generic.IList<object> obj)
    {
        if (obj is null)
        {
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @FilesAddSelectedItemsToPlaylist");
            return;
        }

        var collection = items.Cast<NodeFile>();

        List<string> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.OriginalFileUri);
        }

        if (selectedList.Count == 0)
        {
            return;
        }

        var result = await _dialogs.ShowAddToDialog(this);

        if (result is null)
        {
            return;
        }

        _ = AddToPlaylist(result.PlaylistName, selectedList);
    }

    [RelayCommand]
    public async Task FilesAddSelectedItemsToQueue(System.Collections.Generic.IList<object> obj)
    {
        if (obj is null)
        {
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @FilesAddSelectedItemsToQueue");
            return;
        }

        var collection = items.Cast<NodeFile>();

        List<string> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.OriginalFileUri);
        }

        if (selectedList.Count == 1)
        {
            await _mpc.MpdAdd(selectedList[0]);
        }
        else if (selectedList.Count > 1)
        {
            await _mpc.MpdAdd(selectedList);
        }
    }

    [RelayCommand]
    public async Task ClearQueueAndLoadPlaylist(string playlistName)
    {
        if (IsBusy) return;
        if (IsWorking) return;
        if (SelectedNodeMenu is null)
            return;
        if (SelectedNodeMenu is not NodeMenuPlaylistItem)
            return;

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            Queue.Clear();
            CurrentSong = null;
        });

        await _mpc.MpdChangePlaylist(SelectedNodeMenu.Name, Convert.ToInt32(_volume));

        await Task.Yield();
        await Task.Delay(200);
        UpdateCurrentSong();
    }

    [RelayCommand]
    public async Task LoadPlaylist()
    {
        if (IsBusy) return;
        if (IsWorking) return;
        if (SelectedNodeMenu is null)
            return;
        if (SelectedNodeMenu is not NodeMenuPlaylistItem)
            return;

        await _mpc.MpdLoadPlaylist(SelectedNodeMenu.Name);
    }

    [RelayCommand]
    public async Task RenamePlaylist(string playlist)
    {
        if (string.IsNullOrEmpty(_selectedPlaylistName))
        {
            Debug.WriteLine("(string.IsNullOrEmpty(_selectedPlaylistName))");
            return;
        }

        if (!_selectedPlaylistName.Equals(playlist, StringComparison.CurrentCulture))
        {
            Debug.WriteLine($"({_selectedPlaylistName} != {playlist})");
            return;
        }

        var result = await _dialogs.ShowRenameToDialog(this);

        if (result is null)
        {
            Debug.WriteLine("if (result is null)");
            return;
        }

        if (CheckIfPlaylistExists(result.PlaylistName))
        {
            if (App.MainWnd is null)
            {
                return;
            }

            if (App.MainWnd?.Content is not ShellPage)
            {
                return;
            }

            var resultHint = new ContentDialog()
            {
                XamlRoot = App.MainWnd?.Content.XamlRoot,
                Content = _resourceLoader.GetString("Dialog_PlaylistNameAlreadyExists"),//$"Playlist \"{plname}\" already exists.", //
                Title = result.PlaylistName,
                PrimaryButtonText = "OK"
            };

            _ = resultHint.ShowAsync();

            return;
        }

        var ret = await _mpc.MpdRenamePlaylist(_selectedPlaylistName, result.PlaylistName);

        if (ret.IsSuccess)
        {
            //SelectedPlaylistName = newPlaylistName;
            RenamedSelectPendingPlaylistName = result.PlaylistName;

            // This is not going to work because renamed listviewitem is not yet created.
            //GoToRenamedPlaylistPage(newPlaylistName);
        }
    }

    // CheckPlaylistNameExists when Rename playlists.
    public bool CheckIfPlaylistExists(string playlistName)
    {
        bool match = false;

        if (Playlists.Count > 0)
        {
            foreach (var hoge in Playlists)
            {
                if (string.Equals(playlistName, hoge.Name, StringComparison.CurrentCultureIgnoreCase))
                {
                    match = true;
                    break;
                }
            }
        }

        return match;
    }

    [RelayCommand]
    public async Task RemovePlaylist(string playlist)
    {
        if (string.IsNullOrEmpty(_selectedPlaylistName))
        {
            return;
        }

        if (_selectedPlaylistName != playlist)
        {
            return;
        }

        var ret = await _mpc.MpdRemovePlaylist(_selectedPlaylistName);

        if (ret.IsSuccess)
        {
            SelectedPlaylistName = string.Empty;
            RenamedSelectPendingPlaylistName = string.Empty;
            // Clear listview 
            PlaylistSongs.Clear();

            _playlistPageSubTitleSongCount = string.Empty;
            OnPropertyChanged(nameof(PlaylistPageSubTitleSongCount));
        }
    }

    [RelayCommand]
    public async Task ClearPlaylist(string playlist)
    {
        if (string.IsNullOrEmpty(_selectedPlaylistName))
        {
            return;
        }

        if (_selectedPlaylistName != playlist)
        {
            return;
        }

        var ret = await _mpc.MpdPlaylistClear(playlist);

        if (ret.IsSuccess)
        {
            PlaylistSongs.Clear();
        }
    }

    [RelayCommand]
    public async Task PlaylistRemoveSelectedItem(object obj)
    {
        if (obj is null)
        {
            Debug.WriteLine("_obj is null @PlaylistRemoveSelectedItem");
            return;
        }

        if (string.IsNullOrEmpty(_selectedPlaylistName))
        {
            Debug.WriteLine("string.IsNullOrEmpty(_selectedPlaylistName) @PlaylistRemoveSelectedItem");
            return;
        }

        if (obj is not IList<object> items)
        {
            Debug.WriteLine("obj is not IList<object> @PlaylistRemoveSelectedItems");
            return;
        }

        var collection = items.Cast<SongInfo>();

        List<int> selectedList = [];

        foreach (var item in collection)
        {
            selectedList.Add(item.Index);
        }

        if (selectedList.Count <= 0)
        {
            return;
        }

        if (SelectedNodeMenu is NodeMenuPlaylistItem nmpli)
        {
            if (_selectedPlaylistName != nmpli.Name)
            {
                Debug.WriteLine("_selectedPlaylistName != nmpli.Name @PlaylistRemoveSelectedItem");
                return;
            }

            if (nmpli.IsUpdateRequied)
            {
                Debug.WriteLine("nmpli.IsUpdateRequied @PlaylistRemoveSelectedItem");
                return;
            }
            else
            {
                await _mpc.MpdPlaylistDelete(_selectedPlaylistName, selectedList[0]);
                /*
                if (obj is SongInfo song)
                {
                    await _mpc.MpdPlaylistDelete(_selectedPlaylistName, song.Index);
                }
                else
                {
                    Debug.WriteLine("(obj is NOT SongInfo song) @PlaylistRemoveSelectedItem");
                }
                */
            }
        }
        else
        {
            Debug.WriteLine("SelectedNodeMenu is NOT NodeMenuPlaylistItem nmpli @PlaylistRemoveSelectedItem");
            return;
        }
    }

    [RelayCommand]
    public void ReConnectWithSelectedProfile()
    {
        Debug.WriteLine("ReConnectWithSelectedProfile");
        if (IsBusy) return;
        if (IsConnecting) return;
        if (SelectedProfile is null) return;

        IsBusy = true;
        IsWorking = true;

        // Disconnect if connected.
        if (IsConnected)
        {
            _mpc.MpdStop = true;
            _mpc.MpdDisconnect(true);
            _mpc.MpdStop = false;
        }

        // Save volume.
        SelectedProfile.Volume = Convert.ToInt32(Volume);
        // Set current.
        CurrentProfile = SelectedProfile;

        // Clearing values
        MpdVersion = string.Empty;
        CurrentSong = null;
        SelectedQueueSong = null;

        SelectedNodeMenu = null;

        Queue.Clear();
        _mpc.CurrentQueue.Clear();

        _mpc.MpdStatus.Reset();

        _mainMenuItems.PlaylistsDirectory?.Children.Clear();

        Playlists.Clear();
        _mpc.Playlists.Clear();

        //SelectedPlaylistSong = null;

        if (_mainMenuItems.FilesDirectory is not null)
            _mainMenuItems.FilesDirectory.IsAcquired = false;

        MusicEntries.Clear();

        _musicDirectories.IsCanceled = true;
        if (_musicDirectories.Children.Count > 0)
            _musicDirectories.Children[0].Children.Clear();
        MusicDirectories.Clear();

        FilterMusicEntriesQuery = "";

        SearchResult?.Clear();
        SearchQuery = "";

        SelectedPlaylistName = string.Empty;
        //SelectedPlaylistSong = null;
        SelectedAlbum = null;
        SelectedAlbumArtist = null;
        //SelectedAlbumSongs = [];
        SelectedArtistAlbums = null;
        SelectedAlbumArtist = null;

        // TODO: more?

        //IsAlbumArtVisible = false;
        AlbumArtBitmapSource = _albumArtBitmapSourceDefault;

        _ = Task.Run(() => Start(_host, _port));
        /*
        ConnectionResult r = await _mpc.MpdIdleConnect(_host, _port);

        if (r.IsSuccess)
        {
            //CurrentProfile = prof;

            if (SelectedNodeMenu?.Children.Count > 0)
            {

                SelectedNodeMenu = MainMenuItems[0];
            }
        }
        */

        //IsSwitchingProfile = false;
        IsBusy = false;
        IsWorking = false;
    }

    [RelayCommand]
    public async Task ShowProfileEditDialog()
    {
        Debug.WriteLine("ShowProfileEditDialog");

        if (SelectedProfile is null)
        {
            return;
        }

        var res = await _dialogs.ShowProfileEditDialog(SelectedProfile);

        if (res is null)
        {
            return;
        }

        SelectedProfile = res;

        if (SelectedProfile.IsDefault)
        {
            foreach (var hoge in Profiles)
            {
                hoge.IsDefault = false;
            }

            SelectedProfile.IsDefault = true;
        }
        else
        {
            var fuga = Profiles.FirstOrDefault(x => x.IsDefault == true);
            if (fuga is null)
            {
                if (Profiles.Count > 0)
                {
                    Profiles[0].IsDefault = true;
                }
            }
        }
    }

    [RelayCommand]
    public void ShowProfileRemoveNoneDialog()
    {
        Debug.WriteLine("ShowProfileRemoveNoneDialog");
        if (Profiles.Count <= 0)
        {
            return;
        }

        if (SelectedProfile is null)
        {
            return;
        }

        bool isDefault = SelectedProfile.IsDefault;

        if (Profiles.Remove(SelectedProfile))
        {
            SelectedProfile = null;

            if (Profiles.Count > 0)
            {
                if (isDefault)
                {
                    Profiles[0].IsDefault = true;
                }

                SelectedProfile = Profiles[0];
            }
        }

        OnPropertyChanged(nameof(Profiles));

        if (Profiles.Count == 0)
        {
            IsConnectButtonEnabled = false;
        }
    }

    [RelayCommand]
    public async Task ShowProfileAddDialog()
    {
        Debug.WriteLine("ShowProfileAddDialog");
        var pro = await _dialogs.ShowProfileAddDialog();

        if (pro is null)
        {
            return;
        }

        if (pro.IsDefault)
        {
            foreach (var hoge in Profiles)
            {
                hoge.IsDefault = false;
            }
        }
        else
        {
            var fuga = Profiles.FirstOrDefault(x => x.IsDefault == true);
            if (fuga is null)
            {
                pro.IsDefault = true;
            }
        }

        Profiles.Add(pro);
        OnPropertyChanged(nameof(Profiles));
        SelectedProfile = pro;

        if (Profiles.Count > 0)
        {
            IsConnectButtonEnabled = true;
        }
    }

    #endregion
}
