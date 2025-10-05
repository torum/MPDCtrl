using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;

namespace MPDCtrl.Models;

public class SongInfoForSystemMediaTransportControls
{
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string AlbumArtist { get; set; } = string.Empty;
    public string AlbumTitle { get; set; } = string.Empty;
    public MediaPlaybackStatus PlaybackStatus { get; set; } = MediaPlaybackStatus.Paused;
    public bool IsPlayEnabled { get; set; } = true;
    public bool IsPauseEnabled { get; set; } = true;
    public bool IsNextEnabled { get; set; } = true;
    public bool IsPreviousEnabled { get; set; } = true;
    public bool IsStopEnabled { get; set; } = true;


    public bool IsThumbnailIncluded { get; set; } = false;
    public string FilePath { get; set; } = string.Empty;
    public Windows.Storage.Streams.RandomAccessStreamReference? Thumbnail { get; set; }

    public SongInfoForSystemMediaTransportControls()
    {

    }
}