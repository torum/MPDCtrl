# MPDCtrl

MPDCtrl is a Windows client app for [MPD (Music player daemon)](http://www.musicpd.org/). 

## Repo
https://github.com/torum/MPDCtrl

## Download App

 [Download from Microsoft Store](https://www.microsoft.com/store/apps/9NCC3NTG9DP3)
 

## TODO

### Things that currentry working on

- Refactoring and modernizing. (DI and Nullable, etc)

### Planning
*  Better keyboard bindings.
*  Add "Play Next" menu item in playlists and search result.　
*  Add "update" and "rescan" button in Setting's page.
*  Add inline renaming in the TreeView popup menu. (and right click select)
*  Queue reorder with drag and drop.
*  Surpress or fix "Playlist has changed, do you want update" nortification.
*  Remove some garbage in the TreeView's popup menu.
*  Remember left pane's width.
*  organize and clean up resourses such as translation and styles.
*  Queue: multiple items reorder.

## Ideas
*  Auto connect to localhost?
*  Cache CashAlbumArt and Album view?
*  Add Search option "Exact" or "Contain".


## Change log
* v3.0.21   (2023/2.29) Use Win32 api to paint background and blur and lots of UI tweaks. Refactoring WIP.
* v3.0.20.0 (2023/2.29) Implemented dependency injection using Generic host. Nullable enabled. Needs a lot of work. 
* v3.0.19   (2023/1/11) App Store release. 
* v3.0.18.1 When maximize, CourneRradius needed to be reset.
* v3.0.18   (2023/1/11) App Store release. 
* v3.0.17.2 Fixed libraly treeview's selected item color issue. Minor UI tweaks like listview's header gripper and sort glyph's pos.
* v3.0.17.1 Tweaked transparent background to fix white background flashing at startup.
* v3.0.17   (2023/1/3) App Store release. 
* v3.0.16   Allow hostname input removing ip address input restriction.
* v3.0.15.2 tweaked the light theme (mainly setting > connection profile combobox and checkbox).
* v3.0.15.1 tweaked the light theme (mainly sliders).
* v3.0.15   MS Store release.
* v3.0.14.3 fixed a few issues of LightTheme mainly combobox and scrollviewer's thumb.
* v3.0.14.2 Tweaked and refined LightTheme. Remember the selection and change Theme on startup.
* v3.0.14.1 Fixed little lisetview selection bug. (use ScrollIntoViewAndSelect instead of ScrollIntoView when "UpdateStatus") 
* v3.0.14   MS Store release.
* v3.0.13.2 Moved to Windows11 PC and .NET6 environment. Fixed a bug caused by a bug in MPD 0.23.5(included in Ubuntu 22.04.1 LTS).
* v3.0.13.1 Little cleanup.
* v3.0.13   MS Store release.
* v3.0.12.3 Fixed bug : Move to NowPlaying after queue update was not working . 
* v3.0.12.2 Fixed listview column header margin.
* v3.0.12.1 MS Store release.listview virtualization fix.
* v3.0.12   --MS Store release.
* v3.0.11.3 minor ui improvements.
* v3.0.11.2 Little clean up and queue update tweak. 
* v3.0.11.1 PlaylistSongsListview: clear selection and "bring to top" when items source changed.
* v3.0.11   Store release. (x86 only)  It wasn't workig > v3.0.10 Bundle x86 and x64.
* v3.0.10   Store release.  It wasn't workig > v3.0.9 Bundle x86 and x64.
* v3.0.8.3 Removed none English comments in the source code as much as possible. Little bit of clean up.
* v3.0.8.2 Fixed some typo and translations.
* v3.0.8.1 Changed Popup's Placement="Mouse" to "Center".
* v3.0.8   MS Store 公開。
* v3.0.7.3 [Bug] Directryの絞り込みで"/Hoge/Hoge" and "/Hoge/Hoge2" を区別しない問題を修正。
* v3.0.7.2 Minor UI improvements.
* v3.0.7.1 Minor UI improvements. Quick Profile switch Combobox visibility, StatusBar text clear, Listview & TreeView's mouse over text color.
* v3.0.7   MS Store 公開。Github issue #4 "Incomplete rendering of tracks" fix (multiple "Genre" key/tag). Exceptionをログにちゃんと保存するようにした。
* v3.0.6   MS Store 公開。
* v3.0.5.2 Fixed Queue update conflicts when consume mode is on. Progress updateをステータスバーにちゃんと表示するようにした。 
* v3.0.5.1 TextBoxをRoundCorner化してみた。DebugWindowクリアするボタンにToolTipを付けた。AlbumArtの表示タイミングを少し遅らせてスムーズにした。IDEのメッセージに対処した。
* v3.0.5   MS Store 公開。
* v3.0.4.3 CurrentSongのIsNowPlayingがクリアされないシチュエーションがあった。profileが一つある状態で、もう一つ追加する際にデフォルトにするのオプションが強制される状態だった。
* v3.0.4.2 新規で立ち上げた時、QuickSwitchを非表示に。
* v3.0.4.1 VirtualizingPanel.IsVirtualizing="True"。コードを少しclieaning up.
* v3.0.4   MS Store 公開。
* v3.0.3.3 Startから別スレッドで。
* v3.0.3.2 _mpc.CurrentQueueとQueueをロックするようにしてみた・・・けどダメだったのでIsWorkingでなんとかするように変更。
* v3.0.3.1 currentsong commandを使って、起動直後にqueueのロードを待たずとも曲情報を表示できるようにした。UpdateCurrentQueueで_mpc.CurrentQueueをループしてAddしている最中（IsWorking中」）にQueueにAddすると落ちてた。
* v3.0.3   MS Store 公開。
* v3.0.2.3 password間違いの際にエラーでてた。別スレッドにした関係でAckのメインスレッド実行が出来ていなかった。コマンド送信でInvokeする処理を待たないように一部変更（高速化）。listallを起動時にせず、as neededに変更。
* v3.0.2.2 Listviewのつまみの色と幅を少し変えた。クリックでPageUp/Down出来ていなかった。
* v3.0.2.1 QueueのaddにTask.Delayを入れてみた(空の時だけ)>profileを切り替えると奇妙な挙動・・。Dir&FilesのAddをTaskで。App.currentのnullチェック。PlaylistItemsの切り替えで、Newするようにした。
* v3.0.2   MS Store 公開。
* v3.0.1.1 ReleaseビルドでDeveloperモードになってデバッグウィンドウが非表示になっていた。profile空の状態でテストしたらボロボロだった。
* v3.0.1   MS Store 公開。
* v3.0.0.6 パスワード変更ダイアログ消しちゃってた。ちょっとリファクタリング。playlistsに最終更新日を追加する為にString型からPlaylist型にした。TreeView menuのプレイリスト選択からキューに追加のコンテキストメニュー。ログの保存方法を少し変更。
* v3.0.0.5 Search iconを復活させた。キューのMoveが動いていなかった。
* v3.0.0.4 Queue listview Ctrl+Fのコマンドが正しく指定されてなかった。
* v3.0.0.3 Find is done.
* v3.0.0.2 MPD protocol のバージョンが0.19.x以下だったらステータスバーにメッセージを出すようにした。Closeボタンの背景を赤にした。playlistのコンテキストメニューの文字変更。
* v3.0.0.1 SysButtonの背景を変えた。接続シークエンスで諸々の情報取得を独立的に行うようにした（一つ失敗しても他はロードされるように）。LocalFilesが正しくClearされるようにした。
* v3.0.0.  とりあえずひと段落したので。
* v3.0.0.7 とりあえず、プレイリスト系は大体できた。
* v3.0.0.6 とりあえずAlbumArtの取得はできるようにしたけれど、Downloaderクラスが必要。
* v3.0.0.5 色々やり過ぎて覚えていない・・・
* v3.0.0.4 色々やり過ぎて覚えていない・・・とりあえず。
* v3.0.3 基本のコマンドとidleからの更新ができるようになった・・・。
* v3.0.2 MPCを作り直し中。とりあえず接続とデータ取得まで。
* v3.0.1 MPCを作り直し中。 
* v3.0.0 v2.1.2から分岐。レイアウトを見直し。

## Memo:
###  Key Gestures:
*  Ctrl+S Show Settings
*  Ctrl+F Show Find 
*  Ctrl+P Playback Play
*  Ctrl+U QueueListview Queue Move Up
*  Ctrl+D QueueListview Queue Move Down
*  Ctrl+Delete QueueListview Queue Selected Item delete.
*  Ctrl+J QueueListview Jump to now playing.
*  Space Play > reserved for listview..
*  Ctrl+Delete QueueListview Remove from Queue
*  Esc Close dialogs.