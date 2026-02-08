using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using Amazon.SQS.Model;
using Bounan.Downloader.Worker.Configuration;
using Bounan.Downloader.Worker.Helpers;
using Bounan.Downloader.Worker.Interfaces;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Worker.Clients;

public sealed partial class SqsClient : ISqsClient, IDisposable
{
    private readonly int errorRetryIntervalMs;
    private readonly ReceiveMessageRequest receiveMessageRequest;
    private readonly SemaphoreSlim semaphore;

    public SqsClient(
        ILogger<SqsClient> logger,
        IOptions<SqsOptions> sqsOptions,
        IOptions<ProcessingOptions> processingOptions,
        IOptions<ThreadingOptions> threadingOptions,
        IAmazonSQS amazonSqs)
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

        semaphore = new SemaphoreSlim(threadingOptions.Value.Threads, threadingOptions.Value.Threads);
    }

    private ILogger<SqsClient> Logger { get; }

    private IAmazonSQS AmazonAmazonSqs { get; }

    public void Dispose()
    {
        semaphore.Dispose();
        AmazonAmazonSqs.Dispose();
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task WaitForMessageAsync(CancellationToken cancellationToken)
    {
        Guard.NotNull(receiveMessageRequest.WaitTimeSeconds, nameof(receiveMessageRequest.WaitTimeSeconds));
        Guard.NotNullOrEmpty(receiveMessageRequest.QueueUrl, nameof(receiveMessageRequest.QueueUrl));

        Log.WaitingForMessage(Logger);

        await semaphore.WaitAsync(cancellationToken);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var response = await AmazonAmazonSqs.ReceiveMessageAsync(receiveMessageRequest, cancellationToken);
                    Log.ReceivedMessages(Logger, response.Messages?.Count ?? 0);
                    if (response.Messages is not { Count: > 0 })
                    {
                        continue;
                    }

                    _ = AmazonAmazonSqs.DeleteMessageAsync(
                        new DeleteMessageRequest
                        {
                            QueueUrl = receiveMessageRequest.QueueUrl,
                            ReceiptHandle = response.Messages[0].ReceiptHandle,
                        },
                        cancellationToken);

                    Log.RunningVideoProcessing(Logger);
                    return;
                }
                catch (Exception ex)
                {
                    Log.FailedToReceiveMessage(Logger, ex.Message);
                    await Task.Delay(errorRetryIntervalMs, cancellationToken);
                }
            }
        }
        finally
        {
            _ = semaphore.Release();
        }
    }
}
