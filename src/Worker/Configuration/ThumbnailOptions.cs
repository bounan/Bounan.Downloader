using System.ComponentModel.DataAnnotations;

namespace Bounan.Downloader.Worker.Configuration;

internal record ThumbnailOptions : IOptions
{
    public static string SectionName => "Thumbnail";

    [Required]
    [StringLength(40, MinimumLength = 2)]
    public required string BotId { get; init; }
}
