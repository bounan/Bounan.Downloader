using Bounan.Common;

namespace Bounan.Downloader.Application.Abstractions;

internal interface IThumbnailService
{
    Task<Stream> GetThumbnailJpegStreamAsync(
        Uri originalThumbnailUrl,
        IVideoKey videoKey,
        CancellationToken cancellationToken);
}
