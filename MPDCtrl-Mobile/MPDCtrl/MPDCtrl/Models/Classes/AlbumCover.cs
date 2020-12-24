using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace MPDCtrl.Models
{
    public class AlbumCover
    {
        public bool IsDownloading { get; set; }

        public bool IsSuccess { get; set; }

        public string SongFilePath { get; set; }

        public byte[] BinaryData { get; set; } = new byte[0];

        public int BinarySize { get; set; }

        //public BitmapImage AlbumImageSource { get; set; }
        public ImageSource AlbumImageSource { get; set; }
    }
}
