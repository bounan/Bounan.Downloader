using JetBrains.Annotations;

namespace Bounan.Downloader.Worker.Configuration;

public record ThreadingOptions
{
    public static readonly string SectionName = "Threading";

    /// <summary>
    /// Number of threads to process in parallel.
    /// </summary>
    public int Threads { get; [UsedImplicitly] init; } = 1;
}
