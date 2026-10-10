using MPDCtrl.Models;
using System.Threading.Tasks;
using System.Threading;

namespace MPDCtrl.Services.Contracts;

public interface IMpcBinaryService
{
    //AlbumImage AlbumCover { get; }

    void MpdBinaryConnectionDisconnect(bool isReconnect);
    Task<bool> MpdBinaryConnectionStart(string host, int port, string password, CancellationToken cancellationToken = default);
    Task<CommandImageResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture);
}
