namespace Bounan.Downloader.Worker.Abstractions;

internal interface IJobSignalReceiver
{
    Task WaitForJobAsync(CancellationToken cancellationToken);
}
