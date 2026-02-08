using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record ThreadingOptions : IOptions
{
    public static string SectionName => "Threading";

    /// <summary>
    /// Number of threads to process in parallel.
    /// </summary>
    [Required]
    [Range(1, 2)]
    public int Threads { get; [UsedImplicitly] init; } = 1;
}
