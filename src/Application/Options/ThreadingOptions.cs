using System.ComponentModel.DataAnnotations;
using Bounan.Downloader.Domain.Options;
using JetBrains.Annotations;

namespace Bounan.Downloader.Application.Options;

public record ThreadingOptions : IOptions
{
    public static string SectionName => "Threading";

    /// <summary>
    /// Gets number of threads to process in parallel.
    /// </summary>
    [Required]
    [Range(1, 2)]
    public int Threads { get; [UsedImplicitly] init; } = 1;
}
