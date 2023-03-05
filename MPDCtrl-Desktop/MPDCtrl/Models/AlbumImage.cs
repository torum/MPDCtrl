using System;
using System.Windows.Media;

namespace MPDCtrl.Models;

public class AlbumImage
{
    public bool IsDownloading { get; set; }

    public bool IsSuccess { get; set; }

    public string SongFilePath { get; set; }

    public byte[] BinaryData { get; set; } = Array.Empty<byte>();

    public int BinarySize { get; set; }

    public ImageSource AlbumImageSource { get; set; }
}
