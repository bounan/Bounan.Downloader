using System.Threading.Channels;
using Bounan.Downloader.Worker.Configuration;
using Bounan.Downloader.Worker.Interfaces;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Worker.Services;

internal class JobSignalingService : IJobSignalSender, IJobSignalReceiver
{
    private readonly Channel<bool> channel;

    public JobSignalingService(IOptions<ThreadingOptions> threadingOptions)
    {
        ArgumentNullException.ThrowIfNull(threadingOptions);

        var boundedChannelOptions = new BoundedChannelOptions(threadingOptions.Value.Threads)
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleWriter = false,
            SingleReader = false,
        };

        channel = Channel.CreateBounded<bool>(boundedChannelOptions);
    }

    public Task WaitForCapacityAsync(CancellationToken cancellationToken)
    {
        return channel.Writer.WaitToWriteAsync(cancellationToken).AsTask();
    }

    public Task SignalJobAsync(CancellationToken cancellationToken)
    {
        return channel.Writer.WriteAsync(true, cancellationToken).AsTask();
    }

    public Task WaitForJobAsync(CancellationToken cancellationToken)
    {
        return channel.Reader.ReadAsync(cancellationToken).AsTask();
    }
}
