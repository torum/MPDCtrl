using MPDCtrlX.Models;
using System.Threading.Tasks;

namespace MPDCtrlX.Services.Contracts;

public interface IBinaryDownloader
{
    AlbumImage AlbumCover { get; }

    void MpdBinaryConnectionDisconnect();
    Task<bool> MpdBinaryConnectionStart(string host, int port, string password);
    Task<CommandResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture);
}
