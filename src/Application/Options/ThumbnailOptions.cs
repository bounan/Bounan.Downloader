using System.ComponentModel.DataAnnotations;
using Bounan.Downloader.Domain.Options;

namespace Bounan.Downloader.Application.Options;

public record ThumbnailOptions : IOptions
{
    public static string SectionName => "Thumbnail";

    [Required]
    [StringLength(40, MinimumLength = 2)]
    public required string BotId { get; init; }
}
