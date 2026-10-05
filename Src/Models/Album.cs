using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace MPDCtrl.Models;

public partial class Album : ObservableObject
{
    public string Name { get; set; } = string.Empty;

    public string NameSort { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ReleaseYear { get; set; } = string.Empty;

    public bool IsSongsAcquired { get; set; } = false;

    [ObservableProperty]
    public partial ObservableCollection<SongInfo> Songs { get; set; } = [];
}
