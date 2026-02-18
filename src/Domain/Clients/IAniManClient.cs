using Bounan.Common;

namespace Bounan.Downloader.Domain.Clients;

public interface IAniManClient
{
    Task<DownloaderResponse?> GetNextVideo(CancellationToken cancellationToken);

    Task SendResult(DownloaderResultRequest result, CancellationToken cancellationToken);
}
