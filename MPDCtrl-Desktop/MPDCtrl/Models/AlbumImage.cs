﻿using System.Windows.Media;

namespace MPDCtrl.Models;

public class AlbumCoverObject
{
    public bool IsDownloading { get; set; } = false;

    public bool IsSuccess { get; set; } = false;

    public string? SongFilePath { get; set; }

    public byte[]? BinaryData { get; set; }// = Array.Empty<byte>();

    public int BinarySize { get; set; }

    public ImageSource? AlbumImageSource { get; set; }
}
