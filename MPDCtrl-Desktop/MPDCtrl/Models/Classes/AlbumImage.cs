using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MPDCtrl.Models
{
    /// <summary>
    /// AlbumCover class. 
    /// </summary>
    public class AlbumImage
    {
        public bool IsDownloading { get; set; }

        public bool IsSuccess { get; set; }

        public string SongFilePath { get; set; }

        public byte[] BinaryData { get; set; } = Array.Empty<byte>();

        public int BinarySize { get; set; }

        public ImageSource AlbumImageSource { get; set; }

    }
}
