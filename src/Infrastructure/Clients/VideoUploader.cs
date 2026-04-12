using Bounan.Downloader.Domain.Clients;
using Hls2TlgrUploader.Interfaces;

namespace Bounan.Downloader.Infrastructure.Clients;

internal class VideoUploader(IVideoUploadingService videoUploadingService) : IVideoUploader
{
    public async Task<int> CopyToTelegramAsync(
        IList<Uri> hlsParts,
        Task<Stream> jpegThumbnailStreamTask,
        string? caption,
        CancellationToken cancellationToken)
    {
        var message = await videoUploadingService.CopyToTelegramAsync(
            hlsParts,
            jpegThumbnailStreamTask,
            caption,
            cancellationToken);

        return message.MessageId;
    }
}
