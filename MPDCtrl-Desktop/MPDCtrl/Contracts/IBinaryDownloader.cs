using MPDCtrl.Models;
using System.Threading.Tasks;

namespace MPDCtrl.Contracts
{
    public interface IBinaryDownloader
    {
        AlbumImage AlbumCover { get; }

        void MpdBinaryConnectionDisconnect();
        Task<bool> MpdBinaryConnectionStart(string host, int port, string password);
        Task<CommandResult> MpdQueryAlbumArt(string uri, bool isUsingReadpicture);
    }
}