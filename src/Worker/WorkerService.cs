using System.Diagnostics;
using Bounan.Downloader.Worker.Configuration;
using Bounan.Downloader.Worker.Extensions;
using Bounan.Downloader.Worker.Interfaces;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Worker;

public partial class WorkerService(
    ILogger<WorkerService> logger,
    IOptions<ThreadingOptions> threadingOptions,
    IAniManClient aniManClient,
    IJobSignalReceiver jobSignalReceiver,
    IVideoCopyingService videoCopyingService) : BackgroundService
{
    private readonly ThreadingOptions threadingOptions = threadingOptions.Value;

    private ILogger Logger => logger;

    private IAniManClient AniManClient => aniManClient;

    private IJobSignalReceiver JobSignalReceiver => jobSignalReceiver;

    private IVideoCopyingService VideoCopyingService => videoCopyingService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Log.WorkerRunning(Logger, DateTimeOffset.Now);

        var workers = Enumerable.Range(0, threadingOptions.Threads)
            .Select(i => RunWorkerInstance(i, stoppingToken))
            .ToArray();

        await Task.WhenAll(workers);
    }

    private async Task RunWorkerInstance(int i, CancellationToken stoppingToken)
    {
        using var scope1 = Log.BeginScopeWorkerId(Logger, i);
        var stopwatch = new Stopwatch();

        Logger.LogInformation("Worker instance {WorkerId} started", i);

        while (!stoppingToken.IsCancellationRequested)
        {
            await JobSignalReceiver.WaitForJobAsync(stoppingToken);

            var message = await AniManClient.GetNextVideo(stoppingToken);
            if (message?.VideoKey is null)
            {
                Logger.LogInformation("No video to process, skipping...");
                continue;
            }

            ArgumentNullException.ThrowIfNull(message.VideoKey);
            using var scope2 = Log.BeginScopeMsg(Logger, message.VideoKey.CalculateHash());

            stopwatch.Restart();
            await VideoCopyingService.ProcessVideo(message.VideoKey, stoppingToken);
            Log.VideoProcessed(Logger, stopwatch.Elapsed);
            stopwatch.Stop();
        }

        Logger.LogInformation("Worker instance {WorkerId} stopping", i);
    }
}
