using CommunityToolkit.Mvvm.ComponentModel;

namespace MPDCtrl.Models;

/// <summary>
/// Song class with some extra info. Extends SongInfo. (for queue)
/// </summary>
public sealed partial class SongInfoEx : SongInfo
{
    // Queue specific

    public string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Pos { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsPlaying { get; set; }

    [ObservableProperty]
    public partial bool IsAlbumCoverNeedsUpdate { get; set; } = true;
}
