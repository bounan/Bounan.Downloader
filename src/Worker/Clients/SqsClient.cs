using System.Diagnostics.CodeAnalysis;
using Amazon.SQS;
using Amazon.SQS.Model;
using Bounan.Downloader.Worker.Configuration;
using Bounan.Downloader.Worker.Helpers;
using Bounan.Downloader.Worker.Interfaces;
using Microsoft.Extensions.Options;

namespace Bounan.Downloader.Worker.Clients;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global - This class is partially inherited
public partial class SqsClient : ISqsClient, IDisposable
{
    private bool _isDisposed;
    private readonly int _errorRetryIntervalMs;
    private readonly ReceiveMessageRequest _receiveMessageRequest;
    private readonly SemaphoreSlim _semaphore;

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

        _errorRetryIntervalMs = sqsOptions.Value.ErrorRetryIntervalSeconds * 1000;
        _receiveMessageRequest = new ReceiveMessageRequest
        {
            QueueUrl = sqsOptions.Value.NotificationQueueUrl.ToString(),
            MaxNumberOfMessages = 1,
            WaitTimeSeconds = sqsOptions.Value.PollingIntervalSeconds,
        };

        _semaphore = new SemaphoreSlim(threadingOptions.Value.Threads, threadingOptions.Value.Threads);
    }

    private ILogger<SqsClient> Logger { get; }

    private IAmazonSQS AmazonAmazonSqs { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_isDisposed) return;

        if (disposing)
        {
            _semaphore.Dispose();
        }

        _isDisposed = true;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types")]
    public async Task WaitForMessageAsync(CancellationToken cancellationToken)
    {
        Guard.NotNull(_receiveMessageRequest.WaitTimeSeconds, nameof (_receiveMessageRequest.WaitTimeSeconds));
        Guard.NotNullOrEmpty(_receiveMessageRequest.QueueUrl, nameof (_receiveMessageRequest.QueueUrl));

        Log.WaitingForMessage(Logger);

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var response = await AmazonAmazonSqs.ReceiveMessageAsync(_receiveMessageRequest, cancellationToken);
                    Log.ReceivedMessages(Logger, response.Messages?.Count ?? 0);
                    if (response.Messages is not { Count: > 0 }) continue;

                    _ = AmazonAmazonSqs.DeleteMessageAsync(
                        new DeleteMessageRequest
                        {
                            QueueUrl = _receiveMessageRequest.QueueUrl,
                            ReceiptHandle = response.Messages[0].ReceiptHandle,
                        },
                        cancellationToken);

                    Log.RunningVideoProcessing(Logger);
                    return;
                }
                catch (Exception ex)
                {
                    Log.FailedToReceiveMessage(Logger, ex.Message);
                    await Task.Delay(_errorRetryIntervalMs, cancellationToken);
                }
            }
        }
        finally
        {
            _ = _semaphore.Release();
        }
    }
}
