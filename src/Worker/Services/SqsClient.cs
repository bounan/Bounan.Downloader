using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using Amazon.SQS.Model;
using Bounan.Downloader.Application.Options;
using Bounan.Downloader.Domain.Helpers;
using Bounan.Downloader.Infrastructure.Options;
using Bounan.Downloader.Worker.Abstractions;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Worker.Services;

internal sealed partial class SqsClient : BackgroundService
{
    private readonly int errorRetryIntervalMs;
    private readonly ReceiveMessageRequest receiveMessageRequest;

    public SqsClient(
        ILogger<SqsClient> logger,
        IOptions<SqsOptions> sqsOptions,
        IOptions<ProcessingOptions> processingOptions,
        IOptions<ThreadingOptions> threadingOptions,
        IAmazonSQS amazonSqs,
        IJobSignalSender jobSignalSender)
    {
        ArgumentNullException.ThrowIfNull(sqsOptions);
        ArgumentNullException.ThrowIfNull(processingOptions);
        ArgumentNullException.ThrowIfNull(threadingOptions);

        Logger = logger;
        AmazonAmazonSqs = amazonSqs;

        errorRetryIntervalMs = sqsOptions.Value.ErrorRetryIntervalSeconds * 1000;
        receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = sqsOptions.Value.NotificationQueueUrl.ToString(),
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = sqsOptions.Value.PollingIntervalSeconds,
        };

        JobSignalSender = jobSignalSender;
    }

    private ILogger<SqsClient> Logger { get; }

    private IAmazonSQS AmazonAmazonSqs { get; }

    private IJobSignalSender JobSignalSender { get; }

    public override void Dispose()
    {
        base.Dispose();
        AmazonAmazonSqs.Dispose();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Guard.NotNull(receiveMessageRequest.WaitTimeSeconds, nameof(receiveMessageRequest.WaitTimeSeconds));
        Guard.NotNullOrEmpty(receiveMessageRequest.QueueUrl, nameof(receiveMessageRequest.QueueUrl));

        Log.WaitingForMessage(Logger);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await JobSignalSender.WaitForCapacityAsync(stoppingToken);

                var response = await AmazonAmazonSqs.ReceiveMessageAsync(receiveMessageRequest, stoppingToken);
                Log.ReceivedMessages(Logger, response.Messages?.Count ?? 0);

                if (response.Messages is not { Count: > 0 })
                {
                    continue;
                }

                // Enqueue signal (may block if capacity vanished between check and write)
                await JobSignalSender.SignalJobAsync(stoppingToken);

                // Only delete after successfully enqueueing the work signal
                await AmazonAmazonSqs.DeleteMessageAsync(
                    new DeleteMessageRequest
                    {
                        QueueUrl = receiveMessageRequest.QueueUrl,
                        ReceiptHandle = response.Messages[0].ReceiptHandle,
                    },
                    stoppingToken);

                Log.RunningVideoProcessing(Logger);
            }
            catch (Exception ex)
            {
                Log.FailedToReceiveMessage(Logger, ex.Message);
                await Task.Delay(errorRetryIntervalMs, stoppingToken);
            }
        }
    }
}
