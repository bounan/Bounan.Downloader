namespace Bounan.Downloader.Worker.Interfaces;

public interface IJobSignalReceiver
{
    Task WaitForJobAsync(CancellationToken cancellationToken);
}
