using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record SqsOptions : IOptions
{
    public static string SectionName => "Sqs";

    /// <summary>
    /// Number of seconds to wait for a message.
    /// </summary>
    [Required]
    [Range(1, 20)]
    public int PollingIntervalSeconds { get; [UsedImplicitly] init; } = 20;

    /// <summary>
    /// Number of seconds to wait before retrying after an error.
    /// </summary>
    [Required]
    [Range(1, int.MaxValue)]
    public int ErrorRetryIntervalSeconds { get; [UsedImplicitly] init; } = 5;

    /// <summary>
    /// URL of the notification queue.
    /// </summary>
    [Required]
    public required Uri NotificationQueueUrl { get; [UsedImplicitly] init; }
}
