using CommunityToolkit.Mvvm.ComponentModel;

namespace MPDCtrl.Models;

/// <summary>
/// Song class with some extra info. Extends SongInfo. (for queue)
/// </summary>
#pragma warning disable CA1711 // Identifiers should not have incorrect suffix
public sealed partial class SongInfoEx : SongInfo
#pragma warning restore CA1711 // Identifiers should not have incorrect suffix
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
