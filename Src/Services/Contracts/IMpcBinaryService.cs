using MPDCtrl.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MPDCtrl.Services.Contracts;

public interface IMpcBinaryService: IDisposable
{
    //AlbumImage AlbumCover { get; }

    void MpdBinaryConnectionDisconnect(bool isReconnect);
    Task<bool> MpdBinaryConnectionStart(string host, int port, string password, CancellationToken cancellationToken = default);
    Task<CommandImageResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture);
}
