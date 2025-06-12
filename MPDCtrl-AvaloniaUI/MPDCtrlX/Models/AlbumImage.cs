using Avalonia.Media.Imaging;
using System;

namespace MPDCtrlX.Models;

public class AlbumImage
{
    public bool IsDownloading { get; set; } = false;

    public bool IsSuccess { get; set; } = false;

    public string? SongFilePath { get; set; }

    public byte[]? BinaryData { get; set; }// = Array.Empty<byte>();

    public int BinarySize { get; set; }

    public Bitmap? AlbumImageSource { get; set; }
}
