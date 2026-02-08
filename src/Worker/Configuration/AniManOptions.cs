using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record AniManOptions
{
    public static readonly string SectionName = "AniMan";

    /// <summary>
    /// Name of the Lambda function to get the next video to download.
    /// </summary>
    public required string GetVideoToDownloadLambdaFunctionName { get; [UsedImplicitly] init; }

    /// <summary>
    /// Name of the Lambda function to update the status of the video.
    /// </summary>
    public required string UpdateVideoStatusLambdaFunctionName { get; [UsedImplicitly] init; }
}
