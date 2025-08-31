using MPDCtrl.Models;
using System.Threading.Tasks;

namespace MPDCtrl.Services.Contracts;

public interface IBinaryDownloader
{
    //AlbumImage AlbumCover { get; }

    void MpdBinaryConnectionDisconnect();
    Task<bool> MpdBinaryConnectionStart(string host, int port, string password);
    Task<CommandImageResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture);
}
