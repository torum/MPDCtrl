using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Animations;
using Microsoft.Extensions.Hosting;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
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
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
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

            //SelectedProfile = _currentProfile;

            if (_currentProfile is not null)
            {
                _volume = _currentProfile.Volume;
                OnPropertyChanged(nameof(Volume));

                Host = _currentProfile.Host;
                Port = _currentProfile.Port.ToString();
                _password = _currentProfile.Password;
            }
        }
    }

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

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(PlayButton));
            });
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
                return;
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

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                OnPropertyChanged(nameof(Time));
                OnPropertyChanged(nameof(TimeFormatted));
            });
        }
    }

    public string TimeFormatted
    {
        get
        {
            int sec, min, hour, s;

            sec = Time / 10;

            min = sec / 60;
            s = sec % 60;
            hour = min / 60;
            min %= 60;
            return string.Format("{0}:{1:00}:{2:00}", hour, min, s);
        }
    }

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

            sec = _elapsed / 10;

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

    private readonly System.Timers.Timer _elapsedTimer = new(100);
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

            if (value is null)
            {
                return;
            }

            if (value is NodeMenuQueue)
            {

            }
            else if (value is NodeMenuSearch)
            {

            }
            else if (value is NodeMenuArtist)
            {
                if ((Artists.Count > 0) && (SelectedAlbumArtist is null))
                {
                    SelectedAlbumArtist = Artists[0];
                }
            }
            else if (value is NodeMenuAlbum)
            {

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
                SelectedPlaylistSong = null;
                PlaylistSongs = nmpli.PlaylistSongs;
                SelectedPlaylistName = nmpli.Name;

                if ((nmpli.PlaylistSongs.Count == 0) || nmpli.IsUpdateRequied)
                {
                    GetPlaylistSongs(nmpli);
                }
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
        return song.Title.Contains(FilterQueueQuery, StringComparison.InvariantCultureIgnoreCase);
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
        }
    }

    private AlbumArtist? _selectedAlbumArtist;
    public AlbumArtist? SelectedAlbumArtist
    {
        get { return _selectedAlbumArtist; }
        set
        {
            if (_selectedAlbumArtist != value)
            {
                _selectedAlbumArtist = value;

                OnPropertyChanged(nameof(SelectedAlbumArtist));

                SelectedArtistAlbums = _selectedAlbumArtist?.Albums;

                GetArtistSongs(_selectedAlbumArtist);

                GetAlbumPictures(SelectedArtistAlbums);
            }
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

    private bool _isAlbumContentPanelVisible = false;
    public bool IsAlbumContentPanelVisible
    {
        get { return _isAlbumContentPanelVisible; }
        set
        {
            if (_isAlbumContentPanelVisible == value)
                return;

            _isAlbumContentPanelVisible = value;
            OnPropertyChanged(nameof(IsAlbumContentPanelVisible));
        }
    }

    private AlbumEx? _selectedAlbum = new();
    public AlbumEx? SelectedAlbum
    {
        get { return _selectedAlbum; }
        set
        {
            if (_selectedAlbum == value)
                return;

            _selectedAlbum = value;
            OnPropertyChanged(nameof(SelectedAlbum));
            OnPropertyChanged(nameof(SelectedAlbumSongs));
        }
    }

    private ObservableCollection<SongInfo>? _selectedAlbumSongs = [];
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
        set
        {
            if (_selectedAlbumSongs == value)
                return;

            _selectedAlbumSongs = value;
            OnPropertyChanged(nameof(SelectedAlbumSongs));
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

    private ObservableCollection<Object> _visibleItemsAlbumsEx = [];
    public ObservableCollection<Object> VisibleItemsAlbumsEx
    {
        get => _visibleItemsAlbumsEx;
        set
        {
            if (_visibleItemsAlbumsEx == value)
                return;

            _visibleItemsAlbumsEx = value;
            OnPropertyChanged(nameof(VisibleItemsAlbumsEx));
            
            if (_visibleItemsAlbumsEx is null)
            {
                return;
            }

            GetAlbumPictures(VisibleItemsAlbumsEx);
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

    #region == Playlist Items ==

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

    private SongInfo? _selectedPlaylistSong;
    public SongInfo? SelectedPlaylistSong
    {
        get
        {
            return _selectedPlaylistSong;
        }
        set
        {
            if (_selectedPlaylistSong != value)
            {
                _selectedPlaylistSong = value;
                OnPropertyChanged(nameof(SelectedPlaylistSong));
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

    #region == Events ==

    // Queue listview ScrollIntoView
    public event EventHandler<object>? ScrollIntoView;

    // Queue listview ScrollIntoView and select (for filter and first time loading the queue)
    public event EventHandler<object>? ScrollIntoViewAndSelect;

    public event EventHandler<string>? UpdateProgress;

    #endregion

    private readonly ResourceLoader _resourceLoader = new();

    private readonly IMpcService _mpc;

    public MainViewModel(IMpcService mpcService)
    {
        _mpc = mpcService;

        InitializeAndSubscribe();


#if DEBUG
        //IsDebugWindowEnabled = true;
#else
        //IsDebugWindowEnabled = false;
#endif
    }

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

    public void StartMPC()
    {
        _password = "hoge";
        Start("127.0.0.1", 6600);
    }

    private async void Start(string host, int port)
    {
        HostIpAddress = null;
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(host);
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

    private async void LoadInitialData()
    {
        IsBusy = true;

        await Task.Delay(5);
        await Task.Yield();

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

                    await Task.Delay(50);
                    UpdateCurrentSong();

                    await Task.Delay(50);
                    await _mpc.MpdIdleQueryPlaylists();

                    await Task.Delay(50);
                    UpdatePlaylists();

                    await Task.Delay(50);
                    await _mpc.MpdIdleQueryCurrentQueue();

                    await Task.Delay(50);
                    UpdateCurrentQueue();

                    await Task.Delay(300);
                    await _mpc.MpdQueryListAlbumArtists();
                    UpdateAlbumsAndArtists();

                    await Task.Delay(50);

                    // Idle start.
                    _mpc.MpdIdleStart();
                }

            }
        }

        IsBusy = false;

        await Task.Delay(500);

        // MPD protocol ver check.
        if (_mpc.MpdVerText != "")
        {
            if (CompareVersionString(_mpc.MpdVerText, "0.20.0") == -1)
            {
                // TODO:
                //MpdStatusButton = _pathMpdAckErrorButton;
                //StatusBarMessage = string.Format(MPDCtrlX.Properties.Resources.StatusBarMsg_MPDVersionIsOld, _mpc.MpdVerText);
                //MpdStatusMessage = string.Format(MPDCtrlX.Properties.Resources.StatusBarMsg_MPDVersionIsOld, _mpc.MpdVerText);
            }
        }
    }

    private void UpdateStatus()
    {
        UpdateButtonStatus();

        UpdateProgress?.Invoke(this, "[UI] Status updating...");

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
        {
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
                                                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
                                                {
                                                    AlbumCover = res.AlbumCover;
                                                    AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                                    //IsAlbumArtVisible = true;
                                                    SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                                    CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                                });
                                            }
                                        }
                                    }
                                }
                            }
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
        });

        UpdateProgress?.Invoke(this, "");
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

                if (_mpc.MpdStatus.MpdVolumeIsSet)
                {
                    //Debug.WriteLine($"Volume is set to {_mpc.MpdStatus.MpdVolume}. @UpdateButtonStatus()");

                    double tmpVol = Convert.ToDouble(_mpc.MpdStatus.MpdVolume);
                    if (_volume != tmpVol)
                    {
                        // "quietly" update.
                        _volume = tmpVol;
                        OnPropertyChanged(nameof(Volume));

                        // save
                        if (_currentProfile is not null)
                        {
                            _currentProfile.Volume = _volume;
                        }
                    }
                }
                else
                {
                    //Debug.WriteLine("Volume is NOT set. @UpdateButtonStatus()");

                    if (_currentProfile is not null)
                    {
                        //Debug.WriteLine($"Volume is set to _currentProfile {_currentProfile.Volume}. @UpdateButtonStatus()");
                        //_volume = _currentProfile.Volume;
                    }
                    else
                    {
                        //Debug.WriteLine("Volume is set to default 20. @UpdateButtonStatus()");
                        //_volume = 20; // default volume.
                    }
                    //NotifyPropertyChanged(nameof(Volume));
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
                    Time *= 10;
                    _elapsed = Convert.ToInt32(_mpc.MpdStatus.MpdSongElapsed);
                    _elapsed *= 10;
                    if (!_elapsedTimer.Enabled)
                        _elapsedTimer.Start();
                }
                else
                {
                    _elapsedTimer.Stop();

                    // no need to care about "double" updates for time.
                    Time = Convert.ToInt32(_mpc.MpdStatus.MpdSongTime);
                    Time *= 10;
                    _elapsed = Convert.ToInt32(_mpc.MpdStatus.MpdSongElapsed);
                    _elapsed *= 10;
                    OnPropertyChanged(nameof(Elapsed));
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
        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
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
            }

            if (_mpc.MpdCurrentSong != null)
            {
                if (_mpc.MpdCurrentSong.Id == _mpc.MpdStatus.MpdSongID)
                {
                    if (isSongChanged || isCurrentSongWasNull)
                    {
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
                                        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
                                        {
                                            AlbumCover = res.AlbumCover;
                                            //AlbumArtBitmapSource = AlbumCover.AlbumImageSource;
                                            AlbumArtBitmapSource = await BitmapSourceFromByteArray(AlbumCover.BinaryData);
                                            if (CurrentSong is not null)
                                            {
                                                SaveAlbumCoverImage(CurrentSong, res.AlbumCover);
                                                CurrentSong.IsAlbumCoverNeedsUpdate = false;
                                            }
                                        });
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Debug.WriteLine("if (isSongChanged || isCurrentSongWasNull). @UpdateCurrentSong()");
                    }
                }
                else
                {
                    Debug.WriteLine($"{_mpc.MpdCurrentSong.Id} != {_mpc.MpdStatus.MpdSongID}. @UpdateCurrentSong()");
                }
            }
            else
            {
                Debug.WriteLine("_mpc.MpdCurrentSong is null. @UpdateCurrentSong()");
            }
        });
    }

    private async void UpdateCurrentQueue()
    {
        IsQueueFindVisible = false;

        if (Queue.Count > 0)
        {
            UpdateProgress?.Invoke(this, "[UI] Updating the queue...");
            await Task.Delay(20);

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsWorking = true;
            });

            try
            {
                // The simplest way, but all the selections and listview position will be cleared. Kind of annoying when moving items.
                #region == simple & fast == 
                /*
                UpdateProgress?.Invoke(this, "[UI] Loading the queue...");

                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    Queue = new ObservableCollection<SongInfoEx>(_mpc.CurrentQueue);

                    UpdateProgress?.Invoke(this, "[UI] Checking current song after Queue update.");

                    // Set Current and NowPlaying.
                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem is not null)
                    {
                        if (CurrentSong is not null)
                        {
                            if (CurrentSong.Id != curitem.Id)
                            {
                                CurrentSong = curitem;
                                CurrentSong.IsPlaying = true;

                                if (IsAutoScrollToNowPlaying)
                                    // ScrollIntoView while don't change the selection 
                                    ScrollIntoView?.Invoke(this, CurrentSong.Index);

                                // AlbumArt


                            }
                            else
                            {
                                curitem.IsPlaying = true;

                                if (IsAutoScrollToNowPlaying)
                                    // ScrollIntoView while don't change the selection 
                                    ScrollIntoView?.Invoke(this, curitem.Index);

                            }
                        }
                    }
                    else
                    {
                        CurrentSong = null;

                        //IsAlbumArtVisible = false;
                        AlbumArtBitmapSource = _albumArtBitmapSourceDefault;
                    }

                    UpdateProgress?.Invoke(this, "");

                    IsWorking = false;

                    UpdateProgress?.Invoke(this, "");

                });
                */
                #endregion

                #region == better way ==

                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
                {
                    IsWorking = true;

                    UpdateProgress?.Invoke(this, "[UI] Updating the queue...");

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
                    }

                    // Sorting.
                    // Sort here because Queue list may have been re-ordered.
                    UpdateProgress?.Invoke(this, "[UI] Queue list sorting...");

                    // AvaloniaUI doesn't have this.
                    //var collectionView = CollectionViewSource.GetDefaultView(Queue);
                    // no need to add because It's been added when "loading".
                    //collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                    //collectionView.Refresh();

                    // This is not good, all the selections will be cleared, but no problem?.
                    Queue = new ObservableCollection<SongInfoEx>(Queue.OrderBy(n => n.Index));
                    //UpdateProgress?.Invoke(this, "");

                    UpdateProgress?.Invoke(this, "[UI] Checking current song after Queue update.");

                    // Set Current and NowPlaying.
                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem is not null)
                    {
                        CurrentSong = curitem;
                        CurrentSong.IsPlaying = true;

                        /*
                        // Don't. because it's not like song is changed.
                        if (IsAutoScrollToNowPlaying)
                        {
                            // ScrollIntoView while don't change the selection 
                            ScrollIntoView?.Invoke(this, CurrentSong.Index);
                        }
                        */
                        
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
                });

                #endregion
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@UpdateCurrentQueue: " + e.Message);

                UpdateProgress?.Invoke(this, "Exception@UpdateCurrentQueue: " + e.Message);

                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    IsWorking = false;
                    App.AppendErrorLog("Exception@UpdateCurrentQueue", e.Message);
                });

                return;
            }
            finally
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    IsWorking = false;
                });
                UpdateProgress?.Invoke(this, "");
            }

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsWorking = false;
            });
        }
        else
        {
            UpdateProgress?.Invoke(this, "[UI] Loading the queue...");
            await Task.Delay(20);
            /*
            if (IsSwitchingProfile)
                return;
            */

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                IsWorking = true;
            });

            try
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(async () =>
                {
                    IsWorking = true;

                    UpdateProgress?.Invoke(this, "[UI] Loading the queue...");

                    Queue = new ObservableCollection<SongInfoEx>(_mpc.CurrentQueue);

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

                            // Testing for WinUI3.
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
                });
            }
            catch (Exception e)
            {
                Debug.WriteLine("Exception@UpdateCurrentQueue: " + e.Message);

                StatusBarMessage = "Exception@UpdateCurrentQueue: " + e.Message;

                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    IsWorking = false;

                    App.AppendErrorLog("Exception@UpdateCurrentQueue", e.Message);
                });

                return;
            }
            finally
            {
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    IsWorking = false;
                });
                UpdateProgress?.Invoke(this, "");
            }


        }

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            IsWorking = false;
        });
    }

    private async void UpdatePlaylists()
    {
        UpdateProgress?.Invoke(this, "[UI] Playlists loading...");

        await Task.Delay(10);

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
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
                            //GoToJustPlaylistPage(nmpli);
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
            /*
            if (!string.IsNullOrEmpty(RenamedSelectPendingPlaylistName))
            {
                GoToRenamedPlaylistPage(RenamedSelectPendingPlaylistName);
                RenamedSelectPendingPlaylistName = string.Empty;
            }
            */
        });
    }

    private Task<bool> UpdateLibraryMusicAsync()
    {
        // Music files
        /*
        if (IsSwitchingProfile)
            return Task.FromResult(false);
        */

        UpdateProgress?.Invoke(this, "[UI] Library songs loading...");

        //IsBusy = true;
        IsWorking = true;

        /*
        Dispatcher.UIThread.Post(() => {
            MusicEntries.Clear();
        });
        */

        var tmpMusicEntries = new ObservableCollection<NodeFile>();

        foreach (var songfile in _mpc.LocalFiles)
        {
            /*
            if (IsSwitchingProfile)
                break;
            */
            //await Task.Delay(5);

            //if (IsSwitchingProfile)
            //    break;

            //IsBusy = true;
            IsWorking = true;

            if (string.IsNullOrEmpty(songfile.File)) continue;

            try
            {
                Uri uri = new(@"file:///" + songfile.File);
                if (uri.IsFile)
                {
                    string filename = System.IO.Path.GetFileName(songfile.File);//System.IO.Path.GetFileName(uri.LocalPath);
                    NodeFile hoge = new(filename, uri, songfile.File);
                    /*
                    
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MusicEntries.Add(hoge);
                    });
                    */
                    tmpMusicEntries.Add(hoge);
                }
                /*
                if (IsSwitchingProfile)
                    break;
                */
            }
            catch (Exception e)
            {
                Debug.WriteLine(songfile + e.Message);

                //Application.Current?.Dispatcher.Invoke(() => { (App.Current as App)?.AppendErrorLog("Exception@UpdateLibraryMusic", e.Message); });
                App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
                {
                    //IsBusy = false;
                    IsWorking = false;
                    App.AppendErrorLog("Exception@UpdateLibraryMusic", e.Message);
                });
                return Task.FromResult(false);
            }
        }
        /*
        if (IsSwitchingProfile)
            return Task.FromResult(false);
        */
        //IsBusy = true;
        IsWorking = true;


        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() => {
            UpdateProgress?.Invoke(this, "[UI] Library songs loading...");
            MusicEntries = new ObservableCollection<NodeFile>(tmpMusicEntries);// COPY

            _musicEntriesFiltered = _musicEntriesFiltered = new ObservableCollection<NodeFile>(tmpMusicEntries);
            OnPropertyChanged(nameof(MusicEntriesFiltered));

            UpdateProgress?.Invoke(this, "");
            //IsBusy = false;
            IsWorking = false;
        });

        //IsBusy = false;
        IsWorking = false;
        return Task.FromResult(true);
    }

    private Task<bool> UpdateLibraryDirectoriesAsync()
    {
        // Directories
        /*
        if (IsSwitchingProfile)
            return Task.FromResult(false);
        */

        UpdateProgress?.Invoke(this, "[UI] Library directories loading...");

        //IsBusy = true;
        IsWorking = true;

        try
        {
            var tmpMusicDirectories = new DirectoryTreeBuilder("");
            //tmpMusicDirectories.Load([.. _mpc.LocalDirectories]);
            //_musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());
            tmpMusicDirectories.Load(_mpc.LocalDirectories);

            IsWorking = true;

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() => 
            {

                UpdateProgress?.Invoke(this, "[UI] Library directories loading...");

                MusicDirectories = new ObservableCollection<NodeTree>(tmpMusicDirectories.Children);// COPY
                if (MusicDirectories.Count > 0)
                {
                    if (MusicDirectories[0] is NodeDirectory nd)
                    {
                        _selectedNodeDirectory = nd;
                        OnPropertyChanged(nameof(SelectedNodeDirectory));
                    }
                }

                /*
                MusicDirectoriesSource = new HierarchicalTreeDataGridSource<NodeTree>(tmpMusicDirectories.Children)
                {
                    Columns =
                    {
                        new HierarchicalExpanderColumn<NodeTree>(
                            new TextColumn<NodeTree, string>("Directory", x => x.Name),
                            x => x.Children)
                    },
                };
                MusicDirectoriesSource.Expand(0);
                */

            });
            UpdateProgress?.Invoke(this, "");

            //IsBusy = false;
            IsWorking = false;
        }
        catch (Exception e)
        {
            Debug.WriteLine("_musicDirectories.Load: " + e.Message);

            App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
            {
                //IsBusy = false;
                IsWorking = false;
                App.AppendErrorLog("Exception@UpdateLibraryDirectories", e.Message);
            });
            return Task.FromResult(false);
        }
        finally
        {
            //IsBusy = false;
            IsWorking = false;
        }

        //IsBusy = false;
        IsWorking = false;
        UpdateProgress?.Invoke(this, "");

        return Task.FromResult(true);
    }

    private void GetFiles(NodeMenuFiles filestNode)
    {
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
            await Task.Delay(10);
            //await Task.Yield();

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
        });
    }

    private void UpdateAlbumsAndArtists()
    {
        UpdateProgress?.Invoke(this, "[UI] Updating the AlbumArtists...");
        Artists = new ObservableCollection<AlbumArtist>(_mpc.AlbumArtists);// COPY. //.OrderBy(x => x.Name, comp)

        UpdateProgress?.Invoke(this, "[UI] Updating the Albums...");
        Albums = new ObservableCollection<AlbumEx>(_mpc.Albums); // COPY. // Sort .OrderBy(x => x.Name, comp)

        UpdateProgress?.Invoke(this, "");
    }


    private async void GetArtistSongs(AlbumArtist? artist)
    {
        if (artist is null)
        {
            Debug.WriteLine("GetArtistSongs: artist is null, returning.");
            return;
        }

        var r = await SearchArtistSongs(artist.Name).ConfigureAwait(ConfigureAwaitOptions.None);

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
            UpdateProgress?.Invoke(this, "");
        });

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

        App.MainWnd?.CurrentDispatcherQueue?.TryEnqueue(() =>
        {
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
                    if (song.Album.Equals(slbm.Name))
                    {
                        slbm.Songs?.Add(song);
                    }
                }

                slbm.IsSongsAcquired = true;
            }
        });
    }

    private async void GetAlbumPictures(IEnumerable<object>? AlbumExItems)
    {
        if (AlbumExItems is null)
        {
            Debug.WriteLine("GetAlbumPictures: (AlbumExItems is null)");
            return;
        }

        if (Albums.Count < 1)
        {
            Debug.WriteLine("GetAlbumPictures: Albums.Count < 1, returning.");
            return;
        }

        //UpdateProgress?.Invoke(this, "[UI] Loading album covers ...");
        //IsBusy = true;
        //IsWorking = true;

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

            var strArtist = EscapeFilePathNames(album.AlbumArtist).Trim();
            var strAlbum = EscapeFilePathNames(album.Name).Trim();
            if (string.IsNullOrEmpty(strArtist))
            {
                strArtist = "Unknown Artist";
            }

            string filePath = System.IO.Path.Combine(App.AppDataCacheFolder, System.IO.Path.Combine(strArtist, strAlbum)) + ".bmp";

            if (File.Exists(filePath))
            {
                try
                {
                    BitmapImage? bitmap = new(new Uri(filePath));
                    album.AlbumImage = bitmap;
                    album.IsImageAcquired = true;
                    album.IsImageLoading = false;
                }
                catch (Exception e)
                {
                    Debug.WriteLine("GetAlbumPictures: Exception while loading: " + filePath + Environment.NewLine + e.Message);
                    continue;
                }

                //Debug.WriteLine($"GetAlbumPictures: Successfully loaded album art from cache {filePath}");
            }
            else
            {
                string fileTempPath = System.IO.Path.Combine(App.AppDataCacheFolder, System.IO.Path.Combine(strArtist, strAlbum)) + ".tmp";
                string strDirPath = System.IO.Path.Combine(App.AppDataCacheFolder, strArtist);

                if (File.Exists(fileTempPath))
                {
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
                    Debug.WriteLine("GetAlbumPictures: ret.SearchResult is null, skipping.");
                    continue;
                }

                var sresult = new ObservableCollection<SongInfo>(ret.SearchResult);
                if (sresult.Count < 1)
                {
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
                    if (aat == album.AlbumArtist)
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

                        // Testing
                        //await Task.Delay(10);
                        //await Task.Yield();

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
                        await Task.Yield();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine("GetAlbumPictures: Exception while saving album art DUMMY file: " + e.Message);
                    }
                }


            }

        }

        //UpdateProgress?.Invoke(this, "");
        //IsBusy = false;
        //IsWorking = false;

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
        catch
        {
            Debug.WriteLine("Exception Bitmap.DecodeToWidth @BitmapSourceFromByteArray");

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

            // save album cover to cache.
            var strAlbum = current?.Album ?? string.Empty;
            if (!string.IsNullOrEmpty(strAlbum.Trim()))
            {
                var aat = current?.AlbumArtist.Trim();
                if (string.IsNullOrEmpty(aat))
                {
                    aat = current?.Artist.Trim();
                }
                var strArtist = aat;
                if (string.IsNullOrEmpty(strArtist))
                {
                    strArtist = "Unknown Artist";
                }
                strArtist = EscapeFilePathNames(strArtist).Trim();
                strAlbum = EscapeFilePathNames(strAlbum).Trim();

                string strDirPath = System.IO.Path.Combine(App.AppDataCacheFolder, strArtist);
                string filePath = System.IO.Path.Combine(App.AppDataCacheFolder, System.IO.Path.Combine(strArtist, strAlbum)) + ".bmp";
                try
                {
                    Directory.CreateDirectory(strDirPath);
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
            }
        });
    }

    private static string EscapeFilePathNames(string str)
    {
        string s = str.Replace("<", "LT");
        s = s.Replace(">", "GT");
        s = s.Replace(":", "COL");
        s = s.Replace("\"", "DQ");
        s = s.Replace("/", "FS");
        s = s.Replace("\\", "BS");
        s = s.Replace("/", "FS");
        s = s.Replace("|", "PIP");
        s = s.Replace("?", "QM");
        s = s.Replace("*", "ASTR");

        return s;
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

        string queryShiki = "==";
        var res = await _mpc.MpdSearch("Album", queryShiki, name); // No name.Trim() because of "=="

        if (!res.IsSuccess)
        {
            Debug.WriteLine("SearchAlbumSongs failed: " + res.ErrorMessage);
        }

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

        UpdateProgress?.Invoke(this, "");
        return res;
    }

    private static int CompareVersionString(string a, string b)
    {
        return (new System.Version(a)).CompareTo(new System.Version(b));
    }


    #region == MPD events == 

    private void OnMpdIdleConnected(MpcService sender)
    {
        Debug.WriteLine("OK MPD " + _mpc.MpdVerText + " @OnMpdConnected");

        MpdVersion = _mpc.MpdVerText;

        ////MpdStatusMessage = MpdVersion;// + ": " + MPDCtrlX.Properties.Resources.MPD_StatusConnected;

        //MpdStatusButton = _pathMpdOkButton;

        // 
        //IsConnected = true;

        IsShowAckWindow = false;
        IsShowErrWindow = false;

        /*
        // Connected from InitWindow, so save and clean up. 
        App.CurrentDispatcherQueue ?
        {
            if (_initWin is not null)
            {
                if (_initWin.IsActive || _initWin.IsVisible)
                {
                    if (IsRememberAsProfile)
                    {
                        var prof = new Profile
                        {
                            Name = _host,
                            Host = _host,
                            Port = _port,
                            Password = _password,
                            IsDefault = true,
                            Volume = _volume
                        };

                        if (!string.IsNullOrEmpty(prof.Host.Trim()))
                        {
                            CurrentProfile = prof;
                            Profiles.Add(prof);

                            NotifyPropertyChanged(nameof(IsCurrentProfileSet));
                        }
                        else
                        {
                            Debug.WriteLine("Host info is empty. @OnMpdIdleConnected");
                        }
                        Debug.WriteLine($"Password = {_password}");
                    }

                    Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _initWin.Close();
                    });
                }
            }
        });
        */
        // 
        _ = Task.Run(LoadInitialData);
    }

    private void OnMpdPlayerStatusChanged(MpcService sender)
    {
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
        /*
        if (IsShowDebugWindow)
        {
            Dispatcher.UIThread.Post(() =>
            {
                DebugCommandOutput?.Invoke(this, data);
            });
        }
        */
    }

    private void OnDebugIdleOutput(MpcService sender, string data)
    {
        /*
        if (IsShowDebugWindow)
        {
            Dispatcher.UIThread.Post(() =>
            {
                DebugIdleOutput?.Invoke(this, data);
            });
        }
        */
    }

    private void OnConnectionError(MpcService sender, string msg)
    {
        if (string.IsNullOrEmpty(msg))
            return;
        /*
        IsConnected = false;
        IsConnecting = false;
        IsConnectionSettingShow = true;

        ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_ConnectionError + ": " + msg;
        StatusButton = _pathErrorInfoButton;

        StatusBarMessage = ConnectionStatusMessage;
        */
    }

    private void OnConnectionStatusChanged(MpcService sender, MpcService.ConnectionStatus status)
    {
        if (status == MpcService.ConnectionStatus.NeverConnected)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
            IsConnectionSettingShow = true;
            StatusButton = _pathDisconnectedButton;
            */

            //ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_NeverConnected;
        }
        else if (status == MpcService.ConnectionStatus.Connected)
        {
            /*
            IsConnected = true;
            IsConnecting = false;
            IsNotConnectingNorConnected = false;
            IsConnectionSettingShow = false;
            StatusButton = _pathConnectedButton;
            */

            //ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Connected;
        }
        else if (status == MpcService.ConnectionStatus.Connecting)
        {
            /*
            IsConnected = false;
            IsConnecting = true;
            IsNotConnectingNorConnected = false;
            //IsConnectionSettingShow = true;
            StatusButton = _pathConnectingButton;
            */

            //ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Connecting;

            StatusBarMessage = ConnectionStatusMessage;
        }
        else if (status == MpcService.ConnectionStatus.ConnectFail_Timeout)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
            IsConnectionSettingShow = true;

            Debug.WriteLine("ConnectionStatus_ConnectFail_Timeout");
            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_ConnectFail_Timeout;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
        }
        else if (status == MpcService.ConnectionStatus.SeeConnectionErrorEvent)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
            IsConnectionSettingShow = true;
            StatusButton = _pathErrorInfoButton;
            */

            Debug.WriteLine("ConnectionStatus_SeeConnectionErrorEvent");
        }
        else if (status == MpcService.ConnectionStatus.Disconnected)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
            IsConnectionSettingShow = true;
            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Disconnected;
            StatusButton = _pathErrorInfoButton;
            StatusBarMessage = ConnectionStatusMessage;
            */

            Debug.WriteLine("ConnectionStatus_Disconnected");

        }
        else if (status == MpcService.ConnectionStatus.DisconnectedByHost)
        {
            // TODO: not really usued now...
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
            IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_DisconnectedByHost;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
            Debug.WriteLine("ConnectionStatus_DisconnectedByHost");
        }
        else if (status == MpcService.ConnectionStatus.Disconnecting)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = false;
            //IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_Disconnecting;
            StatusButton = _pathConnectingButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
        }
        else if (status == MpcService.ConnectionStatus.DisconnectedByUser)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
            //IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_DisconnectedByUser;
            StatusButton = _pathDisconnectedButton;

            StatusBarMessage = ConnectionStatusMessage;
            */
            Debug.WriteLine("ConnectionStatus_DisconnectedByUser");
        }
        else if (status == MpcService.ConnectionStatus.SendFail_NotConnected)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
            IsConnectionSettingShow = true;
            ConnectionStatusMessage = MPDCtrlX.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
            */

            Debug.WriteLine("ConnectionStatus_SendFail_NotConnected");
        }
        else if (status == MpcService.ConnectionStatus.SendFail_Timeout)
        {
            /*
            IsConnected = false;
            IsConnecting = false;
            IsNotConnectingNorConnected = true;
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
        /*
        string s = ackMsg;
        string patternStr = @"[\[].+?[\]]";//@"[{\[].+?[}\]]";
        s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
        s = s.Replace("ACK ", string.Empty);
        s = s.Replace("{} ", string.Empty);

        Dispatcher.UIThread.Post(() =>
        {
            AckWindowOutput?.Invoke(this, MpdVersion + ": " + MPDCtrlX.Properties.Resources.MPD_CommandError + " - " + s + Environment.NewLine);
        });

        if (origin.Equals("Command"))
        {
            InfoBarAckTitle = MpdVersion + " " + MPDCtrlX.Properties.Resources.MPD_CommandError;
        }
        else if (origin.Equals("Idle"))
        {
            InfoBarAckTitle = MpdVersion;// + " " + MPDCtrlX.Properties.Resources.MPD_StatusError; // TODO:
        }
        else
        {
            InfoBarAckTitle = MpdVersion;
        }

        InfoBarAckMessage = s;

        IsShowAckWindow = true;
        */
    }

    private void OnMpdFatalError(MpcService sender, string errMsg, string origin)
    {
        if (string.IsNullOrEmpty(errMsg))
            return;

        Debug.WriteLine($"MpdFatalError: {errMsg}");
        /*
        string s = errMsg;
        string patternStr = @"[\[].+?[\]]";//@"[{\[].+?[}\]]";
        s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
        s = s.Replace("ACK ", string.Empty);
        s = s.Replace("{} ", string.Empty);

        // ErrWindowOutput?.Invoke(this, MpdVersion + ": " + MPDCtrlX.Properties.Resources.MPD_CommandError + " - " + s + Environment.NewLine);

        if (origin.Equals("Command"))
        {
            InfoBarErrTitle = MpdVersion + " " + MPDCtrlX.Properties.Resources.MPD_CommandError;
        }
        else if (origin.Equals("Idle"))
        {
            InfoBarErrTitle = MpdVersion;// + " " + MPDCtrlX.Properties.Resources.MPD_StatusError; // TODO:
        }
        else
        {
            InfoBarErrTitle = MpdVersion;
        }

        InfoBarErrMessage = s;

        IsShowErrWindow = true;
        */
    }

    private void OnMpcProgress(MpcService sender, string msg)
    {
        StatusBarMessage = msg;
    }

    private void OnMpcIsBusy(MpcService sender, bool on)
    {
        //this.IsBusy = on;
    }

    private void OnUpdateProgress(string msg)
    {
        StatusBarMessage = msg;
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
            App.MainWnd.SetCapitionButtonColorForWin11();
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
    public async Task SetSeek()
    {
        if (IsBusy) return;
        double elapsed = _elapsed / 10;
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
                    SearchResult = new ObservableCollection<SongInfo>(res.SearchResult); // COPY ON PURPOSE
                });
            }
            else
            {
                Debug.WriteLine("Search result is null.");
            }
        }
        else
        {
            Debug.WriteLine("Search failed: " + res.ErrorMessage);
            SearchResult?.Clear();
        }

        UpdateProgress?.Invoke(this, "");
    }

    #endregion
}
