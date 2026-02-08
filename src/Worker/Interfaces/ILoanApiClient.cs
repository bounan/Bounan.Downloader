using Bounan.Downloader.Worker.Models;

namespace Bounan.Downloader.Worker.Interfaces;

public interface ILoanApiClient
{
    Task<GetVideoResponse> GetVideo(int myAnimeListId, string dub, int episode, CancellationToken cancellationToken);
}