namespace Bounan.Downloader.Worker.Interfaces;

public interface IJobSignalSender
{
    Task WaitForCapacityAsync(CancellationToken cancellationToken);

    Task SignalJobAsync(CancellationToken cancellationToken);
}
