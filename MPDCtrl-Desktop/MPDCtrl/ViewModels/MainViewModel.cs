using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Media;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Windows.Input;
using System.IO;
using System.ComponentModel;
using System.Windows.Threading;
using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using MPDCtrl.Common;
using MPDCtrl.Views;
using MPDCtrl.Models;
using MPDCtrl.ViewModels.Classes;

namespace MPDCtrl.ViewModels
{
    /// TODO: 
    /// 
    /// Progress イベントの翻訳。
    /// profileを切り替えるとQueueとAlbumArtが奇妙な挙動をする件。
    /// 
    /// v3.0.4.x 以降
    /// 
    /// 設定画面でDBのupdateとrescan。
    /// 「プレイリストの名前変更」をインラインで。
    /// キューの順番変更をドラッグアンドドロップで。
    /// 
    /// シークやボリュームの進行度に合わせた背景色。
    /// スクロールバーを押した際の色が見にくい。
    /// 
    /// Playlistの変更通知がウザい件。
    /// プレイリストのリストビューで、プレイリストを切り替える際に、選択などをクリアする。
    /// TreeViewのポップアップメニューでゴミが表示される件。
    /// 左ペインの幅を覚える件。
    /// 
    /// イベントのidleからの"再読み込み"通知。
    /// 
    /// v3.1.0 以降
    /// 
    /// リソース系：翻訳やスタイル、名前の整理と見直し。
    /// 
    /// UI：テーマの切り替え
    /// 
    /// [未定]
    /// localhost に自動でつなげる?
    /// AlbumArt画像のキャッシュとアルバムビュー。
    /// "追加して再生"メニューを追加。　
    /// 検索で、ExactかContainのオプション。
    /// スライダーの上でスクロールして音量変更。
    /// スライダーの変更時にブレる件。


    /// 更新履歴：
    /// v3.0.3   MS Store 公開。
    /// v3.0.2.3 password間違いの際にエラーでてた。別スレッドにした関係でAckのメインスレッド実行が出来ていなかった。コマンド送信でInvokeする処理を待たないように一部変更（高速化）。listallを起動時にせず、as neededに変更。
    /// v3.0.2.2 Listviewのつまみの色と幅を少し変えた。クリックでPageUp/Down出来ていなかった。
    /// v3.0.2.1 QueueのaddにTask.Delayを入れてみた(空の時だけ)>profileを切り替えると奇妙な挙動・・。Dir&FilesのAddをTaskで。App.currentのnullチェック。PlaylistItemsの切り替えで、Newするようにした。
    /// v3.0.2   MS Store 公開。
    /// v3.0.1.1 ReleaseビルドでDeveloperモードになってデバッグウィンドウが非表示になっていた。profile空の状態でテストしたらボロボロだった。
    /// v3.0.1   MS Store 公開。
    /// v3.0.0.6 パスワード変更ダイアログ消しちゃってた。ちょっとリファクタリング。playlistsに最終更新日を追加する為にString型からPlaylist型にした。TreeView menuのプレイリスト選択からキューに追加のコンテキストメニュー。ログの保存方法を少し変更。
    /// v3.0.0.5 Search iconを復活させた。キューのMoveが動いていなかった。
    /// v3.0.0.4 Queue listview Ctrl+Fのコマンドが正しく指定されてなかった。
    /// v3.0.0.3 Find is done.
    /// v3.0.0.2 MPD protocol のバージョンが0.19.x以下だったらステータスバーにメッセージを出すようにした。Closeボタンの背景を赤にした。playlistのコンテキストメニューの文字変更。
    /// v3.0.0.1 SysButtonの背景を変えた。接続シークエンスで諸々の情報取得を独立的に行うようにした（一つ失敗しても他はロードされるように）。LocalFilesが正しくClearされるようにした。
    /// v3.0.0.  とりあえずひと段落したので。
    /// v3.0.0.7 とりあえず、プレイリスト系は大体できた。
    /// v3.0.0.6 とりあえずAlbumArtの取得はできるようにしたけれど、Downloaderクラスが必要。
    /// v3.0.0.5 色々やり過ぎて覚えていない・・・
    /// v3.0.0.4 色々やり過ぎて覚えていない・・・とりあえず。
    /// v3.0.3 基本のコマンドとidleからの更新ができるようになった・・・。
    /// v3.0.2 MPCを作り直し中。とりあえず接続とデータ取得まで。
    /// v3.0.1 MPCを作り直し中。 
    /// v3.0.0 v2.1.2から分岐。レイアウトを見直し。


    /// Key Gestures:
    /// Ctrl+S Show Settings
    /// Ctrl+F Show Find 
    /// Ctrl+P Playback Play
    /// Ctrl+U QueueListview Queue Move Up
    /// Ctrl+D QueueListview Queue Move Down
    /// Ctrl+Delete QueueListview Queue Selected Item delete.
    /// Ctrl+J QueueListview Jump to now playing.
    /// Space Play > reserved for listview..
    /// Ctrl+Delete QueueListview Remove from Queue
    /// Esc Close dialogs.


    public class MainViewModel : ViewModelBase
    {
        #region == Basic ==  

        // Application name
        const string _appName = "MPDCtrl";

        // Application version
        const string _appVer = "v3.0.3";

        public static string AppVer
        {
            get
            {
                return _appVer;
            }
        }

        // Application Title (for system)
        public static string AppTitle
        {
            get
            {
                return _appName;
            }
        }

        // Application Window Title (for display)
        public static string AppTitleVer
        {
            get
            {
                return _appName + " " + _appVer;
            }
        }

        // For the application config file folder
        const string _appDeveloper = "torum";

        // もう使ってない気がする。
        public static bool DeveloperMode
        {
            get
            {
#if DEBUG
                return true;
#else
                return false; 
#endif
            }
        }

        #endregion

        #region == 設定フォルダ関連 ==  

        private static string _envDataFolder = System.Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        private static string _appDataFolder;
        private static string _appConfigFilePath;

        private bool _isFullyLoaded;
        public bool IsFullyLoaded
        {
            get
            {
                return _isFullyLoaded;
            }
            set
            {
                if (_isFullyLoaded == value)
                    return;

                _isFullyLoaded = value;
                this.NotifyPropertyChanged("IsFullyLoaded");
            }
        }

        private bool _isFullyRendered;
        public bool IsFullyRendered
        {
            get
            {
                return _isFullyRendered;
            }
            set
            {
                if (_isFullyRendered == value)
                    return;

                _isFullyRendered = value;
                this.NotifyPropertyChanged("IsFullyRendered");
            }
        }

        #endregion

        #region == レイアウト関連 ==

        // TODO:現在のレイアウトでは無効

        private double _mainLeftPainActualWidth = 241;
        public double MainLeftPainActualWidth
        {
            get
            {
                return _mainLeftPainActualWidth;
            }
            set
            {
                if (value == _mainLeftPainActualWidth) return;

                _mainLeftPainActualWidth = value;

                NotifyPropertyChanged("MainLeftPainActualWidth");
            }
        }

        private double _mainLeftPainWidth = 241;
        public double MainLeftPainWidth
        {
            get
            {
                return _mainLeftPainWidth;
            }
            set
            {
                if (value == _mainLeftPainWidth) return;

                _mainLeftPainWidth = value;

                NotifyPropertyChanged("MainLeftPainWidth");
            }
        }

        #region == Queueカラムヘッダー ==

        private bool _queueColumnHeaderPositionVisibility = true;
        public bool QueueColumnHeaderPositionVisibility
        {
            get
            {
                return _queueColumnHeaderPositionVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderPositionVisibility)
                    return;

                _queueColumnHeaderPositionVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderPositionVisibility");
            }
        }

        private double _queueColumnHeaderPositionWidth = 53;
        public double QueueColumnHeaderPositionWidth
        {
            get
            {
                return _queueColumnHeaderPositionWidth;
            }
            set
            {
                if (value == _queueColumnHeaderPositionWidth)
                    return;

                _queueColumnHeaderPositionWidth = value;

                if (value > 0)
                    QueueColumnHeaderPositionWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderPositionWidth");
            }
        }

        private double _queueColumnHeaderPositionWidthUser = 53;
        public double QueueColumnHeaderPositionWidthRestore
        {
            get
            {
                return _queueColumnHeaderPositionWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderPositionWidthUser)
                    return;

                _queueColumnHeaderPositionWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderPositionWidthRestore");
            }
        }

        private bool _queueColumnHeaderNowPlayingVisibility = true;
        public bool QueueColumnHeaderNowPlayingVisibility
        {
            get
            {
                return _queueColumnHeaderNowPlayingVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingVisibility)
                    return;

                _queueColumnHeaderNowPlayingVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderNowPlayingVisibility");
            }
        }

        private double _queueColumnHeaderNowPlayingWidth = 32;
        public double QueueColumnHeaderNowPlayingWidth
        {
            get
            {
                return _queueColumnHeaderNowPlayingWidth;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingWidth)
                    return;

                _queueColumnHeaderNowPlayingWidth = value;

                if (value > 0)
                    QueueColumnHeaderNowPlayingWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderNowPlayingWidth");
            }
        }

        private double _queueColumnHeaderNowPlayingWidthUser = 32;
        public double QueueColumnHeaderNowPlayingWidthRestore
        {
            get
            {
                return _queueColumnHeaderNowPlayingWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderNowPlayingWidthUser)
                    return;

                _queueColumnHeaderNowPlayingWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderNowPlayingWidthRestore");
            }
        }

        private double _queueColumnHeaderTitleWidth = 180;
        public double QueueColumnHeaderTitleWidth
        {
            get
            {
                return _queueColumnHeaderTitleWidth;
            }
            set
            {
                if (value == _queueColumnHeaderTitleWidth)
                    return;

                _queueColumnHeaderTitleWidth = value;

                if (value > 0)
                    QueueColumnHeaderTitleWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderTitleWidth");
            }
        }

        private double _queueColumnHeaderTitleWidthUser = 180;
        public double QueueColumnHeaderTitleWidthRestore
        {
            get
            {
                return _queueColumnHeaderTitleWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderTitleWidthUser)
                    return;

                _queueColumnHeaderTitleWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderTitleWidthRestore");
            }
        }

        private bool _queueColumnHeaderTimeVisibility = true;
        public bool QueueColumnHeaderTimeVisibility
        {
            get
            {
                return _queueColumnHeaderTimeVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderTimeVisibility)
                    return;

                _queueColumnHeaderTimeVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderTimeVisibility");
            }
        }

        private double _queueColumnHeaderTimeWidth = 62;
        public double QueueColumnHeaderTimeWidth
        {
            get
            {
                return _queueColumnHeaderTimeWidth;
            }
            set
            {
                if (value == _queueColumnHeaderTimeWidth)
                    return;

                _queueColumnHeaderTimeWidth = value;

                if (value > 0)
                    QueueColumnHeaderTitleWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderTimeWidth");
            }
        }

        private double _queueColumnHeaderTimeWidthUser = 62;
        public double QueueColumnHeaderTimeWidthRestore
        {
            get
            {
                return _queueColumnHeaderTimeWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderTimeWidthUser)
                    return;

                _queueColumnHeaderTimeWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderTimeWidthRestore");
            }
        }

        private bool _queueColumnHeaderArtistVisibility = true;
        public bool QueueColumnHeaderArtistVisibility
        {
            get
            {
                return _queueColumnHeaderArtistVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderArtistVisibility)
                    return;

                _queueColumnHeaderArtistVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderArtistVisibility");
            }
        }

        private double _queueColumnHeaderArtistWidth = 120;
        public double QueueColumnHeaderArtistWidth
        {
            get
            {
                return _queueColumnHeaderArtistWidth;
            }
            set
            {
                if (value == _queueColumnHeaderArtistWidth)
                    return;

                _queueColumnHeaderArtistWidth = value;

                if (value > 0)
                    QueueColumnHeaderArtistWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderArtistWidth");
            }
        }

        private double _queueColumnHeaderArtistWidthUser = 120;
        public double QueueColumnHeaderArtistWidthRestore
        {
            get
            {
                return _queueColumnHeaderArtistWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderArtistWidthUser)
                    return;

                _queueColumnHeaderArtistWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderArtistWidthRestore");
            }
        }

        private bool _queueColumnHeaderAlbumVisibility = true;
        public bool QueueColumnHeaderAlbumVisibility
        {
            get
            {
                return _queueColumnHeaderAlbumVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumVisibility)
                    return;

                _queueColumnHeaderAlbumVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderAlbumVisibility");
            }
        }

        private double _queueColumnHeaderAlbumWidth = 120;
        public double QueueColumnHeaderAlbumWidth
        {
            get
            {
                return _queueColumnHeaderAlbumWidth;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumWidth)
                    return;

                _queueColumnHeaderAlbumWidth = value;

                if (value > 0)
                    QueueColumnHeaderAlbumWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderAlbumWidth");
            }
        }

        private double _queueColumnHeaderAlbumWidthUser = 120;
        public double QueueColumnHeaderAlbumWidthRestore
        {
            get
            {
                return _queueColumnHeaderAlbumWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderAlbumWidthUser)
                    return;

                _queueColumnHeaderAlbumWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderAlbumWidthRestore");
            }
        }

        private bool _queueColumnHeaderGenreVisibility = true;
        public bool QueueColumnHeaderGenreVisibility
        {
            get
            {
                return _queueColumnHeaderGenreVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderGenreVisibility)
                    return;

                _queueColumnHeaderGenreVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderGenreVisibility");
            }
        }

        private double _queueColumnHeaderGenreWidth = 100;
        public double QueueColumnHeaderGenreWidth
        {
            get
            {
                return _queueColumnHeaderGenreWidth;
            }
            set
            {
                if (value == _queueColumnHeaderGenreWidth)
                    return;

                _queueColumnHeaderGenreWidth = value;

                if (value > 0)
                    QueueColumnHeaderGenreWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderGenreWidth");
            }
        }

        private double _queueColumnHeaderGenreWidthUser = 100;
        public double QueueColumnHeaderGenreWidthRestore
        {
            get
            {
                return _queueColumnHeaderGenreWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderGenreWidthUser)
                    return;

                _queueColumnHeaderGenreWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderGenreWidthRestore");
            }
        }

        private bool _queueColumnHeaderLastModifiedVisibility = true;
        public bool QueueColumnHeaderLastModifiedVisibility
        {
            get
            {
                return _queueColumnHeaderLastModifiedVisibility;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedVisibility)
                    return;

                _queueColumnHeaderLastModifiedVisibility = value;

                NotifyPropertyChanged("QueueColumnHeaderLastModifiedVisibility");
            }
        }

        private double _queueColumnHeaderLastModifiedWidth = 180;
        public double QueueColumnHeaderLastModifiedWidth
        {
            get
            {
                return _queueColumnHeaderLastModifiedWidth;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedWidth)
                    return;

                _queueColumnHeaderLastModifiedWidth = value;

                if (value > 0)
                    QueueColumnHeaderLastModifiedWidthRestore = value;

                NotifyPropertyChanged("QueueColumnHeaderLastModifiedWidth");
            }
        }

        private double _queueColumnHeaderLastModifiedWidthUser = 180;
        public double QueueColumnHeaderLastModifiedWidthRestore
        {
            get
            {
                return _queueColumnHeaderLastModifiedWidthUser;
            }
            set
            {
                if (value == _queueColumnHeaderLastModifiedWidthUser)
                    return;

                _queueColumnHeaderLastModifiedWidthUser = value;

                NotifyPropertyChanged("QueueColumnHeaderLastModifiedWidthRestore");
            }
        }

        #endregion

        #endregion

        // TODO: 整理
        #region == 画面表示切り替え系 ==  

        private bool _isConnected;
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                if (_isConnected == value)
                    return;

                _isConnected = value;
                NotifyPropertyChanged("IsConnected");
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
                NotifyPropertyChanged("IsConnecting");
                NotifyPropertyChanged("IsNotConnecting");

                NotifyPropertyChanged("IsProfileSwitchOK");
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
                NotifyPropertyChanged("IsNotConnectingNorConnected");
            }
        }

        public bool IsNotConnecting
        {
            get
            {
                return !_isConnecting;
            }
        }

        private bool _isSettingsShow;
        public bool IsSettingsShow
        {
            get { return _isSettingsShow; }
            set
            {
                if (_isSettingsShow == value)
                    return;

                _isSettingsShow = value;

                if (_isSettingsShow)
                {
                    if (CurrentProfile == null)
                    {
                        IsConnectionSettingShow = false;
                    }
                    else
                    {
                        IsConnectionSettingShow = false;
                    }
                }
                else
                {
                    if (CurrentProfile == null)
                    {
                        IsConnectionSettingShow = true;
                    }
                    else
                    {
                        if (!IsConnected)
                        {
                            IsConnectionSettingShow = true;
                        }
                    }
                }

                NotifyPropertyChanged("IsSettingsShow");

            }
        }

        private bool _isConnectionSettingShow;
        public bool IsConnectionSettingShow
        {
            get { return _isConnectionSettingShow; }
            set
            {
                if (_isConnectionSettingShow == value)
                    return;

                _isConnectionSettingShow = value;
                NotifyPropertyChanged("IsConnectionSettingShow");
            }
        }

        private bool _isChangePasswordDialogShow;
        public bool IsChangePasswordDialogShow
        {
            get
            {
                return _isChangePasswordDialogShow;
            }
            set
            {
                if (_isChangePasswordDialogShow == value)
                    return;

                _isChangePasswordDialogShow = value;
                NotifyPropertyChanged("IsChangePasswordDialogShow");
            }
        }

        public bool IsCurrentProfileSet
        {
            get
            {
                if (Profiles.Count > 1)
                    return true;
                else
                    return false;
            }
        }

        private bool _isAlbumArtVisible;
        public bool IsAlbumArtVisible
        {
            get
            {
                return _isAlbumArtVisible;
            }
            set
            {
                if (_isAlbumArtVisible == value)
                    return;

                _isAlbumArtVisible = value;
                NotifyPropertyChanged("IsAlbumArtVisible");
            }
        }

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
                    return;

                _isBusy = value;
                NotifyPropertyChanged("IsBusy");

                NotifyPropertyChanged("IsProfileSwitchOK");

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
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
                NotifyPropertyChanged("IsWorking");

                NotifyPropertyChanged("IsProfileSwitchOK");

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());
            }
        }

        private bool _isShowAckWindow
