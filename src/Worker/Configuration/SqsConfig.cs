using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record SqsConfig
{
    public static readonly string SectionName = "Sqs";

    /// <summary>
    /// Number of seconds to wait for a message.
    /// </summary>
    public int PollingIntervalSeconds { get; [UsedImplicitly] init; } = 20;

    /// <summary>
    /// Number of seconds to wait before retrying after an error.
    /// </summary>
    public int ErrorRetryIntervalSeconds { get; [UsedImplicitly] init; } = 5;

    /// <summary>
    /// URL of the notification queue.
    /// </summary>
    public required Uri NotificationQueueUrl { get; [UsedImplicitly] init; }
}
