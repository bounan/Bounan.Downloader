namespace Bounan.Downloader.Worker.Abstractions;

internal interface IJobSignalSender
{
    Task WaitForCapacityAsync(CancellationToken cancellationToken);

    Task SignalJobAsync(CancellationToken cancellationToken);
}
