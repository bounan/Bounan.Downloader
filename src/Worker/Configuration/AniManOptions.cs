using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record AniManOptions : IOptions
{
    public static string SectionName => "AniMan";

    /// <summary>
    /// Name of the Lambda function to get the next video to download.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 10)]
    public required string GetVideoToDownloadLambdaFunctionName { get; [UsedImplicitly] init; }

    /// <summary>
    /// Name of the Lambda function to update the status of the video.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 10)]
    public required string UpdateVideoStatusLambdaFunctionName { get; [UsedImplicitly] init; }
}
