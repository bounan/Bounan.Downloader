using System.ComponentModel.DataAnnotations;
using Bounan.Downloader.Domain.Options;
using JetBrains.Annotations;

namespace Bounan.Downloader.Infrastructure.Options;

public record AniManOptions : IOptions
{
    public static string SectionName => "AniMan";

    /// <summary>
    /// Gets name of the Lambda function to get the next video to download.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 10)]
    public required string GetVideoToDownloadLambdaFunctionName { get; [UsedImplicitly] init; }

    /// <summary>
    /// Gets name of the Lambda function to update the status of the video.
    /// </summary>
    [Required]
    [StringLength(100, MinimumLength = 10)]
    public required string UpdateVideoStatusLambdaFunctionName { get; [UsedImplicitly] init; }
}
