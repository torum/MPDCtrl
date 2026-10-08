# MPDCtrl

<img width="48" height="48" src="https://github.com/torum/MPDCtrl/blob/master/images/MPDCtrl.png">

MPDCtrl is a native Windows client for [MPD (Music Player Daemon)](http://www.musicpd.org/), featuring the latest WinUI framework and Fluent Design System.

For other platforms like Linux, please check out [MPDCtrlX](https://github.com/torum/MPDCtrlX), an [Avalonia UI](https://avaloniaui.net/)-based cross-platform GUI client ported from MPDCtrl.

## Download  
You can install via the [Microsoft Store](https://apps.microsoft.com/store/detail/mpdctrl/9NV2BBJ82BRX) or download the executables directly from the [releases page](https://github.com/torum/MPDCtrl/releases). The store package natively supports x64, x86, and arm64 architectures through Native AOT compilation for faster performance and memory efficiency.
  
## Screenshots

![MPDCtrl](https://github.com/torum/MPDCtrl/blob/master/images/screenshots/v4/MPDCtrl-Queue.png)  


![MPDCtrl](https://github.com/torum/MPDCtrl/blob/master/images/screenshots/v4/MPDCtrl-Search.png)  


![MPDCtrl](https://github.com/torum/MPDCtrl/blob/master/images/screenshots/v4/MPDCtrl-Albums.png)  


![MPDCtrl](https://github.com/torum/MPDCtrl/blob/master/images/screenshots/v4/MPDCtrl-Artists.png)  


![MPDCtrl](https://github.com/torum/MPDCtrl/blob/master/images/screenshots/v4/MPDCtrl-Files.png)  


![MPDCtrl](https://github.com/torum/MPDCtrl/blob/master/images/screenshots/v4/MPDCtrl-Playlists.png)  


## Contributing
Feel free to open issues and send PRs. 

## Technologies & Frameworks
* [.NET](https://github.com/dotnet/runtime)  
* ~~[WPF (Windows Presentation Foundation)](https://github.com/dotnet/wpf)~~   
* [WinUI3](https://github.com/microsoft/microsoft-ui-xaml)
* [WindowsAppSDK](https://github.com/microsoft/microsoft-ui-xaml)
* [CsWinRT](https://github.com/microsoft/CsWinRT)

## Getting Started

### Requirements
* Windows 10.0.19041.0 or higher

### Building
1. Visual Studio 2022 or higher with support for .NET Desktop App development
2. Clone this repository
3. Open solution in Visual Studio and run

## MPDCtrlX for cross-platoform

[MPDCtrlX](https://github.com/torum/MPDCtrlX) is a cross-platform version of MPDCtrl. 

Built using [Avalonia UI](https://avaloniaui.net/), this application is a direct port of the original MPDCtrl (which has since migrated from WPF to WinUI 3). While cross-platform, MPDCtrlX is specifically optimized for Linux desktop users, offering platform-specific features like native MPRIS (Media Player Remote Interfacing Specification) integration over D-Bus.

<img width="800" alt="MPDCtrlX based on Avalonia UI, a port of WPF-based Windows client MPDCtrl" src="https://github.com/torum/MPDCtrlX/blob/main/Docs/Images/MPDCtrlX-Albums.png?raw=true">




