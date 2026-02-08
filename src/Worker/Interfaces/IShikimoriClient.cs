namespace Bounan.Downloader.Worker.Interfaces;

public interface IShikimoriClient
{
    Task<string> GetTitleAsync(int myAnimeListId, CancellationToken cancellationToken);
}
