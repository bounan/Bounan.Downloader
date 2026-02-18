using System.Diagnostics.CodeAnalysis;
using Bounan.Downloader.Application.Options;
using Bounan.Downloader.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Worker.Services;

internal class ConfigurationLoggingService(
    ILogger<ConfigurationLoggingService> logger,
    IOptions<AniManOptions> aniManOptions,
    IOptions<SqsOptions> sqsOptions,
    IOptions<ProcessingOptions> processingOptions,
    IOptions<ThreadingOptions> threadingOptions,
    IOptions<ThumbnailOptions> thumbnailOptions,
    IOptions<LoanApiOptions> loanApiOptions)
    : IHostedService
{
    [SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging")]
    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Configuration values:\n" +
            "AniManOptions: {@AniManOptions}\n" +
            "SqsOptions: {@SqsOptions}\n" +
            "ProcessingOptions: {@ProcessingOptions}\n" +
            "ThreadingOptions: {@ThreadingOptions}\n" +
            "ThumbnailOptions: {@ThumbnailOptions}\n" +
            "LoanApiOptions: {@LoanApiOptions}",
            aniManOptions.Value,
            sqsOptions.Value,
            processingOptions.Value,
            threadingOptions.Value,
            thumbnailOptions.Value,
            loanApiOptions.Value);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