;
        public bool IsShowAckWindow

        {
            get { return _isShowAckWindow; }
            set
            {
                if (_isShowAckWindow == value)
                    return;

                _isShowAckWindow = value;

                NotifyPropertyChanged("IsShowAckWindow");
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

                NotifyPropertyChanged("IsShowDebugWindow");

                if (_isShowDebugWindow)
                {
                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        DebugWindowShowHide?.Invoke();
                    });
                }
            }
        }

        #endregion

        #region == コントロール関連 ==  

        private SongInfoEx _currentSong;
        public SongInfoEx CurrentSong
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
                NotifyPropertyChanged("CurrentSong");
                NotifyPropertyChanged("CurrentSongTitle");
                NotifyPropertyChanged("CurrentSongArtist");
                NotifyPropertyChanged("CurrentSongAlbum");

                if (value == null)
                    _elapsedTimer.Stop();
            }
        }

        public string CurrentSongTitle
        {
            get
            {
                if (_currentSong != null)
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
                if (_currentSong != null)
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
                if (_currentSong != null)
                {
                    if (!string.IsNullOrEmpty(_currentSong.Album))
                        return _currentSong.Album.Trim();
                    else
                        return "";
                }
                else
                {
                    return String.Empty;
                }
            }
        }

        #region == Playback ==  

        private static string _pathPlayButton = "M10,16.5V7.5L16,12M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
        private static string _pathPauseButton = "M15,16H13V8H15M11,16H9V8H11M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2Z";
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
                NotifyPropertyChanged("PlayButton");
            }
        }

        private double _volume;
        public double Volume
        {
            get
            {
                return _volume;
            }
            set
            {
                if (_volume != value)
                {
                    _volume = value;
                    NotifyPropertyChanged("Volume");

                    if (_mpc != null)
                    {
                        if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) != _volume)
                        {
                            // If we have a timer and we are in this event handler, a user is still interact with the slider
                            // we stop the timer
                            if (_volumeDelayTimer != null)
                                _volumeDelayTimer.Stop();

                            //System.Diagnostics.Debug.WriteLine("Volume value is still changing. Skipping.");

                            // we always create a new instance of DispatcherTimer
                            _volumeDelayTimer = new System.Timers.Timer();
                            _volumeDelayTimer.AutoReset = false;

                            // if one second passes, that means our user has stopped interacting with the slider
                            // we do real event
                            _volumeDelayTimer.Interval = (double)1000;
                            _volumeDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeVolume);

                            _volumeDelayTimer.Start();
                        }
                    }
                }
            }
        }

        private System.Timers.Timer _volumeDelayTimer = null;
        private void DoChangeVolume(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_mpc != null)
            {
                if (Convert.ToDouble(_mpc.MpdStatus.MpdVolume) != _volume)
                {
                    if (SetVolumeCommand.CanExecute(null))
                    {
                        SetVolumeCommand.Execute(null);
                    }
                }
            }
        }

        private bool _repeat;
        public bool Repeat
        {
            get { return _repeat; }
            set
            {
                _repeat = value;
                NotifyPropertyChanged("Repeat");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdRepeat != value)
                    {
                        if (SetRpeatCommand.CanExecute(null))
                        {
                            SetRpeatCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _random;
        public bool Random
        {
            get { return _random; }
            set
            {
                _random = value;
                NotifyPropertyChanged("Random");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdRandom != value)
                    {
                        if (SetRandomCommand.CanExecute(null))
                        {
                            SetRandomCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _consume;
        public bool Consume
        {
            get { return _consume; }
            set
            {
                _consume = value;
                NotifyPropertyChanged("Consume");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdConsume != value)
                    {
                        if (SetConsumeCommand.CanExecute(null))
                        {
                            SetConsumeCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private bool _single;
        public bool Single
        {
            get { return _single; }
            set
            {
                _single = value;
                NotifyPropertyChanged("Single");

                if (_mpc != null)
                {
                    if (_mpc.MpdStatus.MpdSingle != value)
                    {
                        if (SetSingleCommand.CanExecute(null))
                        {
                            SetSingleCommand.Execute(null);
                        }
                    }
                }
            }
        }

        private double _time;
        public double Time
        {
            get
            {
                return _time;
            }
            set
            {
                _time = value;
                NotifyPropertyChanged("Time");
            }
        }

        private double _elapsed;
        public double Elapsed
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
                    NotifyPropertyChanged("Elapsed");

                    // If we have a timer and we are in this event handler, a user is still interact with the slider
                    // we stop the timer
                    if (_elapsedDelayTimer != null)
                        _elapsedDelayTimer.Stop();

                    //System.Diagnostics.Debug.WriteLine("Elapsed value is still changing. Skipping.");

                    // we always create a new instance of DispatcherTimer
                    _elapsedDelayTimer = new System.Timers.Timer();
                    _elapsedDelayTimer.AutoReset = false;

                    // if one second passes, that means our user has stopped interacting with the slider
                    // we do real event
                    _elapsedDelayTimer.Interval = (double)1000;
                    _elapsedDelayTimer.Elapsed += new System.Timers.ElapsedEventHandler(DoChangeElapsed);

                    _elapsedDelayTimer.Start();
                }
            }
        }

        private System.Timers.Timer _elapsedDelayTimer = null;
        private void DoChangeElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_mpc != null)
            {
                if ((_elapsed < _time))
                {
                    if (SetSeekCommand.CanExecute(null))
                    {
                        SetSeekCommand.Execute(null);
                    }
                }
            }
        }


        #endregion

        #region == AlbumArt == 

        private ImageSource _albumArtDefault = null;
        private ImageSource _albumArt;
        public ImageSource AlbumArt
        {
            get
            {
                return _albumArt;
            }
            set
            {
                if (_albumArt == value)
                    return;

                _albumArt = value;
                NotifyPropertyChanged("AlbumArt");
            }
        }

        #endregion

        #endregion

        #region == メイン画面のメニューと各画面の機能 ==

        #region == Queue ==  

        private ObservableCollection<SongInfoEx> _queue = new ObservableCollection<SongInfoEx>();
        public ObservableCollection<SongInfoEx> Queue
        {
            get
            {
                if (_mpc != null)
                {
                    return _queue;
                    //return _mpc.CurrentQueue;
                }
                else
                {
                    return null;
                }
            }
            set
            {
                if (_queue == value)
                    return;

                _queue = value;
                NotifyPropertyChanged("Queue");
            }
        }

        private SongInfoEx _selectedQueueSong;
        public SongInfoEx SelectedQueueSong
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
                NotifyPropertyChanged("SelectedQueueSong");
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
                NotifyPropertyChanged("IsQueueFindVisible");
            }
        }

        private ObservableCollection<SongInfoEx> _queueForFilter = new ObservableCollection<SongInfoEx>();
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
                NotifyPropertyChanged("QueueForFilter");
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
                NotifyPropertyChanged("SelectedQueueFilterTags");

                if (_filterQueueQuery == "")
                    return;

                var collectionView = CollectionViewSource.GetDefaultView(_queueForFilter);
                collectionView.Filter = x =>
                {
                    var entry = (SongInfoEx)x;

                    if (SelectedQueueFilterTags == SearchTags.Title)
                    {
                        return entry.Title.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else if (SelectedQueueFilterTags == SearchTags.Artist)
                    {
                        return entry.Artist.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else if (SelectedQueueFilterTags == SearchTags.Album)
                    {
                        return entry.Album.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else if (SelectedQueueFilterTags == SearchTags.Genre)
                    {
                        return entry.Genre.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                    }
                    else
                    {
                        return false;
                    }
                };
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
                NotifyPropertyChanged("FilterQueueQuery");

                var collectionView = CollectionViewSource.GetDefaultView(_queueForFilter);
                collectionView.Filter = x =>
                {
                    if (_filterQueueQuery == "")
                    {
                        return false;
                    }
                    else
                    {
                        var entry = (SongInfoEx)x;

                        if (SelectedQueueFilterTags == SearchTags.Title)
                        {
                            return entry.Title.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else if (SelectedQueueFilterTags == SearchTags.Artist)
                        {
                            return entry.Artist.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else if (SelectedQueueFilterTags == SearchTags.Album)
                        {
                            return entry.Album.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else if (SelectedQueueFilterTags == SearchTags.Genre)
                        {
                            return entry.Genre.Contains(_filterQueueQuery, StringComparison.CurrentCultureIgnoreCase);
                        }
                        else
                        {
                            return false;
                        }
                    }
                };

                //collectionView.Refresh();

            }
        }

        private SongInfoEx _selectedQueueFilterSong;
        public SongInfoEx SelectedQueueFilterSong
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
                NotifyPropertyChanged("SelectedQueueFilterSong");
            }
        }

        #endregion

        #region == Library ==

        private DirectoryTreeBuilder _musicDirectories = new DirectoryTreeBuilder();
        public ObservableCollection<NodeTree> MusicDirectories
        {
            get { return _musicDirectories.Children; }
            set
            {
                _musicDirectories.Children = value;
                NotifyPropertyChanged(nameof(MusicDirectories));
            }
        }

        private NodeTree _selectedNodeDirectory = new NodeDirectory(".",new Uri(@"file:///./"));
        public NodeTree SelectedNodeDirectory
        {
            get { return _selectedNodeDirectory; }
            set
            {
                if (_selectedNodeDirectory == value)
                    return;

                _selectedNodeDirectory = value;
                NotifyPropertyChanged(nameof(SelectedNodeDirectory));

                if (_selectedNodeDirectory == null)
                    return;

                if (MusicEntries == null)
                    return;
                if (MusicEntries.Count == 0)
                    return;

                // TODO: 絞り込みモードか、マッチしたフォルダ内だけかの切り替え
                bool filteringMode = true;

                // Treeview で選択ノードが変更されたのでListview でフィルターを掛ける。
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);
                if (collectionView == null)
                    return;

                try
                {
                    collectionView.Filter = x =>
                    {
                        var entry = (NodeFile)x;

                        if (entry == null)
                            return false;

                        if (entry.FileUri == null)
                            return false;

                        string path = entry.FileUri.LocalPath; //person.FileUri.AbsoluteUri;
                        if (string.IsNullOrEmpty(path))
                            return false;
                        string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);
                        if (string.IsNullOrEmpty(filename))
                            return false;

                        if ((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath == "/")
                        {
                            if (filteringMode)
                            {
                                // 絞り込みモード
                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    return (filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return true;
                                }
                            }
                            else
                            {
                                // マッチしたフォルダ内だけ
                                path = path.Replace("/", "");

                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    return ((path == filename) && filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path == filename);
                                }
                            }
                        }
                        else
                        {
                            path = path.Replace(("/" + filename), "");

                            if (filteringMode)
                            {
                                // 絞り込みモード

                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath));
                                }
                            }
                            else
                            {
                                // マッチしたフォルダ内だけ
                                if (!string.IsNullOrEmpty(FilterMusicEntriesQuery))
                                {
                                    return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(FilterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                                }
                                else
                                {
                                    return (path == (_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath);
                                }
                            }
                        }
                    };

                }
                catch (Exception e)
                {
                    Debug.WriteLine("collectionView.Filter = x => " + e.Message);
                }

            }
        }

        private ObservableCollection<NodeFile> _musicEntries = new ObservableCollection<NodeFile>();
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
                NotifyPropertyChanged(nameof(MusicEntries));

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
                NotifyPropertyChanged("FilterMusicEntriesQuery");

                /*
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);

                collectionView.Filter = x =>
                {
                    var entry = (NodeFile)x;

                    string test = entry.FilePath + entry.Name;

                    // 絞り込み
                    return (test.Contains(_filterQuery, StringComparison.CurrentCultureIgnoreCase));
                };
                collectionView.Refresh();

                */

                if (_selectedNodeDirectory == null)
                    return;

                // TODO: 絞り込みモードか、マッチしたフォルダ内だけかの切り替え
                bool filteringMode = true;

                // Treeview で選択ノードが変更されたのでListview でフィルターを掛ける。
                var collectionView = CollectionViewSource.GetDefaultView(MusicEntries);
                collectionView.Filter = x =>
                {
                    var entry = (NodeFile)x;

                    if (entry.FileUri == null)
                        return false;

                    string path = entry.FileUri.LocalPath; //person.FileUri.AbsoluteUri;
                    string filename = System.IO.Path.GetFileName(path);//System.IO.Path.GetFileName(uri.LocalPath);

                    if ((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath == "/")
                    {
                        if (filteringMode)
                        {
                            // 絞り込みモード
                            if (FilterMusicEntriesQuery != "")
                            {
                                return (filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            // マッチしたフォルダ内だけ
                            path = path.Replace("/", "");

                            if (FilterMusicEntriesQuery != "")
                            {
                                return ((path == filename) && filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path == filename);
                            }
                        }
                    }
                    else
                    {
                        path = path.Replace(("/" + filename), "");

                        if (filteringMode)
                        {
                            // 絞り込みモード

                            if (FilterMusicEntriesQuery != "")
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath));
                            }
                        }
                        else
                        {
                            // マッチしたフォルダ内だけ
                            if (FilterMusicEntriesQuery != "")
                            {
                                return (path.StartsWith((_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath) && filename.Contains(_filterMusicEntriesQuery, StringComparison.CurrentCultureIgnoreCase));
                            }
                            else
                            {
                                return (path == (_selectedNodeDirectory as NodeDirectory).DireUri.LocalPath);
                            }
                        }
                    }
                };

                collectionView.Refresh();

            }
        }

        #endregion

        #region == Search ==

        public ObservableCollection<SongInfo> SearchResult
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.SearchResult;
                }
                else
                {
                    return null;
                }
            }
        }

        private SearchTags _selectedSearchTags = SearchTags.Title;
        public SearchTags SelectedSearchTags
        {
            get
            {
                return _selectedSearchTags;
            }
            set
            {
                if (_selectedSearchTags == value)
                    return;

                _selectedSearchTags = value;
                NotifyPropertyChanged("SelectedSearchTags");
            }
        }

        private string _searchQuery;
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
                NotifyPropertyChanged("SearchQuery");
            }
        }

        #endregion

        #region == Playlists ==  

        public ObservableCollection<Playlist> Playlists
        {
            get
            {
                if (_mpc != null)
                {
                    return _mpc.Playlists;
                }
                else
                {
                    return null;
                }
            }
        }

        private Playlist _selecctedPlaylist;
        public Playlist SelectedPlaylist
        {
            get
            {
                return _selecctedPlaylist;
            }
            set
            {
                if (_selecctedPlaylist != value)
                {
                    _selecctedPlaylist = value;
                    NotifyPropertyChanged("SelectedPlaylist");
                }
            }
        }

        #endregion

        #region == Playlist Items ==

        private ObservableCollection<SongInfo> _playlistSongs = new ObservableCollection<SongInfo>();
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
                    NotifyPropertyChanged("PlaylistSongs");
                }
            }
        }

        private SongInfo _selecctedPlaylistSong;
        public SongInfo SelectedPlaylistSong
        {
            get
            {
                return _selecctedPlaylistSong;
            }
            set
            {
                if (_selecctedPlaylistSong != value)
                {
                    _selecctedPlaylistSong = value;
                    NotifyPropertyChanged("SelectedPlaylistSong");
                }
            }
        }

        #endregion

        #region == MenuTree ==

        private MenuTreeBuilder _mainMenuItems = new MenuTreeBuilder();
        public ObservableCollection<NodeTree> MainMenuItems
        {
            get { return _mainMenuItems.Children; }
            set
            {
                _mainMenuItems.Children = value;
                NotifyPropertyChanged(nameof(MainMenuItems));
            }
        }

        private NodeTree _selectedNodeMenu = new NodeMenu("root");
        public NodeTree SelectedNodeMenu
        {
            get { return _selectedNodeMenu; }
            set
            {
                if (_selectedNodeMenu == value)
                    return;

                if (value == null)
                {
                    //Debug.WriteLine("selectedNodeMenu is null");
                    return;
                }

                _selectedNodeMenu = value;
                NotifyPropertyChanged(nameof(SelectedNodeMenu));

                if (value is NodeMenuQueue)
                {
                    IsQueueVisible = true;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = false;
                    IsSearchVisible = false;
                }
                else if (value is NodeMenuPlaylists)
                {
                    IsQueueVisible = false;
                    IsPlaylistsVisible = true;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = false;
                    IsSearchVisible = false;
                }
                else if (value is NodeMenuPlaylistItem)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenuPlaylistItem");
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = true;
                    IsLibraryVisible = false;
                    IsSearchVisible = false;

                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PlaylistSongs = new ObservableCollection<SongInfo>(); // Don't Clear();
                        PlaylistSongs = (value as NodeMenuPlaylistItem).PlaylistSongs;
                    });

                    if (((value as NodeMenuPlaylistItem).PlaylistSongs.Count == 0) || (value as NodeMenuPlaylistItem).IsUpdateRequied)
                        GetPlaylistSongs(value as NodeMenuPlaylistItem);
                }
                else if (value is NodeMenuLibrary)
                {
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = true;
                    IsSearchVisible = false;

                    if ((MusicDirectories.Count <= 1 ) && (MusicEntries.Count == 0))
                        GetLibrary(value as NodeMenuLibrary);
                }
                else if (value is NodeMenuSearch)
                {
                    IsQueueVisible = false;
                    IsPlaylistsVisible = false;
                    IsPlaylistItemVisible = false;
                    IsLibraryVisible = false;
                    IsSearchVisible = true;
                }
                else if (value is NodeMenu)
                {
                    //Debug.WriteLine("selectedNodeMenu is NodeMenu ...unknown:" + _selectedNodeMenu.Name);

                    if (value.Name != "root")
                        throw new NotImplementedException();

                    IsQueueVisible = true;
                }
                else
                {
                    //Debug.WriteLine(value.Name);

                    throw new NotImplementedException();
                }

            }
        }

        private bool _isQueueVisible = true;
        public bool IsQueueVisible
        {
            get { return _isQueueVisible; }
            set
            {
                if (_isQueueVisible == value)
                    return;

                _isQueueVisible = value;
                NotifyPropertyChanged("IsQueueVisible");
            }
        }

        private bool _isPlaylistsVisible = true;
        public bool IsPlaylistsVisible
        {
            get { return _isPlaylistsVisible; }
            set
            {
                if (_isPlaylistsVisible == value)
                    return;

                _isPlaylistsVisible = value;
                NotifyPropertyChanged("IsPlaylistsVisible");
            }
        }

        private bool _isPlaylistItemVisible = true;
        public bool IsPlaylistItemVisible
        {
            get { return _isPlaylistItemVisible; }
            set
            {
                if (_isPlaylistItemVisible == value)
                    return;

                _isPlaylistItemVisible = value;
                NotifyPropertyChanged("IsPlaylistItemVisible");
            }
        }

        private bool _isLibraryVisible = true;
        public  bool IsLibraryVisible
        {
            get { return _isLibraryVisible; }
            set
            {
                if (_isLibraryVisible == value)
                    return;

                _isLibraryVisible = value;
                NotifyPropertyChanged("IsLibraryVisible");
            }
        }

        private bool _isSearchVisible = true;
        public bool IsSearchVisible
        {
            get { return _isSearchVisible; }
            set
            {
                if (_isSearchVisible == value)
                    return;

                _isSearchVisible = value;
                NotifyPropertyChanged("IsSearchVisible");
            }
        }

        #endregion

        #endregion

        #region == ステータス系 == 
        
        private string _statusBarMessage;
        public string StatusBarMessage
        {
            get
            {
                return _statusBarMessage;
            }
            set
            {
                _statusBarMessage = value;
                NotifyPropertyChanged("StatusBarMessage");
            }
        }

        private string _connectionStatusMessage;
        public string ConnectionStatusMessage
        {
            get
            {
                return _connectionStatusMessage;
            }
            set
            {
                _connectionStatusMessage = value;
                NotifyPropertyChanged("ConnectionStatusMessage");
            }
        }

        private string _mpdStatusMessage;
        public string MpdStatusMessage
        {
            get
            {
                return _mpdStatusMessage;
            }
            set
            {
                _mpdStatusMessage = value;
                NotifyPropertyChanged("MpdStatusMessage");

                if (_mpdStatusMessage != "")
                    _isMpdStatusMessageContainsText = true;
                else
                    _isMpdStatusMessageContainsText = false;
                NotifyPropertyChanged("IsMpdStatusMessageContainsText");
            }
        }

        private bool _isMpdStatusMessageContainsText;
        public bool IsMpdStatusMessageContainsText
        {
            get
            {
                return _isMpdStatusMessageContainsText;
            }
        }

        private static string _pathDefaultNoneButton = "";
        private static string _pathDisconnectedButton = "M4,1C2.89,1 2,1.89 2,3V7C2,8.11 2.89,9 4,9H1V11H13V9H10C11.11,9 12,8.11 12,7V3C12,1.89 11.11,1 10,1H4M4,3H10V7H4V3M14,13C12.89,13 12,13.89 12,15V19C12,20.11 12.89,21 14,21H11V23H23V21H20C21.11,21 22,20.11 22,19V15C22,13.89 21.11,13 20,13H14M3.88,13.46L2.46,14.88L4.59,17L2.46,19.12L3.88,20.54L6,18.41L8.12,20.54L9.54,19.12L7.41,17L9.54,14.88L8.12,13.46L6,15.59L3.88,13.46M14,15H20V19H14V15Z";

        private static string _pathConnectingButton = "M11 14H9C9 9.03 13.03 5 18 5V7C14.13 7 11 10.13 11 14M18 11V9C15.24 9 13 11.24 13 14H15C15 12.34 16.34 11 18 11M7 4C7 2.89 6.11 2 5 2S3 2.89 3 4 3.89 6 5 6 7 5.11 7 4M11.45 4.5H9.45C9.21 5.92 8 7 6.5 7H3.5C2.67 7 2 7.67 2 8.5V11H8V8.74C9.86 8.15 11.25 6.5 11.45 4.5M19 17C20.11 17 21 16.11 21 15S20.11 13 19 13 17 13.89 17 15 17.89 17 19 17M20.5 18H17.5C16 18 14.79 16.92 14.55 15.5H12.55C12.75 17.5 14.14 19.15 16 19.74V22H22V19.5C22 18.67 21.33 18 20.5 18Z";
        private static string _pathConnectedButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";
        //private static string _pathConnectedButton = "";
        //private static string _pathDisconnectedButton = "";
        private static string _pathNewConnectionButton = "M20,4C21.11,4 22,4.89 22,6V18C22,19.11 21.11,20 20,20H4C2.89,20 2,19.11 2,18V6C2,4.89 2.89,4 4,4H20M8.5,15V9H7.25V12.5L4.75,9H3.5V15H4.75V11.5L7.3,15H8.5M13.5,10.26V9H9.5V15H13.5V13.75H11V12.64H13.5V11.38H11V10.26H13.5M20.5,14V9H19.25V13.5H18.13V10H16.88V13.5H15.75V9H14.5V14A1,1 0 0,0 15.5,15H19.5A1,1 0 0,0 20.5,14Z";
        private static string _pathErrorInfoButton = "M23,12L20.56,14.78L20.9,18.46L17.29,19.28L15.4,22.46L12,21L8.6,22.47L6.71,19.29L3.1,18.47L3.44,14.78L1,12L3.44,9.21L3.1,5.53L6.71,4.72L8.6,1.54L12,3L15.4,1.54L17.29,4.72L20.9,5.54L20.56,9.22L23,12M20.33,12L18.5,9.89L18.74,7.1L16,6.5L14.58,4.07L12,5.18L9.42,4.07L8,6.5L5.26,7.09L5.5,9.88L3.67,12L5.5,14.1L5.26,16.9L8,17.5L9.42,19.93L12,18.81L14.58,19.92L16,17.5L18.74,16.89L18.5,14.1L20.33,12M11,15H13V17H11V15M11,7H13V13H11V7";

        private string _statusButton = _pathDefaultNoneButton;
        public string StatusButton
        {
            get
            {
                return _statusButton;
            }
            set
            {
                if (_statusButton == value)
                    return;

                _statusButton = value;
                NotifyPropertyChanged("StatusButton");
            }
        }

        private static string _pathMpdOkButton = "M12 2C6.5 2 2 6.5 2 12S6.5 22 12 22 22 17.5 22 12 17.5 2 12 2M12 20C7.59 20 4 16.41 4 12S7.59 4 12 4 20 7.59 20 12 16.41 20 12 20M16.59 7.58L10 14.17L7.41 11.59L6 13L10 17L18 9L16.59 7.58Z";

        private static string _pathMpdAckErrorButton = "M11,15H13V17H11V15M11,7H13V13H11V7M12,2C6.47,2 2,6.5 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,20A8,8 0 0,1 4,12A8,8 0 0,1 12,4A8,8 0 0,1 20,12A8,8 0 0,1 12,20Z";

        private string _mpdStatusButton = _pathMpdOkButton;
        public string MpdStatusButton
        {
            get
            {
                return _mpdStatusButton;
            }
            set
            {
                if (_mpdStatusButton == value)
                    return;

                _mpdStatusButton = value;
                NotifyPropertyChanged("MpdStatusButton");
            }
        }

        private bool _isUpdatingMpdDb;
        public bool IsUpdatingMpdDb
        {
            get
            {
                return _isUpdatingMpdDb;
            }
            set
            {
                _isUpdatingMpdDb = value;
                NotifyPropertyChanged("IsUpdatingMpdDb");
            }
        }

        private string _mpdVersion;
        public string MpdVersion
        {
            get
            {
                if (_mpdVersion != "")
                    return "MPD protocol v" + _mpdVersion;
                else
                    return _mpdVersion;

            }
            set
            {
                if (value == _mpdVersion)
                    return;

                _mpdVersion = value;
                NotifyPropertyChanged("MpdVersion");
            }
        }

        #endregion

        #region == Profileと設定画面 ==

        private ObservableCollection<Profile> _profiles = new ObservableCollection<Profile>();
        public ObservableCollection<Profile> Profiles
        {
            get { return _profiles; }
        }

        private Profile _currentProfile;
        public Profile CurrentProfile
        {
            get { return _currentProfile; }
            set
            {

                if (_currentProfile == value)
                    return;

                _currentProfile = value;
                NotifyPropertyChanged("CurrentProfile");

                SelectedProfile = _currentProfile;

                _volume = _currentProfile.Volume;
                NotifyPropertyChanged("Volume");
            }
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

                NotifyPropertyChanged("SelectedProfile");

                // "quietly"
                _selectedQuickProfile = _selectedProfile;
                NotifyPropertyChanged("SelectedQuickProfile");

            }
        }

        public bool IsProfileSwitchOK
        {
            get
            {
                if (IsBusy || IsConnecting || IsWorking)
                    return false;
                else
                    return true;
            }
        }

        private Profile _selectedQuickProfile;
        public Profile SelectedQuickProfile
        {
            get
            {
                return _selectedQuickProfile;
            }
            set
            {
                if (_selectedQuickProfile == value)
                    return;

                if (IsProfileSwitchOK)
                {
                    _selectedQuickProfile = value;

                    if (_selectedQuickProfile != null)
                    {
                        SelectedProfile = _selectedQuickProfile;
                        CurrentProfile = _selectedQuickProfile;

                        ChangeConnection(_selectedQuickProfile);
                    }
                }

                NotifyPropertyChanged("SelectedQuickProfile");
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
                    SetError("Host", MPDCtrl.Properties.Resources.Settings_ErrorHostMustBeSpecified);

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
                        SetError("Host", MPDCtrl.Properties.Resources.Settings_ErrorHostInvalidAddressFormat);
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
                    SetError("Port", MPDCtrl.Properties.Resources.Settings_ErrorPortMustBeSpecified);
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
                        SetError("Port", MPDCtrl.Properties.Resources.Settings_ErrorInvalidPortNaN);
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
                return DummyPassword(_password);
            }
            set
            {
                // Don't. if (_password == value) ...

                _password = value;

                NotifyPropertyChanged("IsNotPasswordSet");
                NotifyPropertyChanged("IsPasswordSet");
                NotifyPropertyChanged("Password");
            }
        }

        private string Encrypt(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }

            byte[] entropy = new byte[] { 0x72, 0xa2, 0x12, 0x04 };

            try
            {
                byte[] userData = System.Text.Encoding.UTF8.GetBytes(s);

                byte[] encryptedData = ProtectedData.Protect(userData, entropy, DataProtectionScope.CurrentUser);

                return System.Convert.ToBase64String(encryptedData);
            }
            catch
            {
                return "";
            }
        }

        private string Decrypt(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }

            byte[] entropy = new byte[] { 0x72, 0xa2, 0x12, 0x04 };

            try
            {
                byte[] encryptedData = System.Convert.FromBase64String(s);

                byte[] userData = ProtectedData.Unprotect(encryptedData, entropy, DataProtectionScope.CurrentUser);

                return System.Text.Encoding.UTF8.GetString(userData);
            }
            catch
            {
                return "";
            }
        }

        private string DummyPassword(string s)
        {
            if (String.IsNullOrEmpty(s)) { return ""; }
            string e = "";
            for (int i = 1; i <= s.Length; i++)
            {
                e = e + "*";
            }
            return e;
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

        public bool IsPasswordSet
        {
            get
            {
                if (SelectedProfile != null)
                {
                    if (!string.IsNullOrEmpty(SelectedProfile.Password))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsNotPasswordSet
        {
            get
            {
                if (IsPasswordSet)
                    return false;
                else
                    return true;
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
                    if (Profiles.Count == 1)
                    {
                        return;
                    }
                }

                _setIsDefault = value;

                NotifyPropertyChanged("SetIsDefault");
            }
        }

        private bool _isUpdateOnStartup = true;
        public bool IsUpdateOnStartup
        {
            get { return _isUpdateOnStartup; }
            set
            {
                if (_isUpdateOnStartup == value)
                    return;

                _isUpdateOnStartup = value;

                NotifyPropertyChanged("IsUpdateOnStartup");
            }
        }

        private bool _isAutoScrollToNowPlaying = false;
        public bool IsAutoScrollToNowPlaying
        {
            get { return _isAutoScrollToNowPlaying; }
            set
            {
                if (_isAutoScrollToNowPlaying == value)
                    return;

                _isAutoScrollToNowPlaying = value;

                NotifyPropertyChanged("IsAutoScrollToNowPlaying");
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

                NotifyPropertyChanged("IsSaveLog");
            }
        }

        private bool _isDownloadAlbumArt = false;
        public bool IsDownloadAlbumArt
        {
            get { return _isDownloadAlbumArt; }
            set
            {
                if (_isDownloadAlbumArt == value)
                    return;

                _isDownloadAlbumArt = value;

                NotifyPropertyChanged("IsDownloadAlbumArt");
            }
        }

        private bool _isDownloadAlbumArtEmbeddedUsingReadPicture = false;
        public bool IsDownloadAlbumArtEmbeddedUsingReadPicture
        {
            get { return _isDownloadAlbumArtEmbeddedUsingReadPicture; }
            set
            {
                if (_isDownloadAlbumArtEmbeddedUsingReadPicture == value)
                    return;

                _isDownloadAlbumArtEmbeddedUsingReadPicture = value;

                NotifyPropertyChanged("IsDownloadAlbumArtEmbeddedUsingReadPicture");
            }
        }

        private bool _isSwitchingProfile;
        public bool IsSwitchingProfile
        {
            get
            {
                return _isSwitchingProfile;
            }
            set
            {
                if (_isSwitchingProfile == value)
                    return;

                _isSwitchingProfile = value;
                NotifyPropertyChanged("IsSwitchingProfile");
            }
        }

        #endregion

        // TODO: 整理
        #region == ダイアログ ==

        private string _changePasswordDialogMessage;
        public string ChangePasswordDialogMessage
        {
            get { return _changePasswordDialogMessage; }
            set
            {
                if (_changePasswordDialogMessage == value)
                    return;

                _changePasswordDialogMessage = value;
                NotifyPropertyChanged("ChangePasswordDialogMessage");
            }
        }


        /*
        private string _dialogTitle;
        public string DialogTitle
        {
            get { return _dialogTitle; }
            set
            {
                if (_dialogTitle == value)
                    return;

                _dialogTitle = value;
                NotifyPropertyChanged("DialogTitle");
            }
        }



        private string _dialogInputText;
        public string DialogInputText
        {
            get { return _dialogInputText; }
            set
            {
                if (_dialogInputText == value)
                    return;

                _dialogInputText = value;
                NotifyPropertyChanged("DialogInputText");
            }
        }

        public ObservableCollection<String> PlaylistNamesWithNew { get; set; } = new ObservableCollection<String>();

        // Not smart...
        public Func<string, bool> DialogResultFunction { get; set; }
        public Func<string, string, bool> DialogResultFunctionWith2Params { get; set; }
        public Func<string, List<string>, bool> DialogResultFunctionWith2ParamsWithObject { get; set; }
        public string DialogResultFunctionParamString { get; set; }
        public List<string> DialogResultFunctionParamObject { get; set; }

        public void ResetDialog()
        {
            DialogTitle = "";
            DialogMessage = "";
            DialogInputText = "";
            DialogResultFunction = null;
            DialogResultFunctionWith2Params = null;
            DialogResultFunctionWith2ParamsWithObject = null;
            DialogResultFunctionParamString = "";
            DialogResultFunctionParamObject = null;
        }
        */

        #endregion

        #region == Popup ==

        private List<string> queueListviewSelectedQueueSongIdsForPopup = new List<string>();
        private List<string> searchResultListviewSelectedQueueSongUriForPopup = new List<string>();
        private List<string> songFilesListviewSelectedQueueSongUriForPopup = new List<string>();

        private bool _isSaveAsPlaylistPopupVisible;
        public bool IsSaveAsPlaylistPopupVisible
        {
            get
            {
                return _isSaveAsPlaylistPopupVisible;
            }
            set
            {
                if (_isSaveAsPlaylistPopupVisible == value)
                    return;

                _isSaveAsPlaylistPopupVisible = value;
                NotifyPropertyChanged("IsSaveAsPlaylistPopupVisible");
            }
        }

        private bool _isConfirmClearQueuePopupVisible;
        public bool IsConfirmClearQueuePopupVisible
        {
            get
            {
                return _isConfirmClearQueuePopupVisible;
            }
            set
            {
                if (_isConfirmClearQueuePopupVisible == value)
                    return;

                _isConfirmClearQueuePopupVisible = value;
                NotifyPropertyChanged("IsConfirmClearQueuePopupVisible");
            }
        }

        private bool _isSelectedSaveToPopupVisible;
        public bool IsSelectedSaveToPopupVisible
        {
            get
            {
                return _isSelectedSaveToPopupVisible;
            }
            set
            {
                if (_isSelectedSaveToPopupVisible == value)
                    return;

                _isSelectedSaveToPopupVisible = value;
                NotifyPropertyChanged("IsSelectedSaveToPopupVisible");
            }
        }

        private bool _isSelectedSaveAsPopupVisible;
        public bool IsSelectedSaveAsPopupVisible
        {
            get
            {
                return _isSelectedSaveAsPopupVisible;
            }
            set
            {
                if (_isSelectedSaveAsPopupVisible == value)
                    return;

                _isSelectedSaveAsPopupVisible = value;
                NotifyPropertyChanged("IsSelectedSaveAsPopupVisible");
            }
        }

        private bool _isConfirmDeleteQueuePopupVisible;
        public bool IsConfirmDeleteQueuePopupVisible
        {
            get
            {
                return _isConfirmDeleteQueuePopupVisible;
            }
            set
            {
                if (_isConfirmDeleteQueuePopupVisible == value)
                    return;

                _isConfirmDeleteQueuePopupVisible = value;
                NotifyPropertyChanged("IsConfirmDeleteQueuePopupVisible");
            }
        }

        private bool _isConfirmDeletePlaylistPopupVisible;
        public bool IsConfirmDeletePlaylistPopupVisible
        {
            get
            {
                return _isConfirmDeletePlaylistPopupVisible;
            }
            set
            {
                if (_isConfirmDeletePlaylistPopupVisible == value)
                    return;

                _isConfirmDeletePlaylistPopupVisible = value;
                NotifyPropertyChanged("IsConfirmDeletePlaylistPopupVisible");
            }
        }

        private bool _isConfirmUpdatePlaylistSongsPopupVisible;
        public bool IsConfirmUpdatePlaylistSongsPopupVisible
        {
            get
            {
                return _isConfirmUpdatePlaylistSongsPopupVisible;
            }
            set
            {
                if (_isConfirmUpdatePlaylistSongsPopupVisible == value)
                    return;

                _isConfirmUpdatePlaylistSongsPopupVisible = value;
                NotifyPropertyChanged("IsConfirmUpdatePlaylistSongsPopupVisible");
            }
        }

        private bool _isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible;
        public bool IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible
        {
            get
            {
                return _isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible;
            }
            set
            {
                if (_isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible == value)
                    return;

                _isConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible = value;
                NotifyPropertyChanged("IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible");
            }
        }

        private bool _isConfirmDeletePlaylistSongPopupVisible;
        public bool IsConfirmDeletePlaylistSongPopupVisible
        {
            get
            {
                return _isConfirmDeletePlaylistSongPopupVisible;
            }
            set
            {
                if (_isConfirmDeletePlaylistSongPopupVisible == value)
                    return;

                _isConfirmDeletePlaylistSongPopupVisible = value;
                NotifyPropertyChanged("IsConfirmDeletePlaylistSongPopupVisible");
            }
        }

        private bool _isConfirmPlaylistClearPopupVisible;
        public bool IsConfirmPlaylistClearPopupVisible
        {
            get
            {
                return _isConfirmPlaylistClearPopupVisible;
            }
            set
            {
                if (_isConfirmPlaylistClearPopupVisible == value)
                    return;

                _isConfirmPlaylistClearPopupVisible = value;
                NotifyPropertyChanged("IsConfirmPlaylistClearPopupVisible");
            }
        }

        private bool _isSearchResultSelectedSaveAsPopupVisible;
        public bool IsSearchResultSelectedSaveAsPopupVisible
        {
            get
            {
                return _isSearchResultSelectedSaveAsPopupVisible;
            }
            set
            {
                if (_isSearchResultSelectedSaveAsPopupVisible == value)
                    return;

                _isSearchResultSelectedSaveAsPopupVisible = value;
                NotifyPropertyChanged("IsSearchResultSelectedSaveAsPopupVisible");
            }
        }

        private bool _isSearchResultSelectedSaveToPopupVisible;
        public bool IsSearchResultSelectedSaveToPopupVisible
        {
            get
            {
                return _isSearchResultSelectedSaveToPopupVisible;
            }
            set
            {
                if (_isSearchResultSelectedSaveToPopupVisible == value)
                    return;

                _isSearchResultSelectedSaveToPopupVisible = value;
                NotifyPropertyChanged("IsSearchResultSelectedSaveToPopupVisible");
            }
        }

        private bool _sSongFilesSelectedSaveAsPopupVisible;
        public bool IsSongFilesSelectedSaveAsPopupVisible
        {
            get
            {
                return _sSongFilesSelectedSaveAsPopupVisible;
            }
            set
            {
                if (_sSongFilesSelectedSaveAsPopupVisible == value)
                    return;

                _sSongFilesSelectedSaveAsPopupVisible = value;
                NotifyPropertyChanged("IsSongFilesSelectedSaveAsPopupVisible");
            }
        }

        private bool _isSongFilesSelectedSaveToPopupVisible;
        public bool IsSongFilesSelectedSaveToPopupVisible
        {
            get
            {
                return _isSongFilesSelectedSaveToPopupVisible;
            }
            set
            {
                if (_isSongFilesSelectedSaveToPopupVisible == value)
                    return;

                _isSongFilesSelectedSaveToPopupVisible = value;
                NotifyPropertyChanged("IsSongFilesSelectedSaveToPopupVisible");
            }
        }

        #endregion

        #region == イベント ==

        // DebugWindow
        public delegate void DebugWindowShowHideEventHandler();
        public event DebugWindowShowHideEventHandler DebugWindowShowHide;

        public event EventHandler<string> DebugCommandOutput;

        public event EventHandler<string> DebugIdleOutput;

        public delegate void DebugCommandClearEventHandler();
        public event DebugCommandClearEventHandler DebugCommandClear;

        public delegate void DebugIdleClearEventHandler();
        public event DebugIdleClearEventHandler DebugIdleClear;

        // AckWindow
        public event EventHandler<string> AckWindowOutput;

        public delegate void AckWindowClearEventHandler();
        public event AckWindowClearEventHandler AckWindowClear;

        // Queue listview ScrollIntoView
        public event EventHandler<int> ScrollIntoView;

        // Queue listview ScrollIntoView and select (for filter)
        public event EventHandler<int> ScrollIntoViewAndSelect;

        //public delegate void QueueSelectionClearEventHandler();
        //public event QueueSelectionClearEventHandler QueueSelectionClear;

        #endregion

        private MPC _mpc = new MPC();

        public MainViewModel()
        {
            #region == データ保存フォルダ ==

            // データ保存フォルダの取得
            _appDataFolder = _envDataFolder + System.IO.Path.DirectorySeparatorChar + _appDeveloper + System.IO.Path.DirectorySeparatorChar + _appName;
            // 設定ファイルのパス
            _appConfigFilePath = _appDataFolder + System.IO.Path.DirectorySeparatorChar + _appName + ".config";
            // 存在していなかったら作成
            System.IO.Directory.CreateDirectory(_appDataFolder);

            #endregion

            #region == コマンドの初期化 ==

            PlayCommand = new RelayCommand(PlayCommand_ExecuteAsync, PlayCommand_CanExecute);
            PlayStopCommand = new RelayCommand(PlayStopCommand_Execute, PlayStopCommand_CanExecute);
            PlayPauseCommand = new RelayCommand(PlayPauseCommand_Execute, PlayPauseCommand_CanExecute);
            PlayNextCommand = new RelayCommand(PlayNextCommand_ExecuteAsync, PlayNextCommand_CanExecute);
            PlayPrevCommand = new RelayCommand(PlayPrevCommand_ExecuteAsync, PlayPrevCommand_CanExecute);
            ChangeSongCommand = new RelayCommand(ChangeSongCommand_ExecuteAsync, ChangeSongCommand_CanExecute);

            SetRpeatCommand = new RelayCommand(SetRpeatCommand_ExecuteAsync, SetRpeatCommand_CanExecute);
            SetRandomCommand = new RelayCommand(SetRandomCommand_ExecuteAsync, SetRandomCommand_CanExecute);
            SetConsumeCommand = new RelayCommand(SetConsumeCommand_ExecuteAsync, SetConsumeCommand_CanExecute);
            SetSingleCommand = new RelayCommand(SetSingleCommand_ExecuteAsync, SetSingleCommand_CanExecute);

            SetVolumeCommand = new RelayCommand(SetVolumeCommand_ExecuteAsync, SetVolumeCommand_CanExecute);
            VolumeMuteCommand = new RelayCommand(VolumeMuteCommand_Execute, VolumeMuteCommand_CanExecute);
            VolumeUpCommand = new RelayCommand(VolumeUpCommand_Execute, VolumeUpCommand_CanExecute);
            VolumeDownCommand = new RelayCommand(VolumeDownCommand_Execute, VolumeDownCommand_CanExecute);

            SetSeekCommand = new RelayCommand(SetSeekCommand_ExecuteAsync, SetSeekCommand_CanExecute);

            PlaylistListviewEnterKeyCommand = new RelayCommand(PlaylistListviewEnterKeyCommand_ExecuteAsync, PlaylistListviewEnterKeyCommand_CanExecute);
            PlaylistListviewLoadPlaylistCommand = new RelayCommand(PlaylistListviewLoadPlaylistCommand_ExecuteAsync, PlaylistListviewLoadPlaylistCommand_CanExecute);
            PlaylistListviewClearLoadPlaylistCommand = new RelayCommand(PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync, PlaylistListviewClearLoadPlaylistCommand_CanExecute);
            PlaylistListviewLeftDoubleClickCommand = new GenericRelayCommand<Playlist>(param => PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(param), param => PlaylistListviewLeftDoubleClickCommand_CanExecute());
            ChangePlaylistCommand = new RelayCommand(ChangePlaylistCommand_ExecuteAsync, ChangePlaylistCommand_CanExecute);
            PlaylistListviewRemovePlaylistCommand = new GenericRelayCommand<Playlist>(param => PlaylistListviewRemovePlaylistCommand_Execute(param), param => PlaylistListviewRemovePlaylistCommand_CanExecute());

            PlaylistListviewConfirmRemovePlaylistPopupCommand = new RelayCommand(PlaylistListviewConfirmRemovePlaylistPopupCommand_Execute, PlaylistListviewConfirmRemovePlaylistPopupCommand_CanExecute);
            PlaylistListviewRenamePlaylistCommand = new GenericRelayCommand<Playlist>(param => PlaylistListviewRenamePlaylistCommand_Execute(param), param => PlaylistListviewRenamePlaylistCommand_CanExecute());

            PlaylistListviewConfirmUpdatePopupCommand = new RelayCommand(PlaylistListviewConfirmUpdatePopupCommand_Execute, PlaylistListviewConfirmUpdatePopupCommand_CanExecute);
            PlaylistListviewClearCommand = new RelayCommand(PlaylistListviewClearCommand_Execute, PlaylistListviewClearCommand_CanExecute);
            PlaylistListviewClearPopupCommand = new RelayCommand(PlaylistListviewClearPopupCommand_Execute, PlaylistListviewClearPopupCommand_CanExecute);
            /*
            LocalfileListviewEnterKeyCommand = new GenericRelayCommand<object>(param => LocalfileListviewEnterKeyCommand_Execute(param), param => LocalfileListviewEnterKeyCommand_CanExecute());
            LocalfileListviewAddCommand = new GenericRelayCommand<object>(param => LocalfileListviewAddCommand_Execute(param), param => LocalfileListviewAddCommand_CanExecute());
            LocalfileListviewLeftDoubleClickCommand = new GenericRelayCommand<object>(param => LocalfileListviewLeftDoubleClickCommand_Execute(param), param => LocalfileListviewLeftDoubleClickCommand_CanExecute());
            */

            QueueListviewEnterKeyCommand = new RelayCommand(QueueListviewEnterKeyCommand_ExecuteAsync, QueueListviewEnterKeyCommand_CanExecute);
            QueueListviewLeftDoubleClickCommand = new GenericRelayCommand<SongInfoEx>(param => QueueListviewLeftDoubleClickCommand_ExecuteAsync(param), param => QueueListviewLeftDoubleClickCommand_CanExecute());
            QueueListviewDeleteCommand = new GenericRelayCommand<object>(param => QueueListviewDeleteCommand_Execute(param), param => QueueListviewDeleteCommand_CanExecute());
            QueueListviewConfirmDeletePopupCommand = new RelayCommand(QueueListviewConfirmDeletePopupCommand_Execute, QueueListviewConfirmDeletePopupCommand_CanExecute);

            QueueListviewClearCommand = new RelayCommand(QueueListviewClearCommand_ExecuteAsync, QueueListviewClearCommand_CanExecute);
            QueueListviewConfirmClearPopupCommand = new RelayCommand(QueueListviewConfirmClearPopupCommand_Execute, QueueListviewConfirmClearPopupCommand_CanExecute);
            QueueListviewSaveAsCommand = new RelayCommand(QueueListviewSaveAsCommand_ExecuteAsync, QueueListviewSaveAsCommand_CanExecute);
            QueueListviewSaveAsPopupCommand = new GenericRelayCommand<String>(param => QueueListviewSaveAsPopupCommand_Execute(param), param => QueueListviewSaveAsPopupCommand_CanExecute());
            QueueListviewMoveUpCommand = new GenericRelayCommand<object>(param => QueueListviewMoveUpCommand_Execute(param), param => QueueListviewMoveUpCommand_CanExecute());
            QueueListviewMoveDownCommand = new GenericRelayCommand<object>(param => QueueListviewMoveDownCommand_Execute(param), param => QueueListviewMoveDownCommand_CanExecute());

            QueueListviewSaveSelectedAsCommand = new GenericRelayCommand<object>(param => QueueListviewSaveSelectedAsCommand_Execute(param), param => QueueListviewSaveSelectedAsCommand_CanExecute());
            QueueListviewSaveSelectedAsPopupCommand = new GenericRelayCommand<String>(param => QueueListviewSaveSelectedAsPopupCommand_Execute(param), param => QueueListviewSaveSelectedAsPopupCommand_CanExecute());

            QueueListviewSaveSelectedToCommand = new GenericRelayCommand<object>(param => QueueListviewSaveSelectedToCommand_Execute(param), param => QueueListviewSaveSelectedToCommand_CanExecute());
            QueueListviewSaveSelectedToPopupCommand = new GenericRelayCommand<Playlist>(param => QueueListviewSaveSelectedToPopupCommand_Execute(param), param => QueueListviewSaveSelectedToPopupCommand_CanExecute());

            ShowSettingsCommand = new RelayCommand(ShowSettingsCommand_Execute, ShowSettingsCommand_CanExecute);
            SettingsOKCommand = new RelayCommand(SettingsOKCommand_Execute, SettingsOKCommand_CanExecute);

            NewProfileCommand = new RelayCommand(NewProfileCommand_Execute, NewProfileCommand_CanExecute);
            DeleteProfileCommand = new RelayCommand(DeleteProfileCommand_Execute, DeleteProfileCommand_CanExecute);
            SaveProfileCommand = new GenericRelayCommand<object>(param => SaveProfileCommand_Execute(param), param => SaveProfileCommand_CanExecute());
            UpdateProfileCommand = new GenericRelayCommand<object>(param => UpdateProfileCommand_Execute(param), param => UpdateProfileCommand_CanExecute());
            ChangeConnectionProfileCommand = new GenericRelayCommand<object>(param => ChangeConnectionProfileCommand_Execute(param), param => ChangeConnectionProfileCommand_CanExecute());

            /*
            ListAllCommand = new RelayCommand(ListAllCommand_ExecuteAsync, ListAllCommand_CanExecute);
            ComfirmationDialogOKCommand = new RelayCommand(ComfirmationDialogOKCommand_Execute, ComfirmationDialogOKCommand_CanExecute);
            ComfirmationDialogCancelCommand = new RelayCommand(ComfirmationDialogCancelCommand_Execute, ComfirmationDialogCancelCommand_CanExecute);
            InformationDialogOKCommand = new RelayCommand(InformationDialogOKCommand_Execute, InformationDialogOKCommand_CanExecute);

            InputDialogOKCommand = new RelayCommand(InputDialogOKCommand_Execute, InputDialogOKCommand_CanExecute);
            InputDialogCancelCommand = new RelayCommand(InputDialogCancelCommand_Execute, InputDialogCancelCommand_CanExecute);

            PlaylistSelectDialogOKCommand = new GenericRelayCommand<string>(param => PlaylistSelectDialogOKCommand_Execute(param), param => PlaylistSelectDialogOKCommand_CanExecute());
            PlaylistSelectDialogCancelCommand = new RelayCommand(PlaylistSelectDialogCancelCommand_Execute, PlaylistSelectDialogCancelCommand_CanExecute);
*/
            ShowChangePasswordDialogCommand = new GenericRelayCommand<object>(param => ShowChangePasswordDialogCommand_Execute(param), param => ShowChangePasswordDialogCommand_CanExecute());
            ChangePasswordDialogOKCommand = new GenericRelayCommand<object>(param => ChangePasswordDialogOKCommand_Execute(param), param => ChangePasswordDialogOKCommand_CanExecute());
            ChangePasswordDialogCancelCommand = new RelayCommand(ChangePasswordDialogCancelCommand_Execute, ChangePasswordDialogCancelCommand_CanExecute);

            EscapeCommand = new RelayCommand(EscapeCommand_ExecuteAsync, EscapeCommand_CanExecute);

            QueueColumnHeaderPositionShowHideCommand = new RelayCommand(QueueColumnHeaderPositionShowHideCommand_Execute, QueueColumnHeaderPositionShowHideCommand_CanExecute);
            QueueColumnHeaderNowPlayingShowHideCommand = new RelayCommand(QueueColumnHeaderNowPlayingShowHideCommand_Execute, QueueColumnHeaderNowPlayingShowHideCommand_CanExecute);
            QueueColumnHeaderTimeShowHideCommand = new RelayCommand(QueueColumnHeaderTimeShowHideCommand_Execute, QueueColumnHeaderTimeShowHideCommand_CanExecute);
            QueueColumnHeaderArtistShowHideCommand = new RelayCommand(QueueColumnHeaderArtistShowHideCommand_Execute, QueueColumnHeaderArtistShowHideCommand_CanExecute);
            QueueColumnHeaderAlbumShowHideCommand = new RelayCommand(QueueColumnHeaderAlbumShowHideCommand_Execute, QueueColumnHeaderAlbumShowHideCommand_CanExecute);
            QueueColumnHeaderGenreShowHideCommand = new RelayCommand(QueueColumnHeaderGenreShowHideCommand_Execute, QueueColumnHeaderGenreShowHideCommand_CanExecute);
            QueueColumnHeaderLastModifiedShowHideCommand = new RelayCommand(QueueColumnHeaderLastModifiedShowHideCommand_Execute, QueueColumnHeaderLastModifiedShowHideCommand_CanExecute);

            ShowFindCommand = new RelayCommand(ShowFindCommand_Execute, ShowFindCommand_CanExecute);

            QueueFindShowHideCommand = new RelayCommand(QueueFindShowHideCommand_Execute, QueueFindShowHideCommand_CanExecute);

            SearchExecCommand = new RelayCommand(SearchExecCommand_Execute, SearchExecCommand_CanExecute);
            FilterMusicEntriesClearCommand = new RelayCommand(FilterMusicEntriesClearCommand_Execute, FilterMusicEntriesClearCommand_CanExecute);

            FilterQueueClearCommand = new RelayCommand(FilterQueueClearCommand_Execute, FilterQueueClearCommand_CanExecute);

            SearchResultListviewSaveSelectedAsCommand = new GenericRelayCommand<object>(param => SearchResultListviewSaveSelectedAsCommand_Execute(param), param => SearchResultListviewSaveSelectedAsCommand_CanExecute());
            SearchResultListviewSaveSelectedAsPopupCommand = new GenericRelayCommand<string>(param => SearchResultListviewSaveSelectedAsPopupCommand_Execute(param), param => SearchResultListviewSaveSelectedAsPopupCommand_CanExecute());
            SearchResultListviewSaveSelectedToCommand = new GenericRelayCommand<object>(param => SearchResultListviewSaveSelectedToCommand_Execute(param), param => SearchResultListviewSaveSelectedToCommand_CanExecute());
            SearchResultListviewSaveSelectedToPopupCommand = new GenericRelayCommand<Playlist>(param => SearchResultListviewSaveSelectedToPopupCommand_Execute(param), param => SearchResultListviewSaveSelectedToPopupCommand_CanExecute());


            SongsListviewAddCommand = new GenericRelayCommand<object>(param => SongsListviewAddCommand_Execute(param), param => SongsListviewAddCommand_CanExecute());
            SongFilesListviewAddCommand = new GenericRelayCommand<object>(param => SongFilesListviewAddCommand_Execute(param), param => SongFilesListviewAddCommand_CanExecute());

            SongFilesListviewSaveSelectedAsCommand = new GenericRelayCommand<object>(param => SongFilesListviewSaveSelectedAsCommand_Execute(param), param => SongFilesListviewSaveSelectedAsCommand_CanExecute());
            SongFilesListviewSaveSelectedAsPopupCommand = new GenericRelayCommand<string>(param => SongFilesListviewSaveSelectedAsPopupCommand_Execute(param), param => SongFilesListviewSaveSelectedAsPopupCommand_CanExecute());
            SongFilesListviewSaveSelectedToCommand = new GenericRelayCommand<object>(param => SongFilesListviewSaveSelectedToCommand_Execute(param), param => SongFilesListviewSaveSelectedToCommand_CanExecute());
            SongFilesListviewSaveSelectedToPopupCommand = new GenericRelayCommand<Playlist>(param => SongFilesListviewSaveSelectedToPopupCommand_Execute(param), param => SongFilesListviewSaveSelectedToPopupCommand_CanExecute());


            ScrollIntoNowPlayingCommand = new RelayCommand(ScrollIntoNowPlayingCommand_Execute, ScrollIntoNowPlayingCommand_CanExecute);

            ClearDebugCommandTextCommand = new RelayCommand(ClearDebugCommandTextCommand_Execute, ClearDebugCommandTextCommand_CanExecute);
            ClearDebugIdleTextCommand = new RelayCommand(ClearDebugIdleTextCommand_Execute, ClearDebugIdleTextCommand_CanExecute);
            ShowDebugWindowCommand = new RelayCommand(ShowDebugWindowCommand_Execute, ShowDebugWindowCommand_CanExecute);

            ClearAckTextCommand = new RelayCommand(ClearAckTextCommand_Execute, ClearAckTextCommand_CanExecute);
            ShowAckWindowCommand = new RelayCommand(ShowAckWindowCommand_Execute, ShowAckWindowCommand_CanExecute);

            PlaylistListviewDeletePosCommand = new GenericRelayCommand<object>(param => PlaylistListviewDeletePosCommand_Execute(param), param => PlaylistListviewDeletePosCommand_CanExecute());
            PlaylistListviewDeletePosPopupCommand = new RelayCommand(PlaylistListviewDeletePosPopupCommand_Execute, PlaylistListviewDeletePosPopupCommand_CanExecute);
            PlaylistListviewConfirmDeletePosNotSupportedPopupCommand = new RelayCommand(PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_Execute, PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_CanExecute);

            QueueFilterSelectCommand = new GenericRelayCommand<object>(param => QueueFilterSelectCommand_Execute(param), param => QueueFilterSelectCommand_CanExecute());


            TreeviewMenuItemLoadPlaylistCommand = new RelayCommand(TreeviewMenuItemLoadPlaylistCommand_Execute, TreeviewMenuItemLoadPlaylistCommand_CanExecute);
            TreeviewMenuItemClearLoadPlaylistCommand = new RelayCommand(TreeviewMenuItemClearLoadPlaylistCommand_Execute, TreeviewMenuItemClearLoadPlaylistCommand_CanExecute);

            #endregion

            #region == イベントへのサブスクライブ ==

            _mpc.IsBusy += new MPC.IsBusyEvent(OnMpcIsBusy);

            _mpc.MpdIdleConnected += new MPC.IsMpdIdleConnectedEvent(OnMpdIdleConnected);

            _mpc.DebugCommandOutput += new MPC.DebugCommandOutputEvent(OnDebugCommandOutput);
            _mpc.DebugIdleOutput += new MPC.DebugIdleOutputEvent(OnDebugIdleOutput);

            _mpc.ConnectionStatusChanged += new MPC.ConnectionStatusChangedEvent(OnConnectionStatusChanged);
            _mpc.ConnectionError += new MPC.ConnectionErrorEvent(OnConnectionError);

            _mpc.MpdPlayerStatusChanged += new MPC.MpdPlayerStatusChangedEvent(OnMpdPlayerStatusChanged);
            _mpc.MpdCurrentQueueChanged += new MPC.MpdCurrentQueueChangedEvent(OnMpdCurrentQueueChanged);
            _mpc.MpdPlaylistsChanged += new MPC.MpdPlaylistsChangedEvent(OnMpdPlaylistsChanged);

            _mpc.MpdAckError += new MPC.MpdAckErrorEvent(OnMpdAckError);

            _mpc.MpdAlbumArtChanged += new MPC.MpdAlbumArtChangedEvent(OnAlbumArtChanged);

            //_mpc.MpcInfo += new MPC.MpcInfoEvent(OnMpcInfoEvent);

            _mpc.MpcProgress += new MPC.MpcProgressEvent(OnMpcProgress);

            #endregion

            #region == タイマー ==  

            // Init Song's time elapsed timer.
            _elapsedTimer = new System.Timers.Timer(500);
            _elapsedTimer.Elapsed += new System.Timers.ElapsedEventHandler(ElapsedTimer);

            #endregion

        }

        #region == Startup and Shutdown ==

        // 起動時の処理
        public void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            #region == アプリ設定のロード  ==

            try
            {
                // アプリ設定情報の読み込み
                if (File.Exists(_appConfigFilePath))
                {
                    XDocument xdoc = XDocument.Load(_appConfigFilePath);

                    #region == ウィンドウ関連 ==

                    if (sender is Window)
                    {
                        // Main Window element
                        var mainWindow = xdoc.Root.Element("MainWindow");
                        if (mainWindow != null)
                        {
                            var hoge = mainWindow.Attribute("top");
                            if (hoge != null)
                            {
                                (sender as Window).Top = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("left");
                            if (hoge != null)
                            {
                                (sender as Window).Left = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("height");
                            if (hoge != null)
                            {
                                (sender as Window).Height = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("width");
                            if (hoge != null)
                            {
                                (sender as Window).Width = double.Parse(hoge.Value);
                            }

                            hoge = mainWindow.Attribute("state");
                            if (hoge != null)
                            {
                                if (hoge.Value == "Maximized")
                                {
                                    (sender as Window).WindowState = WindowState.Maximized;
                                }
                                else if (hoge.Value == "Normal")
                                {
                                    (sender as Window).WindowState = WindowState.Normal;
                                }
                                else if (hoge.Value == "Minimized")
                                {
                                    (sender as Window).WindowState = WindowState.Normal;
                                }
                            }
                        }
                    }



                    #endregion

                    #region == オプション設定 ==

                    var opts = xdoc.Root.Element("Options");
                    if (opts != null)
                    {
                        var hoge = opts.Attribute("AutoScrollToNowPlaying");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsAutoScrollToNowPlaying = true;
                            }
                            else
                            {
                                IsAutoScrollToNowPlaying = false;
                            }
                        }

                        hoge = opts.Attribute("UpdateOnStartup");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsUpdateOnStartup = true;
                            }
                            else
                            {
                                IsUpdateOnStartup = false;
                            }
                        }

                        hoge = opts.Attribute("ShowDebugWindow");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsShowDebugWindow = true;

                            }
                            else
                            {
                                IsShowDebugWindow = false;
                            }
                        }

                        hoge = opts.Attribute("SaveLog");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsSaveLog = true;

                            }
                            else
                            {
                                IsSaveLog = false;
                            }
                        }

                        hoge = opts.Attribute("DownloadAlbumArt");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsDownloadAlbumArt = true;

                            }
                            else
                            {
                                IsDownloadAlbumArt = false;
                            }
                        }

                        hoge = opts.Attribute("DownloadAlbumArtEmbeddedUsingReadPicture");
                        if (hoge != null)
                        {
                            if (hoge.Value == "True")
                            {
                                IsDownloadAlbumArtEmbeddedUsingReadPicture = true;

                            }
                            else
                            {
                                IsDownloadAlbumArtEmbeddedUsingReadPicture = false;
                            }
                        }
                    }

                    #endregion

                    #region == プロファイル設定  ==

                    var xProfiles = xdoc.Root.Element("Profiles");
                    if (xProfiles != null)
                    {
                        var profileList = xProfiles.Elements("Profile");

                        foreach (var p in profileList)
                        {
                            Profile pro = new Profile();

                            if (p.Attribute("Name") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Name").Value))
                                    pro.Name = p.Attribute("Name").Value;
                            }
                            if (p.Attribute("Host") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Host").Value))
                                    pro.Host = p.Attribute("Host").Value;
                            }
                            if (p.Attribute("Port") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Port").Value))
                                {
                                    try
                                    {
                                        pro.Port = Int32.Parse(p.Attribute("Port").Value);
                                    }
                                    catch
                                    {
                                        pro.Port = 6600;
                                    }
                                }
                            }
                            if (p.Attribute("Password") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Password").Value))
                                    pro.Password = Decrypt(p.Attribute("Password").Value);
                            }
                            if (p.Attribute("IsDefault") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("IsDefault").Value))
                                {
                                    if (p.Attribute("IsDefault").Value == "True")
                                    {
                                        pro.IsDefault = true;

                                        CurrentProfile = pro;
                                        SelectedProfile = pro;
                                    }
                                }
                            }
                            if (p.Attribute("Volume") != null)
                            {
                                if (!string.IsNullOrEmpty(p.Attribute("Volume").Value))
                                {
                                    try
                                    {
                                        pro.Volume = double.Parse(p.Attribute("Volume").Value);
                                    }
                                    catch
                                    {
                                        pro.Volume = 50;
                                    }
                                }
                            }

                            Profiles.Add(pro);
                        }
                    }
                    #endregion

                    #region == ヘッダーカラム設定 ==

                    var Headers = xdoc.Root.Element("Headers");///Queue/Position
                    if (Headers != null)
                    {
                        var Que = Headers.Element("Queue");
                        if (Que != null)
                        {
                            var column = Que.Element("Position");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                        {
                                            QueueColumnHeaderPositionVisibility = true;
                                        }
                                        else
                                        {
                                            QueueColumnHeaderPositionVisibility = false;
                                        }
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderPositionWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderPositionWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderPositionWidth > 0)
                                    QueueColumnHeaderPositionWidthRestore = QueueColumnHeaderPositionWidth;
                            }
                            column = Que.Element("NowPlaying");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderNowPlayingVisibility = true;
                                        else
                                            QueueColumnHeaderNowPlayingVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderNowPlayingWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderNowPlayingWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderNowPlayingWidth > 0)
                                    QueueColumnHeaderNowPlayingWidthRestore = QueueColumnHeaderNowPlayingWidth;
                            }
                            column = Que.Element("Title");
                            if (column != null)
                            {
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderTitleWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                            if (QueueColumnHeaderTitleWidth < 120)
                                                QueueColumnHeaderTitleWidth = 160;
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderTitleWidth = 160;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderTitleWidth > 0)
                                    QueueColumnHeaderTitleWidthRestore = QueueColumnHeaderTitleWidth;
                            }
                            column = Que.Element("Time");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderTimeVisibility = true;
                                        else
                                            QueueColumnHeaderTimeVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderTimeWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderTimeWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderTimeWidth > 0)
                                    QueueColumnHeaderTimeWidthRestore = QueueColumnHeaderTimeWidth;
                            }
                            column = Que.Element("Artist");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderArtistVisibility = true;
                                        else
                                            QueueColumnHeaderArtistVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderArtistWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderArtistWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderArtistWidth > 0)
                                    QueueColumnHeaderArtistWidthRestore = QueueColumnHeaderArtistWidth;
                            }
                            column = Que.Element("Album");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderAlbumVisibility = true;
                                        else
                                            QueueColumnHeaderAlbumVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderAlbumWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderAlbumWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderAlbumWidth > 0)
                                    QueueColumnHeaderAlbumWidthRestore = QueueColumnHeaderAlbumWidth;
                            }
                            column = Que.Element("Genre");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderGenreVisibility = true;
                                        else
                                            QueueColumnHeaderGenreVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderGenreWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderGenreWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderGenreWidth > 0)
                                    QueueColumnHeaderGenreWidthRestore = QueueColumnHeaderGenreWidth;
                            }
                            column = Que.Element("LastModified");
                            if (column != null)
                            {
                                if (column.Attribute("Visible") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Visible").Value))
                                    {
                                        if (column.Attribute("Visible").Value.ToString() == "True")
                                            QueueColumnHeaderLastModifiedVisibility = true;
                                        else
                                            QueueColumnHeaderLastModifiedVisibility = false;
                                    }
                                }
                                if (column.Attribute("Width") != null)
                                {
                                    if (!string.IsNullOrEmpty(column.Attribute("Width").Value))
                                    {
                                        try
                                        {
                                            QueueColumnHeaderLastModifiedWidth = Double.Parse(column.Attribute("Width").Value.ToString());
                                        }
                                        catch
                                        {
                                            QueueColumnHeaderLastModifiedWidth = 53;
                                        }
                                    }
                                }
                                if (QueueColumnHeaderLastModifiedWidth > 0)
                                    QueueColumnHeaderLastModifiedWidthRestore = QueueColumnHeaderLastModifiedWidth;
                            }
                        }
                    }

                    #endregion

                    #region == レイアウト ==

                    var lay = xdoc.Root.Element("Layout");
                    if (lay != null)
                    {
                        var leftpain = lay.Element("LeftPain");
                        if (leftpain != null)
                        {
                            if (leftpain.Attribute("Width") != null)
                            {
                                if (!string.IsNullOrEmpty(leftpain.Attribute("Width").Value))
                                {
                                    try
                                    {
                                        MainLeftPainWidth = Double.Parse(leftpain.Attribute("Width").Value.ToString());
                                    }
                                    catch
                                    {
                                        MainLeftPainWidth = 241;
                                    }
                                }
                            }
                        }
                    }

                    #endregion

                }
            }
            catch (System.IO.FileNotFoundException)
            {
                Debug.WriteLine("■■■■■ Error  設定ファイルのロード中 - FileNotFoundException : " + _appConfigFilePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("■■■■■ Error  設定ファイルのロード中: " + ex + " while opening : " + _appConfigFilePath);
            }

            #endregion

            IsFullyLoaded = true;

            NotifyPropertyChanged("IsCurrentProfileSet");
            if (CurrentProfile == null)
            {
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.Init_NewConnectionSetting;
                StatusButton = _pathNewConnectionButton;
                IsConnectionSettingShow = true;
            }
            else
            {
                IsConnectionSettingShow = false;

                // set this "quietly"
                _volume = CurrentProfile.Volume;
                NotifyPropertyChanged("Volume");

                Start(CurrentProfile.Host, CurrentProfile.Port, CurrentProfile.Password);
            }

            // log
            if (IsSaveLog)
            {
                App app = App.Current as App;
                if (app != null)
                {
                    app.IsSaveErrorLog = true;
                    app.LogFilePath = System.Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + System.IO.Path.DirectorySeparatorChar + "MPDCtrl_errors.txt";
                }
            }
        }

        // 起動後画面が描画された時の処理
        public void OnContentRendered(object sender, EventArgs e)
        {
            IsFullyRendered = true;
        }

        // 終了時の処理
        public void OnWindowClosing(object sender, CancelEventArgs e)
        {
            // 二重起動とか途中でシャットダウンした時にデータが消えないように。
            if (!IsFullyLoaded)
                return;

            double windowWidth = 780;

            #region == アプリ設定の保存 ==

            // 設定ファイル用のXMLオブジェクト
            XmlDocument doc = new XmlDocument();
            XmlDeclaration xmlDeclaration = doc.CreateXmlDeclaration("1.0", "UTF-8", null);
            doc.InsertBefore(xmlDeclaration, doc.DocumentElement);

            // Root Document Element
            XmlElement root = doc.CreateElement(string.Empty, "App", string.Empty);
            doc.AppendChild(root);

            XmlAttribute attrs = doc.CreateAttribute("Version");
            attrs.Value = _appVer;
            root.SetAttributeNode(attrs);

            #region == ウィンドウ関連 ==

            // MainWindow
            if (sender is Window)
            {
                // Main Window element
                XmlElement mainWindow = doc.CreateElement(string.Empty, "MainWindow", string.Empty);

                Window w = (sender as Window);
                // Main Window attributes
                attrs = doc.CreateAttribute("height");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Height.ToString();
                }
                else
                {
                    attrs.Value = w.Height.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("width");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Width.ToString();
                    windowWidth = w.RestoreBounds.Width;
                }
                else
                {
                    attrs.Value = w.Width.ToString();
                    windowWidth = w.Width;

                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("top");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Top.ToString();
                }
                else
                {
                    attrs.Value = w.Top.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("left");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = w.RestoreBounds.Left.ToString();
                }
                else
                {
                    attrs.Value = w.Left.ToString();
                }
                mainWindow.SetAttributeNode(attrs);

                attrs = doc.CreateAttribute("state");
                if (w.WindowState == WindowState.Maximized)
                {
                    attrs.Value = "Maximized";
                }
                else if (w.WindowState == WindowState.Normal)
                {
                    attrs.Value = "Normal";

                }
                else if (w.WindowState == WindowState.Minimized)
                {
                    attrs.Value = "Minimized";
                }
                mainWindow.SetAttributeNode(attrs);

                // set MainWindow element to root.
                root.AppendChild(mainWindow);

            }

            #endregion

            #region == オプション設定の保存 ==

            XmlElement opts = doc.CreateElement(string.Empty, "Options", string.Empty);

            //
            attrs = doc.CreateAttribute("AutoScrollToNowPlaying");
            if (IsAutoScrollToNowPlaying)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            // 
            attrs = doc.CreateAttribute("UpdateOnStartup");
            if (IsUpdateOnStartup)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("ShowDebugWindow");
            if (IsShowDebugWindow)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("SaveLog");
            if (IsSaveLog)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("DownloadAlbumArt");
            if (IsDownloadAlbumArt)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            //
            attrs = doc.CreateAttribute("DownloadAlbumArtEmbeddedUsingReadPicture");
            if (IsDownloadAlbumArtEmbeddedUsingReadPicture)
            {
                attrs.Value = "True";
            }
            else
            {
                attrs.Value = "False";
            }
            opts.SetAttributeNode(attrs);

            /// 
            root.AppendChild(opts);

            #endregion

            #region == プロファイル設定  ==

            XmlElement xProfiles = doc.CreateElement(string.Empty, "Profiles", string.Empty);

            XmlElement xProfile;
            XmlAttribute xAttrs;

            foreach (var p in Profiles)
            {
                xProfile = doc.CreateElement(string.Empty, "Profile", string.Empty);

                xAttrs = doc.CreateAttribute("Name");
                xAttrs.Value = p.Name;
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Host");
                xAttrs.Value = p.Host;
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Port");
                xAttrs.Value = p.Port.ToString();
                xProfile.SetAttributeNode(xAttrs);

                xAttrs = doc.CreateAttribute("Password");
                xAttrs.Value = Encrypt(p.Password);
                xProfile.SetAttributeNode(xAttrs);

                if (p.IsDefault)
                {
                    xAttrs = doc.CreateAttribute("IsDefault");
                    xAttrs.Value = "True";
                    xProfile.SetAttributeNode(xAttrs);
                }

                xAttrs = doc.CreateAttribute("Volume");
                if (p == CurrentProfile)
                {
                    xAttrs.Value = _volume.ToString();
                }
                else
                {
                    xAttrs.Value = p.Volume.ToString();
                }
                xProfile.SetAttributeNode(xAttrs);


                xProfiles.AppendChild(xProfile);
            }

            root.AppendChild(xProfiles);

            #endregion

            #region == ヘッダーカラム設定の保存 ==

            XmlElement headers = doc.CreateElement(string.Empty, "Headers", string.Empty);

            XmlElement queueHeader;
            XmlElement queueHeaderColumn;

            XmlAttribute qAttrs;

            queueHeader = doc.CreateElement(string.Empty, "Queue", string.Empty);


            // Position
            queueHeaderColumn = doc.CreateElement(string.Empty, "Position", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderPositionVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderPositionWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderPositionWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Now Playing
            queueHeaderColumn = doc.CreateElement(string.Empty, "NowPlaying", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderNowPlayingVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderNowPlayingWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderNowPlayingWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Title skip visibility
            queueHeaderColumn = doc.CreateElement(string.Empty, "Title", string.Empty);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderTitleWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderTitleWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Time
            queueHeaderColumn = doc.CreateElement(string.Empty, "Time", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderTimeVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderTimeWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderTimeWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Artist
            queueHeaderColumn = doc.CreateElement(string.Empty, "Artist", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderArtistVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderArtistWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderArtistWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Album
            queueHeaderColumn = doc.CreateElement(string.Empty, "Album", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderAlbumVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderAlbumWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderAlbumWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Genre
            queueHeaderColumn = doc.CreateElement(string.Empty, "Genre", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderGenreVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderGenreWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderGenreWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            // Last Modified
            queueHeaderColumn = doc.CreateElement(string.Empty, "LastModified", string.Empty);

            qAttrs = doc.CreateAttribute("Visible");
            qAttrs.Value = QueueColumnHeaderLastModifiedVisibility.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            qAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
                qAttrs.Value = QueueColumnHeaderLastModifiedWidth.ToString();
            else
                qAttrs.Value = _queueColumnHeaderLastModifiedWidthUser.ToString();
            queueHeaderColumn.SetAttributeNode(qAttrs);

            queueHeader.AppendChild(queueHeaderColumn);

            //
            headers.AppendChild(queueHeader);
            ////
            root.AppendChild(headers);

            #endregion

            #region == レイアウトの保存 (使ってない、というか無効) ==

            XmlElement lay = doc.CreateElement(string.Empty, "Layout", string.Empty);

            XmlElement leftpain;
            XmlAttribute lAttrs;

            // LeftPain
            leftpain = doc.CreateElement(string.Empty, "LeftPain", string.Empty);
            lAttrs = doc.CreateAttribute("Width");
            if (IsFullyRendered)
            {
                if (windowWidth > (MainLeftPainActualWidth - 24))
                {
                    lAttrs.Value = MainLeftPainActualWidth.ToString();
                }
                else
                {
                    lAttrs.Value = "241";
                }
            }
            else
            {
                lAttrs.Value = MainLeftPainWidth.ToString();
            }
            leftpain.SetAttributeNode(lAttrs);

            //
            lay.AppendChild(leftpain);
            ////
            root.AppendChild(lay);

            #endregion

            try
            {
                // 設定ファイルの保存
                doc.Save(_appConfigFilePath);
            }
            //catch (System.IO.FileNotFoundException) { }
            catch (Exception ex)
            {
                Debug.WriteLine("■■■■■ Error  設定ファイルの保存中: " + ex + " while opening : " + _appConfigFilePath);
            }

            #endregion

            try
            {
                if (IsConnected)
                {
                    _mpc.MpdStop = true;

                    // TODO: this causes anoying exception in the dev environment. Although it's a good thing to close...
                    _mpc.MpdDisconnect();
                }
            }
            catch { }

        }

        #endregion

        #region == メソッド ==

        private async void Start(string host, int port, string password)
        {
            await _mpc.MpdIdleConnect(host, port);
        }

        private void UpdateButtonStatus()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
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
                        double tmpVol = Convert.ToDouble(_mpc.MpdStatus.MpdVolume);
                        if (_volume != tmpVol)
                        {
                            // "quietly" update.
                            _volume = tmpVol;
                            NotifyPropertyChanged("Volume");
                        }
                    }

                    _random = _mpc.MpdStatus.MpdRandom;
                    NotifyPropertyChanged("Random");

                    _repeat = _mpc.MpdStatus.MpdRepeat;
                    NotifyPropertyChanged("Repeat");

                    _consume = _mpc.MpdStatus.MpdConsume;
                    NotifyPropertyChanged("Consume");

                    _single = _mpc.MpdStatus.MpdSingle;
                    NotifyPropertyChanged("Single");

                    // no need to care about "double" updates for time.
                    Time = _mpc.MpdStatus.MpdSongTime;

                    _elapsed = _mpc.MpdStatus.MpdSongElapsed;
                    //NotifyPropertyChanged("Elapsed");

                    //start elapsed timer.
                    if (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play)
                    {
                        if (!_elapsedTimer.Enabled)
                            _elapsedTimer.Start();
                    }
                    else
                    {
                        _elapsedTimer.Stop();
                    }

                    //if (Application.Current == null) { return; }
                    //Application.Current.Dispatcher.Invoke(() => CommandManager.InvalidateRequerySuggested());

                }
                catch
                {
                    Debug.WriteLine("Error@UpdateButtonStatus");
                }

            });

        }

        private async void UpdateStatus()
        {
            UpdateButtonStatus();

            bool isAlbumArtChanged = false;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
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

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }
                }
                else
                {
                    isCurrentSongWasNull = true;
                }

                // Sets Current Song
                var item = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                if (item != null)
                {
                    CurrentSong = (item as SongInfoEx);
                    CurrentSong.IsPlaying = true;

                    if (isSongChanged)
                    {
                        if (IsAutoScrollToNowPlaying)
                            ScrollIntoView?.Invoke(this, CurrentSong.Index);

                        // AlbumArt
                        if (!String.IsNullOrEmpty(CurrentSong.File))
                        {
                            //Debug.WriteLine("AlbumArt isPlayer: " + CurrentSong.file);
                            isAlbumArtChanged = true;
                        }
                    }
                    else
                    {
                        if (isCurrentSongWasNull)
                        {
                            if (IsAutoScrollToNowPlaying)
                                ScrollIntoView?.Invoke(this, CurrentSong.Index);

                            // AlbumArt
                            if (!String.IsNullOrEmpty(CurrentSong.File))
                            {
                                //Debug.WriteLine("AlbumArt isPlayer: isCurrentSongWasNull");
                                //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                isAlbumArtChanged = true;
                            }
                        }
                        else
                        {
                            //Debug.WriteLine("AlbumArt isPlayer: !isSongChanged.");
                        }
                    }
                }
                else
                {
                    CurrentSong = null;

                    IsAlbumArtVisible = false;
                    AlbumArt = _albumArtDefault;
                }
            });

            if (IsDownloadAlbumArt)
                if (isAlbumArtChanged)
                    await _mpc.MpdQueryAlbumArt(CurrentSong.File, CurrentSong.Id, IsDownloadAlbumArtEmbeddedUsingReadPicture);
        }

        private async void UpdateCurrentQueue()
        {
            if (IsSwitchingProfile)
                return;

            bool isAlbumArtChanged = false;

            IsQueueFindVisible = false;

            if (Queue.Count > 0)
            {
                IsBusy = true;

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    // 削除する曲の一時リスト
                    List<SongInfoEx> _tmpQueue = new List<SongInfoEx>();

                    // 既存のリストの中で新しいリストにないものを削除
                    foreach (var sng in Queue)
                    {
                        if (IsSwitchingProfile)
                            return;

                        IsBusy = true;

                        var queitem = _mpc.CurrentQueue.FirstOrDefault(i => i.Id == sng.Id);
                        if (queitem == null)
                        {
                            // 削除リストに追加
                            _tmpQueue.Add(sng);
                        }
                    }

                    // 削除リストをループ
                    foreach (var hoge in _tmpQueue)
                    {
                        if (IsSwitchingProfile)
                            return;

                        IsBusy = true;

                        Queue.Remove(hoge);
                    }

                    // 新しいリストの中から既存のリストにないものを追加または更新
                    foreach (var sng in _mpc.CurrentQueue)
                    {
                        if (IsSwitchingProfile)
                            return;

                        IsBusy = true;

                        var fuga = Queue.FirstOrDefault(i => i.Id == sng.Id);
                        if (fuga != null)
                        {
                            // TODO:
                            fuga.Pos = sng.Pos;
                            //fuga.Id = sng.Id; // 流石にIDは変わらないだろう。
                            fuga.LastModified = sng.LastModified;
                            //fuga.Time = sng.Time; // format exception が煩い。
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

                            //Queue.Move(fuga.Index, sng.Index);
                            fuga.Index = sng.Index;
                        }
                        else
                        {
                            Queue.Add(sng);
                            //Queue.Insert(sng.Index, sng);
                        }
                    }

                    if (IsSwitchingProfile)
                        return;

                    IsBusy = true;

                    // Set Current and NowPlaying.
                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem != null)
                    {
                        CurrentSong = (curitem as SongInfoEx);
                        CurrentSong.IsPlaying = true;

                        if (IsAutoScrollToNowPlaying)
                            ScrollIntoView?.Invoke(this, CurrentSong.Index);

                        // AlbumArt
                        if (_mpc.AlbumCover.SongFilePath != curitem.File)
                        {
                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;

                            //Debug.WriteLine("AlbumArt isCurrentQueue: " + CurrentSong.file);

                            if (!String.IsNullOrEmpty(CurrentSong.File))
                            {
                                //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                isAlbumArtChanged = true;
                            }
                        }
                    }
                    else
                    {
                        CurrentSong = null;

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }

                    // 移動したりするとPosが変更されても順番が反映されないので、
                    var collectionView = CollectionViewSource.GetDefaultView(Queue);
                    collectionView.SortDescriptions.Add(new SortDescription("Index", ListSortDirection.Ascending));
                    collectionView.Refresh();

                });

                IsBusy = false;
            }
            else
            {
                //IsBusy = true;
                IsWorking = true;

                //Queue = _mpc.CurrentQueue;
                foreach (var hoge in _mpc.CurrentQueue)
                {
                    await Task.Delay(1);
                    
                    if (IsSwitchingProfile)
                        break;

                    //IsBusy = true;
                    IsWorking = true;

                    if (Application.Current == null) { return; }
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        Queue.Add(hoge);
                    });
                }

                /*
                // This is gonna freeze the UI...for seconds for older PCs.
                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Queue = new ObservableCollection<SongInfoEx>(_mpc.CurrentQueue);
                });
                */

                if (IsSwitchingProfile)
                    return;

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (IsSwitchingProfile)
                        return;

                    // Set Current and NowPlaying.
                    var curitem = Queue.FirstOrDefault(i => i.Id == _mpc.MpdStatus.MpdSongID);
                    if (curitem != null)
                    {
                        CurrentSong = (curitem as SongInfoEx);
                        CurrentSong.IsPlaying = true;

                        if (IsAutoScrollToNowPlaying)
                            ScrollIntoView?.Invoke(this, CurrentSong.Index);

                        // AlbumArt
                        if (_mpc.AlbumCover.SongFilePath != curitem.File)
                        {
                            IsAlbumArtVisible = false;
                            AlbumArt = _albumArtDefault;

                            //Debug.WriteLine("AlbumArt isCurrentQueue from Clear: " + CurrentSong.file);
                            if (!String.IsNullOrEmpty(CurrentSong.File))
                            {
                                //_mpc.MpdQueryAlbumArt(CurrentSong.file, CurrentSong.Id);
                                isAlbumArtChanged = true;
                            }
                        }
                    }
                    else
                    {
                        // just in case.
                        CurrentSong = null;

                        IsAlbumArtVisible = false;
                        AlbumArt = _albumArtDefault;
                    }
                });

                //IsBusy = false;
                IsWorking = false;
            }

            if (CurrentSong != null)
                if (IsDownloadAlbumArt)
                    if (isAlbumArtChanged)
                        await _mpc.MpdQueryAlbumArt(CurrentSong.File, CurrentSong.Id, IsDownloadAlbumArtEmbeddedUsingReadPicture);

        }

        private void UpdatePlaylists()
        {
            //IsBusy = true;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                NodeMenuPlaylists playlistDir = _mainMenuItems.PlaylistsDirectory;

                if (playlistDir != null)
                {
                    // Sort playlists.
                    List<string> slTmp = new List<string>();

                    foreach (var v in _mpc.Playlists)
                    {
                        slTmp.Add(v.Name);
                    }
                    slTmp.Sort();

                    foreach (var hoge in slTmp)
                    {
                        var fuga = playlistDir.Children.FirstOrDefault(i => i.Name == hoge);
                        if (fuga == null)
                        {
                            NodeMenuPlaylistItem playlistNode = new NodeMenuPlaylistItem(hoge);
                            playlistDir.Children.Add(playlistNode);
                        }
                    }

                    List<NodeTree> tobedeleted = new List<NodeTree>();
                    foreach (var hoge in playlistDir.Children)
                    {
                        var fuga = slTmp.FirstOrDefault(i => i == hoge.Name);
                        if (fuga == null)
                        {
                            tobedeleted.Add(hoge);
                        }
                        else
                        {
                            if (hoge is NodeMenuPlaylistItem)
                                (hoge as NodeMenuPlaylistItem).IsUpdateRequied = true;
                        }
                    }

                    foreach (var hoge in tobedeleted)
                    {
                        playlistDir.Children.Remove(hoge);
                    }

                    // 通知する
                    if (SelectedNodeMenu is NodeMenuPlaylistItem)
                    {
                        if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                        {
                            IsConfirmUpdatePlaylistSongsPopupVisible = true;
                        }
                    }
                }
            });

            //IsBusy = false;
        }

        private void UpdateLibrary()
        {
            Task.Run(() =>
            {
                UpdateLibraryMusic();
            });

            Task.Run(() =>
            {
                UpdateLibraryDirectories();
            });
        }

        private void UpdateLibraryMusic()
        {
            // Music files

            //IsBusy = true;
            //IsWorking = true;

            if (IsSwitchingProfile)
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MusicEntries.Clear();
            });

            var tmpMusicEntries = new ObservableCollection<NodeFile>();

            foreach (var songfile in _mpc.LocalFiles)
            {
                if (IsSwitchingProfile)
                    break;

                //await Task.Delay(5);

                if (IsSwitchingProfile)
                    break;

                //IsBusy = true;
                //IsWorking = true;

                if (string.IsNullOrEmpty(songfile.File)) continue;

                try
                {
                    Uri uri = new Uri(@"file:///" + songfile.File);
                    if (uri.IsFile)
                    {
                        string filename = System.IO.Path.GetFileName(songfile.File);//System.IO.Path.GetFileName(uri.LocalPath);
                        NodeFile hoge = new NodeFile(filename, uri, songfile.File);
                        /*
                        if (Application.Current == null) { return; }
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MusicEntries.Add(hoge);
                        });
                        */
                        tmpMusicEntries.Add(hoge);
                    }

                    if (IsSwitchingProfile)
                        break;
                }
                catch (Exception e)
                {
                    Debug.WriteLine(songfile + e.Message);

                    //IsBusy = false;
                    //IsWorking = false;
                    
                    return;
                }
            }

            if (IsSwitchingProfile)
                return;

            //IsBusy = true;
            IsWorking = true;
            
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MusicEntries = tmpMusicEntries;
            });
            
            //IsBusy = false;
            IsWorking = false;
        }

        private void UpdateLibraryDirectories()
        {
            // Directories

            //IsBusy = true;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                MusicDirectories.Clear();
            });

            try
            {
                
                var tmpMusicDirectories = new DirectoryTreeBuilder();
                /*
                await Task.Run(() =>
                {
                    //_musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());
                    tmpMusicDirectories.Load(_mpc.LocalDirectories.ToList<String>());
                });
                */
                tmpMusicDirectories.Load(_mpc.LocalDirectories.ToList<String>());

                IsWorking = true;

                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    MusicDirectories = tmpMusicDirectories.Children;
                });
                
                //_musicDirectories.Load(_mpc.LocalDirectories.ToList<String>());

                if (MusicDirectories.Count > 0)
                {
                    SelectedNodeDirectory = MusicDirectories[0] as NodeDirectory;
                }

                IsWorking = false;

                //IsBusy = false;
            }
            catch (Exception e)
            {
                Debug.WriteLine("_musicDirectories.Load: " + e.Message);

                //IsBusy = false;
            }

            //IsBusy = false;
        }

        private async void GetPlaylistSongs(NodeMenuPlaylistItem playlistNode)
        {
            if (playlistNode == null) 
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (playlistNode.PlaylistSongs.Count > 0)
                    playlistNode.PlaylistSongs.Clear();
            });

            CommandPlaylistResult result = await _mpc.MpdQueryPlaylistSongs(playlistNode.Name);
            if (result.IsSuccess)
            {
                playlistNode.PlaylistSongs = result.PlaylistSongs;

                if (SelectedNodeMenu == playlistNode)
                    PlaylistSongs = playlistNode.PlaylistSongs;

                playlistNode.IsUpdateRequied = false;
            }
        }

        private async void GetLibrary(NodeMenuLibrary librarytNode)
        {
            if (librarytNode == null)
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (MusicEntries.Count > 0)
                MusicEntries.Clear();

                if (MusicDirectories.Count > 0)
                    MusicDirectories.Clear();
            });

            CommandResult result = await _mpc.MpdQueryListAll();
            if (result.IsSuccess)
            {
                UpdateLibrary();
            }
        }

        private static int CompareVersionString(string a, string b)
        {
            return (new System.Version(a)).CompareTo(new System.Version(b));
        }

        #endregion

        #region == イベント ==

        private void OnMpdIdleConnected(MPC sender)
        {
            Debug.WriteLine("OK MPD " + _mpc.MpdVerText + " @OnMpdConnected");

            MpdVersion = _mpc.MpdVerText;

            MpdStatusButton = _pathMpdOkButton;

            // これしないとCurrentSongが表示されない。
            IsConnected = true;

            // 別スレッドで実行。
            Task.Run(() => LoadInitialData());
        }

        private async void LoadInitialData()
        {
            IsBusy = true;

            CommandResult result = await _mpc.MpdIdleSendPassword(_password);

            if (result.IsSuccess)
            {
                bool r = await _mpc.MpdCommandConnectionStart(_mpc.MpdHost, _mpc.MpdPort, _mpc.MpdPassword);

                if (r)
                {
                    if (IsUpdateOnStartup)
                    {
                        await _mpc.MpdSendUpdate();
                    }

                    result = await _mpc.MpdIdleQueryStatus();

                    if (result.IsSuccess)
                    {
                        await Task.Delay(5);
                        UpdateStatus();

                        await _mpc.MpdIdleQueryCurrentQueue();
                        await Task.Delay(5);
                        UpdateCurrentQueue();

                        await _mpc.MpdIdleQueryPlaylists();
                        await Task.Delay(5);
                        UpdatePlaylists();

                        //await _mpc.MpdIdleQueryListAll();
                        //await Task.Delay(5);
                        //UpdateLibrary();

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
                    StatusBarMessage = string.Format(MPDCtrl.Properties.Resources.StatusBarMsg_MPDVersionIsOld, _mpc.MpdVerText);
                }
                else
                {
                    StatusBarMessage = "";
                }
            }
        }

        private void OnMpdPlayerStatusChanged(MPC sender)
        {
            if (_mpc.MpdStatus.MpdError != "")
            {
                MpdStatusMessage = MpdVersion + ": " + MPDCtrl.Properties.Resources.MPD_StatusError + " - " + _mpc.MpdStatus.MpdError;
                MpdStatusButton = _pathMpdAckErrorButton;
            }
            else
            {
                MpdStatusMessage = "";
                MpdStatusButton = _pathMpdOkButton;
            }

            UpdateStatus();
        }

        private void OnMpdCurrentQueueChanged(MPC sender)
        {
            UpdateCurrentQueue();
        }

        private void OnMpdPlaylistsChanged(MPC sender)
        {
            UpdatePlaylists();
        }

        private void OnAlbumArtChanged(MPC sender)
        {
            // AlbumArt
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                if ((!_mpc.AlbumCover.IsDownloading) && _mpc.AlbumCover.IsSuccess)
                {
                    if ((CurrentSong != null) && (_mpc.AlbumCover.AlbumImageSource != null))
                    {
                        if (!String.IsNullOrEmpty(CurrentSong.File))
                        {
                            if (CurrentSong.File == _mpc.AlbumCover.SongFilePath)
                            {
                                //AlbumArt = null;
                                AlbumArt = _mpc.AlbumCover.AlbumImageSource;
                                IsAlbumArtVisible = true;
                            }
                        }
                    }
                }
            });
        }

        private void OnDebugCommandOutput(MPC sender, string data)
        {
            if (IsShowDebugWindow)
            {
                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugCommandOutput?.Invoke(this, data);
                });
            }
        }

        private void OnDebugIdleOutput(MPC sender, string data)
        {
            if (IsShowDebugWindow)
            {
                if (Application.Current == null) { return; }
                Application.Current.Dispatcher.Invoke(() =>
                {
                    DebugIdleOutput?.Invoke(this, data);
                });
            }
        }

        private void OnConnectionError(MPC sender, string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;

            IsConnected = false;
            IsConnecting = false;
            IsConnectionSettingShow = true;

            ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectionError + ": " + msg;
            StatusButton = _pathErrorInfoButton;

            StatusBarMessage = ConnectionStatusMessage;
        }

        private void OnConnectionStatusChanged(MPC sender, MPC.ConnectionStatus status)
        {

            if (status == MPC.ConnectionStatus.NeverConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_NeverConnected;
                StatusButton = _pathDisconnectedButton;

                StatusBarMessage = "";
            }
            else if (status == MPC.ConnectionStatus.Connected)
            {
                IsConnected = true;
                IsConnecting = false;
                IsNotConnectingNorConnected = false;
                IsConnectionSettingShow = false;

                //Debug.WriteLine("ConnectionStatus_Connected");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Connected;
                StatusButton = _pathConnectedButton;

                StatusBarMessage = "";
            }
            else if (status == MPC.ConnectionStatus.Connecting)
            {
                IsConnected = false;
                IsConnecting = true;
                IsNotConnectingNorConnected = false;
                //IsConnectionSettingShow = true;

                //Debug.WriteLine("ConnectionStatus_Connecting");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Connecting;
                StatusButton = _pathConnectingButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.ConnectFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_ConnectFail_Timeout");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_ConnectFail_Timeout;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.SeeConnectionErrorEvent)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_SeeConnectionErrorEvent");
                //ConnectionStatusMessage = "";
                StatusButton = _pathErrorInfoButton;

            }
            else if (status == MPC.ConnectionStatus.Disconnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_Disconnected");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Disconnected;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.DisconnectedByHost)
            {
                // TODO: not really usued now...

                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_DisconnectedByHost");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByHost;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.Disconnecting)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = false;
                //IsConnectionSettingShow = true;

                //Debug.WriteLine("ConnectionStatus_Disconnecting");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_Disconnecting;
                StatusButton = _pathConnectingButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.DisconnectedByUser)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                //IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_DisconnectedByUser");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_DisconnectedByUser;
                StatusButton = _pathDisconnectedButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.SendFail_NotConnected)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_SendFail_NotConnected");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_NotConnected;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
            else if (status == MPC.ConnectionStatus.SendFail_Timeout)
            {
                IsConnected = false;
                IsConnecting = false;
                IsNotConnectingNorConnected = true;
                IsConnectionSettingShow = true;

                Debug.WriteLine("ConnectionStatus_SendFail_Timeout");
                ConnectionStatusMessage = MPDCtrl.Properties.Resources.ConnectionStatus_SendFail_Timeout;
                StatusButton = _pathErrorInfoButton;

                StatusBarMessage = ConnectionStatusMessage;
            }
        }

        private void OnMpdAckError(MPC sender, string ackMsg)
        {
            if (string.IsNullOrEmpty(ackMsg))
                return;
            
            string s = ackMsg;
            string patternStr = @"[\[].+?[\]]";//@"[{\[].+?[}\]]";
            s = System.Text.RegularExpressions.Regex.Replace(s, patternStr, string.Empty);
            s = s.Replace("ACK ", string.Empty);
            s = s.Replace("{} ", string.Empty);
            //s = s.Replace("[] ", string.Empty);

            //AckWindowOutput?.Invoke(this, MpdVersion + ": " + MPDCtrl.Properties.Resources.MPD_CommandError + " - " + s + Environment.NewLine);
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                AckWindowOutput?.Invoke(this, MpdVersion + ": " + MPDCtrl.Properties.Resources.MPD_CommandError + " - " + s + Environment.NewLine);
            });
            
            IsShowAckWindow = true;
        }

        private void OnMpcProgress(MPC sender, string msg)
        {
            StatusBarMessage = msg;
        }

        private void OnMpcIsBusy(MPC sender, bool on)
        {
            this.IsBusy = on;
        }

        #endregion

        #region == タイマー ==

        private System.Timers.Timer _elapsedTimer;
        private void ElapsedTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if ((_elapsed < _time) && (_mpc.MpdStatus.MpdState == Status.MpdPlayState.Play))
            {
                _elapsed += 0.5;
                NotifyPropertyChanged("Elapsed");
            }
            else
            {
                _elapsedTimer.Stop();
            }
        }

        #endregion

        #region == コマンド ==

        #region == Playback play ==

        public ICommand PlayCommand { get; }
        public bool PlayCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlayCommand_ExecuteAsync()
        {
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
            }

        }

        public ICommand PlayNextCommand { get; }
        public bool PlayNextCommand_CanExecute()
        {
            if (IsBusy) return false;
            //if (Queue.Count < 1) { return false; }
            return true;
        }
        public async void PlayNextCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackNext(Convert.ToInt32(_volume));
        }

        public ICommand PlayPrevCommand { get; }
        public bool PlayPrevCommand_CanExecute()
        {
            if (IsBusy) return false;
            //if (Queue.Count < 1) { return false; }
            return true;
        }
        public async void PlayPrevCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackPrev(Convert.ToInt32(_volume));
        }

        public ICommand ChangeSongCommand { get; set; }
        public bool ChangeSongCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count < 1) { return false; }
            if (_selectedQueueSong == null) { return false; }
            return true;
        }
        public async void ChangeSongCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume), _selectedQueueSong.Id);
        }

        public ICommand PlayPauseCommand { get; }
        public bool PlayPauseCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlayPauseCommand_Execute()
        {
            await _mpc.MpdPlaybackPause();
        }

        public ICommand PlayStopCommand { get; }
        public bool PlayStopCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlayStopCommand_Execute()
        {
            await _mpc.MpdPlaybackStop();
        }

        #endregion

        #region == Playback opts ==

        public ICommand SetRandomCommand { get; }
        public bool SetRandomCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetRandomCommand_ExecuteAsync()
        {
            await _mpc.MpdSetRandom(_random);
        }

        public ICommand SetRpeatCommand { get; }
        public bool SetRpeatCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetRpeatCommand_ExecuteAsync()
        {
            await _mpc.MpdSetRepeat(_repeat);
        }

        public ICommand SetConsumeCommand { get; }
        public bool SetConsumeCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetConsumeCommand_ExecuteAsync()
        {
            await _mpc.MpdSetConsume(_consume);
        }

        public ICommand SetSingleCommand { get; }
        public bool SetSingleCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetSingleCommand_ExecuteAsync()
        {
            await _mpc.MpdSetSingle(_single);
        }

        #endregion

        #region == Playback seek and volume ==

        public ICommand SetVolumeCommand { get; }
        public bool SetVolumeCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetVolumeCommand_ExecuteAsync()
        {
            await _mpc.MpdSetVolume(Convert.ToInt32(_volume));
        }

        public ICommand SetSeekCommand { get; }
        public bool SetSeekCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SetSeekCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackSeek(_mpc.MpdStatus.MpdSongID, Convert.ToInt32(_elapsed));
        }

        public ICommand VolumeMuteCommand { get; }
        public bool VolumeMuteCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void VolumeMuteCommand_Execute()
        {
            await _mpc.MpdSetVolume(0);
        }

        public ICommand VolumeDownCommand { get; }
        public bool VolumeDownCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void VolumeDownCommand_Execute()
        {
            if (_volume >= 10)
            {
                await _mpc.MpdSetVolume(Convert.ToInt32(_volume - 10));
            }
        }

        public ICommand VolumeUpCommand { get; }
        public bool VolumeUpCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void VolumeUpCommand_Execute()
        {
            if (_volume <= 90)
            {
                await _mpc.MpdSetVolume(Convert.ToInt32(_volume + 10));
            }
        }

        #endregion

        #region == Queue ==

        public ICommand QueueListviewSaveAsCommand { get; }
        public bool QueueListviewSaveAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public void QueueListviewSaveAsCommand_ExecuteAsync()
        {
            IsSaveAsPlaylistPopupVisible = true;
        }

        public ICommand QueueListviewSaveAsPopupCommand { get; }
        public bool QueueListviewSaveAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public async void QueueListviewSaveAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            await _mpc.MpdSave(playlistName);
            
            IsSaveAsPlaylistPopupVisible = false;
        }

        public ICommand QueueListviewEnterKeyCommand { get; set; }
        public bool QueueListviewEnterKeyCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count < 1) { return false; }
            if (_selectedQueueSong == null) { return false; }
            return true;
        }
        public async void QueueListviewEnterKeyCommand_ExecuteAsync()
        {
            await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume), _selectedQueueSong.Id);
        }

        public ICommand QueueListviewLeftDoubleClickCommand { get; set; }
        public bool QueueListviewLeftDoubleClickCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count < 1) { return false; }
            if (_selectedQueueSong == null) { return false; }
            return true;
        }
        public async void QueueListviewLeftDoubleClickCommand_ExecuteAsync(SongInfoEx song)
        {
            await _mpc.MpdPlaybackPlay(Convert.ToInt32(_volume), song.Id);
        }

        public ICommand QueueListviewClearCommand { get; }
        public bool QueueListviewClearCommand_CanExecute()
        {
            if (Queue.Count == 0) { return false; }
            return true;
        }
        public void QueueListviewClearCommand_ExecuteAsync()
        {
            IsConfirmClearQueuePopupVisible = true;
        }

        public ICommand QueueListviewConfirmClearPopupCommand { get; }
        public bool QueueListviewConfirmClearPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public async void QueueListviewConfirmClearPopupCommand_Execute()
        {
            await _mpc.MpdClear();

            IsConfirmClearQueuePopupVisible = false;
        }

        public ICommand QueueListviewDeleteCommand { get; }
        public bool QueueListviewDeleteCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public void QueueListviewDeleteCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfoEx> selectedList = new List<SongInfoEx>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            List<string> deleteIdList = new List<string>();

            foreach (var item in selectedList)
            {
                deleteIdList.Add(item.Id);
            }

            queueListviewSelectedQueueSongIdsForPopup = deleteIdList;

            IsConfirmDeleteQueuePopupVisible = true;

        }

        public ICommand QueueListviewConfirmDeletePopupCommand { get; }
        public bool QueueListviewConfirmDeletePopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SelectedQueueSong == null) return false;
            if (Queue.Count == 0) return false;
            return true;
        }
        public async void QueueListviewConfirmDeletePopupCommand_Execute()
        {
            if (queueListviewSelectedQueueSongIdsForPopup.Count < 1)
                return;

            if (queueListviewSelectedQueueSongIdsForPopup.Count == 1)
                await _mpc.MpdDeleteId(queueListviewSelectedQueueSongIdsForPopup[0]);
            else if (queueListviewSelectedQueueSongIdsForPopup.Count >= 1)
                await _mpc.MpdDeleteId(queueListviewSelectedQueueSongIdsForPopup);

            queueListviewSelectedQueueSongIdsForPopup.Clear();

            IsConfirmDeleteQueuePopupVisible = false;
        }

        public ICommand QueueListviewMoveUpCommand { get; }
        public bool QueueListviewMoveUpCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewMoveUpCommand_Execute(object obj)
        {
            if (obj == null) return;

            if (Queue.Count <= 1)
                return;

            // 選択アイテム保持用
            List<SongInfoEx> selectedList = new List<SongInfoEx>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            Dictionary<string, string> IdToNewPos = new Dictionary<string, string>();

            foreach (var item in selectedList)
            {
                int i = 0;
                try
                {
                    i = Int32.Parse(item.Pos);

                    if (i == 0) return;

                    i -= 1;

                    Debug.WriteLine("asdf");
                    IdToNewPos.Add(item.Id, i.ToString());
                }
                catch
                {
                    return;
                }
            }

            await _mpc.MpdMoveId(IdToNewPos);
        }

        public ICommand QueueListviewMoveDownCommand { get; }
        public bool QueueListviewMoveDownCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewMoveDownCommand_Execute(object obj)
        {
            if (obj == null) return;

            if (Queue.Count <= 1)
                return;

            // 選択アイテム保持用
            List<SongInfoEx> selectedList = new List<SongInfoEx>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            Dictionary<string, string> IdToNewPos = new Dictionary<string, string>();

            foreach (var item in selectedList)
            {
                int i = 0;
                try
                {
                    i = Int32.Parse(item.Pos);

                    if (i >= Queue.Count-1) return;

                    i += 1;

                    IdToNewPos.Add(item.Id, i.ToString());
                }
                catch
                {
                    return;
                }
            }

            await _mpc.MpdMoveId(IdToNewPos);
        }

        public ICommand QueueListviewSaveSelectedAsCommand { get; }
        public bool QueueListviewSaveSelectedAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public void QueueListviewSaveSelectedAsCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfoEx> selectedList = new List<SongInfoEx>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            List<string> fileUrisToAddList = new List<string>();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            queueListviewSelectedQueueSongIdsForPopup = fileUrisToAddList;

            IsSelectedSaveAsPopupVisible = true;

        }

        public ICommand QueueListviewSaveSelectedAsPopupCommand { get; }
        public bool QueueListviewSaveSelectedAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewSaveSelectedAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            if (queueListviewSelectedQueueSongIdsForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlistName, queueListviewSelectedQueueSongIdsForPopup);

            queueListviewSelectedQueueSongIdsForPopup.Clear();

            IsSelectedSaveAsPopupVisible = false;
        }

        public ICommand QueueListviewSaveSelectedToPopupCommand { get; }
        public bool QueueListviewSaveSelectedToPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public async void QueueListviewSaveSelectedToPopupCommand_Execute(Playlist playlist)
        {
            if (playlist == null)
                return;

            if (string.IsNullOrEmpty(playlist.Name))
                return;

            if (queueListviewSelectedQueueSongIdsForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlist.Name, queueListviewSelectedQueueSongIdsForPopup);

            queueListviewSelectedQueueSongIdsForPopup.Clear();

            IsSelectedSaveToPopupVisible = false;
        }

        public ICommand QueueListviewSaveSelectedToCommand { get; }
        public bool QueueListviewSaveSelectedToCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) return false;
            if (SelectedQueueSong == null) return false;
            return true;
        }
        public void QueueListviewSaveSelectedToCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfoEx> selectedList = new List<SongInfoEx>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfoEx>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfoEx);
                }
            });

            List<string> fileUrisToAddList = new List<string>();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            queueListviewSelectedQueueSongIdsForPopup = fileUrisToAddList;

            IsSelectedSaveToPopupVisible = true;

        }

        public ICommand ScrollIntoNowPlayingCommand { get; }
        public bool ScrollIntoNowPlayingCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (Queue.Count == 0) { return false; }
            if (CurrentSong == null) { return false; }
            return true;
        }
        public void ScrollIntoNowPlayingCommand_Execute()
        {
            if (Queue.Count == 0) return;
            if (CurrentSong == null) return;
            if (Queue.Count < CurrentSong.Index + 1) return;

            // should I?
            //SelectedQueueSong = CurrentSong;

            ScrollIntoView?.Invoke(this, CurrentSong.Index);

        }

        public ICommand FilterQueueClearCommand { get; }
        public bool FilterQueueClearCommand_CanExecute()
        {
            if (string.IsNullOrEmpty(FilterQueueQuery))
                return false;
            return true;
        }
        public void FilterQueueClearCommand_Execute()
        {
            FilterQueueQuery = "";
        }

        public ICommand QueueFindShowHideCommand { get; }
        public bool QueueFindShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueFindShowHideCommand_Execute()
        {
            if (IsQueueFindVisible)
            {
                IsQueueFindVisible = false;
            }
            else
            {
                QueueForFilter.Clear();
                QueueForFilter =  new ObservableCollection<SongInfoEx>(Queue);
                
                var collectionView = CollectionViewSource.GetDefaultView(QueueForFilter);
                collectionView.Filter = x =>
                {
                    return false;
                };
                
                FilterQueueQuery = "";

                IsQueueFindVisible = true;
            }
        }

        #endregion

        #region == Search ==

        public ICommand SearchExecCommand { get; }
        public bool SearchExecCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (string.IsNullOrEmpty(SearchQuery))
                return false;
            return true;
        }
        public async void SearchExecCommand_Execute()
        {
            if (string.IsNullOrEmpty(SearchQuery)) return;

            // TODO: Make this an option.
            //"==";

            string queryShiki = "contains";

            await _mpc.MpdSearch(SelectedSearchTags.ToString(), queryShiki, SearchQuery);
        }

        public ICommand SearchResultListviewSaveSelectedAsCommand { get; }
        public bool SearchResultListviewSaveSelectedAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public void SearchResultListviewSaveSelectedAsCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfo> selectedList = new List<SongInfo>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            List<string> fileUrisToAddList = new List<string>();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            searchResultListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSearchResultSelectedSaveAsPopupVisible = true;
        }

        public ICommand SearchResultListviewSaveSelectedAsPopupCommand { get; }
        public bool SearchResultListviewSaveSelectedAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public async void SearchResultListviewSaveSelectedAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            if (searchResultListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlistName, searchResultListviewSelectedQueueSongUriForPopup);

            searchResultListviewSelectedQueueSongUriForPopup.Clear();

            IsSearchResultSelectedSaveAsPopupVisible = false;
        }

        public ICommand SearchResultListviewSaveSelectedToCommand { get; }
        public bool SearchResultListviewSaveSelectedToCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public void SearchResultListviewSaveSelectedToCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<SongInfo> selectedList = new List<SongInfo>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<SongInfo>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as SongInfo);
                }
            });

            List<string> fileUrisToAddList = new List<string>();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.File))
                    fileUrisToAddList.Add(item.File);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            searchResultListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSearchResultSelectedSaveToPopupVisible = true;

        }

        public ICommand SearchResultListviewSaveSelectedToPopupCommand { get; }
        public bool SearchResultListviewSaveSelectedToPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (SearchResult.Count == 0) return false;
            return true;
        }
        public async void SearchResultListviewSaveSelectedToPopupCommand_Execute(Playlist playlist)
        {
            if (playlist == null)
                return;

            if (string.IsNullOrEmpty(playlist.Name))
                return;

            if (searchResultListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlist.Name, searchResultListviewSelectedQueueSongUriForPopup);

            searchResultListviewSelectedQueueSongUriForPopup.Clear();

            IsSearchResultSelectedSaveToPopupVisible = false;
        }

        #endregion

        #region == Library ==

        public ICommand SongFilesListviewAddCommand { get; }
        public bool SongFilesListviewAddCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public async void SongFilesListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<NodeFile>();

                List<String> uriList = new List<String>();

                foreach (var item in collection)
                {
                    uriList.Add((item as NodeFile).OriginalFileUri);
                }

                await _mpc.MpdAdd(uriList);
            }
            else
            {
                if (items.Count == 1)
                    await _mpc.MpdAdd((items[0] as NodeFile).OriginalFileUri);
            }
        }

        public ICommand SongFilesListviewSaveSelectedAsCommand { get; }
        public bool SongFilesListviewSaveSelectedAsCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public void SongFilesListviewSaveSelectedAsCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<NodeFile> selectedList = new List<NodeFile>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<NodeFile>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as NodeFile);
                }
            });

            List<string> fileUrisToAddList = new List<string>();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.OriginalFileUri))
                    fileUrisToAddList.Add(item.OriginalFileUri);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            songFilesListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSongFilesSelectedSaveAsPopupVisible = true;
        }

        public ICommand SongFilesListviewSaveSelectedAsPopupCommand { get; }
        public bool SongFilesListviewSaveSelectedAsPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public async void SongFilesListviewSaveSelectedAsPopupCommand_Execute(string playlistName)
        {
            if (string.IsNullOrEmpty(playlistName))
                return;

            if (songFilesListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlistName, songFilesListviewSelectedQueueSongUriForPopup);

            songFilesListviewSelectedQueueSongUriForPopup.Clear();

            IsSongFilesSelectedSaveAsPopupVisible = false;
        }

        public ICommand SongFilesListviewSaveSelectedToCommand { get; }
        public bool SongFilesListviewSaveSelectedToCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public void SongFilesListviewSaveSelectedToCommand_Execute(object obj)
        {
            if (obj == null) return;

            // 選択アイテム保持用
            List<NodeFile> selectedList = new List<NodeFile>();

            // 念のため、UIスレッドで。
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                System.Collections.IList items = (System.Collections.IList)obj;

                var collection = items.Cast<NodeFile>();

                foreach (var item in collection)
                {
                    selectedList.Add(item as NodeFile);
                }
            });

            List<string> fileUrisToAddList = new List<string>();

            foreach (var item in selectedList)
            {
                if (!string.IsNullOrEmpty(item.OriginalFileUri))
                    fileUrisToAddList.Add(item.OriginalFileUri);
            }

            if (fileUrisToAddList.Count == 0)
                return;

            songFilesListviewSelectedQueueSongUriForPopup = fileUrisToAddList;

            IsSongFilesSelectedSaveToPopupVisible = true;

        }

        public ICommand SongFilesListviewSaveSelectedToPopupCommand { get; }
        public bool SongFilesListviewSaveSelectedToPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (MusicEntries.Count == 0) return false;
            return true;
        }
        public async void SongFilesListviewSaveSelectedToPopupCommand_Execute(Playlist playlist)
        {
            if (playlist == null)
                return;

            if (string.IsNullOrEmpty(playlist.Name))
                return;

            if (songFilesListviewSelectedQueueSongUriForPopup.Count < 1)
                return;

            await _mpc.MpdPlaylistAdd(playlist.Name, songFilesListviewSelectedQueueSongUriForPopup);

            songFilesListviewSelectedQueueSongUriForPopup.Clear();

            IsSongFilesSelectedSaveToPopupVisible = false;
        }

        public ICommand FilterMusicEntriesClearCommand { get; }
        public bool FilterMusicEntriesClearCommand_CanExecute()
        {
            if (string.IsNullOrEmpty(FilterMusicEntriesQuery))
                return false;
            return true;
        }
        public void FilterMusicEntriesClearCommand_Execute()
        {
            FilterMusicEntriesQuery = "";
        }

        #endregion

        #region == Playlists ==

        public ICommand ChangePlaylistCommand { get; set; }
        public bool ChangePlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selecctedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;
            return true;
        }
        public async void ChangePlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            await _mpc.MpdChangePlaylist(_selecctedPlaylist.Name);
        }

        public ICommand PlaylistListviewLeftDoubleClickCommand { get; set; }
        public bool PlaylistListviewLeftDoubleClickCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selecctedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewLeftDoubleClickCommand_ExecuteAsync(Playlist playlist)
        {
            if (_selecctedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            if (_selecctedPlaylist != playlist)
                return;

            await _mpc.MpdLoadPlaylist(playlist.Name);
        }

        public ICommand PlaylistListviewEnterKeyCommand { get; set; }
        public bool PlaylistListviewEnterKeyCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selecctedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewEnterKeyCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            await _mpc.MpdLoadPlaylist(_selecctedPlaylist.Name);
        }

        public ICommand PlaylistListviewLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewLoadPlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selecctedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            await _mpc.MpdLoadPlaylist(_selecctedPlaylist.Name);
        }

        public ICommand PlaylistListviewClearLoadPlaylistCommand { get; set; }
        public bool PlaylistListviewClearLoadPlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selecctedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewClearLoadPlaylistCommand_ExecuteAsync()
        {
            if (_selecctedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            await _mpc.MpdChangePlaylist(_selecctedPlaylist.Name);
        }

        // TODO:
        public ICommand PlaylistListviewRenamePlaylistCommand { get; set; }
        public bool PlaylistListviewRenamePlaylistCommand_CanExecute()
        {
            if (IsBusy) 
                return false;
            if (_selecctedPlaylist == null) 
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;

            return true;
        }
        public void PlaylistListviewRenamePlaylistCommand_Execute(Playlist playlist)
        {
            if (_selecctedPlaylist == null) 
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            if (_selecctedPlaylist != playlist)
                return;

            /*
            ResetDialog();
            DialogTitle = MPDCtrl.Properties.Resources.Dialog_Input;
            DialogMessage = MPDCtrl.Properties.Resources.Dialog_NewPlaylistName;
            IsInputDialogShow = true;

            DialogResultFunction = null;
            DialogResultFunctionWith2Params = DoRenamePlaylist;
            DialogResultFunctionParamString = _selecctedPlaylist;
            */

        }
        public async void DoRenamePlaylist(String oldPlaylistName, String newPlaylistName)
        {
            /*
            if (CheckPlaylistNameExists(newPlaylistName))
            {
                ResetDialog();
                DialogTitle = MPDCtrl.Properties.Resources.Dialog_Information;
                DialogMessage = MPDCtrl.Properties.Resources.Dialog_PlaylistNameAlreadyExists;
                IsInformationDialogShow = true;

                return false;
            }
            */
            await _mpc.MpdRenamePlaylist(oldPlaylistName, newPlaylistName);
        }

        // TODO:
        private bool CheckPlaylistNameExists(string playlistName)
        {
            bool match = false;

            if (Playlists.Count > 0)
            {

                foreach (var hoge in Playlists)
                {
                    if (hoge.Name.ToLower() == playlistName.ToLower())
                    {
                        match = true;
                        break;
                    }
                }
            }

            return match;
        }

        public ICommand PlaylistListviewRemovePlaylistCommand { get; set; }
        public bool PlaylistListviewRemovePlaylistCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selecctedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;
            return true;
        }
        public void PlaylistListviewRemovePlaylistCommand_Execute(Playlist playlist)
        {
            if (_selecctedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            if (_selecctedPlaylist != playlist)
                return;

            IsConfirmDeletePlaylistPopupVisible = true;
        }

        public ICommand PlaylistListviewConfirmRemovePlaylistPopupCommand { get; set; }
        public bool PlaylistListviewConfirmRemovePlaylistPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (_selecctedPlaylist == null)
                return false;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return false;
            return true;
        }
        public async void PlaylistListviewConfirmRemovePlaylistPopupCommand_Execute()
        {
            if (_selecctedPlaylist == null)
                return;
            if (string.IsNullOrEmpty(_selecctedPlaylist.Name))
                return;

            await _mpc.MpdRemovePlaylist(_selecctedPlaylist.Name);

            IsConfirmDeletePlaylistPopupVisible = false;
        }

        #endregion

        #region == PlaylistItems ==

        // プレイリストの中身をリロードするか確認後の処理
        public ICommand PlaylistListviewConfirmUpdatePopupCommand { get; set; }
        public bool PlaylistListviewConfirmUpdatePopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public void PlaylistListviewConfirmUpdatePopupCommand_Execute()
        {
            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    GetPlaylistSongs((SelectedNodeMenu as NodeMenuPlaylistItem));
                }
            }

            IsConfirmUpdatePlaylistSongsPopupVisible = false;
        }

        // プレイリストの中の曲を削除コマンド
        public ICommand PlaylistListviewDeletePosCommand { get; set; }
        public bool PlaylistListviewDeletePosCommand_CanExecute()
        {
            if (SelectedPlaylistSong == null) return false;
            if (IsBusy) return false;
            return true;
        }
        public void PlaylistListviewDeletePosCommand_Execute(object obj)
        {
            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    return;
                }
            }
            else
            {
                return;
            }

            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                // not supported by MPD protocol error.
                IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible = true;
            }
            else
            {
                if (items.Count == 1)
                    IsConfirmDeletePlaylistSongPopupVisible = true;
                    //await _mpc.MpdPlaylistDelete(playlistName, (items[0] as SongInfo).Index);
            }
        }

        // プレイリストの中の曲を削除で複数削除は未対応ダイアログを閉じる
        public ICommand PlaylistListviewConfirmDeletePosNotSupportedPopupCommand { get; set; }
        public bool PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_CanExecute()
        {
            return true;
        }
        public void PlaylistListviewConfirmDeletePosNotSupportedPopupCommand_Execute()
        {
            IsConfirmMultipleDeletePlaylistSongsNotSupportedPopupVisible = false;
        }

        // プレイリストの中の曲を削除で確認後の処理
        public ICommand PlaylistListviewDeletePosPopupCommand { get; set; }
        public bool PlaylistListviewDeletePosPopupCommand_CanExecute()
        {
            if (SelectedPlaylistSong == null) return false;
            if (IsBusy) return false;
            return true;
        }
        public async void PlaylistListviewDeletePosPopupCommand_Execute()
        {
            string playlistName = "";

            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    return;
                }
                else
                {
                    playlistName = (SelectedNodeMenu as NodeMenuPlaylistItem).Name;
                }
            }
            else
            {
                return;
            }

            if (SelectedPlaylistSong == null)
                return;

            await _mpc.MpdPlaylistDelete(playlistName, SelectedPlaylistSong.Index);

            IsConfirmDeletePlaylistSongPopupVisible = false;
        }

        // プレイリストの中身をクリアするコマンド
        public ICommand PlaylistListviewClearCommand { get; set; }
        public bool PlaylistListviewClearCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public void PlaylistListviewClearCommand_Execute()
        {
            IsConfirmPlaylistClearPopupVisible = true;
        }

        public ICommand PlaylistListviewClearPopupCommand { get; set; }
        public bool PlaylistListviewClearPopupCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void PlaylistListviewClearPopupCommand_Execute()
        {
            string playlistName;

            if (SelectedNodeMenu is NodeMenuPlaylistItem)
            {
                if ((SelectedNodeMenu as NodeMenuPlaylistItem).IsUpdateRequied)
                {
                    return;
                }
                else
                {
                    playlistName = (SelectedNodeMenu as NodeMenuPlaylistItem).Name;
                }
            }
            else
            {
                return;
            }

            await _mpc.MpdPlaylistClear(playlistName);

            IsConfirmPlaylistClearPopupVisible = false;
        }

        #endregion

        #region == Search and PlaylistItems ==

        public ICommand SongsListviewAddCommand { get; }
        public bool SongsListviewAddCommand_CanExecute()
        {
            if (IsBusy) return false;
            return true;
        }
        public async void SongsListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<SongInfo>();

                List<String> uriList = new List<String>();

                foreach (var item in collection)
                {
                    uriList.Add((item as SongInfo).File);
                }

                await _mpc.MpdAdd(uriList);
            }
            else
            {
                if (items.Count == 1)
                    await _mpc.MpdAdd((items[0] as SongInfo).File);
            }
        }

        #endregion

        #region == Settings ==

        public ICommand ShowSettingsCommand { get; }
        public bool ShowSettingsCommand_CanExecute()
        {
            if (IsConnecting) return false;
            return true;
        }
        public void ShowSettingsCommand_Execute()
        {
            if (IsConnecting) return;

            // TODO:
            //ConnectionStatusMessage = "";

            if (IsSettingsShow)
            {
                IsSettingsShow = false;
            }
            else
            {
                IsSettingsShow = true;
            }
        }

        public ICommand SettingsOKCommand { get; }
        public bool SettingsOKCommand_CanExecute()
        {
            return true;
        }
        public void SettingsOKCommand_Execute()
        {
            IsSettingsShow = false;
        }

        public ICommand NewProfileCommand { get; }
        public bool NewProfileCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            return true;
        }
        public void NewProfileCommand_Execute()
        {
            SelectedProfile = null;
        }

        public ICommand DeleteProfileCommand { get; }
        public bool DeleteProfileCommand_CanExecute()
        {
            if (Profiles.Count < 2) return false;
            if (SelectedProfile == null) return false;
            return true;
        }
        public void DeleteProfileCommand_Execute()
        {
            if (SelectedProfile == null) return;
            if (Profiles.Count < 2) return;

            var tmpNama = SelectedProfile.Name;
            var tmpIsDefault = SelectedProfile.IsDefault;

            if (Profiles.Remove(SelectedProfile))
            {
                SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileDeleted + " (" + tmpNama + ")";

                SelectedProfile = Profiles[0];

                if (tmpIsDefault)
                    Profiles[0].IsDefault = tmpIsDefault;
            }
        }

        public ICommand SaveProfileCommand { get; }
        public bool SaveProfileCommand_CanExecute()
        {
            if (SelectedProfile != null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void SaveProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (SelectedProfile != null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (_port == 0) return;

            Profile pro = new Profile();
            pro.Host = _host;
            pro.Port = _port;

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                Password = passwordBox.Password;
            }

            if (SetIsDefault)
            {
                foreach (var p in Profiles)
                {
                    p.IsDefault = false;
                }
                pro.IsDefault = true;
            }
            else
            {
                pro.IsDefault = false;
            }

            pro.Name = Host + ":" + _port.ToString();

            Profiles.Add(pro);

            SelectedProfile = pro;

            SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileSaved;

            if (CurrentProfile == null)
            {
                SetIsDefault = true;
                pro.IsDefault = true;
                CurrentProfile = pro;
            }
        }

        public ICommand UpdateProfileCommand { get; }
        public bool UpdateProfileCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            return true;
        }
        public void UpdateProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (SelectedProfile == null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (_port == 0) return;

            SelectedProfile.Host = _host;
            SelectedProfile.Port = _port;
            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                SelectedProfile.Password = passwordBox.Password;
                Password = passwordBox.Password;

                if (SelectedProfile == CurrentProfile)
                {
                    //_mpc.MpdPassword = passwordBox.Password;
                }
            }

            if (SetIsDefault)
            {
                foreach (var p in Profiles)
                {
                    p.IsDefault = false;
                }
                SelectedProfile.IsDefault = true;
            }
            else
            {
                if (SelectedProfile.IsDefault)
                {
                    SelectedProfile.IsDefault = false;
                    Profiles[0].IsDefault = true;
                }
                else
                {
                    SelectedProfile.IsDefault = false;
                }
            }

            SelectedProfile.Name = Host + ":" + _port.ToString();

            SettingProfileEditMessage = MPDCtrl.Properties.Resources.Settings_ProfileUpdated;
        }

        #endregion

        #region == Connection ==

        public ICommand ChangeConnectionProfileCommand { get; }
        public bool ChangeConnectionProfileCommand_CanExecute()
        {
            if (IsBusy) return false;
            if (string.IsNullOrWhiteSpace(Host)) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (IsConnecting) return false;
            return true;
        }
        public async void ChangeConnectionProfileCommand_Execute(object obj)
        {
            if (obj == null) return;
            if (String.IsNullOrEmpty(Host)) return;
            if (string.IsNullOrWhiteSpace(Host)) return;
            if (_port == 0) return;
            if (IsConnecting) return;
            if (IsBusy) return;

            IsSwitchingProfile = true;

            if (IsConnected)
            {
                _mpc.MpdStop = true;
                _mpc.MpdDisconnect();
                _mpc.MpdStop = false;
            }

            // Save volume.
            if (CurrentProfile != null)
                CurrentProfile.Volume = Convert.ToInt32(Volume);

            // Validate Host input.
            if (Host == "")
            {
                SetError("Host", "Error: Host must be epecified."); //TODO: translate
                NotifyPropertyChanged("Host");
                return;
            }
            else
            {
                if (Host == "localhost")
                {
                    Host = "127.0.0.1";
                }

                IPAddress ipAddress = null;
                try
                {
                    ipAddress = IPAddress.Parse(Host);
                    if (ipAddress != null)
                    {
                        ClearErrror("Host");
                    }
                }
                catch
                {
                    //System.FormatException
                    SetError("Host", "Error: Invalid address format."); //TODO: translate

                    return;
                }
            }

            if (_port == 0)
            {
                SetError("Port", "Error: Port must be epecified."); //TODO: translate.
                return;
            }

            // for Unbindable PasswordBox.
            var passwordBox = obj as PasswordBox;
            if (!String.IsNullOrEmpty(passwordBox.Password))
            {
                Password = passwordBox.Password;
            }

            // Clear current...
            if (CurrentSong != null)
            {
                CurrentSong.IsPlaying = false;
                CurrentSong = null;
            }
            if (CurrentSong != null)
            {
                SelectedQueueSong = null;
            }

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedNodeMenu = null;

                Queue.Clear();
                _mpc.CurrentQueue.Clear();

                _mpc.MpdStatus.Reset();

                if (_mainMenuItems.PlaylistsDirectory != null)
                    _mainMenuItems.PlaylistsDirectory.Children.Clear();
                
                Playlists.Clear();
                _mpc.Playlists.Clear();
                SelectedPlaylist = null;

                SelectedPlaylistSong = null;

                MusicEntries.Clear();

                //MusicDirectories.Clear();// Don't
                //_mpc.LocalDirectories.Clear();// Don't
                //_mpc.LocalFiles.Clear();// Don't
                //SelectedNodeDirectory.Children.Clear();// Don't
                _musicDirectories.IsCanceled = true;
                if (_musicDirectories.Children.Count > 0)
                    _musicDirectories.Children[0].Children.Clear();
                MusicDirectories.Clear();

                FilterMusicEntriesQuery = "";

                SelectedQueueSong = null;
                CurrentSong = null;

                SearchResult.Clear();
                SearchQuery = "";

                IsAlbumArtVisible = false;
                AlbumArt = _albumArtDefault;

                // TODO: more
            });

            IsConnecting = true;

            ///
            ConnectionResult r = await _mpc.MpdIdleConnect(_host, _port);

            if (r.IsSuccess)
            {
                IsSettingsShow = false;

                if (CurrentProfile == null)
                {
                    // Create new profile
                    Profile prof = new Profile();
                    prof.Name = _host + ":" + _port.ToString();
                    prof.Host = _host;
                    prof.Port = _port;
                    prof.Password = _password;
                    prof.IsDefault = true;

                    CurrentProfile = prof;
                    SelectedProfile = prof;

                    Profiles.Add(prof);
                }
                else
                {
                    SelectedProfile.Host = _host;
                    SelectedProfile.Port = _port;
                    SelectedProfile.Password = _password;

                    if (SetIsDefault)
                    {
                        foreach (var p in Profiles)
                        {
                            p.IsDefault = false;
                        }

                        SelectedProfile.IsDefault = true;
                    }
                    else
                    {
                        SelectedProfile.IsDefault = false;
                    }

                    SelectedProfile.Name = Host + ":" + _port.ToString();

                    CurrentProfile = SelectedProfile;
                }
            }

            IsSwitchingProfile = false;
        }

        private async void ChangeConnection(Profile prof)
        {
            if (IsConnecting) return;
            if (IsBusy) return;

            IsBusy = true;
            IsSwitchingProfile = true;

            if (IsConnected)
            {
                _mpc.MpdStop = true;
                _mpc.MpdDisconnect();
                _mpc.MpdStop = false;
            }

            // Save volume.
            if (CurrentProfile != null)
                CurrentProfile.Volume = Volume;

            // Clear current...
            if (CurrentSong != null)
            {
                CurrentSong.IsPlaying = false;
                CurrentSong = null;
            }
            if (CurrentSong != null)
            {
                SelectedQueueSong = null;
            }

            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                SelectedNodeMenu = null;

                SelectedQueueSong = null;
                CurrentSong = null;

                _mpc.MpdStatus.Reset();

                Queue.Clear();
                //Queue = new ObservableCollection<SongInfoEx>();
                _mpc.CurrentQueue.Clear();
                //QueueSelectionClear?.Invoke();

                if (_mainMenuItems.PlaylistsDirectory != null)
                    _mainMenuItems.PlaylistsDirectory.Children.Clear();

                Playlists.Clear();
                _mpc.Playlists.Clear();
                SelectedPlaylist = null;

                SelectedPlaylistSong = null;

                MusicEntries.Clear();

                // TODO: not good when directory is being built.
                //MusicDirectories.Clear(); // Don't
                //_mpc.LocalDirectories.Clear();// Don't
                //_mpc.LocalFiles.Clear();// Don't
                //SelectedNodeDirectory.Children.Clear();// Don't
                _musicDirectories.IsCanceled = true;
                if (_musicDirectories.Children.Count > 0)
                    _musicDirectories.Children[0].Children.Clear();
                //MusicDirectories.Clear();

                FilterMusicEntriesQuery = "";

                SearchResult.Clear();
                SearchQuery = "";

                IsAlbumArtVisible = false;
                AlbumArt = _albumArtDefault;

                // TODO: more?
            });

            _volume = prof.Volume;
            NotifyPropertyChanged("Volume");

            Host = prof.Host;
            _port = prof.Port;
            Password = prof.Password;

            IsConnecting = true;
            ///
            ConnectionResult r = await _mpc.MpdIdleConnect(_host, _port);

            if (r.IsSuccess)
            {
                CurrentProfile = prof;

                SelectedNodeMenu = MainMenuItems[0];
            }

            IsSwitchingProfile = false;
            IsBusy = false;
        }

        #endregion

        // TODO: 整理
        #region == Local Files ==

        /*
        public ICommand LocalfileListviewAddCommand { get; }
        public bool LocalfileListviewAddCommand_CanExecute()
        {
            return true;
        }
        public async void LocalfileListviewAddCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<NodeFile>();

                List<String> uriList = new List<String>();

                foreach (var item in collection)
                {
                    uriList.Add((item as NodeFile).OriginalFileUri);
                }

                await _mpc.MpdAdd(uriList);
            }
            else
            {
                if (items.Count == 1)
                    await _mpc.MpdAdd((items[0] as NodeFile).OriginalFileUri);
            }
        }

        public ICommand LocalfileListviewEnterKeyCommand { get; set; }
        public bool LocalfileListviewEnterKeyCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public async void LocalfileListviewEnterKeyCommand_Execute(object obj)
        {
            if (obj == null) return;

            System.Collections.IList items = (System.Collections.IList)obj;

            if (items.Count > 1)
            {
                var collection = items.Cast<NodeFile>();

                List<string> uriList = new List<string>();

                foreach (var item in collection)
                {
                    uriList.Add((item as NodeFile).OriginalFileUri);
                }

                await _mpc.MpdAdd(uriList);
            }
            else
            {
                if (items.Count == 1)
                    await _mpc.MpdAdd((items[0] as NodeFile).OriginalFileUri);
            }
        }

        public ICommand LocalfileListviewLeftDoubleClickCommand { get; set; }
        public bool LocalfileListviewLeftDoubleClickCommand_CanExecute()
        {
            if (_mpc == null) { return false; }
            return true;
        }
        public void LocalfileListviewLeftDoubleClickCommand_Execute(object obj)
        {

        }
        */

        #endregion

        #region == Dialogs ==

        public ICommand ShowChangePasswordDialogCommand { get; }
        public bool ShowChangePasswordDialogCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void ShowChangePasswordDialogCommand_Execute(object obj)
        {
            if (IsChangePasswordDialogShow)
            {
                IsChangePasswordDialogShow = false;
            }
            else
            {
                if (obj == null) return;
                // for Unbindable PasswordBox.
                var passwordBox = obj as PasswordBox;
                passwordBox.Password = "";

                //ResetDialog();
                IsChangePasswordDialogShow = true;
            }
        }

        public ICommand ChangePasswordDialogOKCommand { get; }
        public bool ChangePasswordDialogOKCommand_CanExecute()
        {
            if (SelectedProfile == null) return false;
            if (String.IsNullOrEmpty(Host)) return false;
            if (_port == 0) return false;
            return true;
        }
        public void ChangePasswordDialogOKCommand_Execute(object obj)
        {
            if (obj == null) return;

            // MultipleCommandParameterConverter で複数のパラメータを可能にしている。
            var values = (object[])obj;

            if ((values[0] is PasswordBox) && (values[1] is PasswordBox))
            {
                if ((values[0] as PasswordBox).Password == _password)
                {
                    SelectedProfile.Password = (values[1] as PasswordBox).Password; //allow empty string.

                    Password = SelectedProfile.Password;
                    NotifyPropertyChanged("IsPasswordSet");
                    NotifyPropertyChanged("IsNotPasswordSet");

                    (values[0] as PasswordBox).Password = "";
                    (values[1] as PasswordBox).Password = "";

                    if (SelectedProfile == CurrentProfile)
                    {
                        //_mpc.MpdPassword = SelectedProfile.Password;
                    }

                    SettingProfileEditMessage = MPDCtrl.Properties.Resources.ChangePasswordDialog_PasswordUpdated;

                }
                else
                {
                    ChangePasswordDialogMessage = MPDCtrl.Properties.Resources.ChangePasswordDialog_CurrentPasswordMismatch;
                    return;
                }

                IsChangePasswordDialogShow = false;
            }
        }

        public ICommand ChangePasswordDialogCancelCommand { get; }
        public bool ChangePasswordDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void ChangePasswordDialogCancelCommand_Execute()
        {
            IsChangePasswordDialogShow = false;
        }

        #endregion

        // TODO: 整理
        #region == Old Dialogs ==

        /*

        public ICommand ComfirmationDialogOKCommand { get; }
        public bool ComfirmationDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void ComfirmationDialogOKCommand_Execute()
        {
            DialogResultFunction(DialogResultFunctionParamString);

            IsComfirmationDialogShow = false;
        }

        public ICommand ComfirmationDialogCancelCommand { get; }
        public bool ComfirmationDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void ComfirmationDialogCancelCommand_Execute()
        {
            IsComfirmationDialogShow = false;
        }

        public ICommand InformationDialogOKCommand { get; }
        public bool InformationDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void InformationDialogOKCommand_Execute()
        {
            IsInformationDialogShow = false;
        }

        public ICommand InputDialogOKCommand { get; }
        public bool InputDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void InputDialogOKCommand_Execute()
        {
            if (DialogResultFunctionParamString != "")
            {
                DialogResultFunctionWith2Params(DialogResultFunctionParamString, DialogInputText);
            }
            else
            {
                DialogResultFunction(DialogInputText);
            }

            IsInputDialogShow = false;
        }

        public ICommand InputDialogCancelCommand { get; }
        public bool InputDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void InputDialogCancelCommand_Execute()
        {
            IsInputDialogShow = false;
        }


        public ICommand PlaylistSelectDialogOKCommand { get; }
        public bool PlaylistSelectDialogOKCommand_CanExecute()
        {
            return true;
        }
        public void PlaylistSelectDialogOKCommand_Execute(string playlistname)
        {
            if (string.IsNullOrEmpty(playlistname)) return;

            // PlaylistAdd
            DialogResultFunctionWith2ParamsWithObject(playlistname, DialogResultFunctionParamObject);

            IsPlaylistSelectDialogShow = false;
        }

        public ICommand PlaylistSelectDialogCancelCommand { get; }
        public bool PlaylistSelectDialogCancelCommand_CanExecute()
        {
            return true;
        }
        public void PlaylistSelectDialogCancelCommand_Execute()
        {
            IsPlaylistSelectDialogShow = false;
        }
        */

        #endregion

        #region == QueueListview header colums Show/Hide ==

        public ICommand QueueColumnHeaderPositionShowHideCommand { get; }
        public bool QueueColumnHeaderPositionShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderPositionShowHideCommand_Execute()
        {
            if (QueueColumnHeaderPositionVisibility)
            {
                QueueColumnHeaderPositionVisibility = false;
                QueueColumnHeaderPositionWidth = 0;
            }
            else
            {
                QueueColumnHeaderPositionVisibility = true;
                QueueColumnHeaderPositionWidth = QueueColumnHeaderPositionWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderNowPlayingShowHideCommand { get; }
        public bool QueueColumnHeaderNowPlayingShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderNowPlayingShowHideCommand_Execute()
        {
            if (QueueColumnHeaderNowPlayingVisibility)
            {
                QueueColumnHeaderNowPlayingVisibility = false;
                QueueColumnHeaderNowPlayingWidth = 0;
            }
            else
            {
                QueueColumnHeaderNowPlayingVisibility = true;
                QueueColumnHeaderNowPlayingWidth = QueueColumnHeaderNowPlayingWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderTimeShowHideCommand { get; }
        public bool QueueColumnHeaderTimeShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderTimeShowHideCommand_Execute()
        {
            if (QueueColumnHeaderTimeVisibility)
            {

                QueueColumnHeaderTimeVisibility = false;
                QueueColumnHeaderTimeWidth = 0;
            }
            else
            {
                QueueColumnHeaderTimeVisibility = true;
                QueueColumnHeaderTimeWidth = QueueColumnHeaderTimeWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderArtistShowHideCommand { get; }
        public bool QueueColumnHeaderArtistShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderArtistShowHideCommand_Execute()
        {
            if (QueueColumnHeaderArtistVisibility)
            {
                QueueColumnHeaderArtistVisibility = false;
                QueueColumnHeaderArtistWidth = 0;
            }
            else
            {
                QueueColumnHeaderArtistVisibility = true;
                QueueColumnHeaderArtistWidth = QueueColumnHeaderArtistWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderAlbumShowHideCommand { get; }
        public bool QueueColumnHeaderAlbumShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderAlbumShowHideCommand_Execute()
        {
            if (QueueColumnHeaderAlbumVisibility)
            {
                QueueColumnHeaderAlbumVisibility = false;
                QueueColumnHeaderAlbumWidth = 0;
            }
            else
            {
                QueueColumnHeaderAlbumVisibility = true;
                QueueColumnHeaderAlbumWidth = QueueColumnHeaderAlbumWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderGenreShowHideCommand { get; }
        public bool QueueColumnHeaderGenreShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderGenreShowHideCommand_Execute()
        {
            if (QueueColumnHeaderGenreVisibility)
            {
                QueueColumnHeaderGenreVisibility = false;
                QueueColumnHeaderGenreWidth = 0;
            }
            else
            {
                QueueColumnHeaderGenreVisibility = true;
                QueueColumnHeaderGenreWidth = QueueColumnHeaderGenreWidthRestore;
            }
        }

        public ICommand QueueColumnHeaderLastModifiedShowHideCommand { get; }
        public bool QueueColumnHeaderLastModifiedShowHideCommand_CanExecute()
        {
            return true;
        }
        public void QueueColumnHeaderLastModifiedShowHideCommand_Execute()
        {
            if (QueueColumnHeaderLastModifiedVisibility)
            {
                QueueColumnHeaderLastModifiedVisibility = false;
                QueueColumnHeaderLastModifiedWidth = 0;
            }
            else
            {
                QueueColumnHeaderLastModifiedVisibility = true;
                QueueColumnHeaderLastModifiedWidth = QueueColumnHeaderLastModifiedWidthRestore;
            }
        }


        #endregion

        #region == DebugWindow and AckWindow ==

        public ICommand ClearDebugCommandTextCommand { get; }
        public bool ClearDebugCommandTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearDebugCommandTextCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugCommandClear?.Invoke();
            });
        }

        public ICommand ClearDebugIdleTextCommand { get; }
        public bool ClearDebugIdleTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearDebugIdleTextCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugIdleClear?.Invoke();
            });
        }

        public ICommand ShowDebugWindowCommand { get; }
        public bool ShowDebugWindowCommand_CanExecute()
        {
            return true;
        }
        public void ShowDebugWindowCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                DebugWindowShowHide?.Invoke();
            });
        }

        public ICommand ClearAckTextCommand { get; }
        public bool ClearAckTextCommand_CanExecute()
        {
            return true;
        }
        public void ClearAckTextCommand_Execute()
        {
            if (Application.Current == null) { return; }
            Application.Current.Dispatcher.Invoke(() =>
            {
                AckWindowClear?.Invoke();
            });
        }

        public ICommand ShowAckWindowCommand { get; }
        public bool ShowAckWindowCommand_CanExecute()
        {
            return true;
        }
        public void ShowAckWindowCommand_Execute()
        {
            if (IsShowAckWindow)
                IsShowAckWindow = false;
            else
                IsShowAckWindow = true;
        }

        #endregion

        #region == Find ==

        public ICommand ShowFindCommand { get; }
        public bool ShowFindCommand_CanExecute()
        {
            return true;
        }
        public void ShowFindCommand_Execute()
        {
            if (SelectedNodeMenu is NodeMenuQueue)
            {
                QueueFindShowHideCommand_Execute();
            }
            else if(SelectedNodeMenu is NodeMenuSearch)
            {

            }
            else
            {
                SelectedNodeMenu = _mainMenuItems.SearchDirectory;

                IsQueueFindVisible = false;
            }
        }

        public ICommand QueueFilterSelectCommand { get; set; }
        public bool QueueFilterSelectCommand_CanExecute()
        {
            return true;
        }
        public void QueueFilterSelectCommand_Execute(object obj)
        {
            if (obj == null)
                return;

            if (obj != _selectedQueueFilterSong)
                return;

            IsQueueFindVisible = false;

            if (_selectedQueueFilterSong != null)
            {
                ScrollIntoViewAndSelect?.Invoke(this, _selectedQueueFilterSong.Index);
            }
        }

        #endregion

        #region == TreeViewMenu ContextMenu ==
        
        public ICommand TreeviewMenuItemLoadPlaylistCommand { get; }
        public bool TreeviewMenuItemLoadPlaylistCommand_CanExecute()
        {
            if (SelectedNodeMenu == null)
                return false;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return false;
            if (IsBusy) return false;

            return true;
        }
        public async void TreeviewMenuItemLoadPlaylistCommand_Execute()
        {
            if (IsBusy)
                return;
            if (SelectedNodeMenu == null)
                return;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return;
            
            await _mpc.MpdLoadPlaylist(SelectedNodeMenu.Name);
        }

        public ICommand TreeviewMenuItemClearLoadPlaylistCommand { get; }
        public bool TreeviewMenuItemClearLoadPlaylistCommand_CanExecute()
        {
            if (SelectedNodeMenu == null)
                return false;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return false;
            if (IsBusy) return false;

            return true;
        }
        public async void TreeviewMenuItemClearLoadPlaylistCommand_Execute()
        {
            if (IsBusy)
                return;
            if (SelectedNodeMenu == null)
                return;
            if (!(SelectedNodeMenu is NodeMenuPlaylistItem))
                return;

            Application.Current.Dispatcher.Invoke(() =>
            {
                Queue.Clear();
            });

            await _mpc.MpdChangePlaylist(SelectedNodeMenu.Name);
        }

        #endregion

        public ICommand EscapeCommand { get; }
        public bool EscapeCommand_CanExecute()
        {
            return true;
        }
        public void EscapeCommand_ExecuteAsync()
        {

            IsChangePasswordDialogShow = false;

            //IsSettingsShow = false; //Don't.

            IsQueueFindVisible = false;
        }


        #endregion
    }

}
