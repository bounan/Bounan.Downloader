using System.ComponentModel.DataAnnotations;
using Bounan.Downloader.Domain.Options;
using JetBrains.Annotations;

namespace Bounan.Downloader.Application.Options;

public record ProcessingOptions : IOptions
{
    public static string SectionName => "Processing";

    /// <summary>
    /// Gets timeout in seconds for each video.
    /// </summary>
    [Required]
    [Range(30, int.MaxValue, ErrorMessage = "TimeoutSeconds must be greater than 30.")]
    public int TimeoutSeconds { get; [UsedImplicitly] init; } = 6 * 60;

    /// <summary>
    /// Gets a value indicating whether use the lowest quality available.
    /// For debugging purposes only.
    /// </summary>
    public bool UseLowestQuality { get; [UsedImplicitly] init; }
}
