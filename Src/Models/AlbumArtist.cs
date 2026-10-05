
using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public sealed partial class AlbumArtist : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string NameSort { get; set; } = string.Empty;

    public ObservableCollection<AlbumEx> Albums { get; private set; } = [];
}
