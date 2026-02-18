using Bounan.Downloader.Domain.Models;

namespace Bounan.Downloader.Domain.Clients;

public interface ILoanApiClient
{
    Task<GetVideoResponse> GetVideo(int myAnimeListId, string dub, int episode, CancellationToken cancellationToken);
}
