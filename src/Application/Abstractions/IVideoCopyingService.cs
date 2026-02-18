using Bounan.Common;

namespace Bounan.Downloader.Application.Abstractions;

public interface IVideoCopyingService
{
    public Task ProcessVideo(IVideoKey videoKey, CancellationToken cancellationToken);
}
