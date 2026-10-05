using CommunityToolkit.Mvvm.ComponentModel;
using MPDCtrl.ViewModels;
using System;
using System.Globalization;

namespace MPDCtrl.Models;

/// <summary>
/// Generic song file class. (for listall)
/// </summary>
public partial class SongFile : ObservableObject
{
    public string File { get; set; } = string.Empty;
}




