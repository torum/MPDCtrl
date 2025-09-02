
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

namespace MPDCtrl.Models;

public class AlbumImage
{
    public bool IsDownloading { get; set; } = false;

    public bool IsSuccess { get; set; } = false;

    public string? SongFilePath { get; set; }

    public byte[]? BinaryData { get; set; }// = Array.Empty<byte>();

    public int BinarySize { get; set; }

    // Let's remove this because WinUI3's bitmap is kind of messed up. Use ByteArray directly. It's easier and simpler when saving to a file.
    //public BitmapImage? AlbumImageSource { get; set; }
}
